using System.Net;
using System.Reflection;
using KidsTraining.App.Application.Learning;
using KidsTraining.App.Application.ParentControl;
using KidsTraining.App.Application.Updates;
using KidsTraining.App.Domain.ParentControl;
using KidsTraining.App.Domain.Updates;
using KidsTraining.App.Infrastructure.Learning;
using KidsTraining.App.Infrastructure.Lifecycle;
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

        var directTrainingRequested = args.Any(static arg =>
            string.Equals(arg, TrainingArg, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(arg, LearnArg, StringComparison.OrdinalIgnoreCase));
        var autoTrainingRequested = args.Any(static arg =>
            string.Equals(arg, AutoTrainingArg, StringComparison.OrdinalIgnoreCase));
        var trainingRequested = directTrainingRequested || autoTrainingRequested;
        using var singleInstance = TryAcquireSingleInstance();
        if (singleInstance is null)
        {
            return 31;
        }

        if (!singleInstance.IsPrimary)
        {
            if (trainingRequested)
            {
                if (!singleInstance.SignalTrainingRequest())
                {
                    UpdateLogger.Info("Could not signal the primary application instance.");
                    return 32;
                }
            }

            return 0;
        }

        ApplicationConfiguration.Initialize();
        var services = CreateApplicationServices();
        if (directTrainingRequested)
        {
            System.Windows.Forms.Application.Run(new TrainingForm(
                services.LearningPagePreparer,
                services.ParentPinProvider,
                services.ProfileNameProvider,
                services.ParentLearningSettingsService,
                services.ParentLearningResetService));
        }
        else
        {
            var context = new TrayApplicationContext(
                autoTrainingRequested,
                services.LearningPagePreparer,
                services.ParentPinProvider,
                services.ProfileNameProvider,
                services.ParentPasswordService,
                services.ParentLearningSettingsService,
                services.ParentLearningResetService,
                services.UpdateService);
            singleInstance.StartListening(context.RequestTraining);
            System.Windows.Forms.Application.Run(context);
        }

        return 0;
    }

    private static SingleInstanceCoordinator? TryAcquireSingleInstance()
    {
        try
        {
            return SingleInstanceCoordinator.Acquire();
        }
        catch (Exception exception)
        {
            UpdateLogger.Error("Could not coordinate the application instance", exception);
            return null;
        }
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
            var profileName = services.ProfileNameProvider.GetProfileName();
            var contractFailures = GeneratedLearningRuntimeContractValidator.Validate(patchedHtml, profileName);
            if (contractFailures.Count > 0)
            {
                foreach (var failure in contractFailures)
                {
                    Console.Error.WriteLine(
                        $"Generated learning runtime contract [{failure.Code}]: {failure.Message}");
                }

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

            var parentPage = ParentControlServer.BuildParentPage(
                ["http://127.0.0.1:44567/"],
                trainingActive: false,
                new LearningSessionSettings(28, 21));
            if (!parentPage.Contains("Kids Training 保護者画面", StringComparison.Ordinal) ||
                !parentPage.Contains("/api/start", StringComparison.Ordinal) ||
                !parentPage.Contains("/api/return", StringComparison.Ordinal) ||
                !parentPage.Contains("/api/pause", StringComparison.Ordinal) ||
                !parentPage.Contains("/api/reset", StringComparison.Ordinal) ||
                !parentPage.Contains("/api/password", StringComparison.Ordinal) ||
                !parentPage.Contains("/api/settings", StringComparison.Ordinal) ||
                !parentPage.Contains("id=\"questionCount\"", StringComparison.Ordinal) ||
                !parentPage.Contains("value=\"28\"", StringComparison.Ordinal) ||
                !parentPage.Contains("id=\"passLine\"", StringComparison.Ordinal) ||
                !parentPage.Contains("value=\"21\"", StringComparison.Ordinal) ||
                !parentPage.Contains("saveLearningSettings", StringComparison.Ordinal) ||
                !parentPage.Contains("@media (prefers-reduced-motion: reduce)", StringComparison.Ordinal) ||
                !parentPage.Contains("勉強を開始", StringComparison.Ordinal) ||
                !parentPage.Contains("学習を一時停止", StringComparison.Ordinal) ||
                !parentPage.Contains("履歴のみリセット", StringComparison.Ordinal) ||
                !parentPage.Contains("すべてリセット", StringComparison.Ordinal) ||
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
        var parentSettingsStore = new JsonParentSettingsStore();
        var parentPasswordService = new ParentPasswordService(parentSettingsStore);
        var parentLearningSettingsService = new ParentLearningSettingsService(parentSettingsStore);
        var parentLearningResetService = new ParentLearningResetService(parentSettingsStore, parentSettingsStore);
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
            parentLearningSettingsService,
            parentLearningResetService,
            updateService);
    }

    private sealed record ApplicationServices(
        ILearningPagePreparer LearningPagePreparer,
        IParentPinProvider ParentPinProvider,
        IUserProfileNameProvider ProfileNameProvider,
        ParentPasswordService ParentPasswordService,
        ParentLearningSettingsService ParentLearningSettingsService,
        ParentLearningResetService ParentLearningResetService,
        UpdateService UpdateService);
}
