using KidsTraining.App.Application.Learning;
using KidsTraining.App.Application.ParentControl;
using KidsTraining.App.Application.Updates;
using KidsTraining.App.Domain.ParentControl;
using KidsTraining.App.Domain.Learning;
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
        Run("Curriculum lanes follow implemented grade scope", TestCurriculumPolicy);
        Run("Learning evidence separates outcomes and readiness", TestLearningEvidence);
        Run("Review schedule uses bounded spaced intervals", TestReviewSchedule);
        Run("Learning markup contains evidence-based progression", () => TestEducationalProgressionMarkup(repositoryRoot));
        Run("Learning page builder rejects missing placeholder", () => TestMissingPlaceholder(repositoryRoot));
        Run("Learning page builder rejects duplicate placeholder", () => TestDuplicatePlaceholder(repositoryRoot));
        Run("Learning markup reports a missing required anchor", () => TestMissingRequiredAnchor(repositoryRoot));
        Run("Preparation result has explicit terminal states", TestPreparationTerminals);
        Run("Parent password changes reach explicit terminal states", TestPasswordServiceTerminals);
        Run("Update checks reach explicit terminal states", TestUpdateServiceTerminals);

        if (Failures.Count == 0)
        {
            Console.WriteLine("Architecture tests passed: 12");
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

    private static void TestCurriculumPolicy()
    {
        Assert(CurriculumPolicy.NormalizeGrade(0) == 1, "grades below the implemented range were not clamped");
        Assert(CurriculumPolicy.NormalizeGrade(6) == 3, "grades above the implemented range were not clamped");
        Assert(CurriculumPolicy.IsAvailable(1, "kokugo"), "grade 1 Japanese was unexpectedly locked");
        Assert(CurriculumPolicy.IsAvailable(1, "chart"), "grade 1 math strands were unexpectedly locked");
        Assert(CurriculumPolicy.IsAvailable(1, "money") && CurriculumPolicy.IsAvailable(1, "groups"), "grade 1 foundations are incomplete");
        Assert(!CurriculumPolicy.IsAvailable(1, "hissan"), "grade 2 written arithmetic leaked into grade 1");
        Assert(CurriculumPolicy.IsAvailable(2, "mul") && CurriculumPolicy.IsAvailable(2, "order"), "grade 2 calculation order or multiplication was unavailable");
        Assert(!CurriculumPolicy.IsAvailable(2, "eigo"), "supplementary English started before grade 3");
        Assert(CurriculumPolicy.IsAvailable(3, "div") && CurriculumPolicy.IsAvailable(3, "eigo"), "grade 3 scope is incomplete");

        var gradeOneLanes = CurriculumPolicy.TopicLanesForGrade(1);
        var gradeTwoLanes = CurriculumPolicy.TopicLanesForGrade(2);
        var gradeThreeLanes = CurriculumPolicy.TopicLanesForGrade(3);
        Assert(gradeOneLanes[0][0] == "kazu" && gradeOneLanes[1][0] == "moji", "grade 1 does not start with number and character foundations");
        Assert(gradeTwoLanes[0].Take(6).SequenceEqual(["chart", "clock", "add", "sub", "measure", "hissan"]), "grade 2 first-term order is incorrect");
        var gradeTwoMath = gradeTwoLanes[0].ToList();
        Assert(gradeTwoMath.IndexOf("order") < gradeTwoMath.IndexOf("mul"), "grade 2 calculation order must precede multiplication");
        Assert(gradeThreeLanes[0].Take(5).SequenceEqual(["mul", "div", "shape", "hissan", "kazu"]), "grade 3 first-term order is incorrect");
        foreach (var grade in new[] { 1, 2, 3 })
        {
            var flattened = CurriculumPolicy.TopicLanesForGrade(grade).SelectMany(static lane => lane).ToArray();
            Assert(flattened.Length == flattened.Distinct(StringComparer.Ordinal).Count(), $"grade {grade} repeats a topic across curriculum lanes");
            Assert(flattened.ToHashSet(StringComparer.Ordinal).SetEquals(CurriculumPolicy.TopicsForGrade(grade)), $"grade {grade} lanes and available topics diverged");
        }
    }

    private static void TestLearningEvidence()
    {
        var now = new DateTimeOffset(2026, 7, 13, 0, 0, 0, TimeSpan.Zero);
        var evidence = new SkillEvidence();
        evidence = evidence.Record(LearningOutcome.Incorrect, now);
        evidence = evidence.Record(LearningOutcome.Revealed, now.AddMinutes(1));
        Assert(evidence.Errors == 2 && evidence.AssistedCorrect == 1, "revealed and incorrect outcomes were not recorded separately");
        Assert(evidence.IndependentCorrect == 0, "helped work counted as independent evidence");

        for (var index = 0; index < 8; index++)
        {
            evidence = evidence.Record(LearningOutcome.IndependentCorrect, now.AddMinutes(index + 2));
        }

        var lastAnswer = now.AddMinutes(9);
        Assert(evidence.Attempts == 10 && evidence.IndependentAccuracy == 0.8, "evidence totals are incorrect");
        Assert(evidence.IsAchievement && evidence.IsReady(lastAnswer), "qualified independent evidence did not create readiness and an achievement");
        Assert(evidence.NextReviewAt.HasValue && !evidence.IsReady(evidence.NextReviewAt.Value), "overdue review did not make current readiness expire");
        Assert(evidence.IsAchievement, "an overdue review erased the historical achievement");

        var assisted = new SkillEvidence().Record(LearningOutcome.AssistedCorrect, now);
        Assert(assisted.IndependentCorrect == 0 && assisted.IsDue(now), "assistance was counted as independent or was not scheduled immediately");
    }

    private static void TestReviewSchedule()
    {
        var now = new DateTimeOffset(2026, 7, 13, 0, 0, 0, TimeSpan.Zero);
        var expectedDays = new[] { 1, 3, 7, 21, 21 };
        var evidence = new SkillEvidence();
        foreach (var days in expectedDays)
        {
            evidence = evidence.Record(LearningOutcome.IndependentCorrect, now);
            Assert(evidence.NextReviewAt == now.AddDays(days), $"review interval was not {days} days");
            now = evidence.NextReviewAt.GetValueOrDefault();
        }
    }

    private static void TestEducationalProgressionMarkup(string repositoryRoot)
    {
        var (template, appDefinition) = ReadLearningSource(repositoryRoot);
        var html = new LearningPageBuilder().Build(template, appDefinition, "Progression Test", ParentPin.Default);

        Assert(html.Contains("migrateProfiles(profiles)", StringComparison.Ordinal), "legacy profile migration is missing");
        Assert(html.Contains("learningSchema===3", StringComparison.Ordinal) && html.Contains("stageAttempts", StringComparison.Ordinal), "ordered five-stage migration is missing");
        Assert(html.Contains("masteredAt", StringComparison.Ordinal) && html.Contains("topicReady", StringComparison.Ordinal), "achievement and readiness are not separate");
        Assert(html.Contains("outcome==='revealed'", StringComparison.Ordinal), "revealed answers have no distinct evidence path");
        Assert(html.Contains("intervals=[86400000,259200000,604800000,1814400000]", StringComparison.Ordinal), "spaced-review intervals are missing");
        Assert(html.Contains("for(let round=0;round<3;round++)", StringComparison.Ordinal), "calibration does not repeat core skills three times");
        Assert(html.Contains("score=(!r||!r.attempts)?0.05", StringComparison.Ordinal), "untested skills are not initialized conservatively");
        Assert(html.Contains("q.sessionRole=role", StringComparison.Ordinal) && html.Contains("'review'", StringComparison.Ordinal) && html.Contains("'target'", StringComparison.Ordinal) && html.Contains("'mixed'", StringComparison.Ordinal) && html.Contains("'exit'", StringComparison.Ordinal), "session roles are incomplete");
        Assert(html.Contains("nextCurriculumTopic(p)", StringComparison.Ordinal) && html.Contains("frontierTopics(p)", StringComparison.Ordinal), "curriculum frontier selection is missing");
        Assert(html.Contains("configured[k]!==false", StringComparison.Ordinal), "new curriculum topics are disabled for migrated settings");
        Assert(html.Contains("reviewCount=due.length?", StringComparison.Ordinal) && html.Contains("this.weightedPick(p,due),'review'", StringComparison.Ordinal), "fresh sessions can start with unscheduled random review questions");
        Assert(!html.Contains("this.shuffle(planned);add(target,'exit')", StringComparison.Ordinal), "session roles are still shuffled out of curriculum order");
        Assert(html.Contains("globalPass&&targetPass", StringComparison.Ordinal), "session completion ignores target-skill evidence");
        Assert(html.Contains("const gradeOpts=[1,2,3].map", StringComparison.Ordinal), "UI still claims unsupported grades");
        Assert(!html.Contains("if(done('add'))staged.push", StringComparison.Ordinal), "cross-subject prerequisite chain remains");
        Assert(html.Contains("1000万を 10こ", StringComparison.Ordinal) && html.Contains("const scale=(g>=3&&stage>=4)?5", StringComparison.Ordinal), "key grade 3 number/chart content is missing");
        Assert(html.Contains("const a=this.rand(12,89),b=this.rand(11,39)", StringComparison.Ordinal) && html.Contains("const a=this.rand(1234,7899)", StringComparison.Ordinal), "advanced grade 3 written arithmetic is missing");
        Assert(html.Contains("pickMoney(p)", StringComparison.Ordinal) && html.Contains("pickGroups(p)", StringComparison.Ordinal), "grade 1 money or equal-group foundations are missing");
        Assert(html.Contains("pickOrder(p)", StringComparison.Ordinal) && html.Contains("（ ）の なかを さきに", StringComparison.Ordinal), "parentheses or inequalities are missing");
        Assert(html.Contains("isTape:true", StringComparison.Ordinal) && html.Contains("isTable:true", StringComparison.Ordinal), "tape-diagram or table questions are missing");
        Assert(html.Contains("pickDiv(p)", StringComparison.Ordinal) && html.Contains("等分除", StringComparison.Ordinal) && html.Contains("包含除", StringComparison.Ordinal), "division concepts are incomplete");
        Assert(html.Contains("difficulty:5", StringComparison.Ordinal) && html.Contains("コンパス", StringComparison.Ordinal), "staged grade 3 written arithmetic or circle work is missing");
        Assert(html.Contains("q.isMoney", StringComparison.Ordinal) && html.Contains("q.isGroups", StringComparison.Ordinal) && html.Contains("q.isTape", StringComparison.Ordinal), "new visual scaffolding is missing");
        Assert(html.Contains("補助活動：音声を聞き、声に出して", StringComparison.Ordinal) && html.Contains("ノートに漢字を書いて", StringComparison.Ordinal), "supplementary output practice is not identified");
        Assert(html.Contains("aria-label=\"答えと説明を見る\"", StringComparison.Ordinal) && html.Contains("outcome==='revealed'", StringComparison.Ordinal), "revealed-answer control is inaccessible or unscored");
        Assert(html.Contains("role=\"button\" tabindex=\"0\"", StringComparison.Ordinal) && html.Contains("document.addEventListener('keydown'", StringComparison.Ordinal) && html.Contains("aria-live=\"polite\"", StringComparison.Ordinal), "keyboard or live-region accessibility is missing");
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
