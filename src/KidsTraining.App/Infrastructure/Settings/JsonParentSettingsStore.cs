using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using KidsTraining.App.Application.ParentControl;
using KidsTraining.App.Domain.ParentControl;

namespace KidsTraining.App.Infrastructure.Settings;

internal sealed class JsonParentSettingsStore : IParentPinStore, IParentLearningSettingsStore, IParentLearningResetStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly object gate = new();

    public ParentPin Read()
    {
        lock (gate)
        {
            return ParentPin.FromOrDefault(ReadStoredOrDefault().ParentPassword);
        }
    }

    public LearningSessionSettings ReadLearningSettings()
    {
        lock (gate)
        {
            var stored = ReadStoredOrDefault();
            return LearningSessionSettings.Normalize(
                stored.QuestionCount,
                stored.PassLine,
                stored.SchoolGrade,
                stored.PreferSchoolGrade);
        }
    }

    public LearningResetMode ReadPendingLearningReset()
    {
        lock (gate)
        {
            return LearningResetModeValues.TryParse(ReadStoredOrDefault().PendingLearningReset, out var mode)
                ? mode
                : LearningResetMode.None;
        }
    }

    public void Write(ParentPin pin)
    {
        lock (gate)
        {
            try
            {
                var current = ReadStoredOrDefault();
                WriteStored(current with { ParentPassword = pin.Value });
            }
            catch (Exception exception)
            {
                UpdateLogger.Error("Could not write parent settings", exception);
                throw;
            }
        }
    }

    public void WriteLearningSettings(LearningSessionSettings settings)
    {
        lock (gate)
        {
            try
            {
                var current = ReadStoredOrDefault();
                WriteStored(current with
                {
                    QuestionCount = settings.QuestionCount,
                    PassLine = settings.PassLine,
                    SchoolGrade = settings.SchoolGrade,
                    PreferSchoolGrade = settings.PreferSchoolGrade
                });
            }
            catch (Exception exception)
            {
                UpdateLogger.Error("Could not write learning settings", exception);
                throw;
            }
        }
    }

    public void WritePendingLearningReset(LearningResetMode mode)
    {
        lock (gate)
        {
            try
            {
                var current = ReadStoredOrDefault();
                WriteStored(current with { PendingLearningReset = mode.ToWireValue() });
            }
            catch (Exception exception)
            {
                UpdateLogger.Error("Could not write pending learning reset", exception);
                throw;
            }
        }
    }

    private static StoredSettings ReadStoredOrDefault()
    {
        if (!File.Exists(AppPaths.ParentSettingsPath))
        {
            return StoredSettings.Default;
        }

        try
        {
            return JsonSerializer.Deserialize<StoredSettings>(
                    File.ReadAllText(AppPaths.ParentSettingsPath),
                    JsonOptions) ??
                StoredSettings.Default;
        }
        catch (Exception exception)
        {
            UpdateLogger.Error("Could not read parent settings", exception);
            return StoredSettings.Default;
        }
    }

    private static void WriteStored(StoredSettings settings)
    {
        AppPaths.EnsureRuntimeDirectories();
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        var tempPath = AppPaths.ParentSettingsPath + ".tmp";
        File.WriteAllText(tempPath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        File.Move(tempPath, AppPaths.ParentSettingsPath, overwrite: true);
    }

    private sealed record StoredSettings(
        [property: JsonPropertyName("parentPassword")] string? ParentPassword,
        [property: JsonPropertyName("questionCount")] int? QuestionCount,
        [property: JsonPropertyName("passLine")] int? PassLine,
        [property: JsonPropertyName("schoolGrade")] int? SchoolGrade,
        [property: JsonPropertyName("preferSchoolGrade")] bool? PreferSchoolGrade,
        [property: JsonPropertyName("pendingLearningReset")] string? PendingLearningReset)
    {
        public static StoredSettings Default { get; } = new(ParentPin.Default.Value, null, null, null, null, null);
    }
}
