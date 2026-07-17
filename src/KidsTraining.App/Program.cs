using System.Net;
using System.Reflection;
using KidsTraining.App.Application.Learning;
using KidsTraining.App.Application.ParentControl;
using KidsTraining.App.Application.Updates;
using KidsTraining.App.Domain.Learning;
using KidsTraining.App.Domain.ParentControl;
using KidsTraining.App.Domain.Updates;
using KidsTraining.App.Infrastructure.Learning;
using KidsTraining.App.Infrastructure.ParentControl;
using KidsTraining.App.Infrastructure.Settings;
using KidsTraining.App.Infrastructure.Updates;
using KidsTraining.App.Presentation.WinForms;
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
            return MsiUpdateApplier.Run(args);
        }

        ApplicationConfiguration.Initialize();
        var services = CreateApplicationServices();
        if (args.Any(static arg =>
                string.Equals(arg, TrainingArg, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(arg, LearnArg, StringComparison.OrdinalIgnoreCase)))
        {
            System.Windows.Forms.Application.Run(new TrainingForm(
                services.LearningPagePreparer,
                services.ParentPinProvider,
                services.ProfileNameProvider));
        }
        else
        {
            System.Windows.Forms.Application.Run(new TrayApplicationContext(
                args.Any(static arg => string.Equals(arg, AutoTrainingArg, StringComparison.OrdinalIgnoreCase)),
                services.LearningPagePreparer,
                services.ParentPinProvider,
                services.ProfileNameProvider,
                services.ParentPasswordService,
                services.UpdateService));
        }

        return 0;
    }

    private static int RunSmokeTest()
    {
        try
        {
            AppPaths.EnsureRuntimeDirectories();
            var services = CreateApplicationServices();

            if (!File.Exists(AppPaths.HtmlTemplatePath) ||
                !File.Exists(AppPaths.LearningAppDefinitionPath))
            {
                return 11;
            }

            var preparation = services.LearningPagePreparer.Prepare();
            if (!preparation.IsSuccess ||
                preparation.RuntimePagePath is null ||
                !File.Exists(preparation.RuntimePagePath))
            {
                return 13;
            }

            var patchedHtml = File.ReadAllText(preparation.RuntimePagePath);
            var template = patchedHtml;
            if (!template.Contains("screen:'start', profileIdx:0,", StringComparison.Ordinal) ||
                template.Contains("screen:'profile', profileIdx:0,", StringComparison.Ordinal) ||
                !template.Contains("profiles:[\n", StringComparison.Ordinal) ||
                !template.Contains($"name:{System.Text.Json.JsonSerializer.Serialize(services.ProfileNameProvider.GetProfileName())}", StringComparison.Ordinal) ||
                !template.Contains("xp:0", StringComparison.Ordinal) ||
                !template.Contains(LearningDefaults.BeginnerMasteryMarkup, StringComparison.Ordinal) ||
                !template.Contains("count:this.props.questionCount??20", StringComparison.Ordinal) ||
                !template.Contains("pass:this.props.passLine??15", StringComparison.Ordinal) ||
                !template.Contains("genAdd(p)", StringComparison.Ordinal) ||
                !template.Contains("genHissan(p)", StringComparison.Ordinal) ||
                !template.Contains("pickMul(p)", StringComparison.Ordinal) ||
                !template.Contains("pickKokugo(p)", StringComparison.Ordinal) ||
                !template.Contains("pickMoji(p)", StringComparison.Ordinal) ||
                !template.Contains("pickMeasure(p)", StringComparison.Ordinal) ||
                !template.Contains("measureCompare()", StringComparison.Ordinal) ||
                !template.Contains("どちらが ながい？", StringComparison.Ordinal) ||
                !template.Contains("1kg は 何g？", StringComparison.Ordinal) ||
                !template.Contains("1km は 何m？", StringComparison.Ordinal) ||
                !template.Contains("1L は 何dL？", StringComparison.Ordinal) ||
                !template.Contains("10のまとまりで かんがえる", StringComparison.Ordinal) ||
                !template.Contains("pickTimeUnits", StringComparison.Ordinal) ||
                !template.Contains("measure:{label:'たんい'", StringComparison.Ordinal) ||
                !template.Contains("isMeasureViz", StringComparison.Ordinal) ||
                !template.Contains("pickKazu(p)", StringComparison.Ordinal) ||
                !template.Contains("pickShape(p)", StringComparison.Ordinal) ||
                !template.Contains("pickDiv(p)", StringComparison.Ordinal) ||
                !template.Contains("pickFrac(p)", StringComparison.Ordinal) ||
                !template.Contains("pickChart(p)", StringComparison.Ordinal) ||
                !template.Contains("pickStory(p)", StringComparison.Ordinal) ||
                !template.Contains("pickMoney(p)", StringComparison.Ordinal) ||
                !template.Contains("pickGroups(p)", StringComparison.Ordinal) ||
                !template.Contains("pickOrder(p)", StringComparison.Ordinal) ||
                !template.Contains("resetLearningProgress()", StringComparison.Ordinal) ||
                !template.Contains("progressResetAt:Date.now()", StringComparison.Ordinal) ||
                !template.Contains("aria-modal=", StringComparison.Ordinal) ||
                !template.Contains("学習状況をリセット", StringComparison.Ordinal) ||
                template.Contains("localStorage.clear()", StringComparison.Ordinal) ||
                !template.Contains("あまり", StringComparison.Ordinal) ||
                !template.Contains("正三角形", StringComparison.Ordinal) ||
                !template.Contains("subtype:'romaji'", StringComparison.Ordinal) ||
                !template.Contains("topicComplete(p,k)", StringComparison.Ordinal) ||
                !template.Contains("isShapeViz", StringComparison.Ordinal) ||
                !template.Contains("promptStyle", StringComparison.Ordinal) ||
                !template.Contains("markCleared", StringComparison.Ordinal) ||
                !template.Contains("topicReady(p,k", StringComparison.Ordinal) ||
                !template.Contains("なんばんめ", StringComparison.Ordinal) ||
                !template.Contains("subtype:'kotoba'", StringComparison.Ordinal) ||
                !template.Contains("isOrder", StringComparison.Ordinal) ||
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
                !template.Contains("gradeTopics(p)", StringComparison.Ordinal) ||
                !template.Contains("s.independent>=8", StringComparison.Ordinal) ||
                !template.Contains("s.attempts>=10", StringComparison.Ordinal) ||
                template.Contains("if(done('add'))staged.push", StringComparison.Ordinal) ||
                !template.Contains("pickBun(p)", StringComparison.Ordinal) ||
                !template.Contains("pickGoi(p)", StringComparison.Ordinal) ||
                !template.Contains("pickDokkai(p)", StringComparison.Ordinal) ||
                !template.Contains("（　）に はいる じは？", StringComparison.Ordinal) ||
                !template.Contains("かぎかっこ", StringComparison.Ordinal) ||
                !template.Contains("しゅご（だれが・なにが）", StringComparison.Ordinal) ||
                !template.Contains("しゅうしょくご", StringComparison.Ordinal) ||
                !template.Contains("カタカナで 書く ことばは どれ？", StringComparison.Ordinal) ||
                !template.Contains("はんたいの ことばは？", StringComparison.Ordinal) ||
                !template.Contains("なかまはずれは どれ？", StringComparison.Ordinal) ||
                !template.Contains("の いみは？", StringComparison.Ordinal) ||
                !template.Contains("国語じてんの じゅんに", StringComparison.Ordinal) ||
                !template.Contains("topic:'dokkai'", StringComparison.Ordinal) ||
                !template.Contains("あつめた 数は？", StringComparison.Ordinal) ||
                !template.Contains("pickEigo(p)", StringComparison.Ordinal) ||
                !template.Contains("topic:'eigo'", StringComparison.Ordinal) ||
                !template.Contains("curriculumLanes(p)", StringComparison.Ordinal) ||
                !template.Contains("nextCurriculumTopic(p)", StringComparison.Ordinal) ||
                !template.Contains("を 英語で いうと？", StringComparison.Ordinal) ||
                !template.Contains("Good morning.", StringComparison.Ordinal) ||
                !template.Contains("q.sessionRole=role", StringComparison.Ordinal) ||
                !template.Contains("globalPass&&targetPass", StringComparison.Ordinal) ||
                !template.Contains("pickStage(stage,buckets,reviewRate=.25)", StringComparison.Ordinal) ||
                !template.Contains("reviewStage(p,k)", StringComparison.Ordinal) ||
                !template.Contains("profileAtStage(p,k,stage)", StringComparison.Ordinal) ||
                !template.Contains("Number.isFinite(saved)", StringComparison.Ordinal) ||
                !template.Contains("learningSchema===3", StringComparison.Ordinal) ||
                !template.Contains("stageAttempts", StringComparison.Ordinal) ||
                !template.Contains("Math.min(5,Number(stage)||1)", StringComparison.Ordinal) ||
                !template.Contains("masteredAt", StringComparison.Ordinal) ||
                !template.Contains("fromPairs([[1,2],[2,1],[2,2]", StringComparison.Ordinal) ||
                !template.Contains("prompt:a+' × '+b", StringComparison.Ordinal) ||
                template.Contains("prompt:a+' x '+b", StringComparison.Ordinal) ||
                !template.Contains("これは等分除", StringComparison.Ordinal) ||
                !template.Contains("これは包含除", StringComparison.Ordinal) ||
                !template.Contains("q.topic==='div'&&this.topicStage(p,'div')<=2", StringComparison.Ordinal) ||
                !template.Contains("speakEnglish(text)", StringComparison.Ordinal) ||
                !template.Contains("if(m)this.stopEnglishSpeech()", StringComparison.Ordinal) ||
                !template.Contains("SpeechSynthesisUtterance", StringComparison.Ordinal) ||
                !template.Contains("utterance.lang='en-US'", StringComparison.Ordinal) ||
                !template.Contains("utterance.rate=.85", StringComparison.Ordinal) ||
                !template.Contains("speakChoices:!!speak", StringComparison.Ordinal) ||
                !template.Contains("class=\"kt-speech-button\"", StringComparison.Ordinal) ||
                !template.Contains("<button type=\"button\" class=\"kt-choice-button\"", StringComparison.Ordinal) ||
                !template.Contains("disabled title=\"{{ c.speakTitle }}\"", StringComparison.Ordinal) ||
                !template.Contains("stage<=1?['hiragana']", StringComparison.Ordinal) ||
                !template.Contains("profileGrade:this.gradeLabel(p)", StringComparison.Ordinal) ||
                !template.Contains("const weakKeys=this.allowedTopics(p).filter", StringComparison.Ordinal) ||
                !template.Contains("linear-gradient(135deg,#ffdad4", StringComparison.Ordinal) ||
                !template.Contains("isMulViz", StringComparison.Ordinal) ||
                !template.Contains("&&this.topicStage(p,q.topic)<=2", StringComparison.Ordinal) ||
                !template.Contains("q.topic==='mul'&&this.topicStage(p,'mul')<=2", StringComparison.Ordinal) ||
                !template.Contains("kokuShowMean=this.topicStage(p,'kokugo')<=2", StringComparison.Ordinal) ||
                !template.Contains("<sc-if value=\"{{ kokuShowMean }}\"", StringComparison.Ordinal) ||
                !template.Contains("kokuShowMean:kokuShowMean", StringComparison.Ordinal) ||
                !template.Contains("migrateProfiles(profiles)", StringComparison.Ordinal) ||
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

            if (!ReleaseVersion.TryParse("v1.1.3", out var parsedVersion) ||
                !ReleaseVersion.IsNewer(parsedVersion, new Version(1, 1, 1, 0)) ||
                !ReleaseVersion.TryParse("1.1.0", out _))
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

            if (!ParentPin.TryCreate("4456", out var validPin) || validPin.Value != "4456" ||
                ParentPin.TryCreate("abcd", out _) ||
                ParentPin.TryCreate("12345", out _))
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
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 99;
        }
    }

    private static ApplicationServices CreateApplicationServices()
    {
        var parentPasswordService = new ParentPasswordService(new JsonParentPinStore());
        IParentPinProvider parentPinProvider = parentPasswordService;
        var profileNameProvider = new WindowsUserProfileNameProvider();
        var learningPagePreparer = new FileLearningPagePreparer(
            new LearningPageBuilder(),
            parentPinProvider,
            profileNameProvider);
        var currentVersion = ReleaseVersion.Normalize(
            Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0, 0));
        var updateService = new UpdateService(
            currentVersion,
            new GitHubReleaseClient(currentVersion),
            new MsiUpdateInstaller());
        return new ApplicationServices(
            learningPagePreparer,
            parentPinProvider,
            profileNameProvider,
            parentPasswordService,
            updateService);
    }

    private sealed record ApplicationServices(
        ILearningPagePreparer LearningPagePreparer,
        IParentPinProvider ParentPinProvider,
        IUserProfileNameProvider ProfileNameProvider,
        ParentPasswordService ParentPasswordService,
        UpdateService UpdateService);
}
