namespace KidsTraining.App;

internal static class AppPaths
{
    public const string AppName = "KidsTraining";

    public static string LocalAppDataRoot =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppName);

    public static string WebViewUserDataFolder =>
        Path.Combine(LocalAppDataRoot, "WebView2UserData");

    public static string UpdatesFolder =>
        Path.Combine(LocalAppDataRoot, "Updates");

    public static string UpdateRunnerPath =>
        Path.Combine(UpdatesFolder, "KidsTraining.UpdateRunner.exe");

    public static string UpdateLogPath =>
        Path.Combine(UpdatesFolder, "updater.log");

    public static string ParentSettingsPath =>
        Path.Combine(LocalAppDataRoot, "parent-settings.json");

    public static string LearningHistoryPath =>
        Path.Combine(LocalAppDataRoot, "learning-history.json");

    public static string LearningRuntimeCacheManifestPath =>
        Path.Combine(LocalAppDataRoot, "learning-runtime-cache.json");

    public static string LearningAssetsFolder =>
        Path.Combine(AppContext.BaseDirectory, "assets", "kids-training");

    public static string HtmlTemplatePath =>
        Path.Combine(LearningAssetsFolder, "index.template.html");

    public static string LearningAppDefinitionPath =>
        Path.Combine(LearningAssetsFolder, "app", "learning-app.dc.html");

    public static string RuntimeHtmlPath =>
        Path.Combine(AppContext.BaseDirectory, "assets", "kids-training.runtime.html");

    public static void EnsureRuntimeDirectories()
    {
        Directory.CreateDirectory(LocalAppDataRoot);
        Directory.CreateDirectory(WebViewUserDataFolder);
        Directory.CreateDirectory(UpdatesFolder);
    }
}
