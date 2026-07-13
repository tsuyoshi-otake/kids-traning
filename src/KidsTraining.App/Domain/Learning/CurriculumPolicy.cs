namespace KidsTraining.App.Domain.Learning;

internal static class CurriculumPolicy
{
    private static readonly string[] GradeOneTopics =
    [
        "add", "sub", "clock", "kokugo", "moji", "measure", "kazu", "shape",
        "chart", "story", "bun", "goi", "dokkai"
    ];

    private static readonly string[] GradeTwoTopics =
    [
        .. GradeOneTopics, "hissan", "mul", "frac"
    ];

    private static readonly string[] GradeThreeTopics =
    [
        .. GradeTwoTopics, "div", "eigo"
    ];

    public static int NormalizeGrade(int grade) => Math.Clamp(grade, 1, 3);

    public static IReadOnlyList<string> TopicsForGrade(int grade) => NormalizeGrade(grade) switch
    {
        1 => GradeOneTopics,
        2 => GradeTwoTopics,
        _ => GradeThreeTopics
    };

    public static bool IsAvailable(int grade, string topic) =>
        TopicsForGrade(grade).Contains(topic, StringComparer.Ordinal);
}

