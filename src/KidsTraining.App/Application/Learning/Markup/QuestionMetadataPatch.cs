namespace KidsTraining.App.Application.Learning.Markup;

internal static partial class LearningMarkupPatcher
{
    private static string PatchQuestionMetadata(string markup)
    {
        markup = ReplaceRequired(
            markup,
            "let calibIdx=0,calibTotal=0,calibProgStyle='',calibTopicLabel='',calibTopicChipStyle='',calibChoices=[]",
            "let calibIdx=0,calibTotal=0,calibProgStyle='',calibGradeLabel='',calibTopicLabel='',calibDifficultyLabel='',calibTopicChipStyle='',calibChoices=[]",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "calibTopicLabel=T[cq.topic].label;calibTopicChipStyle=",
            "calibGradeLabel=(Number(cq.grade)||this.state.setupGrade)+'年生';calibTopicLabel=T[cq.topic].label;calibDifficultyLabel=this.clamp(Number(cq.difficulty)||1,1,5)+' / 5';calibTopicChipStyle=",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "calibProgStyle:calibProgStyle, calibTopicLabel:calibTopicLabel, calibTopicChipStyle:",
            "calibProgStyle:calibProgStyle, calibGradeLabel:calibGradeLabel, calibTopicLabel:calibTopicLabel, calibDifficultyLabel:calibDifficultyLabel, calibTopicChipStyle:",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "topicLabel:topicLabel, hasPracticePrompt:!!practicePrompt, practicePrompt:this.withFurigana(practicePrompt), topicChipStyle:",
            "questionGradeLabel:q?((Number(q.grade)||this.effectiveGrade(p))+'年生'):'', questionCategoryLabel:topicLabel, questionDifficultyLabel:q?(this.clamp(Number(q.difficulty)||1,1,5)+' / 5'):'',\n      topicLabel:topicLabel, hasPracticePrompt:!!practicePrompt, practicePrompt:this.withFurigana(practicePrompt), topicChipStyle:",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "<span style=\"{{ calibTopicChipStyle }}\">{{ calibTopicLabel }}</span>",
            "<span data-calibration-grade=\"{{ calibGradeLabel }}\" style=\"background:#e8f0ff; color:#264f8f; border:2px solid #9bb7e8; border-radius:24px; padding:6px 16px; font-size:18px; font-weight:800; white-space:nowrap;\">学年：{{ calibGradeLabel }}</span>\n        <span data-calibration-category=\"{{ calibTopicLabel }}\" style=\"{{ calibTopicChipStyle }}\">カテゴリ：{{ calibTopicLabel }}</span>\n        <span data-calibration-difficulty=\"{{ calibDifficultyLabel }}\" style=\"background:#fff4d8; color:#795b00; border:2px solid #e2bd4f; border-radius:24px; padding:6px 16px; font-size:18px; font-weight:800; white-space:nowrap;\">難易度：{{ calibDifficultyLabel }}</span>",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "<!-- topic chips -->\n      <div style=\"display:flex; gap:10px; margin-top:18px; align-items:center;\">\n        <span style=\"{{ topicChipStyle }}\">{{ topicLabel }}</span>",
            "<!-- question metadata -->\n      <div class=\"kt-question-metadata\" aria-label=\"問題情報\" style=\"display:flex; gap:10px; margin-top:18px; align-items:center; flex-wrap:wrap;\">\n        <span data-question-grade=\"{{ questionGradeLabel }}\" style=\"background:#e8f0ff; color:#264f8f; border:2px solid #9bb7e8; border-radius:24px; padding:7px 18px; font-size:18px; font-weight:800; white-space:nowrap;\">学年：{{ questionGradeLabel }}</span>\n        <span data-question-category=\"{{ questionCategoryLabel }}\" style=\"{{ topicChipStyle }}\">カテゴリ：{{ questionCategoryLabel }}</span>\n        <span data-question-difficulty=\"{{ questionDifficultyLabel }}\" style=\"background:#fff4d8; color:#795b00; border:2px solid #e2bd4f; border-radius:24px; padding:7px 18px; font-size:18px; font-weight:800; white-space:nowrap;\">難易度：{{ questionDifficultyLabel }}</span>",
            StringComparison.Ordinal);

        return markup;
    }
}
