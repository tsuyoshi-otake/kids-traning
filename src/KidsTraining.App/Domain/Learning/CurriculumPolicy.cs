namespace KidsTraining.App.Domain.Learning;

internal static class CurriculumPolicy
{
    private static readonly string[][] GradeOneLanes =
    [
        ["kazu", "shape", "add", "sub", "clock", "measure", "story", "money", "groups", "chart"],
        ["moji", "bun", "kokugo", "goi", "dokkai"]
    ];

    private static readonly string[][] GradeTwoLanes =
    [
        ["chart", "clock", "add", "sub", "measure", "hissan", "story", "kazu", "money", "order", "groups", "mul", "shape", "frac"],
        ["kokugo", "bun", "goi", "dokkai", "moji"]
    ];

    private static readonly string[][] GradeThreeLanes =
    [
        ["mul", "div", "shape", "hissan", "kazu", "add", "sub", "clock", "measure", "story", "order", "chart", "frac", "money", "groups"],
        ["kokugo", "bun", "goi", "dokkai", "moji"],
        ["eigo"]
    ];

    public static int NormalizeGrade(int grade) => Math.Clamp(grade, 1, 3);

    public static IReadOnlyList<IReadOnlyList<string>> TopicLanesForGrade(int grade) => NormalizeGrade(grade) switch
    {
        1 => GradeOneLanes,
        2 => GradeTwoLanes,
        _ => GradeThreeLanes
    };

    public static IReadOnlyList<string> TopicsForGrade(int grade) =>
        TopicLanesForGrade(grade).SelectMany(static lane => lane).Distinct(StringComparer.Ordinal).ToArray();

    public static bool IsAvailable(int grade, string topic) =>
        TopicsForGrade(grade).Contains(topic, StringComparer.Ordinal);
}
