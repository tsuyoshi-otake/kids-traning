namespace KidsTraining.App;

internal static class UpdateLogger
{
    public static void Info(string message) => Write("INFO", message);

    public static void Error(string message, Exception? exception = null)
    {
        var detail = exception is null ? message : $"{message}: {exception}";
        Write("ERROR", detail);
    }

    private static void Write(string level, string message)
    {
        try
        {
            AppPaths.EnsureRuntimeDirectories();
            var line = $"{DateTimeOffset.Now:O} [{level}] {message}{Environment.NewLine}";
            File.AppendAllText(AppPaths.UpdateLogPath, line);
        }
        catch
        {
            // Logging must never block startup, learning, or update installation.
        }
    }
}
