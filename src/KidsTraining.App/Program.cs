using Microsoft.Web.WebView2.Core;

namespace KidsTraining.App;

internal static class Program
{
    private const string SmokeTestArg = "--smoke-test";
    private const string TrainingArg = "--training";
    private const string LearnArg = "--learn";
    private const string AutoTrainingArg = "--auto-training";
    private const string ApplyUpdateArg = "--apply-update";

    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Any(static arg => string.Equals(arg, SmokeTestArg, StringComparison.OrdinalIgnoreCase)))
        {
            return RunSmokeTest();
        }

        if (args.Any(static arg => string.Equals(arg, ApplyUpdateArg, StringComparison.OrdinalIgnoreCase)))
        {
            return UpdateInstaller.Run(args);
        }

        ApplicationConfiguration.Initialize();
        if (args.Any(static arg =>
                string.Equals(arg, TrainingArg, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(arg, LearnArg, StringComparison.OrdinalIgnoreCase)))
        {
            Application.Run(new TrainingForm());
        }
        else
        {
            Application.Run(new TrayApplicationContext(args.Any(static arg =>
                string.Equals(arg, AutoTrainingArg, StringComparison.OrdinalIgnoreCase))));
        }

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
                !template.Contains($"name:{System.Text.Json.JsonSerializer.Serialize(RuntimeHtmlPreparer.PrimaryProfileName)}", StringComparison.Ordinal) ||
                !template.Contains("mastery:{add:.05,sub:.05,mul:.05,clock:.05,kokugo:.05,hissan:.05}", StringComparison.Ordinal) ||
                !template.Contains("genAdd(p)", StringComparison.Ordinal) ||
                !template.Contains("pickMul(p)", StringComparison.Ordinal) ||
                !template.Contains("pickKokugo(p)", StringComparison.Ordinal) ||
                !template.Contains("subtype:'kanji-choice'", StringComparison.Ordinal) ||
                !template.Contains("kokuInstruction", StringComparison.Ordinal) ||
                !template.Contains("effectiveGrade(p)", StringComparison.Ordinal) ||
                !template.Contains("learningStage(p)", StringComparison.Ordinal) ||
                !template.Contains("profileGrade:this.gradeLabel(p)", StringComparison.Ordinal) ||
                !template.Contains("const weakKeys=this.allowedTopics(p).filter", StringComparison.Ordinal) ||
                !template.Contains("linear-gradient(135deg,#ffdad4", StringComparison.Ordinal) ||
                !template.Contains("isMulViz", StringComparison.Ordinal) ||
                !template.Contains("qs.push(this.genFor(this.weightedPick(p),p))", StringComparison.Ordinal) ||
                template.Contains("<div style=\"{{ avatarStyle }}\">{{ profileInitial }}</div>", StringComparison.Ordinal))
            {
                return 14;
            }

            if (!AppPaths.WebViewUserDataFolder.StartsWith(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    StringComparison.OrdinalIgnoreCase))
            {
                return 12;
            }

            if (!AppPaths.UpdatesFolder.StartsWith(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    StringComparison.OrdinalIgnoreCase))
            {
                return 15;
            }

            if (!UpdateManager.TryGetReleaseVersion("v1.1.3", out var parsedVersion) ||
                !UpdateManager.IsNewerVersion(parsedVersion, new Version(1, 1, 1, 0)) ||
                !UpdateManager.TryGetReleaseVersion("1.1.0", out _))
            {
                return 16;
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
