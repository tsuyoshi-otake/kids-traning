using Microsoft.Web.WebView2.Core;

namespace KidsTraining.App;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Any(static arg => string.Equals(arg, "--smoke-test", StringComparison.OrdinalIgnoreCase)))
        {
            return RunSmokeTest();
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new TrainingForm());
        return 0;
    }

    private static int RunSmokeTest()
    {
        try
        {
            AppPaths.EnsureRuntimeDirectories();

            if (!File.Exists(AppPaths.HtmlPath))
            {
                return 11;
            }

            var runtimeHtml = RuntimeHtmlPreparer.Prepare();
            if (!File.Exists(runtimeHtml))
            {
                return 13;
            }

            var patchedHtml = File.ReadAllText(runtimeHtml);
            var template = RuntimeHtmlPreparer.ExtractBundledTemplate(patchedHtml);
            if (template is null ||
                !template.Contains("screen:'start', profileIdx:0,", StringComparison.Ordinal) ||
                template.Contains("screen:'profile', profileIdx:0,", StringComparison.Ordinal) ||
                !template.Contains("profiles:[\n", StringComparison.Ordinal) ||
                !template.Contains($"name:{System.Text.Json.JsonSerializer.Serialize(RuntimeHtmlPreparer.PrimaryProfileName)}", StringComparison.Ordinal))
            {
                return 14;
            }

            if (!AppPaths.WebViewUserDataFolder.StartsWith(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    StringComparison.OrdinalIgnoreCase))
            {
                return 12;
            }

            _ = CoreWebView2Environment.GetAvailableBrowserVersionString();
            return 0;
        }
        catch (WebView2RuntimeNotFoundException)
        {
            return 21;
        }
        catch
        {
            return 99;
        }
    }
}
