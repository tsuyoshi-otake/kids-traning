namespace KidsTraining.App.Domain.Learning;

internal static partial class CurriculumPolicy
{
    private static void AddThinkingUnits(
        Action<string, string, int, string, string, string, CurriculumQuestion[]> add)
    {
        string[] slugs = ["patterns-and-logic", "patterns-and-space", "logic-and-strategy",
            "constraints-and-planning", "evidence-and-combinations", "deduction-and-strategy"];
        for (var grade = 1; grade <= 6; grade++)
            add("thinking", "thinking", grade, slugs[grade - 1], $"{grade}年 思考トレーニング",
                GeneralSource, ThinkingQuestionBank.Build(grade));
    }
}
