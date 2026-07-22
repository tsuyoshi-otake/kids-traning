namespace KidsTraining.App.Domain.ParentControl;

internal sealed record LearningSessionSettings(int QuestionCount, int PassLine)
{
    public const int MinimumQuestionCount = 10;
    public const int MaximumQuestionCount = 30;
    public const int MinimumPassLine = 1;

    public static LearningSessionSettings Default { get; } = new(20, 15);

    public static LearningSessionSettings Normalize(int? questionCount, int? passLine)
    {
        var count = Math.Clamp(questionCount ?? Default.QuestionCount, MinimumQuestionCount, MaximumQuestionCount);
        var pass = Math.Clamp(passLine ?? Default.PassLine, MinimumPassLine, count);
        return new LearningSessionSettings(count, pass);
    }
}
