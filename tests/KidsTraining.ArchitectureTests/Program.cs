using KidsTraining.App.Application.Learning;
using KidsTraining.App.Application.ParentControl;
using KidsTraining.App.Application.Updates;
using KidsTraining.App.Domain.ParentControl;
using KidsTraining.App.Domain.Updates;

namespace KidsTraining.ArchitectureTests;

internal static class Program
{
    private static readonly List<string> Failures = [];

    private static int Main(string[] args)
    {
        if (args.Length != 1 || !Directory.Exists(args[0]))
        {
            Console.Error.WriteLine("Usage: KidsTraining.ArchitectureTests <repository-root>");
            return 2;
        }

        var repositoryRoot = Path.GetFullPath(args[0]);
        Run("ParentPin accepts exactly four digits", TestParentPin);
        Run("Learning page builder preserves required behavior", () => TestBuilder(repositoryRoot));
        Run("Learning page builder rejects missing placeholder", () => TestMissingPlaceholder(repositoryRoot));
        Run("Learning page builder rejects duplicate placeholder", () => TestDuplicatePlaceholder(repositoryRoot));
        Run("Learning markup reports a missing required anchor", () => TestMissingRequiredAnchor(repositoryRoot));
        Run("Preparation result has explicit terminal states", TestPreparationTerminals);
        Run("Parent password changes reach explicit terminal states", TestPasswordServiceTerminals);
        Run("Update checks reach explicit terminal states", TestUpdateServiceTerminals);

        if (Failures.Count == 0)
        {
            Console.WriteLine("Architecture tests passed: 8");
            return 0;
        }

        foreach (var failure in Failures)
        {
            Console.Error.WriteLine(failure);
        }

        return 1;
    }

    private static void TestParentPin()
    {
        Assert(ParentPin.TryCreate(" 4456 ", out var pin) && pin.Value == "4456", "valid PIN was rejected");
        Assert(!ParentPin.TryCreate("abcd", out _), "letters were accepted");
        Assert(!ParentPin.TryCreate("12345", out _), "five digits were accepted");
        Assert(ParentPin.FromOrDefault(null) == ParentPin.Default, "missing PIN did not reach the default terminal value");
    }

    private static void TestBuilder(string repositoryRoot)
    {
        var (template, appDefinition) = ReadLearningSource(repositoryRoot);
        var html = new LearningPageBuilder().Build(
            template,
            appDefinition,
            "Architecture Test",
            ParentPin.FromOrDefault("4456"));

        Assert(!html.Contains("<!--__KIDS_TRAINING_APP__-->", StringComparison.Ordinal), "placeholder remained");
        Assert(html.Contains("screen:'start', profileIdx:0,", StringComparison.Ordinal), "start screen patch is missing");
        Assert(html.Contains("name:\"Architecture Test\"", StringComparison.Ordinal), "profile patch is missing");
        Assert(html.Contains("localStorage.getItem('kt_parent_pin_v1')||'4456'", StringComparison.Ordinal), "parent PIN patch is missing");
        Assert(html.Contains("kids-training/scripts/runtime.js", StringComparison.Ordinal), "external runtime reference is missing");
        Assert(html.Contains("class=\"kt-speech-button\"", StringComparison.Ordinal), "English speech patch is missing");
    }

    private static void TestMissingPlaceholder(string repositoryRoot)
    {
        var (_, appDefinition) = ReadLearningSource(repositoryRoot);
        ExpectInvalidOperation(
            () => new LearningPageBuilder().Build("<html></html>", appDefinition, "Test", ParentPin.Default),
            "placeholder was not found");
    }

    private static void TestDuplicatePlaceholder(string repositoryRoot)
    {
        var (_, appDefinition) = ReadLearningSource(repositoryRoot);
        const string placeholder = "<!--__KIDS_TRAINING_APP__-->";
        ExpectInvalidOperation(
            () => new LearningPageBuilder().Build(placeholder + placeholder, appDefinition, "Test", ParentPin.Default),
            "must occur exactly once");
    }

    private static void TestMissingRequiredAnchor(string repositoryRoot)
    {
        var (template, appDefinition) = ReadLearningSource(repositoryRoot);
        var brokenDefinition = appDefinition.Replace(
            "screen:'profile', profileIdx:0,",
            "screen:'missing', profileIdx:0,",
            StringComparison.Ordinal);
        ExpectInvalidOperation(
            () => new LearningPageBuilder().Build(template, brokenDefinition, "Test", ParentPin.Default),
            "Required learning markup anchor was not found");
    }

    private static void TestPreparationTerminals()
    {
        var prepared = LearningPagePreparationResult.Prepared("runtime.html");
        var failed = LearningPagePreparationResult.Failed("failure");
        Assert(prepared.IsSuccess && prepared.RuntimePagePath == "runtime.html" && prepared.ErrorMessage is null, "prepared state is incomplete");
        Assert(!failed.IsSuccess && failed.RuntimePagePath is null && failed.ErrorMessage == "failure", "failed state is incomplete");
    }

    private static void TestPasswordServiceTerminals()
    {
        var store = new InMemoryParentPinStore(ParentPin.FromOrDefault("1234"));
        var service = new ParentPasswordService(store);
        Assert(!service.Change("9999", "4456").Success, "wrong current PIN succeeded");
        Assert(!service.Change("1234", "abcd").Success, "invalid next PIN succeeded");
        Assert(service.Change("1234", "4456").Success && store.Read().Value == "4456", "valid PIN change failed");

        store.ThrowOnWrite = true;
        var failure = service.Change("4456", "6677");
        Assert(!failure.Success && store.Read().Value == "4456", "write failure did not reach a failed terminal state");
    }

    private static void TestUpdateServiceTerminals()
    {
        var releaseClient = new StubReleaseClient();
        var installer = new RecordingUpdateInstaller();
        var service = new UpdateService(new Version(1, 0, 0, 0), releaseClient, installer);

        Assert(Check(service).Status == UpdateCheckStatus.Failed, "missing release did not fail");
        releaseClient.Release = new ReleaseInfo("v2.0.0", true, false, []);
        Assert(Check(service).Status == UpdateCheckStatus.NoUpdate, "draft release was not terminal");
        releaseClient.Release = new ReleaseInfo("invalid", false, false, []);
        Assert(Check(service).Status == UpdateCheckStatus.NoUpdate, "invalid tag was not terminal");
        releaseClient.Release = new ReleaseInfo("v1.0.0", false, false, []);
        Assert(Check(service).Status == UpdateCheckStatus.NoUpdate, "current release was not terminal");
        releaseClient.Release = new ReleaseInfo("v2.0.0", false, false, []);
        Assert(Check(service).Status == UpdateCheckStatus.Failed, "missing MSI did not fail");
        releaseClient.Release = new ReleaseInfo(
            "v2.0.0",
            false,
            false,
            [new ReleaseAsset("KidsTraining.msi", "https://example.invalid/KidsTraining.msi")]);
        Assert(Check(service).Status == UpdateCheckStatus.UpdateStarted && installer.StartCount == 1, "valid update did not start exactly once");
    }

    private static UpdateCheckResult Check(UpdateService service) =>
        service.CheckAndInstallLatestAsync(CancellationToken.None).GetAwaiter().GetResult();

    private static (string Template, string AppDefinition) ReadLearningSource(string repositoryRoot)
    {
        var learningRoot = Path.Combine(repositoryRoot, "kids-training");
        return (
            File.ReadAllText(Path.Combine(learningRoot, "index.template.html")),
            File.ReadAllText(Path.Combine(learningRoot, "app", "learning-app.dc.html")));
    }

    private static void ExpectInvalidOperation(Action action, string expectedMessage)
    {
        try
        {
            action();
            throw new InvalidOperationException("expected InvalidOperationException was not thrown");
        }
        catch (InvalidOperationException exception) when (
            exception.Message.Contains(expectedMessage, StringComparison.OrdinalIgnoreCase))
        {
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void Run(string name, Action test)
    {
        try
        {
            test();
        }
        catch (Exception exception)
        {
            Failures.Add($"FAIL: {name}: {exception.Message}");
        }
    }

    private sealed class InMemoryParentPinStore(ParentPin initialPin) : IParentPinStore
    {
        private ParentPin pin = initialPin;

        public bool ThrowOnWrite { get; set; }

        public ParentPin Read() => pin;

        public void Write(ParentPin nextPin)
        {
            if (ThrowOnWrite)
            {
                throw new IOException("simulated write failure");
            }

            pin = nextPin;
        }
    }

    private sealed class StubReleaseClient : IReleaseClient
    {
        public ReleaseInfo? Release { get; set; }

        public Task<ReleaseInfo?> GetLatestAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Release);
    }

    private sealed class RecordingUpdateInstaller : IUpdateInstaller
    {
        public int StartCount { get; private set; }

        public Task StartAsync(
            ReleaseAsset asset,
            Version releaseVersion,
            CancellationToken cancellationToken)
        {
            StartCount++;
            return Task.CompletedTask;
        }
    }
}
