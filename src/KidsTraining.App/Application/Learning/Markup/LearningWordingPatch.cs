namespace KidsTraining.App.Application.Learning.Markup;

internal static partial class LearningMarkupPatcher
{
    /// <summary>
    /// Two labels on the child-facing screens were written for an adult reader: a bare "1 / 5"
    /// difficulty ratio, and a weak-topic card that concatenated every weak unit into one
    /// unbounded line. Both now read as plain Japanese and stay within the card.
    /// </summary>
    private static string PatchLearningLabelWording(string markup)
    {
        markup = ReplaceRequired(
            markup,
            "skillLevel(p){",
            "difficultyWord(n){const level=this.clamp(Number(n)||1,1,5);return ['やさしい','すこし やさしい','ふつう','むずかしい','ちょうせん'][level-1]+'（レベル'+level+'）';}\n  weakSummary(labels){const list=Array.isArray(labels)?labels.filter(Boolean):[];if(!list.length)return '';if(list.length<=3)return list.join('・');return list.slice(0,3).join('・')+' ほか'+(list.length-3)+'こ';}\n  skillLevel(p){",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "calibDifficultyLabel=this.clamp(Number(cq.difficulty)||1,1,5)+' / 5';",
            "calibDifficultyLabel=this.difficultyWord(cq.difficulty);",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "questionDifficultyLabel:q?(this.clamp(Number(q.difficulty)||1,1,5)+' / 5'):'',",
            "questionDifficultyLabel:q?this.difficultyWord(q.difficulty):'',",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "const weakLabels=weakKeys.map(k=>T[k].label).join('・');",
            "const weakLabels=this.weakSummary(weakKeys.map(k=>T[k].label));",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "<div style=\"font-size:24px; font-weight:900; color:#d2503f;\">{{ weakLabels }}</div>",
            "<div style=\"font-size:22px; font-weight:900; color:#d2503f; line-height:1.35; overflow-wrap:anywhere;\">{{ weakLabels }}</div>",
            StringComparison.Ordinal);

        return markup;
    }
}
