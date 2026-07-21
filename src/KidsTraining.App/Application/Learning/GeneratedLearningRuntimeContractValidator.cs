using System.Globalization;
using System.Text.Json;
using KidsTraining.App.Domain.Learning;

namespace KidsTraining.App.Application.Learning;

internal sealed record GeneratedLearningRuntimeContractFailure(string Code, string Message);

internal static class GeneratedLearningRuntimeContractValidator
{
    private static readonly (string Code, string Marker)[] RequiredMarkers =
    [
        ("startup-screen", "screen:'start', profileIdx:0,"),
        ("profile-container", "profiles:[\n"),
        ("profile-progress", "xp:0"),
        ("question-count-default", "count:this.props.questionCount??20"),
        ("pass-line-default", "pass:this.props.passLine??15"),
        ("addition-generator", "genAdd(p)"),
        ("written-arithmetic-generator", "genHissan(p)"),
        ("multiplication-generator", "pickMul(p)"),
        ("japanese-generator", "pickKokugo(p)"),
        ("character-generator", "pickMoji(p)"),
        ("measurement-generator", "pickMeasure(p)"),
        ("measurement-comparison", "measureCompare()"),
        ("measurement-length-copy", "どちらが ながい？"),
        ("measurement-kilogram-copy", "1kg は 何g？"),
        ("measurement-kilometer-copy", "1km は 何m？"),
        ("measurement-liter-copy", "1L は 何dL？"),
        ("number-grouping-copy", "10のまとまりで かんがえる"),
        ("time-unit-picker", "pickTimeUnits"),
        ("measurement-topic", "measure:{label:'たんい'"),
        ("measurement-visual", "isMeasureViz"),
        ("number-generator", "pickKazu(p)"),
        ("shape-generator", "pickShape(p)"),
        ("division-generator", "pickDiv(p)"),
        ("fraction-generator", "pickFrac(p)"),
        ("chart-generator", "pickChart(p)"),
        ("story-generator", "pickStory(p)"),
        ("money-generator", "pickMoney(p)"),
        ("groups-generator", "pickGroups(p)"),
        ("order-generator", "pickOrder(p)"),
        ("keyboard-generator", "pickKeyboard(p)"),
        ("keyboard-topic", "keyboard:{label:'キーボード'"),
        ("typing-mode", "mode:'type'"),
        ("typing-key-listener", "this._typeKeyHandler"),
        ("typing-repeat-guard", "e.repeat"),
        ("typing-word-bank", "['neko','ねこ']"),
        ("progress-reset", "resetLearningProgress()"),
        ("progress-reset-evidence", "progressResetAt:Date.now()"),
        ("reset-dialog", "aria-modal="),
        ("reset-copy", "学習状況をリセット"),
        ("division-remainder", "あまり"),
        ("equilateral-triangle", "正三角形"),
        ("romaji-question", "subtype:'romaji'"),
        ("topic-completion", "topicComplete(p,k)"),
        ("shape-visual", "isShapeViz"),
        ("prompt-style", "promptStyle"),
        ("cleared-marker", "markCleared"),
        ("topic-readiness", "topicReady(p,k"),
        ("ordinal-question", "なんばんめ"),
        ("word-question", "subtype:'kotoba'"),
        ("calculation-order-visual", "isOrder"),
        ("xp-gain", "gainXp"),
        ("xp-level", "xpLevel"),
        ("xp-feedback", "fbXp"),
        ("earned-xp", "earnedXp"),
        ("continue-learning-copy", "べんきょうを つづける"),
        ("alphabet-question", "subtype:'alphabet'"),
        ("hiragana-question", "subtype:'hiragana'"),
        ("katakana-question", "subtype:'katakana'"),
        ("centimeter-question", "1cm は 何mm？"),
        ("kanji-choice", "subtype:'kanji-choice'"),
        ("japanese-instruction", "kokuInstruction"),
        ("effective-grade", "effectiveGrade(p)"),
        ("learning-stage", "learningStage(p)"),
        ("topic-stage", "topicStage(p,k)"),
        ("written-arithmetic-completion", "hissanComplete(p)"),
        ("grade-topics", "gradeTopics(p)"),
        ("independent-evidence-threshold", "s.independent>=8"),
        ("attempt-evidence-threshold", "s.attempts>=10"),
        ("sentence-generator", "pickBun(p)"),
        ("vocabulary-generator", "pickGoi(p)"),
        ("reading-generator", "pickDokkai(p)"),
        ("particle-question", "（　）に はいる じは？"),
        ("quotation-question", "かぎかっこ"),
        ("subject-question", "しゅご（だれが・なにが）"),
        ("modifier-question", "しゅうしょくご"),
        ("katakana-word-question", "カタカナで 書く ことばは どれ？"),
        ("opposite-word-question", "はんたいの ことばは？"),
        ("odd-word-question", "なかまはずれは どれ？"),
        ("word-meaning-question", "の いみは？"),
        ("dictionary-order-question", "国語じてんの じゅんに"),
        ("reading-topic", "topic:'dokkai'"),
        ("reading-count-question", "あつめた 数は？"),
        ("english-generator", "pickEigo(p)"),
        ("english-topic", "topic:'eigo'"),
        ("curriculum-lanes", "curriculumLanes(p)"),
        ("curriculum-frontier", "nextCurriculumTopic(p)"),
        ("english-translation-question", "を 英語で いうと？"),
        ("english-greeting", "Good morning."),
        ("session-role", "q.sessionRole=role"),
        ("session-pass-contract", "globalPass&&targetPass"),
        ("stage-picker", "pickStage(stage,buckets,reviewRate=.25)"),
        ("review-stage", "reviewStage(p,k)"),
        ("stage-profile", "profileAtStage(p,k,stage)"),
        ("finite-saved-value", "Number.isFinite(saved)"),
        ("learning-schema", "learningSchema===3"),
        ("stage-attempts", "stageAttempts"),
        ("bounded-stage", "Math.min(5,Number(stage)||1)"),
        ("mastery-achievement", "masteredAt"),
        ("multiplication-pair-curriculum", "fromPairs([[1,2],[2,1],[2,2]"),
        ("multiplication-symbol", "prompt:a+' × '+b"),
        ("partition-division", "これは等分除"),
        ("quotitive-division", "これは包含除"),
        ("division-scaffold", "q.topic==='div'&&this.topicStage(p,'div')<=2"),
        ("english-speech", "speakEnglish(text)"),
        ("speech-stop", "if(m)this.stopEnglishSpeech()"),
        ("speech-api", "SpeechSynthesisUtterance"),
        ("speech-language", "utterance.lang='en-US'"),
        ("speech-rate", "utterance.rate=.85"),
        ("speech-choice-state", "speakChoices:!!speak"),
        ("speech-button", "class=\"kt-speech-button\""),
        ("choice-button", "<button type=\"button\" class=\"kt-choice-button\""),
        ("disabled-speech-title", "disabled title=\"{{ c.speakTitle }}\""),
        ("early-hiragana-stage", "stage<=1?['hiragana']"),
        ("profile-grade", "profileGrade:this.gradeLabel(p)"),
        ("weak-topic-selection", "const weakKeys=this.allowedTopics(p).filter"),
        ("feedback-gradient", "linear-gradient(135deg,#ffdad4"),
        ("multiplication-visual", "isMulViz"),
        ("topic-scaffold", "&&this.topicStage(p,q.topic)<=2"),
        ("multiplication-scaffold", "q.topic==='mul'&&this.topicStage(p,'mul')<=2"),
        ("japanese-meaning-scaffold", "kokuShowMean=this.topicStage(p,'kokugo')<=2"),
        ("japanese-meaning-template", "<sc-if value=\"{{ kokuShowMean }}\""),
        ("japanese-meaning-state", "kokuShowMean:kokuShowMean"),
        ("profile-migration", "migrateProfiles(profiles)"),
        ("host-bootstrap", "const host=window.__kidsTrainingHost"),
        ("host-profile-name", "host.profileName"),
        ("host-question-count", "host.questionCount"),
        ("host-pass-line", "host.passLine"),
        ("host-parent-pin", "host.parentPin"),
        ("unlock-message", "kidsTraining.unlock")
    ];

    private static readonly (string Code, string Marker)[] ForbiddenMarkers =
    [
        ("legacy-profile-screen", "screen:'profile', profileIdx:0,"),
        ("global-storage-reset", "localStorage.clear()"),
        ("legacy-cross-topic-progression", "if(done('add'))staged.push"),
        ("ascii-multiplication-symbol", "prompt:a+' x '+b"),
        ("unbounded-subtraction-range", "b=this.rand(11,a-1)"),
        ("legacy-addition-range", "b=this.rand(1,40)"),
        ("legacy-two-digit-addition-range", "b=this.rand(12,79)"),
        ("legacy-carry-range", "b=this.rand(11,79)"),
        ("legacy-bounded-addition-range", "b=this.rand(10,99-a)"),
        ("legacy-subtraction-range", "b=this.rand(20,a-1)"),
        ("legacy-subtraction-clamp", "Math.min(40,a-1)"),
        ("avatar-copy", "アバター"),
        ("avatar-ready-state", "avatarReady"),
        ("avatar-parts-state", "avatarParts"),
        ("avatar-finish-action", "finishAvatar"),
        ("avatar-fallback-markup", "<div style=\"{{ avatarStyle }}\">{{ profileInitial }}</div>"),
        ("avatar-fallback-state", "profileInitial:p.name.charAt(0), avatarStyle")
    ];

    public static IReadOnlyList<GeneratedLearningRuntimeContractFailure> Validate(
        string? html,
        string? expectedProfileName)
    {
        var failures = new List<GeneratedLearningRuntimeContractFailure>();
        if (string.IsNullOrEmpty(html))
        {
            failures.Add(new("runtime-empty", "The generated learning runtime is empty."));
            return failures;
        }

        foreach (var (code, marker) in RequiredMarkers)
        {
            if (!html.Contains(marker, StringComparison.Ordinal))
            {
                failures.Add(new(code, $"Required generated runtime marker was not found: {marker}"));
            }
        }

        foreach (var (code, marker) in ForbiddenMarkers)
        {
            if (html.Contains(marker, StringComparison.Ordinal))
            {
                failures.Add(new(code, $"Forbidden generated runtime marker remains: {marker}"));
            }
        }

        if (string.IsNullOrWhiteSpace(expectedProfileName))
        {
            failures.Add(new("profile-name-empty", "The expected learning profile name is empty."));
        }
        else
        {
            var profileMarker = $"name:{JsonSerializer.Serialize(expectedProfileName.Trim())}";
            if (!html.Contains(profileMarker, StringComparison.Ordinal))
            {
                failures.Add(new("profile-name", $"The generated learning profile marker was not found: {profileMarker}"));
            }
        }

        var mastery = LearningDefaults.BeginnerMastery
            .ToString("0.##", CultureInfo.InvariantCulture)
            .TrimStart('0');
        var beginnerMasteryMarker =
            "mastery:{" + string.Join(',', CurriculumPolicy.AllTopics.Select(topic => topic + ":" + mastery)) + "}";
        if (!html.Contains(beginnerMasteryMarker, StringComparison.Ordinal))
        {
            failures.Add(new(
                "beginner-mastery",
                $"The generated beginner mastery defaults were not found: {beginnerMasteryMarker}"));
        }

        return failures;
    }
}
