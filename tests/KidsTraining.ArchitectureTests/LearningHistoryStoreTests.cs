using System.Text;
using System.Text.Json;
using KidsTraining.App.Infrastructure.Learning;

namespace KidsTraining.ArchitectureTests;

internal static class LearningHistoryStoreTests
{
    private const string Snapshot = "{\"schemaVersion\":1,\"history\":[{\"outcome\":\"independent\"}]}";
    private const int MaximumBytes = 4 * 1024 * 1024;

    public static void RoundTripAndClear() => WithStore((store, path) =>
    {
        AssertEmpty(store.ReadSnapshot());
        store.WriteSnapshot(Snapshot);
        Assert(new JsonLearningHistoryStore(path).ReadSnapshot() == Snapshot, "a fresh store could not read the committed snapshot");
        store.Clear();
        Assert(!File.Exists(path), "clear left the committed history file behind");
        AssertEmpty(store.ReadSnapshot());
        store.Clear();

        // A question can discuss passwords; only credential properties are forbidden.
        const string lesson = "{\"schemaVersion\":1,\"history\":[{\"question\":{\"prompt\":\"password parentPin 暗証番号\"}},{\"question\":{\"prompt\":\"同じキーは別のオブジェクトで使える\"}}]}";
        store.WriteSnapshot(lesson);
        Assert(store.ReadSnapshot() == lesson, "ordinary question text or repeated keys in different objects were rejected");
    });

    public static void RejectUnsafeSnapshots() => WithStore((store, _) =>
    {
        store.WriteSnapshot(Snapshot);
        foreach (var invalid in new[] { "", " ", "null", "[]", "{}", "{", "{\"schemaVersion\":1,\"history\":{}}" })
            AssertRejected(store, invalid);
        foreach (var version in new[] { "0", "2", "1.5", "2147483648", "1e100", "\"1\"", "null", "true", "{}", "[]" })
            AssertRejected(store, "{\"schemaVersion\":" + version + ",\"history\":[]}");

        foreach (var name in new[] { "parentPin", "parentPassword", "password", "ParentPin", "PARENTpassword", "PASSWORD", "parent\\u0050in" })
        {
            var credential = "{\"" + name + "\":\"1234\"}";
            AssertRejected(store, "{\"schemaVersion\":1," + credential[1..]);
            foreach (var field in new[] { "settings", "progress", "activeSession" })
                AssertRejected(store, "{\"schemaVersion\":1,\"" + field + "\":" + credential + "}");
            AssertRejected(store, "{\"schemaVersion\":1,\"history\":[{\"question\":{\"choices\":[" + credential + "]}}]}");
        }

        foreach (var ambiguous in new[]
        {
            "{\"schemaVersion\":2,\"schemaVersion\":1}",
            "{\"schemaVersion\":1,\"history\":{},\"history\":[]}",
            "{\"schemaVersion\":1,\"history\":[{\"points\":0,\"points\":1}]}",
            "{\"schemaVersion\":1,\"progress\":{\"xp\":0,\"\\u0078p\":100}}"
        })
            AssertRejected(store, ambiguous);
    });

    public static void EnforceLimits() => WithStore((store, _) =>
    {
        var fullHistory = JsonSerializer.Serialize(new { schemaVersion = 1, history = Enumerable.Repeat(new { outcome = "independent" }, 500) });
        store.WriteSnapshot(fullHistory);
        Assert(store.ReadSnapshot() == fullHistory, "the 500-record boundary was rejected");
        store.WriteSnapshot(Snapshot);
        AssertRejected(store, JsonSerializer.Serialize(new { schemaVersion = 1, history = Enumerable.Repeat(0, 501) }));

        const string prefix = "{\"schemaVersion\":1,\"history\":[],\"note\":\"";
        const string suffix = "\"}";
        var availableBytes = MaximumBytes - Encoding.UTF8.GetByteCount(prefix + suffix);
        var note = new string('あ', availableBytes / 3) + new string('a', availableBytes % 3);
        var boundary = prefix + note + suffix;
        Assert(Encoding.UTF8.GetByteCount(boundary) == MaximumBytes, "the UTF-8 boundary fixture is incorrect");
        store.WriteSnapshot(boundary);
        Assert(store.ReadSnapshot() == boundary, "the exact 4 MiB UTF-8 boundary was rejected");
        store.WriteSnapshot(Snapshot);
        AssertRejected(store, prefix + note + "a" + suffix);
    });

    public static void ReadInvalidDiskData() => WithStore((store, path) =>
    {
        foreach (var corrupt in new[]
        {
            "{", "{\"schemaVersion\":2}",
            "{\"schemaVersion\":1,\"settings\":{\"parentPassword\":\"1234\"}}",
            "{\"schemaVersion\":2,\"schemaVersion\":1}",
            new string(' ', MaximumBytes + 1)
        })
        {
            File.WriteAllText(path, corrupt, new UTF8Encoding(false));
            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            var result = store.ReadSnapshot();
            var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
            AssertEmpty(result);
            if (corrupt.Length > MaximumBytes)
                Assert(allocated < MaximumBytes, "oversized history was allocated before its byte limit was checked");
            Assert(File.ReadAllText(path) == corrupt, "reading invalid history modified the file");
        }

        store.WriteSnapshot(Snapshot);
        using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
            AssertEmpty(store.ReadSnapshot());
        Assert(store.ReadSnapshot() == Snapshot, "reading did not recover after the file lock was released");
    });

    public static void WriteFailurePreservesSnapshot() => WithStore((store, path) =>
    {
        store.WriteSnapshot(Snapshot);
        using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            AssertFileAccessFailure(() => store.WriteSnapshot("{\"schemaVersion\":1,\"history\":[]}"));
            Assert(store.ReadSnapshot() == Snapshot, "a failed replacement damaged the committed snapshot");
            AssertFileAccessFailure(store.Clear);
        }
        Assert(Directory.GetFiles(Path.GetDirectoryName(path)!, "*.tmp").Length == 0,
            "a failed replacement left an unowned temporary snapshot");
        store.WriteSnapshot("{\"schemaVersion\":1}");
        Assert(store.ReadSnapshot() == "{\"schemaVersion\":1}", "writing did not recover after a failed replacement");
    });

    public static void ConcurrentStores() => WithStore((store, path) =>
    {
        var payloads = Enumerable.Range(0, 32)
            .Select(index => JsonSerializer.Serialize(new { schemaVersion = 1, history = new[] { new { index } } }))
            .ToHashSet(StringComparer.Ordinal);
        store.WriteSnapshot(payloads.First());
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        Parallel.ForEach(payloads, new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = timeout.Token }, payload =>
        {
            var writer = new JsonLearningHistoryStore(path);
            try { writer.WriteSnapshot(payload); }
            catch (Exception exception)
            {
                throw new InvalidOperationException($"Concurrent snapshot write failed: {exception}", exception);
            }
            Assert(payloads.Contains(writer.ReadSnapshot()), "a concurrent reader saw an empty, partial, or foreign snapshot");
        });
        Assert(payloads.Contains(store.ReadSnapshot()), "concurrent writes did not leave a complete committed snapshot");
        Assert(Directory.GetFiles(Path.GetDirectoryName(path)!, "*.tmp").Length == 0, "concurrent writes leaked temporary files");
    });

    private static void AssertRejected(JsonLearningHistoryStore store, string payload)
    {
        AssertThrows<InvalidDataException>(() => store.WriteSnapshot(payload));
        Assert(store.ReadSnapshot() == Snapshot, "a rejected snapshot replaced the prior export");
    }

    private static void AssertEmpty(string payload)
    {
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        Assert(root.GetProperty("schemaVersion").GetInt32() == 1 && root.GetProperty("history").GetArrayLength() == 0 &&
            root.GetProperty("settings").EnumerateObject().Count() == 0 && root.GetProperty("progress").EnumerateObject().Count() == 0 &&
            root.GetProperty("activeSession").ValueKind == JsonValueKind.Null, "invalid or unavailable data escaped the empty-snapshot fallback");
    }

    private static void AssertThrows<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name} was not thrown.");
    }

    private static void AssertFileAccessFailure(Action action)
    {
        try { action(); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { return; }
        throw new InvalidOperationException("An operation on the locked history file unexpectedly succeeded.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void WithStore(Action<JsonLearningHistoryStore, string> test)
    {
        var directory = Path.Combine(Path.GetTempPath(), "KidsTraining.ArchitectureTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "history.json");
        try { test(new JsonLearningHistoryStore(path), path); }
        finally { Directory.Delete(directory, recursive: true); }
    }
}
