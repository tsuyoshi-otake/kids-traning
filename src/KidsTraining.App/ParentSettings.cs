using System.Text.Json;
using System.Text.Json.Serialization;

namespace KidsTraining.App;

internal static class ParentSettings
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static string GetParentPassword()
    {
        try
        {
            if (!File.Exists(AppPaths.ParentSettingsPath))
            {
                return RuntimeHtmlPreparer.DefaultEmergencyPin;
            }

            var settings = JsonSerializer.Deserialize<StoredSettings>(
                File.ReadAllText(AppPaths.ParentSettingsPath),
                JsonOptions);
            return NormalizePassword(settings?.ParentPassword) ?? RuntimeHtmlPreparer.DefaultEmergencyPin;
        }
        catch (Exception ex)
        {
            UpdateLogger.Error("Could not read parent settings", ex);
            return RuntimeHtmlPreparer.DefaultEmergencyPin;
        }
    }

    public static PasswordChangeResult ChangeParentPassword(string? currentPassword, string? newPassword)
    {
        var current = NormalizePassword(currentPassword);
        if (current is null || current != GetParentPassword())
        {
            return PasswordChangeResult.Failed("いまのパスワードが違います。");
        }

        var next = NormalizePassword(newPassword);
        if (next is null)
        {
            return PasswordChangeResult.Failed("新しいパスワードは4桁の数字にしてください。");
        }

        try
        {
            AppPaths.EnsureRuntimeDirectories();
            var settings = new StoredSettings(next);
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            var tempPath = AppPaths.ParentSettingsPath + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, AppPaths.ParentSettingsPath, overwrite: true);
            return PasswordChangeResult.Ok("パスワードを変更しました。");
        }
        catch (Exception ex)
        {
            UpdateLogger.Error("Could not write parent settings", ex);
            return PasswordChangeResult.Failed("パスワードを保存できませんでした。");
        }
    }

    public static string? NormalizePassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        var normalized = password.Trim();
        return normalized.Length == 4 && normalized.All(static c => c is >= '0' and <= '9')
            ? normalized
            : null;
    }

    private sealed record StoredSettings(
        [property: JsonPropertyName("parentPassword")] string ParentPassword);
}

internal sealed record PasswordChangeResult(bool Success, string Message)
{
    public static PasswordChangeResult Ok(string message) => new(true, message);

    public static PasswordChangeResult Failed(string message) => new(false, message);
}
