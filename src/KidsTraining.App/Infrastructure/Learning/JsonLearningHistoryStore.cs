using System.Text;
using System.Text.Json;

namespace KidsTraining.App.Infrastructure.Learning;

internal sealed class JsonLearningHistoryStore
{
    private const int SchemaVersion = 1;
    private const int MaximumHistoryRecords = 500;
    private const int MaximumPayloadBytes = 4 * 1024 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };
    private readonly object gate = new();
    private readonly string storagePath;

    public JsonLearningHistoryStore(string? storagePath = null)
    {
        this.storagePath = string.IsNullOrWhiteSpace(storagePath)
            ? AppPaths.LearningHistoryPath
            : Path.GetFullPath(storagePath);
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
                var payload = File.ReadAllText(storagePath, Encoding.UTF8);
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

            var temporaryPath = storagePath + ".tmp";
            File.WriteAllText(temporaryPath, payload, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            File.Move(temporaryPath, storagePath, overwrite: true);
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
                schemaVersion.GetInt32() != SchemaVersion ||
                root.TryGetProperty("parentPin", out _) ||
                root.TryGetProperty("password", out _))
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
