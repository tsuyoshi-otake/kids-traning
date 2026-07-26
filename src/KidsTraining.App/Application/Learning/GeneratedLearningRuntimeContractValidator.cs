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
        ("question-count-range", "sourceSettings.count,def.count)),10,30)"),
        ("pass-line-default", "pass:this.props.passLine??15"),
        ("addition-generator", "genAdd(p)"),
        ("written-arithmetic-generator", "genHissan(p)"),
        ("two-digit-addition-written-arithmetic", "q.topic='add';q.difficulty=5"),
        ("two-digit-subtraction-written-arithmetic", "q.topic='sub';q.difficulty=5"),
        ("multiplication-generator", "pickMul(p)"),
        ("japanese-generator", "pickKokugo(p)"),
        ("kanji-curriculum", "kanjiCurriculumEntries()"),
        ("kanji-grade-2-complete", "const kanjiGrade2='引羽雲園遠何科夏家歌画回会海絵外角楽活間丸岩顔汽記帰弓牛魚京強教近兄形計元言原戸古午後語工公広交光考行高黄合谷国黒今才細作算止市矢姉思紙寺自時室社弱首秋週春書少場色食心新親図数西声星晴切雪船線前組走多太体台地池知茶昼長鳥朝直通弟店点電刀冬当東答頭同道読内南肉馬売買麦半番父風分聞米歩母方北毎妹万明鳴毛門夜野友用曜来里理話';"),
        ("kanji-grade-3-complete", "const kanjiGrade3='悪安暗医委意育員院飲運泳駅央横屋温化荷界階寒感漢館岸起期客究急級宮球去橋業曲局銀区苦具君係軽血決研県庫湖向幸港号根祭皿仕死使始指歯詩次事持式実写者主守取酒受州拾終習集住重宿所暑助昭消商章勝乗植申身神真深進世整昔全相送想息速族他打対待代第題炭短談着注柱丁帳調追定庭笛鉄転都度投豆島湯登等動童農波配倍箱畑発反坂板皮悲美鼻筆氷表秒病品負部服福物平返勉放味命面問役薬由油有遊予羊洋葉陽様落流旅両緑礼列練路和開';"),
        ("character-generator", "pickMoji(p)"),
        ("measurement-generator", "pickMeasure(p)"),
        ("measurement-comparison", "measureCompare()"),
        ("measurement-length-copy", "いちばん ながいのは どれ？"),
        ("measurement-three-way-choice", "どれも おなじ"),
        ("measurement-kilogram-copy", "1kg は 何g？"),
        ("measurement-kilometer-copy", "1km は 何m？"),
        ("measurement-liter-copy", "1L は 何dL？"),
        ("number-grouping-copy", "10のまとまりで かんがえる"),
        ("time-unit-picker", "pickTimeUnits"),
        ("weekday-picker", "pickWeekday(stage)"),
        ("weekday-question", "subtype:'weekday'"),
        ("measurement-topic", "measure:{label:'たんい'"),
        ("measurement-visual", "isMeasureViz"),
        ("number-generator", "pickKazu(p)"),
        ("number-sequence-question", "subtype:'number-sequence'"),
        ("shape-generator", "pickShape(p)"),
        ("division-generator", "pickDiv(p)"),
        ("fraction-generator", "pickFrac(p)"),
        ("grade-aware-fraction-generator", "if(g<=2){const parts="),
        ("decimal-addition", "prompt:'0.'+a+' + 0.'+b+' は？'"),
        ("chart-generator", "pickChart(p)"),
        ("data-classification-activity", "同じ なかまごとに 分けて 数える"),
        ("story-generator", "pickStory(p)"),
        ("missing-addition", "subtype:'missing-add'"),
        ("missing-subtraction", "subtype:'missing-sub'"),
        ("money-generator", "pickMoney(p)"),
        ("groups-generator", "pickGroups(p)"),
        ("order-generator", "pickOrder(p)"),
        ("keyboard-generator", "pickKeyboard(p)"),
        ("keyboard-topic", "keyboard:{label:'キーボード'"),
        ("typing-mode", "mode:'type'"),
        ("typing-key-listener", "this._typeKeyHandler"),
        ("typing-repeat-guard", "e.repeat"),
        ("typing-word-bank", "['neko','ねこ']"),
        ("progress-reset", "applyLearningReset(mode,options)"),
        ("progress-reset-evidence", "progressResetAt:Date.now()"),
        ("reset-dialog", "aria-modal="),
        ("reset-copy", "学習履歴のみリセット"),
        ("full-reset-copy", "すべてリセット"),
        ("reset-pin", "this.state.resetPin!==this.parentPin()"),
        ("fractional-score", "n===1?0.5:(n===2?0.25:0)"),
        ("third-miss-terminal", "if(miss>=3)"),
        ("session-checkpoint", "kt_session_checkpoint_v1"),
        ("session-resume", "restoreLearningCheckpoint()"),
        ("pause-message", "kidsTraining.pause"),
        ("discard-checkpoint", "window.__kidsTrainingDiscard"),
        ("division-remainder", "あまり"),
        ("equilateral-triangle", "正三角形"),
        ("romaji-question", "subtype:'romaji'"),
        ("current-standard-romaji", "['shi','し','si']"),
        ("romaji-input-hint", "キーボードでは「"),
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
        ("stage-five-independent-evidence-threshold", "{attempts:6,independent:5}"),
        ("retention-stage", "topicLearningStage(p,k)"),
        ("retention-confirmations", "s.retentionStep=Math.min(3,s.retentionStep+1)"),
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
        ("counter-question", "subtype:'counter'"),
        ("greeting-question", "subtype:'greeting'"),
        ("feeling-reason-question", "subtype:'feeling-reason'"),
        ("word-meaning-question", "の いみは？"),
        ("dictionary-order-question", "国語じてんの じゅんに"),
        ("reading-topic", "topic:'dokkai'"),
        ("reading-count-question", "あつめた 数は？"),
        ("english-generator", "pickEigo(p)"),
        ("english-topic", "topic:'eigo'"),
        ("english-conversation-activity", "3往復の会話"),
        ("activity-prompt", "activityPrompt"),
        ("activity-card-reflection-label", "活動カード＋振り返り："),
        ("soroban-generator", "pickSoroban(p)"),
        ("life-studies-generator", "pickSeikatsu(p)"),
        ("social-studies-generator", "pickShakai(p)"),
        ("science-generator", "pickRika(p)"),
        ("moral-education-generator", "pickDoutoku(p)"),
        ("information-literacy-generator", "pickJouhou(p)"),
        ("integrated-study-generator", "pickSougou(p)"),
        ("special-activities-generator", "pickTokubetsu(p)"),
        ("soroban-topic", "soroban:{label:'そろばん'"),
        ("life-studies-topic", "seikatsu:{label:'せいかつ'"),
        ("social-studies-topic", "shakai:{label:'しゃかい'"),
        ("science-topic", "rika:{label:'りか'"),
        ("moral-education-topic", "doutoku:{label:'どうとく'"),
        ("information-literacy-topic", "jouhou:{label:'じょうほう'"),
        ("integrated-study-topic", "sougou:{label:'そうごう'"),
        ("special-activities-topic", "tokubetsu:{label:'学校かつどう'"),
        ("curriculum-lanes", "curriculumLanes(p)"),
        ("curriculum-frontier", "nextCurriculumTopic(p)"),
        ("english-translation-question", "を 英語で いうと？"),
        ("english-greeting", "Good morning."),
        ("session-role", "q.sessionRole=role"),
        ("session-pass-contract", "const pass=this.sessionPassOutcome(this.curP(),s).pass;"),
        ("session-pass-grace", "p.passBlockedStreak="),
        ("session-pass-goal-text", "{{ retryGoalText }}"),
        ("session-mission-target", "{{ missionTargetText }}"),
        ("lazy-session-questions", "generateSessionQuestion(p,s,role)"),
        ("bounded-question-deduplication", "for(let attempt=0;attempt<24;attempt++)"),
        ("adaptive-session-support", "support=s.supportTopics[topic]?1:0"),
        ("adaptive-target-advance", "refreshSessionTarget(p,s)"),
        ("question-grade-snapshot", "q.grade=unit.grade"),
        ("question-unit-id", "q.unitId=unit.id"),
        ("question-grade-label", "単元：{{ questionGradeLabel }}"),
        ("question-category-label", "カテゴリ：{{ questionCategoryLabel }}"),
        ("question-difficulty-label", "難易度：{{ questionDifficultyLabel }}"),
        ("calibration-grade-label", "単元：{{ calibGradeLabel }}"),
        ("calibration-category-label", "カテゴリ：{{ calibTopicLabel }}"),
        ("calibration-difficulty-label", "難易度：{{ calibDifficultyLabel }}"),
        ("stage-picker", "pickStage(stage,buckets,reviewRate=.25)"),
        ("review-stage", "reviewStage(p,k)"),
        ("stage-profile", "profileAtStage(p,k,stage)"),
        ("finite-saved-value", "Number.isFinite(saved)"),
        ("learning-schema", "p.learningSchema=5"),
        ("stage-attempts", "stageAttempts"),
        ("bounded-stage", "Math.min(5,Number(stage)||1)"),
        ("mastery-achievement", "masteredAt"),
        ("attention-enabled-default", "attentionEnabled:true"),
        ("attention-local-camera", "getUserMedia({audio:false"),
        ("attention-face-detection", "new FaceDetector({fastMode:true,maxDetectedFaces:1})"),
        ("attention-low-sample-threshold", "this._attentionLowSamples>=4"),
        ("attention-gentle-prompt", "prompt.textContent='集中してね'"),
        ("multiplication-pair-curriculum", "fromPairs([[1,2],[2,1],[2,2]"),
        ("multiplication-symbol", "prompt:a+' × '+b"),
        ("three-digit-times-one-digit", "this.rand(123,899)"),
        ("thousandfold-number-question", "を 1000ばいすると？"),
        ("one-tenth-number-question", "10分の1に なる。"),
        ("partition-division", "1人ぶんを もとめる わりざん。"),
        ("quotitive-division", "いくつ分かを もとめる わりざん。"),
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
        ("weak-topic-selection", "const weakKeys=Object.keys(T).filter"),
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
        ("host-school-grade", "host.schoolGrade"),
        ("host-prefer-school-grade", "host.preferSchoolGrade"),
        ("optional-school-grade-preference", "cfg&&cfg.preferSchoolGrade"),
        ("host-parent-pin", "host.parentPin"),
        ("host-pending-reset", "host.pendingLearningReset"),
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
