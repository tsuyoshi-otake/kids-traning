namespace KidsTraining.App.Domain.Learning;

internal static class CurriculumPolicy
{
    private static readonly string[] TopicKeys =
    [
        "add", "sub", "mul", "clock", "kokugo", "hissan", "moji", "measure", "kazu", "shape", "div",
        "frac", "chart", "story", "bun", "goi", "dokkai", "eigo", "money", "groups", "order", "keyboard"
    ];

    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> TopicPrerequisites =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            ["kazu"] = [],
            ["shape"] = [],
            ["add"] = ["kazu"],
            ["sub"] = ["add"],
            ["clock"] = ["kazu"],
            ["measure"] = ["kazu"],
            ["chart"] = ["kazu"],
            ["hissan"] = ["add", "sub"],
            ["story"] = ["add", "sub"],
            ["money"] = ["kazu"],
            ["groups"] = ["add"],
            ["order"] = ["add", "sub"],
            ["mul"] = ["groups"],
            ["div"] = ["mul"],
            ["frac"] = ["groups", "shape"],
            ["moji"] = [],
            ["bun"] = ["moji"],
            ["kokugo"] = ["moji"],
            ["goi"] = ["moji"],
            ["dokkai"] = ["bun", "kokugo", "goi"],
            ["eigo"] = [],
            ["keyboard"] = []
        };

    private static readonly string[][] GradeOneLanes =
    [
        ["kazu", "shape", "add", "sub", "clock", "measure", "story", "money", "groups", "chart"],
        ["moji", "bun", "kokugo", "goi", "dokkai"],
        ["keyboard"]
    ];

    private static readonly string[][] GradeTwoLanes =
    [
        ["chart", "clock", "add", "sub", "measure", "hissan", "story", "kazu", "money", "order", "groups", "mul", "shape", "frac"],
        ["kokugo", "bun", "goi", "dokkai", "moji"],
        ["keyboard"]
    ];

    private static readonly string[][] GradeThreeLanes =
    [
        ["mul", "div", "shape", "hissan", "kazu", "add", "sub", "clock", "measure", "story", "order", "chart", "frac", "money", "groups"],
        ["kokugo", "bun", "goi", "dokkai", "moji"],
        ["eigo"],
        ["keyboard"]
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

    public static IReadOnlyDictionary<string, IReadOnlyList<string>> PrerequisitesByTopic => TopicPrerequisites;

    public static IReadOnlyList<string> AllTopics => TopicKeys;

    public static IReadOnlyList<string> PrerequisitesFor(string topic) =>
        TopicPrerequisites.TryGetValue(topic, out var prerequisites) ? prerequisites : [];

    public static bool IsAvailable(int grade, string topic) =>
        TopicsForGrade(grade).Contains(topic, StringComparer.Ordinal);
}
