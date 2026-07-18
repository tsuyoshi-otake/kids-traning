using KidsTraining.App.Application.Learning;
using KidsTraining.App.Application.ParentControl;
using KidsTraining.App.Application.Updates;
using KidsTraining.App.Domain.ParentControl;
using KidsTraining.App.Domain.Learning;
using KidsTraining.App.Domain.Updates;
using KidsTraining.App.Infrastructure.Lifecycle;
using KidsTraining.App.Infrastructure.ParentControl;

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
        Run("Learning markup rejects a duplicate required anchor", () => TestDuplicateRequiredAnchor(repositoryRoot));
        Run("Preparation result has explicit terminal states", TestPreparationTerminals);
        Run("Parent password changes reach explicit terminal states", TestPasswordServiceTerminals);
        Run("Parent learning settings reach explicit terminal states", TestLearningSettingsServiceTerminals);
        Run("Update checks reach explicit terminal states", TestUpdateServiceTerminals);
        Run("Parent control server awaits actions and shuts down cleanly", TestParentControlServerLifecycle);
        Run("Single-instance requests reach the primary instance", TestSingleInstanceCoordinator);

        if (Failures.Count == 0)
        {
            Console.WriteLine("Architecture tests passed: 16");
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
    }

    private static void TestCurriculumPolicy()
    {
        Assert(
            Math.Abs(LearningDefaults.BeginnerMastery - 0.05) < double.Epsilon,
            "beginner mastery is not represented as a structured domain value");
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
        Assert(html.Contains("curriculumPrerequisites(){return", StringComparison.Ordinal) && html.Contains("'mul':['groups']", StringComparison.Ordinal) && html.Contains("'dokkai':['bun','kokugo','goi']", StringComparison.Ordinal), "the curriculum prerequisite graph is missing from generated markup");
        Assert(html.Contains("s.attempts>0&&(Number(s.confidence)<.5||this.topicDue(p,k))", StringComparison.Ordinal), "unattempted upper topics are incorrectly treated as remediation triggers");
        Assert(html.Contains("visiting.has(topic)", StringComparison.Ordinal) && html.Contains("emitted.has(topic)", StringComparison.Ordinal) && html.Contains("filter(req=>!this.topicReady(p,req))", StringComparison.Ordinal), "prerequisite remediation is not cycle-safe, deduplicated, and readiness-aware");
        Assert(html.Contains("for(const k of base)for(const req of this.remediationTopics(p,k))", StringComparison.Ordinal) && html.Contains("return remedial.length?remedial:(out.length?out:allowed)", StringComparison.Ordinal), "allowed topics or curriculum frontier do not prioritize remediation");
        Assert(html.Contains("candidates=remedial.length?remedial:[k]", StringComparison.Ordinal) && html.Contains("if(!ks.length)throw new Error('No enabled curriculum topics')", StringComparison.Ordinal), "weighted selection does not fall back explicitly after prerequisite remediation");
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
        Assert(html.Contains("requestLearningReset()", StringComparison.Ordinal) && html.Contains("if(!this.state.resetConfirming)return", StringComparison.Ordinal), "learning reset bypasses two-step confirmation");
        Assert(html.Contains("streak:0,stars:0,xp:0", StringComparison.Ordinal) && html.Contains("level:1,stageAttempts:0,stageIndependent:0", StringComparison.Ordinal) && html.Contains("cleared:{}", StringComparison.Ordinal), "learning reset does not clear all progress evidence");
        Assert(html.Contains("const reset={...current", StringComparison.Ordinal) && html.Contains("progressResetAt:Date.now()", StringComparison.Ordinal), "learning reset does not preserve profile identity and grade");
        Assert(html.Contains("localStorage.setItem('kt_profiles_v1',persisted)", StringComparison.Ordinal) && !html.Contains("localStorage.clear()", StringComparison.Ordinal), "learning reset is not scoped to profile progress");
        Assert(html.Contains("aria-modal=", StringComparison.Ordinal) && html.Contains("cancelLearningReset", StringComparison.Ordinal), "learning reset confirmation is inaccessible or cannot be cancelled");
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
            html.Contains("profiles=[normalizeProfile(savedProfile)]", StringComparison.Ordinal) &&
            html.Contains("topics:{...def.topics,...storedTopics}", StringComparison.Ordinal),
            "the runtime migration does not retain one profile or merge newly introduced topics");
        Assert(
            html.Contains("numberOrDefault(host.questionCount", StringComparison.Ordinal) &&
            html.Contains("numberOrDefault(host.passLine", StringComparison.Ordinal) &&
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
            !bootstrapSource.Contains("localStorage.", StringComparison.Ordinal),
            "TrainingForm injects storage migration logic instead of only the host contract");
        Assert(
            trainingFormSource.Contains("SetLearningSessionSettingsAsync", StringComparison.Ordinal) &&
            trainingFormSource.Contains("SetParentPasswordAsync", StringComparison.Ordinal),
            "WebView setting synchronization does not expose an observable completion result");
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
            "16 - 6 + 7",
            "58 + 29",
            "68 + 22",
            "35 + 25",
            "19 + 43"
        };
        foreach (var expression in requiredExpressions)
        {
            Assert(html.Contains($"prompt:'{expression}'", StringComparison.Ordinal), $"required arithmetic question is missing: {expression}");
        }

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
            html.Contains("React.createElement('rt'", StringComparison.Ordinal),
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
            html.Contains("""<html lang="ja"><head>""", StringComparison.Ordinal) &&
            !html.Contains("<html><head>", StringComparison.Ordinal),
            "assembled learning HTML does not declare Japanese");
        Assert(
            html.Contains("""<style id="kt-layout-typography">""", StringComparison.Ordinal) &&
            html.Contains("""class="kt-question-prompt""", StringComparison.Ordinal) &&
            html.Contains("""class="kt-choice-grid""", StringComparison.Ordinal) &&
            html.Contains("""class="kt-feedback-answer""", StringComparison.Ordinal) &&
            html.Contains("@media (max-width: 720px)", StringComparison.Ordinal) &&
            html.Contains("button:focus-visible", StringComparison.Ordinal) &&
            html.Contains("select:focus-visible", StringComparison.Ordinal) &&
            html.Contains("@media (prefers-reduced-motion: reduce)", StringComparison.Ordinal),
            "responsive typography, focus, or reduced-motion markup is missing");

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
            "JapaneseQuestionPatch.cs"
        };
        var generatorCharacters = CjkCharacters(string.Concat(
            generatorFiles.Select(file => File.ReadAllText(Path.Combine(markupRoot, file)))));
        var furiganaCharacters = CjkCharacters(
            File.ReadAllText(Path.Combine(markupRoot, "QuestionFuriganaPatch.cs")));
        var missingCharacters = generatorCharacters
            .Except(furiganaCharacters)
            .OrderBy(static character => character)
            .ToArray();
        Assert(
            missingCharacters.Length == 0,
            "question generator CJK characters missing from furigana source: " + new string(missingCharacters));

        static HashSet<char> CjkCharacters(string source) =>
            source
                .Where(static character =>
                    (character >= '\u4E00' && character <= '\u9FFF') || character == '々')
                .ToHashSet();
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

    private static void TestLearningSettingsServiceTerminals()
    {
        var store = new InMemoryParentLearningSettingsStore(LearningSessionSettings.Default);
        var service = new ParentLearningSettingsService(store);

        Assert(!service.Update(19, 15).Success, "question counts below 20 were accepted");
        Assert(!service.Update(41, 15).Success, "question counts above 40 were accepted");
        Assert(!service.Update(30, 31).Success, "a pass line above the question count was accepted");
        Assert(service.GetCurrentSettings() == LearningSessionSettings.Default, "invalid input changed saved learning settings");

        var saved = service.Update(30, 24);
        Assert(saved.Success && saved.Settings == new LearningSessionSettings(30, 24), "valid learning settings were rejected");
        Assert(store.ReadLearningSettings() == saved.Settings, "valid learning settings were not persisted");

        store.ThrowOnWrite = true;
        var failed = service.Update(32, 25);
        Assert(!failed.Success && failed.Settings == saved.Settings, "write failure did not preserve the prior learning settings");
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
            () => false,
            (_, _, _) => Task.FromResult(PasswordChangeResult.Ok("ok")),
            () => LearningSessionSettings.Default,
            (_, _, _) => Task.FromResult(
                new LearningSessionSettingsUpdateResult(
                    true,
                    "ok",
                    LearningSessionSettings.Default)));

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
            Assert(
                startResponse.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable &&
                returnResponse.StatusCode == System.Net.HttpStatusCode.OK,
                "parent actions returned before their actual success results were known");
            Assert(startCalls == 1 && returnCalls == 1, "parent actions were not invoked exactly once");
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
