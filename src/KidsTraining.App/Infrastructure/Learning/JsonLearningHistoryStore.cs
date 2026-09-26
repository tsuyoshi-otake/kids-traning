using System.Text;
using System.Text.Json;

namespace KidsTraining.App.Infrastructure.Learning;

internal sealed class JsonLearningHistoryStore
{
    private const int SchemaVersion = 1;
    private const int MaximumHistoryRecords = 500;
    private const int MaximumPayloadBytes = 4 * 1024 * 1024;
    // Bound synchronization memory while sharing exclusion across instances for a path.
    // Windows replacements can conflict even when staging filenames are unique.
    private static readonly object[] StorageGates = Enumerable.Range(0, 16).Select(_ => new object()).ToArray();
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };
    private readonly object gate;
    private readonly string storagePath;

    public JsonLearningHistoryStore(string? storagePath = null)
    {
        this.storagePath = Path.GetFullPath(string.IsNullOrWhiteSpace(storagePath)
            ? AppPaths.LearningHistoryPath
            : storagePath);
        gate = StorageGates[(uint)StringComparer.OrdinalIgnoreCase.GetHashCode(this.storagePath) % (uint)StorageGates.Length];
    }

    public string ReadSnapshot()
    {
        lock (gate)
        {
            if (!File.Exists(storagePath))
            {
                return EmptySnapshot();
            }

            try
            {
                // Readers keep the opened snapshot while another store atomically replaces it.
                // Disallow in-place writers so the length bound also holds during the read.
                using var stream = new FileStream(storagePath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
                if (stream.Length > MaximumPayloadBytes)
                {
                    return EmptySnapshot();
                }

                using var reader = new StreamReader(stream, Encoding.UTF8);
                var payload = reader.ReadToEnd();
                return IsValidSnapshot(payload) ? payload : EmptySnapshot();
            }
            catch (Exception exception)
            {
                UpdateLogger.Error("Could not read the learning-history snapshot", exception);
                return EmptySnapshot();
            }
        }
    }

    public void WriteSnapshot(string payload)
    {
        lock (gate)
        {
            if (!IsValidSnapshot(payload))
            {
                throw new InvalidDataException("The learning-history snapshot is invalid or too large.");
            }

            var directory = Path.GetDirectoryName(storagePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Training and tray adapters can own separate store instances for the same path.
            // Each writer owns its staging file; the final rename publishes one whole snapshot.
            var temporaryPath = storagePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            var ownsTemporaryFile = false;
            try
            {
                using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    ownsTemporaryFile = true;
                    using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                    writer.Write(payload);
                }

                File.Move(temporaryPath, storagePath, overwrite: true);
                ownsTemporaryFile = false;
            }
            finally
            {
                if (ownsTemporaryFile)
                {
                    try
                    {
                        File.Delete(temporaryPath);
                    }
                    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                    {
                        // Preserve the original write failure if cleanup also fails.
                        UpdateLogger.Error("Could not remove a temporary learning-history snapshot", exception);
                    }
                }
            }
        }
    }

    public void Clear()
    {
        lock (gate)
        {
            try
            {
                if (File.Exists(storagePath))
                {
                    File.Delete(storagePath);
                }
            }
            catch (Exception exception)
            {
                UpdateLogger.Error("Could not clear the learning-history snapshot", exception);
                throw;
            }
        }
    }

    private static bool IsValidSnapshot(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload) || Encoding.UTF8.GetByteCount(payload) > MaximumPayloadBytes)
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("schemaVersion", out var schemaVersion) ||
                schemaVersion.ValueKind != JsonValueKind.Number ||
                !schemaVersion.TryGetInt32(out var version) || version != SchemaVersion ||
                !HasSafeProperties(root))
            {
                return false;
            }

            return !root.TryGetProperty("history", out var history) ||
                history.ValueKind == JsonValueKind.Array && history.GetArrayLength() <= MaximumHistoryRecords;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static bool HasSafeProperties(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name) ||
                    property.Name.Equals("parentPin", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Equals("parentPassword", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Equals("password", StringComparison.OrdinalIgnoreCase) ||
                    !HasSafeProperties(property.Value))
                {
                    return false;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                if (!HasSafeProperties(item))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static string EmptySnapshot() => JsonSerializer.Serialize(
        new
        {
            schemaVersion = SchemaVersion,
            exportedAt = DateTimeOffset.UtcNow,
            settings = new { },
            progress = new { },
            history = Array.Empty<object>(),
            activeSession = (object?)null,
        },
        JsonOptions);
}
