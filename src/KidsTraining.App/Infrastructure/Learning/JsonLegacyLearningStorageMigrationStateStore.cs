using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using KidsTraining.App.Application.Learning;
using KidsTraining.App.Domain.Learning;

namespace KidsTraining.App.Infrastructure.Learning;

internal sealed class JsonLegacyLearningStorageMigrationStateStore : ILegacyLearningStorageMigrationStateStore
{
    private const int CurrentSchemaVersion = 1;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly object gate = new();
    private readonly string statePath;

    public JsonLegacyLearningStorageMigrationStateStore()
        : this(Path.Combine(AppPaths.LocalAppDataRoot, "legacy-learning-storage-migration.json"))
    {
    }

    internal JsonLegacyLearningStorageMigrationStateStore(string statePath)
    {
        this.statePath = statePath;
    }

    public LegacyLearningStorageMigrationState Read()
    {
        lock (gate)
        {
            return ReadCore();
        }
    }

    public bool TryMarkCompleted()
    {
        lock (gate)
        {
            if (ReadCore().IsCompleted)
            {
                return true;
            }

            return TryWrite(new StoredMigrationState(
                CurrentSchemaVersion,
                FileOriginToVirtualHostCompleted: true,
                DateTimeOffset.UtcNow,
                FailedAttempts: 0,
                RetryAfterUtc: null));
        }
    }

    public bool TryMarkDeferred(DateTimeOffset nowUtc)
    {
        lock (gate)
        {
            var current = ReadCore();
            if (current.IsCompleted)
            {
                return true;
            }

            var attempts = Math.Max(0, current.FailedAttempts) + 1;
            var delay = attempts switch
            {
                1 => TimeSpan.FromHours(1),
                2 => TimeSpan.FromHours(6),
                _ => TimeSpan.FromHours(24),
            };
            return TryWrite(new StoredMigrationState(
                CurrentSchemaVersion,
                FileOriginToVirtualHostCompleted: false,
                CompletedAtUtc: null,
                FailedAttempts: attempts,
                RetryAfterUtc: nowUtc + delay));
        }
    }

    private LegacyLearningStorageMigrationState ReadCore()
    {
        if (!File.Exists(statePath))
        {
            return LegacyLearningStorageMigrationState.Pending;
        }

        try
        {
            var stored = JsonSerializer.Deserialize<StoredMigrationState>(
                File.ReadAllText(statePath),
                JsonOptions);
            if (stored is not { SchemaVersion: CurrentSchemaVersion })
            {
                return LegacyLearningStorageMigrationState.Pending;
            }

            return new LegacyLearningStorageMigrationState(
                stored.FileOriginToVirtualHostCompleted,
                stored.CompletedAtUtc,
                Math.Max(0, stored.FailedAttempts),
                stored.RetryAfterUtc);
        }
        catch (Exception exception)
        {
            UpdateLogger.Error("Could not read the legacy learning-storage migration marker", exception);
            return LegacyLearningStorageMigrationState.Pending;
        }
    }

    private bool TryWrite(StoredMigrationState stored)
    {
        var tempPath = $"{statePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            var directory = Path.GetDirectoryName(statePath)
                ?? throw new InvalidOperationException("The migration-state directory could not be resolved.");
            Directory.CreateDirectory(directory);
            var json = JsonSerializer.Serialize(stored, JsonOptions);
            File.WriteAllText(tempPath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            File.Move(tempPath, statePath, overwrite: true);
            return true;
        }
        catch (Exception exception)
        {
            UpdateLogger.Error("Could not persist the legacy learning-storage migration state", exception);
            return false;
        }
        finally
        {
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch (Exception exception)
            {
                UpdateLogger.Error("Could not remove a temporary migration-state file", exception);
            }
        }
    }

    private sealed record StoredMigrationState(
        [property: JsonPropertyName("schemaVersion")] int SchemaVersion,
        [property: JsonPropertyName("fileOriginToVirtualHostCompleted")] bool FileOriginToVirtualHostCompleted,
        [property: JsonPropertyName("completedAtUtc")] DateTimeOffset? CompletedAtUtc,
        [property: JsonPropertyName("failedAttempts")] int FailedAttempts,
        [property: JsonPropertyName("retryAfterUtc")] DateTimeOffset? RetryAfterUtc);
}
