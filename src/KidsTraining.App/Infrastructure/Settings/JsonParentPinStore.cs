using System.Text.Json;
using System.Text.Json.Serialization;
using KidsTraining.App.Application.ParentControl;
using KidsTraining.App.Domain.ParentControl;

namespace KidsTraining.App.Infrastructure.Settings;

internal sealed class JsonParentPinStore : IParentPinStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public ParentPin Read()
    {
        try
        {
            if (!File.Exists(AppPaths.ParentSettingsPath))
            {
                return ParentPin.Default;
            }

            var settings = JsonSerializer.Deserialize<StoredSettings>(
                File.ReadAllText(AppPaths.ParentSettingsPath),
                JsonOptions);
            return ParentPin.FromOrDefault(settings?.ParentPassword);
        }
        catch (Exception exception)
        {
            UpdateLogger.Error("Could not read parent settings", exception);
            return ParentPin.Default;
        }
    }

    public void Write(ParentPin pin)
    {
        try
        {
            AppPaths.EnsureRuntimeDirectories();
            var settings = new StoredSettings(pin.Value);
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            var tempPath = AppPaths.ParentSettingsPath + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, AppPaths.ParentSettingsPath, overwrite: true);
        }
        catch (Exception exception)
        {
            UpdateLogger.Error("Could not write parent settings", exception);
            throw;
        }
    }

    private sealed record StoredSettings(
        [property: JsonPropertyName("parentPassword")] string ParentPassword);
}
