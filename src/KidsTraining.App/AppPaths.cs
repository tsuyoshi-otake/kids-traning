namespace KidsTraining.App;

internal static class AppPaths
{
    public const string AppName = "KidsTraining";

    public static string LocalAppDataRoot =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppName);

    public static string WebViewUserDataFolder =>
        Path.Combine(LocalAppDataRoot, "WebView2UserData");

    public static string HtmlPath =>
        Path.Combine(AppContext.BaseDirectory, "assets", "kids-training.html");

    public static string RuntimeHtmlPath =>
        Path.Combine(AppContext.BaseDirectory, "assets", "kids-training.runtime.html");

    public static void EnsureRuntimeDirectories()
    {
        Directory.CreateDirectory(LocalAppDataRoot);
        Directory.CreateDirectory(WebViewUserDataFolder);
    }
}
