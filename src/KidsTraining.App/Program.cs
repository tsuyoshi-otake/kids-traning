using System.Net;
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
                !template.Contains("xp:0", StringComparison.Ordinal) ||
                !template.Contains("mastery:{add:.05,sub:.05,mul:.05,clock:.05,kokugo:.05,hissan:.05,moji:.05}", StringComparison.Ordinal) ||
                !template.Contains("count:this.props.questionCount??20", StringComparison.Ordinal) ||
                !template.Contains("pass:this.props.passLine??15", StringComparison.Ordinal) ||
                !template.Contains("genAdd(p)", StringComparison.Ordinal) ||
                !template.Contains("genHissan(p)", StringComparison.Ordinal) ||
                !template.Contains("pickMul(p)", StringComparison.Ordinal) ||
                !template.Contains("pickKokugo(p)", StringComparison.Ordinal) ||
                !template.Contains("pickMoji(p)", StringComparison.Ordinal) ||
                !template.Contains("gainXp", StringComparison.Ordinal) ||
                !template.Contains("xpLevel", StringComparison.Ordinal) ||
                !template.Contains("fbXp", StringComparison.Ordinal) ||
                !template.Contains("earnedXp", StringComparison.Ordinal) ||
                !template.Contains("べんきょうを つづける", StringComparison.Ordinal) ||
                !template.Contains("subtype:'alphabet'", StringComparison.Ordinal) ||
                !template.Contains("subtype:'hiragana'", StringComparison.Ordinal) ||
                !template.Contains("subtype:'katakana'", StringComparison.Ordinal) ||
                !template.Contains("1cm は 何mm？", StringComparison.Ordinal) ||
                !template.Contains("subtype:'kanji-choice'", StringComparison.Ordinal) ||
                !template.Contains("kokuInstruction", StringComparison.Ordinal) ||
                !template.Contains("effectiveGrade(p)", StringComparison.Ordinal) ||
                !template.Contains("learningStage(p)", StringComparison.Ordinal) ||
                !template.Contains("topicStage(p,k)", StringComparison.Ordinal) ||
                !template.Contains("hissanComplete(p)", StringComparison.Ordinal) ||
                !template.Contains("!hissanDone)staged=['add','sub','clock','kokugo','moji','hissan']", StringComparison.Ordinal) ||
                !template.Contains("else staged=['add','sub','clock','kokugo','moji','hissan','mul']", StringComparison.Ordinal) ||
                !template.Contains("mentalAddendMax=9", StringComparison.Ordinal) ||
                !template.Contains("mentalSubtrahendMax=9", StringComparison.Ordinal) ||
                !template.Contains("pairs=[[1,2],[2,1],[2,2]", StringComparison.Ordinal) ||
                !template.Contains("stage<=1?['hiragana']", StringComparison.Ordinal) ||
                !template.Contains("profileGrade:this.gradeLabel(p)", StringComparison.Ordinal) ||
                !template.Contains("const weakKeys=this.allowedTopics(p).filter", StringComparison.Ordinal) ||
                !template.Contains("linear-gradient(135deg,#ffdad4", StringComparison.Ordinal) ||
                !template.Contains("isMulViz", StringComparison.Ordinal) ||
                !template.Contains("qs.push(this.genFor(this.weightedPick(p),p))", StringComparison.Ordinal) ||
                template.Contains("b=this.rand(11,a-1)", StringComparison.Ordinal) ||
                template.Contains("b=this.rand(1,40)", StringComparison.Ordinal) ||
                template.Contains("b=this.rand(12,79)", StringComparison.Ordinal) ||
                template.Contains("b=this.rand(11,79)", StringComparison.Ordinal) ||
                template.Contains("b=this.rand(10,99-a)", StringComparison.Ordinal) ||
                template.Contains("b=this.rand(20,a-1)", StringComparison.Ordinal) ||
                template.Contains("Math.min(40,a-1)", StringComparison.Ordinal) ||
                template.Contains("アバター", StringComparison.Ordinal) ||
                template.Contains("avatarReady", StringComparison.Ordinal) ||
                template.Contains("avatarParts", StringComparison.Ordinal) ||
                template.Contains("finishAvatar", StringComparison.Ordinal) ||
                template.Contains("<div style=\"{{ avatarStyle }}\">{{ profileInitial }}</div>", StringComparison.Ordinal) ||
                template.Contains("profileInitial:p.name.charAt(0), avatarStyle", StringComparison.Ordinal))
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

            var parentPage = ParentControlServer.BuildParentPage(["http://127.0.0.1:44567/"], trainingActive: false);
            if (!parentPage.Contains("Kids Training 保護者画面", StringComparison.Ordinal) ||
                !parentPage.Contains("/api/start", StringComparison.Ordinal) ||
                !parentPage.Contains("/api/return", StringComparison.Ordinal) ||
                !parentPage.Contains("/api/password", StringComparison.Ordinal) ||
                !parentPage.Contains("勉強を開始", StringComparison.Ordinal) ||
                !parentPage.Contains("パソコンの画面に戻す", StringComparison.Ordinal) ||
                !parentPage.Contains("パスワードを変更", StringComparison.Ordinal))
            {
                return 17;
            }

            if (!ParentControlServer.IsAllowedRemoteAddress(IPAddress.Parse("192.168.1.10")) ||
                !ParentControlServer.IsAllowedRemoteAddress(IPAddress.Parse("10.0.0.2")) ||
                ParentControlServer.IsAllowedRemoteAddress(IPAddress.Parse("8.8.8.8")))
            {
                return 18;
            }

            if (ParentSettings.NormalizePassword("4456") != "4456" ||
                ParentSettings.NormalizePassword("abcd") is not null ||
                ParentSettings.NormalizePassword("12345") is not null)
            {
                return 19;
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
