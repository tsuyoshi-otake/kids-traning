using KidsTraining.App.Application.Learning;
using KidsTraining.App.Application.ParentControl;
using KidsTraining.App.Application.Updates;
using KidsTraining.App.Domain.ParentControl;
using KidsTraining.App.Domain.Learning;
using KidsTraining.App.Domain.Updates;
using KidsTraining.App.Infrastructure.Lifecycle;
using KidsTraining.App.Infrastructure.Learning;
using KidsTraining.App.Infrastructure.ParentControl;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

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
        Run("Curriculum lanes follow implemented grade scope", () => TestCurriculumPolicy(repositoryRoot));
        Run("Learning evidence separates outcomes and readiness", TestLearningEvidence);
        Run("Review schedule uses bounded spaced intervals", TestReviewSchedule);
        Run("Learning markup contains evidence-based progression", () => TestEducationalProgressionMarkup(repositoryRoot));
        Run("Learning hot paths retain bounded-work performance contracts", () => TestPerformanceContracts(repositoryRoot));
        Run("Generated learning algorithms keep bounded operation counts", () => TestAlgorithmPerformanceAudit(repositoryRoot));
        Run("Generated questions satisfy curriculum invariants", () => TestGeneratedQuestionAudit(repositoryRoot));
        Run("Learning page builder rejects missing placeholder", () => TestMissingPlaceholder(repositoryRoot));
        Run("Learning page builder rejects duplicate placeholder", () => TestDuplicatePlaceholder(repositoryRoot));
        Run("Learning markup reports a missing required anchor", () => TestMissingRequiredAnchor(repositoryRoot));
        Run("Learning markup rejects a duplicate required anchor", () => TestDuplicateRequiredAnchor(repositoryRoot));
        Run("Preparation result has explicit terminal states", TestPreparationTerminals);
        Run("Learning runtime preparation reuses a validated cache", () => TestLearningRuntimeCache(repositoryRoot));
        Run("Legacy storage migration retries are bounded across startups", TestLegacyStorageMigrationState);
        Run("Parent password changes reach explicit terminal states", TestPasswordServiceTerminals);
        Run("Parent learning settings reach explicit terminal states", TestLearningSettingsServiceTerminals);
        Run("Parent learning resets reach explicit terminal states", TestLearningResetServiceTerminals);
        Run("Update checks reach explicit terminal states", TestUpdateServiceTerminals);
        Run("Parent control server awaits actions and shuts down cleanly", TestParentControlServerLifecycle);
        Run("Single-instance requests reach the primary instance", TestSingleInstanceCoordinator);

        if (Failures.Count == 0)
        {
            Console.WriteLine("Architecture tests passed: 22");
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

        var runtimeOutput = Environment.GetEnvironmentVariable("KIDS_TRAINING_RUNTIME_OUTPUT");
        if (!string.IsNullOrWhiteSpace(runtimeOutput))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(runtimeOutput))!);
            File.WriteAllText(runtimeOutput, html, new UTF8Encoding(false));
        }

        Assert(!html.Contains("<!--__KIDS_TRAINING_APP__-->", StringComparison.Ordinal), "placeholder remained");
        Assert(html.Contains("localStorage.getItem('kt_parent_pin_v1')||'4456'", StringComparison.Ordinal), "parent PIN patch is missing");
        Assert(html.Contains("kids-training/scripts/runtime.js", StringComparison.Ordinal), "external runtime reference is missing");
        var failures = GeneratedLearningRuntimeContractValidator.Validate(html, "Architecture Test");
        Assert(
            failures.Count == 0,
            "generated runtime contract failed: " +
            string.Join("; ", failures.Select(static failure => $"{failure.Code}: {failure.Message}")));

        var missingUnlockMessage = html.Replace(
            "kidsTraining.unlock",
            "kidsTraining.missingUnlock",
            StringComparison.Ordinal);
        var explicitFailures =
            GeneratedLearningRuntimeContractValidator.Validate(missingUnlockMessage, "Architecture Test");
        Assert(
            explicitFailures.Any(static failure => failure.Code == "unlock-message"),
            "a missing generated runtime marker did not produce an explicit contract failure");

        Assert(
            html.Contains("role=\"img\" aria-label=\"{{ mrow.ariaLabel }}\"", StringComparison.Ordinal) &&
            html.Contains("<div aria-hidden=\"true\" style=\"{{ mcell.style }}\">", StringComparison.Ordinal),
            "visual learning aids do not expose one concise accessible description while hiding decorative cells");
        Assert(
            html.Contains("role=\"img\" aria-label=\"{{ fr.ariaLabel }}\"", StringComparison.Ordinal) &&
            html.Contains("<div aria-hidden=\"true\" style=\"{{ fr.boxStyle }}\">", StringComparison.Ordinal),
            "addition and subtraction ten-frames lack an accessible description or expose decorative dots");
        Assert(
            html.Contains("width:100%; max-width:720px; min-width:0;", StringComparison.Ordinal) &&
            html.Contains("overflow-x:auto;", StringComparison.Ordinal),
            "wide visual learning aids can escape the question area instead of using a bounded local scroller");
        Assert(
            html.Contains("width:min(240px,72vw)", StringComparison.Ordinal) &&
            html.Contains("Number(q.h)", StringComparison.Ordinal) &&
            html.Contains("Number(q.m)", StringComparison.Ordinal) &&
            html.Contains("+'時'+", StringComparison.Ordinal) &&
            html.Contains("+'分を示す時計。'", StringComparison.Ordinal),
            "clock accessibility does not describe the shown hour, minute, and question context");
        Assert(
            html.Contains("if(q.isOrder)row.ariaLabel=", StringComparison.Ordinal) &&
            html.Contains("if(q.isFracViz)row.ariaLabel=", StringComparison.Ordinal) &&
            html.Contains("if(q.isTape)row.ariaLabel=", StringComparison.Ordinal),
            "order, fraction, or tape-diagram visuals are missing answer-relevant accessible descriptions");
    }

    private static void TestCurriculumPolicy(string repositoryRoot)
    {
#if false
        Assert(
            Math.Abs(LearningDefaults.BeginnerMastery - 0.05) < double.Epsilon,
            "beginner mastery is not represented as a structured domain value");
        Assert(CurriculumPolicy.NormalizeGrade(0) == 1, "grades below the implemented range were not clamped");
        Assert(CurriculumPolicy.NormalizeGrade(6) == 3, "grades above the implemented range were not clamped");
        Assert(CurriculumPolicy.IsAvailable(1, "kokugo"), "grade 1 Japanese was unexpectedly locked");
        Assert(CurriculumPolicy.IsAvailable(1, "chart"), "grade 1 math strands were unexpectedly locked");
        Assert(CurriculumPolicy.IsAvailable(1, "money") && CurriculumPolicy.IsAvailable(1, "groups"), "grade 1 foundations are incomplete");
        Assert(
            CurriculumPolicy.IsAvailable(1, "seikatsu") && CurriculumPolicy.IsAvailable(1, "doutoku") &&
            CurriculumPolicy.IsAvailable(1, "jouhou") && CurriculumPolicy.IsAvailable(1, "tokubetsu"),
            "grade 1 cross-curricular scope is incomplete");
        Assert(!CurriculumPolicy.IsAvailable(1, "shakai") && !CurriculumPolicy.IsAvailable(1, "rika"), "grade 3 subjects leaked into grade 1");
        Assert(!CurriculumPolicy.IsAvailable(1, "hissan"), "grade 2 written arithmetic leaked into grade 1");
        Assert(CurriculumPolicy.IsAvailable(2, "mul") && CurriculumPolicy.IsAvailable(2, "order"), "grade 2 calculation order or multiplication was unavailable");
        Assert(CurriculumPolicy.IsAvailable(2, "seikatsu") && CurriculumPolicy.IsAvailable(2, "jouhou"), "grade 2 life or information activity was unavailable");
        Assert(!CurriculumPolicy.IsAvailable(2, "eigo"), "supplementary English started before grade 3");
        Assert(!CurriculumPolicy.IsAvailable(2, "shakai") && !CurriculumPolicy.IsAvailable(2, "rika") && !CurriculumPolicy.IsAvailable(2, "sougou"), "grade 3 subjects leaked into grade 2");
        Assert(
            CurriculumPolicy.IsAvailable(3, "div") && CurriculumPolicy.IsAvailable(3, "soroban") &&
            CurriculumPolicy.IsAvailable(3, "shakai") && CurriculumPolicy.IsAvailable(3, "rika") &&
            CurriculumPolicy.IsAvailable(3, "eigo") && CurriculumPolicy.IsAvailable(3, "sougou"),
            "grade 3 scope is incomplete");
        Assert(!CurriculumPolicy.IsAvailable(3, "seikatsu"), "life studies continued after the grade 3 subject transition");
        Assert(
            !CurriculumPolicy.AllTopics.Contains("ongaku", StringComparer.Ordinal) &&
            !CurriculumPolicy.AllTopics.Contains("taiiku", StringComparer.Ordinal) &&
            !CurriculumPolicy.AllTopics.Contains("zukou", StringComparer.Ordinal),
            "explicitly excluded music, physical education, or arts and crafts entered the curriculum");
        Assert(
            CurriculumPolicy.IsAvailable(1, "keyboard") &&
            CurriculumPolicy.IsAvailable(2, "keyboard") &&
            CurriculumPolicy.IsAvailable(3, "keyboard"),
            "physical keyboard practice is not independently available in every implemented grade");
        Assert(CurriculumPolicy.PrerequisitesFor("keyboard").Count == 0, "keyboard practice unexpectedly has a prerequisite");
        Assert(CurriculumPolicy.PrerequisitesFor("soroban").SequenceEqual(["kazu"]), "soroban is not gated by number foundations");
        Assert(CurriculumPolicy.PrerequisitesFor("sougou").SequenceEqual(["jouhou"]), "integrated study is not gated by information literacy");

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

        var allTopics = Enumerable.Range(1, 3)
            .SelectMany(static grade => CurriculumPolicy.TopicsForGrade(grade))
            .ToHashSet(StringComparer.Ordinal);
        Assert(
            CurriculumPolicy.AllTopics.ToHashSet(StringComparer.Ordinal).SetEquals(allTopics),
            "the canonical topic list diverged from the implemented curriculum");
        var prerequisitesByTopic = CurriculumPolicy.PrerequisitesByTopic;
        Assert(
            prerequisitesByTopic.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(allTopics),
            "the prerequisite graph does not define every curriculum topic exactly once");
        foreach (var (topic, prerequisites) in prerequisitesByTopic)
        {
            Assert(allTopics.Contains(topic), $"unknown prerequisite graph topic: {topic}");
            Assert(prerequisites.All(allTopics.Contains), $"{topic} references an unknown prerequisite");
            Assert(
                prerequisites.Count == prerequisites.Distinct(StringComparer.Ordinal).Count(),
                $"{topic} repeats a prerequisite");
            Assert(!prerequisites.Contains(topic, StringComparer.Ordinal), $"{topic} depends on itself");
        }

        foreach (var grade in new[] { 1, 2, 3 })
        {
            foreach (var topic in CurriculumPolicy.TopicsForGrade(grade))
            {
                Assert(
                    CurriculumPolicy.PrerequisitesFor(topic).All(prerequisite => CurriculumPolicy.IsAvailable(grade, prerequisite)),
                    $"grade {grade} topic {topic} depends on an unavailable prerequisite");
            }
        }

        var visiting = new HashSet<string>(StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        bool IsAcyclic(string topic)
        {
            if (visited.Contains(topic))
            {
                return true;
            }

            if (!visiting.Add(topic))
            {
                return false;
            }

            foreach (var prerequisite in CurriculumPolicy.PrerequisitesFor(topic))
            {
                if (!IsAcyclic(prerequisite))
                {
                    return false;
                }
            }

            visiting.Remove(topic);
            visited.Add(topic);
            return true;
        }

        foreach (var topic in allTopics)
        {
            Assert(IsAcyclic(topic), $"the prerequisite graph contains a cycle through {topic}");
        }

        Assert(CurriculumPolicy.PrerequisitesFor("mul").SequenceEqual(["groups"]), "multiplication does not depend on equal groups");
        Assert(CurriculumPolicy.PrerequisitesFor("div").SequenceEqual(["mul"]), "division does not depend on multiplication");
        Assert(CurriculumPolicy.PrerequisitesFor("hissan").ToHashSet(StringComparer.Ordinal).SetEquals(["add", "sub"]), "written arithmetic prerequisites are incomplete");
        Assert(CurriculumPolicy.PrerequisitesFor("dokkai").ToHashSet(StringComparer.Ordinal).SetEquals(["bun", "kokugo", "goi"]), "reading prerequisites are incomplete");
#endif
        Assert(
            Math.Abs(LearningDefaults.BeginnerMastery - 0.05) < double.Epsilon,
            "beginner mastery is not represented as a structured domain value");
        Assert(CurriculumPolicy.NormalizeGrade(0) == 1, "grades below the implemented range were not clamped");
        Assert(CurriculumPolicy.NormalizeGrade(10) == 9, "grades above the implemented range were not clamped");

        var units = CurriculumPolicy.AllUnits;
        Assert(units.Count > 0, "the curriculum catalog is empty");
        Assert(units.Select(static unit => unit.Id).Distinct(StringComparer.Ordinal).Count() == units.Count, "curriculum unit IDs are not unique");
        Assert(
            Enumerable.Range(1, 9).All(grade => units.Any(unit => unit.Grade == grade)),
            "one or more grades from elementary 1 through junior-high 3 have no curriculum units");
        Assert(
            units.All(static unit =>
                !string.IsNullOrWhiteSpace(unit.SubjectId) &&
                !string.IsNullOrWhiteSpace(unit.TopicId) &&
                !string.IsNullOrWhiteSpace(unit.GeneratorKey) &&
                !string.IsNullOrWhiteSpace(unit.AssessmentMode) &&
                Uri.TryCreate(unit.SourceReference, UriKind.Absolute, out _)),
            "a curriculum unit is missing subject, area, generator, assessment, or official source metadata");
        Assert(
            units.Where(static unit => unit.GeneratorKey == "curriculum-bank").All(static unit => unit.Questions.Count > 0),
            "a curriculum-bank unit has no questions");
        Assert(
            units.All(static unit => unit.Grade is >= 1 and <= 9 && unit.Order > 0),
            "a curriculum unit has an invalid grade or order");

        var ids = units.Select(static unit => unit.Id).ToHashSet(StringComparer.Ordinal);
        Assert(
            units.SelectMany(static unit => unit.Prerequisites).All(ids.Contains),
            "the curriculum graph references an unknown prerequisite unit");
        var visiting = new HashSet<string>(StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        bool IsAcyclic(string unitId)
        {
            if (visited.Contains(unitId)) return true;
            if (!visiting.Add(unitId)) return false;
            foreach (var prerequisite in CurriculumPolicy.Unit(unitId).Prerequisites)
            {
                if (!IsAcyclic(prerequisite)) return false;
            }
            visiting.Remove(unitId);
            visited.Add(unitId);
            return true;
        }

        Assert(ids.All(IsAcyclic), "the curriculum prerequisite graph contains a cycle");
        Assert(
            CurriculumPolicy.CurriculumLanes.SelectMany(static lane => lane).ToHashSet(StringComparer.Ordinal).SetEquals(ids),
            "subject lanes do not cover every curriculum unit exactly once");
        Assert(
            units.Any(static unit => unit.Grade == 6 && unit.SubjectId == "math") &&
            units.Any(static unit => unit.Grade == 6 && unit.SubjectId == "japanese") &&
            units.Any(static unit => unit.Grade == 6 && unit.SubjectId == "science") &&
            units.Any(static unit => unit.Grade == 6 && unit.SubjectId == "social") &&
            units.Any(static unit => unit.Grade == 6 && unit.SubjectId == "english") &&
            units.Any(static unit => unit.Grade == 6 && unit.SubjectId == "home-economics"),
            "the grade 4-6 core curriculum is incomplete");
        Assert(
            new[] { "math", "japanese", "science", "social", "english", "technology", "home-economics", "moral", "integrated", "information", "special-activities" }
                .All(subject => Enumerable.Range(7, 3).All(grade => units.Any(unit => unit.Grade == grade && unit.SubjectId == subject))),
            "the junior-high grade 1-3 curriculum is incomplete");
        Assert(
            !CurriculumPolicy.AllTopics.Contains("ongaku", StringComparer.Ordinal) &&
            !CurriculumPolicy.AllTopics.Contains("taiiku", StringComparer.Ordinal) &&
            !CurriculumPolicy.AllTopics.Contains("zukou", StringComparer.Ordinal),
            "an explicitly excluded subject entered the curriculum");
        var alignment = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "curriculum-alignment.md"));
        Assert(
            units.All(unit => alignment.Contains($"`{unit.Id}`", StringComparison.Ordinal)),
            "the curriculum alignment document does not enumerate every stable unit ID");
    }

    private static void TestLearningEvidence()
    {
        var now = new DateTimeOffset(2026, 7, 13, 0, 0, 0, TimeSpan.Zero);
        var evidence = new SkillEvidence();
        evidence = evidence.Record(LearningOutcome.Incorrect, now);
        evidence = evidence.Record(LearningOutcome.Revealed, now.AddMinutes(1));
        Assert(evidence.Errors == 2 && evidence.AssistedCorrect == 1, "revealed and incorrect outcomes were not recorded separately");
        Assert(evidence.IndependentCorrect == 0, "helped work counted as independent evidence");

        for (var index = 0; index < 10; index++)
        {
            evidence = evidence.Record(LearningOutcome.IndependentCorrect, now.AddMinutes(index + 2));
        }

        var lastAnswer = now.AddMinutes(11);
        Assert(
            evidence.Attempts == 12 && Math.Abs(evidence.IndependentAccuracy - (10d / 12d)) < 0.0001,
            "evidence totals are incorrect");
        Assert(evidence.IsQualifiedForRetention && !evidence.IsAchievement, "stage-five evidence did not qualify for retention or created mastery too early");

        evidence = evidence.StartRetention(lastAnswer);
        Assert(evidence.IsRetentionActive && evidence.RetentionStep == 0, "the sixth retention stage did not start");
        Assert(evidence.NextReviewAt == lastAnswer.AddDays(1) && !evidence.IsReady(lastAnswer), "retention did not begin with a one-day delayed review");

        var firstReview = evidence.NextReviewAt.GetValueOrDefault();
        evidence = evidence.RecordRetentionReview(LearningOutcome.IndependentCorrect, firstReview);
        Assert(evidence.RetentionStep == 1 && evidence.NextReviewAt == firstReview.AddDays(3), "the first retention confirmation did not schedule the three-day review");
        Assert(!evidence.IsAchievement, "one delayed success created mastery too early");

        var secondReview = evidence.NextReviewAt.GetValueOrDefault();
        evidence = evidence.RecordRetentionReview(LearningOutcome.IndependentCorrect, secondReview);
        Assert(evidence.RetentionStep == 2 && evidence.NextReviewAt == secondReview.AddDays(7), "the second retention confirmation did not schedule the seven-day review");

        var thirdReview = evidence.NextReviewAt.GetValueOrDefault();
        evidence = evidence.RecordRetentionReview(LearningOutcome.IndependentCorrect, thirdReview);
        Assert(evidence.RetentionStep == SkillEvidence.RequiredRetentionConfirmations, "three delayed successes did not complete retention");
        Assert(evidence.IsAchievement && evidence.IsReady(thirdReview), "completed retention did not create readiness and a sticky achievement");
        Assert(evidence.NextReviewAt == thirdReview.AddDays(21) && !evidence.IsReady(evidence.NextReviewAt.Value), "the continuing 21-day review did not make current readiness expire");
        Assert(evidence.IsAchievement, "an overdue review erased the historical achievement");

        var maintenanceReview = evidence.NextReviewAt.GetValueOrDefault();
        var failedReview = evidence.RecordRetentionReview(LearningOutcome.Incorrect, maintenanceReview);
        Assert(failedReview.IsAchievement && failedReview.RetentionStep == 0, "a failed review should reset current retention without erasing the achievement");
        Assert(failedReview.NextReviewAt == maintenanceReview.AddDays(1), "a failed review did not schedule a one-day reconfirmation");
        Assert(!failedReview.IsReady(maintenanceReview), "failed retention remained currently ready");
        for (var confirmation = 0; confirmation < SkillEvidence.RequiredRetentionConfirmations; confirmation++)
        {
            var dueAt = failedReview.NextReviewAt.GetValueOrDefault();
            failedReview = failedReview.RecordRetentionReview(LearningOutcome.IndependentCorrect, dueAt);
        }
        Assert(failedReview.IsAchievement && failedReview.IsReady(failedReview.LastAttemptAt.GetValueOrDefault()), "three delayed reconfirmations did not restore current readiness");

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
        Assert(html.Contains("p.learningSchema=5", StringComparison.Ordinal) && html.Contains("unitStats", StringComparison.Ordinal) && html.Contains("stageAttempts", StringComparison.Ordinal), "unit-based schema-v5 migration is missing");
        Assert(html.Contains("if(wasV5)", StringComparison.Ordinal) && html.Contains("legacyTopicStats", StringComparison.Ordinal), "schema-v5 migration is not idempotent or does not retain legacy evidence");
        Assert(html.Contains("curriculumLaneIds()", StringComparison.Ordinal) && !html.Contains("raw=g===1?g1", StringComparison.Ordinal), "school grade still caps curriculum lanes");
        Assert(html.Contains("topicLearningStage(p,k){return this.topicStat(p,k).retentionStartedAt?6", StringComparison.Ordinal), "the sixth retention stage is missing");
        Assert(html.Contains("retentionStep", StringComparison.Ordinal) && html.Contains("retentionStartedAt", StringComparison.Ordinal), "retention evidence is not persisted");
        Assert(html.Contains("retentionReview=!!s.retentionStartedAt&&wasDue&&q.sessionRole==='review'&&difficulty===5", StringComparison.Ordinal), "only due difficulty-five review questions may confirm retention");
        Assert(html.Contains("s.retentionStep=Math.min(3,s.retentionStep+1)", StringComparison.Ordinal), "three delayed retention confirmations are not required");
        Assert(html.Contains("else{s.retentionStep=0;s.reviewStep=0;s.nextReviewAt=now+intervals[0];}", StringComparison.Ordinal), "failed retention does not restart after one day");
        Assert(html.Contains("masteredAt", StringComparison.Ordinal) && html.Contains("topicReady", StringComparison.Ordinal), "achievement and readiness are not separate");
        Assert(html.Contains("outcome==='revealed'", StringComparison.Ordinal), "revealed answers have no distinct evidence path");
        Assert(html.Contains("intervals=[86400000,259200000,604800000,1814400000]", StringComparison.Ordinal), "spaced-review intervals are missing");
        Assert(html.Contains("for(let round=0;round<3;round++)", StringComparison.Ordinal), "calibration does not repeat core skills three times");
        Assert(html.Contains("score=(!r||!r.attempts)?0.05", StringComparison.Ordinal), "untested skills are not initialized conservatively");
        Assert(html.Contains("q.sessionRole=role", StringComparison.Ordinal) && html.Contains("'review'", StringComparison.Ordinal) && html.Contains("'target'", StringComparison.Ordinal) && html.Contains("'mixed'", StringComparison.Ordinal) && html.Contains("'exit'", StringComparison.Ordinal), "session roles are incomplete");
        Assert(
            html.Contains("rolePlan=[]", StringComparison.Ordinal) &&
            html.Contains("session.questions.push(this.generateSessionQuestion(p,session,rolePlan[0]))", StringComparison.Ordinal) &&
            html.Contains("this.generateSessionQuestion(p,s,s.rolePlan[nextIndex])", StringComparison.Ordinal),
            "learning questions are not generated lazily from the latest evidence");
        Assert(
            html.Contains("questionIdentity(q)", StringComparison.Ordinal) &&
            html.Contains("for(let attempt=0;attempt<24;attempt++)", StringComparison.Ordinal) &&
            html.Contains("questionCounts[key]", StringComparison.Ordinal) &&
            html.Contains("if(!best)throw new Error('Unable to generate a learning question')", StringComparison.Ordinal),
            "session question deduplication is missing a bounded terminal fallback");
        Assert(
            html.Contains("support=s.supportTopics[topic]?1:0", StringComparison.Ordinal) &&
            html.Contains("delete sess.supportTopics[id]", StringComparison.Ordinal) &&
            html.Contains("else sess.supportTopics[id]=true", StringComparison.Ordinal),
            "assisted or incorrect outcomes do not adapt the next question difficulty");
        Assert(
            html.Contains("q.difficulty=stage;q.grade=unit.grade;q.unitGrade=unit.grade;q.unitId=unit.id", StringComparison.Ordinal),
            "generated questions do not retain their actual difficulty and grade snapshots");
        Assert(
            html.Contains("data-question-grade=\"{{ questionGradeLabel }}\"", StringComparison.Ordinal) &&
            html.Contains("単元：{{ questionGradeLabel }}", StringComparison.Ordinal) &&
            html.Contains("this.schoolGradeName(Number(q.grade)||this.effectiveGrade(p))+'相当'", StringComparison.Ordinal) &&
            html.Contains("カテゴリ：{{ questionCategoryLabel }}", StringComparison.Ordinal) &&
            html.Contains("難易度：{{ questionDifficultyLabel }}", StringComparison.Ordinal),
            "quiz questions do not display grade, category, and difficulty metadata");
        Assert(
            html.Contains("data-calibration-grade=\"{{ calibGradeLabel }}\"", StringComparison.Ordinal) &&
            html.Contains("単元：{{ calibGradeLabel }}", StringComparison.Ordinal) &&
            html.Contains("カテゴリ：{{ calibTopicLabel }}", StringComparison.Ordinal) &&
            html.Contains("難易度：{{ calibDifficultyLabel }}", StringComparison.Ordinal),
            "calibration questions do not display grade, category, and difficulty metadata");
        Assert(
            html.Contains("登録学年：{{ profileGrade }}", StringComparison.Ordinal) &&
            html.Contains("学習中：{{ currentLearningGrade }}", StringComparison.Ordinal),
            "the parent report does not separate registered and current unit grades");
        Assert(
            html.Contains("parentSchoolGradeText:parentSchoolGradeText", StringComparison.Ordinal) &&
            html.Contains("{{ parentSchoolGradeText }}", StringComparison.Ordinal) &&
            !html.Contains("小学{{ parentSchoolGrade", StringComparison.Ordinal),
            "the protected parent UI does not render the normalized elementary/middle-school grade label exactly once");
        Assert(
            html.Contains("refreshSessionTarget(p,s)", StringComparison.Ordinal) &&
            html.Contains("current&&!this.topicComplete(p,current)", StringComparison.Ordinal) &&
            html.Contains("s.activeTargetTopic=next||current", StringComparison.Ordinal),
            "a mastered target topic does not advance during the active session");
        Assert(html.Contains("nextCurriculumTopic(p)", StringComparison.Ordinal) && html.Contains("frontierTopics(p)", StringComparison.Ordinal), "curriculum frontier selection is missing");
        Assert(html.Contains("prefer=!!(cfg&&cfg.preferSchoolGrade)", StringComparison.Ordinal), "the school-grade preference is not read by curriculum progression");
        Assert(html.Contains(".grade>=minimumGrade", StringComparison.Ordinal), "the school-grade preference does not select units at or above the registered grade");
        Assert(
            html.Contains("unitIdsByGrade=new Map()", StringComparison.Ordinal) &&
            html.Contains("activeCurriculumGrade(p)", StringComparison.Ordinal) &&
            html.Contains("unlockedGradeTopics(p)", StringComparison.Ordinal) &&
            html.Contains("gradeTopics(p){return this.unlockedGradeTopics(p);}", StringComparison.Ordinal),
            "curriculum progression is not gated by the earliest incomplete enabled grade");
        Assert(
            html.Contains("const unlocked=new Set(this.unlockedGradeTopics(p))", StringComparison.Ordinal) &&
            html.Contains("item=>this.curriculumUnit(item).grade===activeGrade", StringComparison.Ordinal),
            "subject frontiers or progress can expose units beyond the active grade");
        Assert(html.Contains("登録学年の単元を優先", StringComparison.Ordinal), "the protected parent UI does not expose the school-grade preference");
        Assert(html.Contains("preferSchoolGradeLabel", StringComparison.Ordinal), "the protected parent UI does not expose the school-grade preference state");
        Assert(html.Contains("prerequisites", StringComparison.Ordinal) && html.Contains("directPrerequisites(p,k)", StringComparison.Ordinal) && html.Contains("(unit.prerequisites||[])", StringComparison.Ordinal), "the curriculum prerequisite graph is missing from generated markup");
        Assert(html.Contains("s.attempts>0&&(Number(s.confidence)<.5||this.topicDue(p,k))", StringComparison.Ordinal), "unattempted upper topics are incorrectly treated as remediation triggers");
        Assert(html.Contains("visiting.has(topic)", StringComparison.Ordinal) && html.Contains("emitted.has(topic)", StringComparison.Ordinal) && html.Contains("filter(req=>!this.topicReady(p,req))", StringComparison.Ordinal), "prerequisite remediation is not cycle-safe, deduplicated, and readiness-aware");
        Assert(html.Contains("for(const id of base)for(const req of this.remediationTopics(p,id))", StringComparison.Ordinal) && html.Contains("return remedial.length?remedial:(out.length?out:allowed)", StringComparison.Ordinal), "allowed topics or curriculum frontier do not prioritize remediation");
        Assert(html.Contains("candidates=remedial.length?remedial:[id]", StringComparison.Ordinal) && html.Contains("if(!ids.length)throw new Error('No enabled curriculum units')", StringComparison.Ordinal), "weighted selection does not fall back explicitly after prerequisite remediation");
        Assert(html.Contains("configured[unit.topicId]!==false", StringComparison.Ordinal), "new curriculum topics are disabled for migrated settings");
        Assert(
            html.Contains("reviewCount=due.length?Math.min(due.length", StringComparison.Ordinal) &&
            html.Contains("const due=s.reviewTopics.filter(k=>this.topicDue(p,k))", StringComparison.Ordinal) &&
            html.Contains("s.reviewTopics.splice(index,1)", StringComparison.Ordinal),
            "fresh sessions can start with unscheduled random review questions");
        Assert(
            html.Contains("practice=allowed.filter(k=>!this.topicComplete(p,k)||this.topicDue(p,k))", StringComparison.Ordinal),
            "retention topics can reappear as ordinary mixed practice before review is due");
        Assert(
            html.Contains("targetTotal=Math.max(4,Math.floor(n*.25))", StringComparison.Ordinal) &&
            html.Contains("mixedCount=n-reviewCount-targetCount-1", StringComparison.Ordinal),
            "adaptive sessions do not reserve diverse mixed work while keeping target evidence");
        Assert(!html.Contains("for(let i=0;i<n;i++)qs.push", StringComparison.Ordinal), "all session questions are still generated before the first answer");
        // Clearing still needs target-skill evidence, but that second goal has to be visible on the
        // mission and retry screens, and a session that only ever fails it must not trap the child.
        Assert(
            html.Contains("const pass=this.sessionPassOutcome(this.curP(),s).pass;", StringComparison.Ordinal) &&
            html.Contains("targetPass=this.targetGoalMet(s)", StringComparison.Ordinal),
            "session completion ignores target-skill evidence");
        Assert(
            html.Contains("grace=globalPass&&!targetPass&&blocked+1>=this.passGraceLimit()", StringComparison.Ordinal) &&
            html.Contains("passGraceLimit(){return 2;}", StringComparison.Ordinal),
            "a session blocked only by the target goal can never be cleared without a parent PIN");
        Assert(
            html.Contains("{{ missionTargetText }}", StringComparison.Ordinal) &&
            html.Contains("{{ retryGoalText }}", StringComparison.Ordinal) &&
            html.Contains("{{ retryAdvice }}", StringComparison.Ordinal) &&
            !html.Contains("ごうかくまで あと <b>{{ retryRemaining }}", StringComparison.Ordinal),
            "the retry screen still shows the score gap only, hiding the real unmet condition");
        Assert(html.Contains("const gradeOpts=[1,2,3,4,5,6,7,8,9].map", StringComparison.Ordinal), "UI does not expose all supported school grades");
        Assert(!html.Contains("if(done('add'))staged.push", StringComparison.Ordinal), "cross-subject prerequisite chain remains");
        Assert(html.Contains("1000万を 10こ", StringComparison.Ordinal) && html.Contains("const scale=(g>=3&&stage>=4)?5", StringComparison.Ordinal), "key grade 3 number/chart content is missing");
        Assert(html.Contains("pickWeekday(stage)", StringComparison.Ordinal) && html.Contains("subtype:'weekday'", StringComparison.Ordinal) && html.Contains("月曜日", StringComparison.Ordinal) && html.Contains("日曜日", StringComparison.Ordinal), "weekday names, order, or calendar offsets are missing");
        Assert(html.Contains("subtype:'number-sequence'", StringComparison.Ordinal) && html.Contains("subtype:'number-compose'", StringComparison.Ordinal), "number sequence or composition variants are missing");
        Assert(html.Contains("subtype:'missing-add'", StringComparison.Ordinal) && html.Contains("subtype:'missing-sub'", StringComparison.Ordinal), "missing-number addition or subtraction variants are missing");
        Assert(html.Contains("const a=this.rand(12,89),b=this.rand(11,39)", StringComparison.Ordinal) && html.Contains("const a=this.rand(1234,7899)", StringComparison.Ordinal), "advanced grade 3 written arithmetic is missing");
        Assert(html.Contains("pickMoney(p)", StringComparison.Ordinal) && html.Contains("pickGroups(p)", StringComparison.Ordinal), "grade 1 money or equal-group foundations are missing");
        Assert(html.Contains("subtype:'counter'", StringComparison.Ordinal) && html.Contains("subtype:'greeting'", StringComparison.Ordinal) && html.Contains("subtype:'feeling-reason'", StringComparison.Ordinal), "counter, greeting, or feeling-reason language variants are missing");
        const string grade1Kanji = "一右雨円王音下火花貝学気九休玉金空月犬見五口校左三山子四糸字耳七車手十出女小上森人水正生青夕石赤千川先早草足村大男竹中虫町天田土二日入年白八百文木本名目立力林六";
        const string grade2Kanji = "引羽雲園遠何科夏家歌画回会海絵外角楽活間丸岩顔汽記帰弓牛魚京強教近兄形計元言原戸古午後語工公広交光考行高黄合谷国黒今才細作算止市矢姉思紙寺自時室社弱首秋週春書少場色食心新親図数西声星晴切雪船線前組走多太体台地池知茶昼長鳥朝直通弟店点電刀冬当東答頭同道読内南肉馬売買麦半番父風分聞米歩母方北毎妹万明鳴毛門夜野友用曜来里理話";
        const string grade3Kanji = "悪安暗医委意育員院飲運泳駅央横屋温化荷界階寒感漢館岸起期客究急級宮球去橋業曲局銀区苦具君係軽血決研県庫湖向幸港号根祭皿仕死使始指歯詩次事持式実写者主守取酒受州拾終習集住重宿所暑助昭消商章勝乗植申身神真深進世整昔全相送想息速族他打対待代第題炭短談着注柱丁帳調追定庭笛鉄転都度投豆島湯登等動童農波配倍箱畑発反坂板皮悲美鼻筆氷表秒病品負部服福物平返勉放味命面問役薬由油有遊予羊洋葉陽様落流旅両緑礼列練路和開";
        Assert(grade1Kanji.Length == 80 && grade1Kanji.Distinct().Count() == 80, "grade 1 canonical kanji allocation is not exactly 80 unique characters");
        Assert(grade2Kanji.Length == 160 && grade2Kanji.Distinct().Count() == 160, "grade 2 canonical kanji allocation is not exactly 160 unique characters");
        Assert(grade3Kanji.Length == 200 && grade3Kanji.Distinct().Count() == 200, "grade 3 canonical kanji allocation is not exactly 200 unique characters");
        Assert((grade1Kanji + grade2Kanji + grade3Kanji).Distinct().Count() == 440, "kanji allocations overlap across grades");
        Assert(html.Contains("const kanjiGrade1='" + grade1Kanji + "';", StringComparison.Ordinal), "grade 1 canonical kanji allocation differs from the official set");
        Assert(html.Contains("const kanjiGrade2='" + grade2Kanji + "';", StringComparison.Ordinal), "grade 2 canonical kanji allocation differs from the official set");
        Assert(html.Contains("const kanjiGrade3='" + grade3Kanji + "';", StringComparison.Ordinal), "grade 3 canonical kanji allocation differs from the official set");
        Assert(html.Contains("return kanji.map((k,index)=>{const r=readings[index],tail=okuri.get(k)||''", StringComparison.Ordinal), "canonical kanji are not converted into independently selectable question targets");
        Assert(html.Contains("word:k+tail", StringComparison.Ordinal), "kanji targets are not written with their okurigana");
        Assert(html.Contains("const L=this.kanjiCurriculumEntries().concat([", StringComparison.Ordinal), "canonical kanji are not included in the Japanese question pool");
        Assert(html.Split("{g:1,t:'", StringSplitOptions.None).Length - 1 + html.Split("{g:2,t:'", StringSplitOptions.None).Length - 1 + html.Split("{g:3,t:'", StringSplitOptions.None).Length - 1 >= 32, "reading passages were not doubled");
        Assert(html.Contains("pickOrder(p)", StringComparison.Ordinal) && html.Contains("（ ）の なかを さきに", StringComparison.Ordinal), "parentheses or inequalities are missing");
        Assert(
            html.Contains("pickKeyboard(p)", StringComparison.Ordinal) &&
            html.Contains("mode:'type'", StringComparison.Ordinal) &&
            html.Contains("keyboard:{label:'キーボード'", StringComparison.Ordinal),
            "the physical keyboard topic or typing question mode is missing");
        Assert(
            html.Contains("this._typeKeyHandler", StringComparison.Ordinal) &&
            html.Contains("e.repeat", StringComparison.Ordinal) &&
            html.Contains("e.isComposing", StringComparison.Ordinal) &&
            html.Contains("e.key==='Process'", StringComparison.Ordinal),
            "typing input does not guard repeat or IME key events");
        Assert(
            html.Contains("fingerColors={little:{background:'#36c8ae'", StringComparison.Ordinal) &&
            html.Contains("fingerFor=key=>'qazp'.includes(key)?'little'", StringComparison.Ordinal) &&
            html.Contains("'wsxol'.includes(key)?'ring'", StringComparison.Ordinal) &&
            html.Contains("'edcik'.includes(key)?'middle':'index'", StringComparison.Ordinal),
            "the typing guide does not map QWERTY keys to the physical keyboard finger colors");
        Assert(
            html.Contains("outline:4px solid #fff", StringComparison.Ordinal) &&
            html.Contains("transform:translateY(-4px)", StringComparison.Ordinal) &&
            html.Contains("role=\"img\" aria-label=", StringComparison.Ordinal),
            "the next typing key lacks a non-color cue or an accessible board description");
        Assert(
            html.Contains("['neko','ねこ']", StringComparison.Ordinal) &&
            html.Contains("['tokei','とけい']", StringComparison.Ordinal),
            "representative four- and five-letter romaji words are missing");
        Assert(html.Contains("isTape:true", StringComparison.Ordinal) && html.Contains("isTable:true", StringComparison.Ordinal), "tape-diagram or table questions are missing");
        // Both division meanings are still taught, but in the words a third grader uses:
        // 等分除 and 包含除 are teacher-facing jargon and were removed from the questions.
        Assert(
            html.Contains("pickDiv(p)", StringComparison.Ordinal) &&
            html.Contains("1人ぶんの こすう", StringComparison.Ordinal) &&
            html.Contains("わけられる 人数", StringComparison.Ordinal),
            "division concepts are incomplete");
        Assert(html.Contains("difficulty:5", StringComparison.Ordinal) && html.Contains("コンパス", StringComparison.Ordinal), "staged grade 3 written arithmetic or circle work is missing");
        Assert(html.Contains("q.isMoney", StringComparison.Ordinal) && html.Contains("q.isGroups", StringComparison.Ordinal) && html.Contains("q.isTape", StringComparison.Ordinal), "new visual scaffolding is missing");
        Assert(html.Contains("requestLearningReset('history')", StringComparison.Ordinal) && html.Contains("this.state.resetPin!==this.parentPin()", StringComparison.Ordinal), "learning reset bypasses PIN confirmation");
        Assert(html.Contains("stars:mode==='full'?0:(Number(current.stars)||0)", StringComparison.Ordinal) && html.Contains("xp:mode==='full'?0:(Number(current.xp)||0)", StringComparison.Ordinal), "history-only and full reset reward semantics are not separated");
        Assert(html.Contains("level:1,stageAttempts:0,stageIndependent:0", StringComparison.Ordinal) && html.Contains("cleared:{}", StringComparison.Ordinal), "learning reset does not clear all progress evidence");
        Assert(html.Contains("const reset={...current", StringComparison.Ordinal) && html.Contains("progressResetAt:Date.now()", StringComparison.Ordinal), "learning reset does not preserve profile identity and grade");
        Assert(html.Contains("localStorage.setItem('kt_profiles_v1',persisted)", StringComparison.Ordinal) && !html.Contains("localStorage.clear()", StringComparison.Ordinal), "learning reset is not scoped to profile progress");
        Assert(html.Contains("aria-modal=", StringComparison.Ordinal) && html.Contains("cancelLearningReset", StringComparison.Ordinal), "learning reset confirmation is inaccessible or cannot be cancelled");
        Assert(html.Contains("kt_session_checkpoint_v1", StringComparison.Ordinal) && html.Contains("restoreLearningCheckpoint()", StringComparison.Ordinal), "in-progress learning cannot be resumed");
        Assert(
            html.Contains("unlockedIds.has(q.unitId)", StringComparison.Ordinal) &&
            html.Contains("grade>=minimumGrade&&grade<=activeGrade", StringComparison.Ordinal),
            "saved learning sessions can bypass the active-grade gate");
        Assert(html.Contains("n===1?0.5:(n===2?0.25:0)", StringComparison.Ordinal) && html.Contains("if(miss>=3)", StringComparison.Ordinal), "three-attempt fractional scoring is missing");
        var trainingFormSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "KidsTraining.App",
            "Presentation",
            "WinForms",
            "TrainingForm.cs"));
        Assert(
            html.Contains("resetToBeginner=!hasMeaningfulProgress(profile)&&!profile.progressResetAt", StringComparison.Ordinal),
            "a reset profile loses its selected grade on the next launch");
        Assert(
            html.Contains("profiles=this.migrateProfiles([normalizeProfile(savedProfile)])", StringComparison.Ordinal) &&
            html.Contains("topics:{...def.topics,...storedTopics}", StringComparison.Ordinal),
            "the runtime migration does not retain one profile or merge newly introduced topics");
        Assert(
            html.Contains("numberOrDefault(host.questionCount", StringComparison.Ordinal) &&
            html.Contains("numberOrDefault(host.passLine", StringComparison.Ordinal) &&
            html.Contains("host.preferSchoolGrade", StringComparison.Ordinal) &&
            html.Contains("localStorage.setItem('kt_parent_pin_v1',parentPin)", StringComparison.Ordinal),
            "host-owned parent settings are not authoritative during runtime migration");

        var bootstrapStart = trainingFormSource.IndexOf(
            "private string BuildProfileStorageScript()",
            StringComparison.Ordinal);
        Assert(bootstrapStart >= 0, "the host bootstrap source is missing");
        var bootstrapSource = trainingFormSource[bootstrapStart..];
        Assert(
            bootstrapSource.Contains("window.__kidsTrainingHost = {", StringComparison.Ordinal) &&
            bootstrapSource.Contains("profileName: __PROFILE_NAME__", StringComparison.Ordinal) &&
            bootstrapSource.Contains("parentPin: __PARENT_PIN__", StringComparison.Ordinal) &&
            bootstrapSource.Contains("questionCount: __QUESTION_COUNT__", StringComparison.Ordinal) &&
            bootstrapSource.Contains("passLine: __PASS_LINE__", StringComparison.Ordinal) &&
            bootstrapSource.Contains("preferSchoolGrade: __PREFER_SCHOOL_GRADE__", StringComparison.Ordinal) &&
            !bootstrapSource.Contains("localStorage.", StringComparison.Ordinal),
            "TrainingForm injects storage migration logic instead of only the host contract");
        Assert(
            trainingFormSource.Contains("SetLearningSessionSettingsAsync", StringComparison.Ordinal) &&
            trainingFormSource.Contains("SetParentPasswordAsync", StringComparison.Ordinal),
            "WebView setting synchronization does not expose an observable completion result");
        var pagePreparationStart = trainingFormSource.IndexOf(
            "Task.Run(PrepareLearningPage)",
            StringComparison.Ordinal);
        var webViewEnvironmentStart = trainingFormSource.IndexOf(
            "CoreWebView2Environment.CreateAsync",
            StringComparison.Ordinal);
        var pagePreparationJoin = trainingFormSource.IndexOf(
            "preparationTask.WaitAsync(cancellationToken)",
            StringComparison.Ordinal);
        Assert(
            pagePreparationStart >= 0 &&
            pagePreparationStart < webViewEnvironmentStart &&
            webViewEnvironmentStart < pagePreparationJoin &&
            trainingFormSource.Contains(
                "private LearningPagePreparationResult PrepareLearningPage()",
                StringComparison.Ordinal),
            "learning-page preparation is not safely overlapped with the independent WebView2 startup span");
        Assert(
            trainingFormSource.Contains("SetVirtualHostNameToFolderMapping", StringComparison.Ordinal) &&
            trainingFormSource.Contains("https://{LearningVirtualHostName}", StringComparison.Ordinal) &&
            !trainingFormSource.Contains("webView.CoreWebView2.Navigate(new Uri", StringComparison.Ordinal),
            "learning content is not hosted in a constrained local virtual host");
        Assert(
            trainingFormSource.Contains("ReadLegacyLearningStorageAsync", StringComparison.Ordinal) &&
            trainingFormSource.Contains("if (localStorage.getItem(key) === null", StringComparison.Ordinal),
            "the secure-origin transition can discard or overwrite legacy learning progress");
        Assert(
            !trainingFormSource.Contains("CoreWebView2PermissionKind.Camera", StringComparison.Ordinal) &&
            !trainingFormSource.Contains("PermissionRequested", StringComparison.Ordinal) &&
            !html.Contains("getUserMedia(", StringComparison.Ordinal) &&
            !html.Contains("FaceDetector", StringComparison.Ordinal) &&
            !html.Contains("attentionEnabled", StringComparison.Ordinal) &&
            !html.Contains("kt-attention-video", StringComparison.Ordinal),
            "camera-based attention monitoring or its permission/UI hooks remain");
        Assert(
            !trainingFormSource.Contains("CompletionBridgeScript", StringComparison.Ordinal) &&
            !trainingFormSource.Contains("document.body.innerText", StringComparison.Ordinal),
            "TrainingForm still derives completion from visible page text");
        Assert(html.Contains("補助活動：音声を聞き、声に出して", StringComparison.Ordinal) && html.Contains("ノートに漢字を書いて", StringComparison.Ordinal), "supplementary output practice is not identified");
        Assert(html.Contains("aria-label=\"答えと説明を見る\"", StringComparison.Ordinal) && html.Contains("outcome==='revealed'", StringComparison.Ordinal), "revealed-answer control is inaccessible or unscored");
        Assert(html.Contains("role=\"button\" tabindex=\"0\"", StringComparison.Ordinal) && html.Contains("document.addEventListener('keydown'", StringComparison.Ordinal) && html.Contains("aria-live=\"polite\"", StringComparison.Ordinal), "keyboard or live-region accessibility is missing");

        var requiredExpressions = new[]
        {
            "3 + 7",
            "7 + 3",
            "10 + 2",
            "10 + 5",
            "9 - 3 - 2",
            "16 - 6 + 7"
        };
        foreach (var expression in requiredExpressions)
        {
            Assert(html.Contains($"prompt:'{expression}'", StringComparison.Ordinal), $"required arithmetic question is missing: {expression}");
        }
        foreach (var expression in new[] { "58,29", "68,22", "35,25", "19,43" })
        {
            Assert(html.Contains($"makeHissan({expression})", StringComparison.Ordinal), $"required two-digit written-arithmetic question is missing: {expression}");
        }
        Assert(
            html.Contains("q.topic='add';q.difficulty=5", StringComparison.Ordinal) &&
            html.Contains("q.topic='sub';q.difficulty=5", StringComparison.Ordinal),
            "two-digit addition or subtraction is not routed through written-arithmetic steps");

        Assert(
            html.Contains("prompt:'大人が 7人、子供が 9人 います。あわせて 何人 いますか？'", StringComparison.Ordinal) &&
            html.Contains("prompt:'切手が 6枚、封筒が 15枚 あります。どちらが 何枚 おおいですか？'", StringComparison.Ordinal) &&
            html.Contains("answer:'封筒が 9枚 おおい'", StringComparison.Ordinal),
            "requested people or stationery story question is missing");
        Assert(
            html.Contains("isPlainEq=modeNumeric&&q.topic!=='story';", StringComparison.Ordinal) &&
            html.Contains("""<sc-if value="{{ isPlainEq }}" hint-placeholder-val="{{ true }}"> = ?</sc-if>""", StringComparison.Ordinal),
            "numeric story questions do not conditionally suppress the equation suffix");
        Assert(
            html.Contains("it.t+'\\nもんだい： '+it.q", StringComparison.Ordinal) &&
            !html.Contains("it.t+'　◆　'+it.q", StringComparison.Ordinal),
            "reading-comprehension text and its explicit question label are not separated");
        Assert(
            html.Contains("React.createElement('ruby'", StringComparison.Ordinal) &&
            html.Contains("React.createElement('rt'", StringComparison.Ordinal) &&
            html.Contains("React.createElement('wbr'", StringComparison.Ordinal),
            "question furigana does not render semantic ruby markup");
        Assert(
            html.Contains("kokuPre:this.withFurigana(kokuPre), kokuWord:kokuWord, kokuPost:this.withFurigana(kokuPost)", StringComparison.Ordinal) &&
            html.Contains("calibKokuPre:this.withFurigana(calibKokuPre), calibKokuWord:calibKokuWord, calibKokuPost:this.withFurigana(calibKokuPost)", StringComparison.Ordinal),
            "kanji-reading targets expose their answer through furigana");
        Assert(
            html.Contains("(q.topic==='kokugo'&&q.subtype==='kanji-choice')?c:this.withFurigana(c)", StringComparison.Ordinal) &&
            html.Contains("(cq.topic==='kokugo'&&cq.subtype==='kanji-choice')?c:this.withFurigana(c)", StringComparison.Ordinal),
            "kanji-selection choices expose their readings through furigana");
        Assert(html.Contains("interrogative=before.endsWith('なん')", StringComparison.Ordinal), "interrogative counter readings are missing");
        Assert(
            html.Contains("['外国語','がいこくご'],['外国','がいこく'],['学級','がっきゅう'],['課題','かだい'],['必要','ひつよう']", StringComparison.Ordinal) &&
            html.Contains("['共通','きょうつう'],['目標','もくひょう'],['判断','はんだん'],['基準','きじゅん'],['共有','きょうゆう'],['一人','ひとり'],['担当','たんとう']", StringComparison.Ordinal) &&
            html.Contains("surface==='残'&&after.startsWith('さ')", StringComparison.Ordinal),
            "common compound furigana can fall back to incorrect single-character readings");
        Assert(
            html.Contains("""<html lang="ja"><head>""", StringComparison.Ordinal) &&
            !html.Contains("<html><head>", StringComparison.Ordinal),
            "assembled learning HTML does not declare Japanese");
        Assert(
            html.Contains("""<style id="kt-layout-typography">""", StringComparison.Ordinal) &&
            html.Contains("""class="kt-question-prompt""", StringComparison.Ordinal) &&
            html.Contains("""class="kt-choice-grid""", StringComparison.Ordinal) &&
            html.Contains("""class="kt-feedback-answer""", StringComparison.Ordinal) &&
            html.Contains("""class="kt-feedback-screen""", StringComparison.Ordinal) &&
            html.Contains("""class="kt-feedback-hero kt-feedback-hero-correct""", StringComparison.Ordinal) &&
            html.Contains("""class="kt-feedback-explanation""", StringComparison.Ordinal) &&
            html.Contains("""class="kt-feedback-xp""", StringComparison.Ordinal) &&
            html.Contains("""class="kt-feedback-next""", StringComparison.Ordinal) &&
            html.Contains("min-height: 100dvh !important", StringComparison.Ordinal) &&
            html.Contains("justify-content: safe center !important", StringComparison.Ordinal) &&
            !html.Contains("justify-content: flex-start !important", StringComparison.Ordinal) &&
            html.Contains("@media (max-height: 900px)", StringComparison.Ordinal) &&
            html.Contains("@media (max-height: 760px)", StringComparison.Ordinal) &&
            html.Contains("@media (max-width: 720px)", StringComparison.Ordinal) &&
            html.Contains("button:focus-visible", StringComparison.Ordinal) &&
            html.Contains("select:focus-visible", StringComparison.Ordinal) &&
            html.Contains("@media (prefers-reduced-motion: reduce)", StringComparison.Ordinal),
            "responsive typography, focus, or reduced-motion markup is missing");
        Assert(
            html.Contains("grid-template-columns: repeat(2, minmax(0, 1fr)) !important", StringComparison.Ordinal) &&
            html.Contains("max-width: min(880px, 92vw) !important", StringComparison.Ordinal) &&
            html.Contains(".kt-choice-button", StringComparison.Ordinal) &&
            html.Contains("font-size: clamp(21px, 1.8vw, 26px) !important", StringComparison.Ordinal) &&
            html.Contains("overflow-wrap: anywhere", StringComparison.Ordinal) &&
            html.Contains("word-break: break-all", StringComparison.Ordinal) &&
            html.Contains("line-break: anywhere", StringComparison.Ordinal) &&
            html.Contains("text-wrap: balance", StringComparison.Ordinal) &&
            html.Contains("overflow: hidden", StringComparison.Ordinal),
            "long choice text can overflow or widen the choice grid");
        Assert(
            html.Contains("[data-screen-label]", StringComparison.Ordinal) &&
            html.Contains("user-select: none", StringComparison.Ordinal) &&
            html.Contains("[contenteditable=\"true\"]", StringComparison.Ordinal) &&
            html.Contains("user-select: text", StringComparison.Ordinal),
            "learning-screen text selection is not disabled while editable controls remain selectable");

        // The runtime lowercases every HTML attribute, so viewBox / pathLength only survive
        // through the sc-camel- escape hatch. Losing it silently breaks scaling and the draw animation.
        Assert(
            html.Contains("""class="kt-feedback-mark kt-mark-hanamaru" sc-camel-view-box="0 0 120 120""", StringComparison.Ordinal) &&
            html.Contains("""class="kt-feedback-mark kt-mark-batsu" sc-camel-view-box="0 0 120 120""", StringComparison.Ordinal) &&
            html.Contains("""class="kt-mark-petals" sc-camel-path-length="100""", StringComparison.Ordinal) &&
            html.Contains("""class="kt-mark-swirl" sc-camel-path-length="100""", StringComparison.Ordinal) &&
            html.Contains("""class="kt-mark-stroke kt-mark-stroke-a" sc-camel-path-length="100""", StringComparison.Ordinal) &&
            html.Contains("""class="kt-mark-stroke kt-mark-stroke-b" sc-camel-path-length="100""", StringComparison.Ordinal) &&
            html.Contains("@keyframes kt-draw-mark", StringComparison.Ordinal),
            "feedback hanamaru/batsu SVG marks or their draw animation are missing");

        var markupRoot = Path.Combine(
            repositoryRoot,
            "src",
            "KidsTraining.App",
            "Application",
            "Learning",
            "Markup");
        var arithmeticSource = File.ReadAllText(Path.Combine(markupRoot, "ArithmeticQuestionPatch.cs"));
        var subGeneratorStart = arithmeticSource.IndexOf("genSub(p)", StringComparison.Ordinal);
        var hissanGeneratorStart = arithmeticSource.IndexOf("private static string BuildGenHissanScript()", StringComparison.Ordinal);
        Assert(subGeneratorStart > 0 && hissanGeneratorStart > subGeneratorStart, "arithmetic generator source boundaries are missing");
        var addGeneratorSource = arithmeticSource[..subGeneratorStart];
        var subGeneratorSource = arithmeticSource[subGeneratorStart..hissanGeneratorStart];
        var gradeOneBucketsStart = subGeneratorSource.IndexOf("const gradeOneBuckets=[", StringComparison.Ordinal);
        var upperGradeBucketsStart = subGeneratorSource.IndexOf("const upperGradeBuckets=[", StringComparison.Ordinal);
        Assert(
            gradeOneBucketsStart > 0 && upperGradeBucketsStart > gradeOneBucketsStart,
            "grade-specific subtraction source boundaries are missing");
        var gradeOneSubGeneratorSource = subGeneratorSource[gradeOneBucketsStart..upperGradeBucketsStart];
        Assert(
            gradeOneSubGeneratorSource.Contains("Math.random()<.08?zeroReview():basic()", StringComparison.Ordinal) &&
            gradeOneSubGeneratorSource.Contains("[threeTerm,missingBorrow,mixed]", StringComparison.Ordinal),
            "subtract-zero review is not rare and isolated from grade 1 difficulty 5");
        Assert(
            // x stops at 18 so that rand(x%10+1,9) always has a single-digit range: at
            // x=19 it collapses to rand(10,9) and hands the child a two-digit middle term.
            subGeneratorSource.Contains("const threeTerm=()=>{const x=this.rand(11,18),y=this.rand(x%10+1,9)", StringComparison.Ordinal) &&
            !gradeOneSubGeneratorSource.Contains("this.rand(15,30)", StringComparison.Ordinal) &&
            !gradeOneSubGeneratorSource.Contains("this.rand(30,99)", StringComparison.Ordinal) &&
            !gradeOneSubGeneratorSource.Contains("makeHissan", StringComparison.Ordinal),
            "grade 1 subtraction can exceed 20 or route to general two-digit written subtraction");
        Assert(
            !addGeneratorSource.Contains("16 - 6 + 7", StringComparison.Ordinal) &&
            subGeneratorSource.Contains("prompt:'16 - 6 + 7'", StringComparison.Ordinal) &&
            subGeneratorSource.Contains(",mixed]", StringComparison.Ordinal),
            "mixed subtraction/addition is not assigned to subtraction stage 5");

        var generatorFiles = new[]
        {
            "ArithmeticQuestionPatch.cs",
            "SupplementalMathQuestionPatch.cs",
            "ClockQuestionPatch.cs",
            "JapaneseQuestionPatch.cs",
            "KanjiCurriculumPatch.cs"
        };
        // Only the kanji a child can read on screen need furigana, so the commentary that
        // explains why a generator is written the way it is does not count.
        var generatorCharacters = CjkCharacters(string.Concat(
            generatorFiles.Select(file => WithoutCommentary(File.ReadAllText(Path.Combine(markupRoot, file))))));
        var furiganaSource = File.ReadAllText(Path.Combine(markupRoot, "QuestionFuriganaPatch.cs"));
        var curriculumSource = File.ReadAllText(Path.Combine(markupRoot, "KanjiCurriculumPatch.cs"));
        Assert(
            // The stem, not the whole reading: 見 is read み when 見る already shows る.
            furiganaSource.Contains("this.kanjiCurriculumEntries().map(entry=>[entry.k,entry.stem||entry.r])", StringComparison.Ordinal),
            "canonical kanji readings are not included in the furigana dictionary");
        var furiganaCharacters = CjkCharacters(furiganaSource)
            .Union(CjkCharacters(curriculumSource))
            .ToHashSet();
        var missingCharacters = generatorCharacters
            .Except(furiganaCharacters)
            .OrderBy(static character => character)
            .ToArray();
        Assert(
            missingCharacters.Length == 0,
            "question generator CJK characters missing from furigana source: " + new string(missingCharacters));

        static string WithoutCommentary(string source) =>
            string.Join(
                '\n',
                source
                    .Split('\n')
                    .Where(static line => !line.TrimStart().StartsWith("//", StringComparison.Ordinal)));

        static HashSet<char> CjkCharacters(string source) =>
            source
                .Where(static character =>
                    (character >= '\u4E00' && character <= '\u9FFF') || character == '々')
                .ToHashSet();
    }

    // Marker checks cannot tell whether a generated question teaches the truth, so the
    // question bank is audited by generating from it. The generators only exist as
    // JavaScript inside the produced page, so the audit runs them on Node rather than
    // re-implementing them here, where a copy would drift away from what ships.
    private static void TestGeneratedQuestionAudit(string repositoryRoot)
    {
        var auditScript = Path.Combine(
            repositoryRoot,
            "tests",
            "KidsTraining.ArchitectureTests",
            "GeneratedQuestionAudit.mjs");
        Assert(File.Exists(auditScript), $"the generated-question audit is missing: {auditScript}");

        var (template, appDefinition) = ReadLearningSource(repositoryRoot);
        var html = new LearningPageBuilder().Build(
            template,
            appDefinition,
            "Architecture Test",
            ParentPin.FromOrDefault("4456"));

        var auditedPage = Path.Combine(
            Path.GetTempPath(),
            "kids-training-generated-question-audit.html");
        File.WriteAllText(auditedPage, html, new UTF8Encoding(false));

        var (exitCode, output) = RunNodeAudit(auditScript, auditedPage);
        Assert(
            exitCode == 0,
            $"the generated question bank violates curriculum invariants (node exit {exitCode}):" +
            Environment.NewLine + output);
    }

    private static void TestPerformanceContracts(string repositoryRoot)
    {
        var (template, appDefinition) = ReadLearningSource(repositoryRoot);
        var html = new LearningPageBuilder().Build(
            template,
            appDefinition,
            "Performance Test",
            ParentPin.FromOrDefault("4456"));

        Assert(
            html.Contains("curriculumData(){if(this._curriculumData)return this._curriculumData", StringComparison.Ordinal) &&
            html.Contains("byId=new Map(),byTopic=new Map()", StringComparison.Ordinal) &&
            html.Contains("normalizedProfiles:new WeakSet()", StringComparison.Ordinal) &&
            html.Contains("if(data.normalizedProfiles.has(p))return p", StringComparison.Ordinal) &&
            !html.Contains("curriculumCatalog(){return [", StringComparison.Ordinal),
            "the curriculum hot path rebuilds or linearly searches the full catalog");
        Assert(
            html.Contains("progressionView(p)", StringComparison.Ordinal) &&
            html.Contains("const progression=this.progressionView(p)", StringComparison.Ordinal) &&
            html.Contains("progression.availableTopicIds.has(k)", StringComparison.Ordinal) &&
            html.Contains("progression.introducedTopicIds.has(k)", StringComparison.Ordinal),
            "derived progression data is recomputed inside the per-topic render loop");
        Assert(
            html.Contains("source=Object.create(base)", StringComparison.Ordinal) &&
            html.Contains("source._stageUnitStat=Object.freeze(", StringComparison.Ordinal) &&
            html.Contains("p&&p._stageUnitId===id&&p._stageUnitStat", StringComparison.Ordinal) &&
            html.Contains("seen=new Set()", StringComparison.Ordinal) &&
            html.Contains("let best=front[0],bestProgress=", StringComparison.Ordinal) &&
            !html.Contains("front.slice().sort(", StringComparison.Ordinal) &&
            !html.Contains("mastery:{...((p&&p.mastery)||{})}", StringComparison.Ordinal),
            "question generation still clones full progress maps or uses superlinear ordered-set work");
        Assert(
            html.Contains("const previous=this._progressionScope,scope={profile:p,values:new Map()}", StringComparison.Ordinal) &&
            html.Contains("if(deterministic)break", StringComparison.Ordinal) &&
            html.Contains("finally{this._progressionScope=previous;}", StringComparison.Ordinal),
            "question generation does not reuse one progression scope or bound deterministic duplicates");
        Assert(
            html.Contains("componentDidUpdate(prevProps,prevState)", StringComparison.Ordinal) &&
            html.Contains("this.scheduleProfilesSave(prevState)", StringComparison.Ordinal) &&
            html.Contains("this.scheduleLearningCheckpoint(prevState)", StringComparison.Ordinal) &&
            html.Contains("_profilesSaveTimer=setTimeout", StringComparison.Ordinal) &&
            html.Contains("_checkpointSaveTimer=setTimeout", StringComparison.Ordinal) &&
            html.Contains("if(!profilesSaved||!checkpointSaved)", StringComparison.Ordinal) &&
            !html.Contains("if(this.profilesChangedSince(prevState))this.saveProfiles()", StringComparison.Ordinal) &&
            !html.Contains("componentDidUpdate(){try{const s=JSON.stringify(this.state.profiles)", StringComparison.Ordinal),
            "every UI update still serializes profiles or writes a checkpoint synchronously");

        var runtimeSource = File.ReadAllText(
            Path.Combine(repositoryRoot, "kids-training", "scripts", "runtime.js"),
            Encoding.UTF8);
        var staticMode = template.IndexOf("<meta name=\"dc-runtime-mode\" content=\"static\">", StringComparison.Ordinal);
        var react = template.IndexOf("scripts/vendor/react-18.3.1.production.min.js", StringComparison.Ordinal);
        var reactDom = template.IndexOf("scripts/vendor/react-dom-18.3.1.production.min.js", StringComparison.Ordinal);
        var runtime = template.IndexOf("scripts/runtime.js", StringComparison.Ordinal);
        Assert(
            staticMode >= 0 && react >= 0 && react < reactDom && reactDom < runtime &&
            runtimeSource.Contains("content !== \"static\"", StringComparison.Ordinal) &&
            runtimeSource.Contains("fetch(location.href)", StringComparison.Ordinal),
            "the static runtime does not suppress its duplicate self-fetch or preload local React correctly");
        Assert(
            !template.Contains("https://", StringComparison.OrdinalIgnoreCase) &&
            !runtimeSource.Contains("unpkg.com", StringComparison.OrdinalIgnoreCase) &&
            !runtimeSource.Contains("fonts.googleapis.com", StringComparison.OrdinalIgnoreCase),
            "the learning startup path still depends on an external HTTP resource");
    }

    private static void TestAlgorithmPerformanceAudit(string repositoryRoot)
    {
        var auditScript = Path.Combine(
            repositoryRoot,
            "tests",
            "KidsTraining.ArchitectureTests",
            "AlgorithmPerformanceAudit.mjs");
        Assert(File.Exists(auditScript), $"the algorithm performance audit is missing: {auditScript}");

        var (template, appDefinition) = ReadLearningSource(repositoryRoot);
        var html = new LearningPageBuilder().Build(
            template,
            appDefinition,
            "Algorithm Performance Test",
            ParentPin.FromOrDefault("4456"));
        var auditedPage = Path.Combine(
            Path.GetTempPath(),
            "kids-training-algorithm-performance-audit.html");
        File.WriteAllText(auditedPage, html, new UTF8Encoding(false));

        var (exitCode, output) = RunNodeAudit(auditScript, auditedPage, "--verify");
        Assert(
            exitCode == 0,
            $"the generated runtime exceeds its bounded-work contracts (node exit {exitCode}):" +
            Environment.NewLine + output);
    }

    private static (int ExitCode, string Output) RunNodeAudit(
        string scriptPath,
        string pagePath,
        params string[] additionalArguments)
    {
        var startInfo = new ProcessStartInfo("node")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add(scriptPath);
        startInfo.ArgumentList.Add(pagePath);
        foreach (var argument in additionalArguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        Process? process;
        try
        {
            process = Process.Start(startInfo);
        }
        catch (Win32Exception exception)
        {
            throw new InvalidOperationException(
                "Node.js is required to audit the generated question bank because the " +
                "generators ship as JavaScript. Install Node and retry. " + exception.Message);
        }

        if (process is null)
        {
            throw new InvalidOperationException("the generated-question audit process did not start");
        }

        using (process)
        {
            var standardOutput = process.StandardOutput.ReadToEndAsync();
            var standardError = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(TimeSpan.FromMinutes(2)))
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit();
                throw new TimeoutException("the generated-question audit exceeded two minutes");
            }

            Task.WhenAll(standardOutput, standardError).GetAwaiter().GetResult();
            return (process.ExitCode, (standardOutput.Result + standardError.Result).Trim());
        }
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

    private static void TestDuplicateRequiredAnchor(string repositoryRoot)
    {
        var (template, appDefinition) = ReadLearningSource(repositoryRoot);
        const string anchor = "screen:'profile', profileIdx:0,";
        var brokenDefinition = appDefinition.Replace(
            anchor,
            anchor + anchor,
            StringComparison.Ordinal);
        ExpectInvalidOperation(
            () => new LearningPageBuilder().Build(
                template,
                brokenDefinition,
                "Test",
                ParentPin.Default),
            "must occur exactly once");
    }

    private static void TestPreparationTerminals()
    {
        var prepared = LearningPagePreparationResult.Prepared("runtime.html");
        var cached = LearningPagePreparationResult.PreparedFromCache("runtime.html");
        var failed = LearningPagePreparationResult.Failed("failure");
        Assert(prepared.IsSuccess && prepared.RuntimePagePath == "runtime.html" && prepared.ErrorMessage is null, "prepared state is incomplete");
        Assert(cached.IsSuccess && cached.Status == LearningPagePreparationStatus.PreparedFromCache, "cache-hit state is incomplete");
        Assert(!failed.IsSuccess && failed.RuntimePagePath is null && failed.ErrorMessage == "failure", "failed state is incomplete");
    }

    private static void TestLearningRuntimeCache(string repositoryRoot)
    {
        var temporaryRoot = Path.Combine(
            Path.GetTempPath(),
            "KidsTrainingArchitectureTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryRoot);
        try
        {
            var sourceRoot = Path.Combine(repositoryRoot, "kids-training");
            var templatePath = Path.Combine(temporaryRoot, "index.template.html");
            var definitionPath = Path.Combine(temporaryRoot, "learning-app.dc.html");
            var runtimePath = Path.Combine(temporaryRoot, "runtime.html");
            var manifestPath = Path.Combine(temporaryRoot, "runtime-cache.json");
            File.Copy(Path.Combine(sourceRoot, "index.template.html"), templatePath);
            File.Copy(Path.Combine(sourceRoot, "app", "learning-app.dc.html"), definitionPath);

            var pinStore = new InMemoryParentPinStore(ParentPin.FromOrDefault("4456"));
            var preparer = new FileLearningPagePreparer(
                new LearningPageBuilder(),
                new ParentPasswordService(pinStore),
                new FixedProfileNameProvider("Cache Test"),
                new LearningPagePreparationPaths(templatePath, definitionPath, runtimePath, manifestPath));

            var first = preparer.Prepare();
            var firstWrite = File.GetLastWriteTimeUtc(runtimePath);
            var second = preparer.Prepare();
            Assert(
                first.Status == LearningPagePreparationStatus.PreparedFresh &&
                second.Status == LearningPagePreparationStatus.PreparedFromCache &&
                File.GetLastWriteTimeUtc(runtimePath) == firstWrite,
                "an unchanged runtime was rebuilt or touched");

            File.AppendAllText(runtimePath, "<!--tampered-->", Encoding.UTF8);
            var repaired = preparer.Prepare();
            Assert(
                repaired.Status == LearningPagePreparationStatus.PreparedFresh &&
                !File.ReadAllText(runtimePath).Contains("<!--tampered-->", StringComparison.Ordinal),
                "a tampered runtime was accepted as a cache hit");

            pinStore.Write(ParentPin.FromOrDefault("7788"));
            var changedPin = preparer.Prepare();
            var manifest = File.ReadAllText(manifestPath, Encoding.UTF8);
            Assert(
                changedPin.Status == LearningPagePreparationStatus.PreparedFresh &&
                !manifest.Contains("4456", StringComparison.Ordinal) &&
                !manifest.Contains("7788", StringComparison.Ordinal),
                "a PIN change did not invalidate the runtime or a raw PIN leaked into the cache manifest");
        }
        finally
        {
            if (Directory.Exists(temporaryRoot))
            {
                Directory.Delete(temporaryRoot, true);
            }
        }
    }

    private static void TestLegacyStorageMigrationState()
    {
        var temporaryRoot = Path.Combine(
            Path.GetTempPath(),
            "KidsTrainingArchitectureTests",
            Guid.NewGuid().ToString("N"));
        var statePath = Path.Combine(temporaryRoot, "legacy-migration.json");
        Directory.CreateDirectory(temporaryRoot);
        try
        {
            var store = new JsonLegacyLearningStorageMigrationStateStore(statePath);
            var now = new DateTimeOffset(2026, 7, 26, 0, 0, 0, TimeSpan.Zero);
            var pending = store.Read();
            Assert(!pending.IsCompleted && pending.ShouldAttempt(now), "a new migration state did not start pending");

            Assert(store.TryMarkDeferred(now), "the first deferred migration state was not saved");
            var first = store.Read();
            Assert(
                first.FailedAttempts == 1 && first.RetryAfterUtc == now.AddHours(1) &&
                !first.ShouldAttempt(now) && first.ShouldAttempt(now.AddHours(1)),
                "the first failed migration did not apply a one-hour retry window");

            var secondAttempt = now.AddHours(1);
            Assert(store.TryMarkDeferred(secondAttempt), "the second deferred migration state was not saved");
            var second = store.Read();
            Assert(
                second.FailedAttempts == 2 && second.RetryAfterUtc == secondAttempt.AddHours(6),
                "the second failed migration did not apply a six-hour retry window");

            var thirdAttempt = secondAttempt.AddHours(6);
            Assert(store.TryMarkDeferred(thirdAttempt), "the third deferred migration state was not saved");
            var third = store.Read();
            Assert(
                third.FailedAttempts == 3 && third.RetryAfterUtc == thirdAttempt.AddHours(24),
                "later failed migrations were not capped at a 24-hour retry window");

            Assert(store.TryMarkCompleted(), "a successful migration was not marked complete");
            var completed = store.Read();
            var completedJson = File.ReadAllText(statePath, Encoding.UTF8);
            Assert(
                completed.IsCompleted && completed.CompletedAtUtc is not null &&
                completed.FailedAttempts == 0 && completed.RetryAfterUtc is null &&
                !completed.ShouldAttempt(DateTimeOffset.MaxValue),
                "completion did not clear retry state or permanently skip file navigation");
            Assert(store.TryMarkCompleted(), "marking an already completed migration was not idempotent");
            Assert(
                File.ReadAllText(statePath, Encoding.UTF8) == completedJson,
                "an idempotent completion rewrote the migration marker");
        }
        finally
        {
            if (Directory.Exists(temporaryRoot))
            {
                Directory.Delete(temporaryRoot, true);
            }
        }
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

    private static void TestLearningSettingsServiceTerminals()
    {
        var store = new InMemoryParentLearningSettingsStore(LearningSessionSettings.Default);
        var service = new ParentLearningSettingsService(store);
        Assert(!LearningSessionSettings.Normalize(20, 15, 4).PreferSchoolGrade, "legacy settings did not default the school-grade preference to OFF");
        Assert(LearningSessionSettings.FormatSchoolGrade(1) == "小学1年", "elementary school grade formatting is incorrect");
        Assert(LearningSessionSettings.FormatSchoolGrade(9) == "中学3年", "middle school grade formatting is incorrect");

        Assert(!service.Update(9, 9, 1, false).Success, "question counts below 10 were accepted");
        Assert(!service.Update(31, 15, 1, false).Success, "question counts above 30 were accepted");
        Assert(!service.Update(30, 31, 1, false).Success, "a pass line above the question count was accepted");
        Assert(!service.Update(20, 15, 0, false).Success && !service.Update(20, 15, 10, false).Success, "an out-of-range school grade was accepted");
        Assert(!service.Update(20, 15, 1, null).Success, "a missing school-grade preference was accepted");
        Assert(service.GetCurrentSettings() == LearningSessionSettings.Default, "invalid input changed saved learning settings");

        var minimum = service.Update(10, 8, 1, false);
        Assert(minimum.Success && minimum.Settings == new LearningSessionSettings(10, 8, 1, false), "minimum learning settings were rejected");

        var saved = service.Update(30, 24, 9, true);
        Assert(saved.Success && saved.Settings == new LearningSessionSettings(30, 24, 9, true), "maximum learning settings were rejected");
        Assert(store.ReadLearningSettings() == saved.Settings, "valid learning settings were not persisted");

        var page = ParentControlPageRenderer.Build([], false, saved.Settings);
        Assert(page.Contains("min=\"10\" max=\"30\"", StringComparison.Ordinal) && page.Contains("10〜30問", StringComparison.Ordinal), "parent page does not expose the 10-to-30 fixed question range");
        Assert(page.Contains("id=\"schoolGrade\"", StringComparison.Ordinal) && page.Contains("value=\"9\" selected", StringComparison.Ordinal) && page.Contains("中学3年", StringComparison.Ordinal), "parent page does not expose the persisted school grade");
        Assert(page.Contains("id=\"preferSchoolGrade\"", StringComparison.Ordinal) && page.Contains("type=\"checkbox\" checked", StringComparison.Ordinal), "parent page does not expose the persisted school-grade preference");

        store.ThrowOnWrite = true;
        var failed = service.Update(25, 20, 5, false);
        Assert(!failed.Success && failed.Settings == saved.Settings, "write failure did not preserve the prior learning settings");
    }

    private static void TestLearningResetServiceTerminals()
    {
        var store = new InMemoryParentResetStore(ParentPin.FromOrDefault("1234"));
        var service = new ParentLearningResetService(store, store);

        Assert(!service.Request("9999", LearningResetModeValues.HistoryOnly).Success, "wrong current PIN queued a reset");
        Assert(!service.Request("1234", "unknown").Success, "unknown reset mode was accepted");
        Assert(service.GetPendingReset() == LearningResetMode.None, "invalid reset input changed pending state");

        var history = service.Request("1234", LearningResetModeValues.HistoryOnly);
        Assert(
            history is { Success: true, Pending: true, Mode: LearningResetMode.HistoryOnly } &&
            service.GetPendingReset() == LearningResetMode.HistoryOnly,
            "history-only reset did not reach the queued terminal state");
        Assert(
            !service.CompleteAppliedReset(LearningResetMode.Full) &&
            service.GetPendingReset() == LearningResetMode.HistoryOnly,
            "a mismatched completion cleared the pending reset");
        Assert(
            service.CompleteAppliedReset(LearningResetMode.HistoryOnly) &&
            service.GetPendingReset() == LearningResetMode.None,
            "the applied history-only reset did not clear its pending marker");

        var full = service.Request("1234", LearningResetModeValues.Full);
        Assert(
            full is { Success: true, Pending: true, Mode: LearningResetMode.Full } &&
            service.CompleteAppliedReset(LearningResetMode.Full),
            "full reset did not reach queued and completed terminal states");

        store.ThrowOnWrite = true;
        var failed = service.Request("1234", LearningResetModeValues.HistoryOnly);
        Assert(!failed.Success && service.GetPendingReset() == LearningResetMode.None, "reset-store failure did not remain failed");
    }

    private static void TestUpdateServiceTerminals()
    {
        var releaseClient = new StubReleaseClient();
        var installer = new RecordingUpdateInstaller();
        var service = new UpdateService(new Version(1, 0, 0, 0), releaseClient, installer);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        releaseClient.ObserveCancellation = true;
        Assert(
            Check(service, cancellation.Token).Status == UpdateCheckStatus.Cancelled,
            "a canceled update check did not reach the canceled terminal state");
        releaseClient.ObserveCancellation = false;

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

    private static void TestSingleInstanceCoordinator()
    {
        using var primary = SingleInstanceCoordinator.Acquire();
        Assert(primary.IsPrimary, "the first coordinator did not become primary");

        using var received = new ManualResetEventSlim();
        primary.StartListening(received.Set);
        using (var secondary = SingleInstanceCoordinator.Acquire())
        {
            Assert(!secondary.IsPrimary, "the second coordinator also became primary");
            Assert(secondary.SignalTrainingRequest(), "the secondary coordinator could not signal a request");
            Assert(
                received.Wait(TimeSpan.FromSeconds(2)),
                "the primary coordinator did not observe the secondary request");
        }
    }

    private static void TestParentControlServerLifecycle()
    {
        var startCalls = 0;
        var returnCalls = 0;
        var pauseCalls = 0;
        var settingsCalls = 0;
        bool? capturedPreference = null;
        var resetCalls = 0;
        string? resetPassword = null;
        string? resetMode = null;
        var server = new ParentControlServer(
            _ =>
            {
                startCalls++;
                return Task.FromResult(false);
            },
            _ =>
            {
                returnCalls++;
                return Task.FromResult(true);
            },
            _ =>
            {
                pauseCalls++;
                return Task.FromResult(true);
            },
            () => false,
            (_, _, _) => Task.FromResult(PasswordChangeResult.Ok("ok")),
            () => LearningSessionSettings.Default,
            (questionCount, passLine, schoolGrade, preferSchoolGrade, _) =>
            {
                settingsCalls++;
                capturedPreference = preferSchoolGrade;
                var valid = schoolGrade is >= 1 and <= 9;
                return Task.FromResult(
                    new LearningSessionSettingsUpdateResult(
                        valid,
                        valid ? "ok" : "invalid grade",
                        LearningSessionSettings.Normalize(questionCount, passLine, schoolGrade, preferSchoolGrade)));
            },
            (currentPassword, mode, _) =>
            {
                resetCalls++;
                resetPassword = currentPassword;
                resetMode = mode;
                return Task.FromResult(
                    new LearningResetResult(
                        true,
                        "ok",
                        LearningResetMode.HistoryOnly,
                        true));
            });

        Assert(
            server.Port == 0 && server.NetworkUrls.Count == 0 && server.PrimaryUrl.Length == 0,
            "the parent control constructor bound a listener before Start");

        try
        {
            server.Start();
            var port = server.Port;
            server.Start();
            Assert(
                port is >= ParentControlServer.DefaultPort and < ParentControlServer.DefaultPort + 10 &&
                server.Port == port,
                "Start did not bind once within the documented port window");

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            using var startResponse = client
                .PostAsync($"http://127.0.0.1:{port}/api/start", content: null)
                .GetAwaiter()
                .GetResult();
            using var returnResponse = client
                .PostAsync($"http://127.0.0.1:{port}/api/return", content: null)
                .GetAwaiter()
                .GetResult();
            using var pauseResponse = client
                .PostAsync($"http://127.0.0.1:{port}/api/pause", content: null)
                .GetAwaiter()
                .GetResult();
            using var resetContent = new StringContent(
                """{"currentPassword":"1234","mode":"history"}""",
                Encoding.UTF8,
                "application/json");
            using var resetResponse = client
                .PostAsync($"http://127.0.0.1:{port}/api/reset", resetContent)
                .GetAwaiter()
                .GetResult();
            using var settingsContent = new StringContent(
                """{"questionCount":20,"passLine":15,"schoolGrade":9,"preferSchoolGrade":true}""",
                Encoding.UTF8,
                "application/json");
            using var settingsResponse = client
                .PostAsync($"http://127.0.0.1:{port}/api/settings", settingsContent)
                .GetAwaiter()
                .GetResult();
            using var invalidSettingsContent = new StringContent(
                """{"questionCount":20,"passLine":15,"schoolGrade":4,"preferSchoolGrade":"true"}""",
                Encoding.UTF8,
                "application/json");
            using var invalidSettingsResponse = client
                .PostAsync($"http://127.0.0.1:{port}/api/settings", invalidSettingsContent)
                .GetAwaiter()
                .GetResult();
            using var outOfRangeSettingsContent = new StringContent(
                """{"questionCount":20,"passLine":15,"schoolGrade":10,"preferSchoolGrade":true}""",
                Encoding.UTF8,
                "application/json");
            using var outOfRangeSettingsResponse = client
                .PostAsync($"http://127.0.0.1:{port}/api/settings", outOfRangeSettingsContent)
                .GetAwaiter()
                .GetResult();
            Assert(
                startResponse.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable &&
                returnResponse.StatusCode == System.Net.HttpStatusCode.OK &&
                pauseResponse.StatusCode == System.Net.HttpStatusCode.OK &&
                resetResponse.StatusCode == System.Net.HttpStatusCode.OK &&
                settingsResponse.StatusCode == System.Net.HttpStatusCode.OK &&
                invalidSettingsResponse.StatusCode == System.Net.HttpStatusCode.BadRequest &&
                outOfRangeSettingsResponse.StatusCode == System.Net.HttpStatusCode.BadRequest,
                "parent actions returned before their actual success results were known");
            Assert(
                startCalls == 1 && returnCalls == 1 && pauseCalls == 1 && resetCalls == 1,
                "parent actions were not invoked exactly once");
            Assert(settingsCalls == 2 && capturedPreference == true, "the parent settings API did not preserve a strict boolean preference or terminal grade validation");
            Assert(
                resetPassword == "1234" && resetMode == LearningResetModeValues.HistoryOnly,
                "parent reset request did not preserve its PIN or reset mode");
        }
        finally
        {
            server.Dispose();
            server.Dispose();
        }

        ExpectObjectDisposed(server.Start, "a disposed parent control server restarted");
    }

    private static UpdateCheckResult Check(
        UpdateService service,
        CancellationToken cancellationToken = default) =>
        service.CheckAndInstallLatestAsync(cancellationToken).GetAwaiter().GetResult();

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

    private static void ExpectObjectDisposed(Action action, string message)
    {
        try
        {
            action();
            throw new InvalidOperationException(message);
        }
        catch (ObjectDisposedException)
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

    private sealed class FixedProfileNameProvider(string profileName) : IUserProfileNameProvider
    {
        public string GetProfileName() => profileName;
    }

    private sealed class InMemoryParentLearningSettingsStore(LearningSessionSettings initialSettings)
        : IParentLearningSettingsStore
    {
        private LearningSessionSettings settings = initialSettings;

        public bool ThrowOnWrite { get; set; }

        public LearningSessionSettings ReadLearningSettings() => settings;

        public void WriteLearningSettings(LearningSessionSettings nextSettings)
        {
            if (ThrowOnWrite)
            {
                throw new IOException("simulated write failure");
            }

            settings = nextSettings;
        }
    }

    private sealed class InMemoryParentResetStore(ParentPin initialPin)
        : IParentPinStore, IParentLearningResetStore
    {
        private LearningResetMode pendingReset;

        public bool ThrowOnWrite { get; set; }

        public ParentPin Read() => initialPin;

        public void Write(ParentPin pin) => throw new NotSupportedException();

        public LearningResetMode ReadPendingLearningReset() => pendingReset;

        public void WritePendingLearningReset(LearningResetMode mode)
        {
            if (ThrowOnWrite)
            {
                throw new IOException("simulated write failure");
            }

            pendingReset = mode;
        }
    }

    private sealed class StubReleaseClient : IReleaseClient
    {
        public ReleaseInfo? Release { get; set; }

        public bool ObserveCancellation { get; set; }

        public Task<ReleaseInfo?> GetLatestAsync(CancellationToken cancellationToken)
        {
            if (ObserveCancellation)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            return Task.FromResult(Release);
        }
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
