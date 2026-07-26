namespace KidsTraining.App.Domain.ParentControl;

internal sealed record LearningSessionSettings(
    int QuestionCount,
    int PassLine,
    int SchoolGrade,
    bool PreferSchoolGrade)
{
    public const int MinimumQuestionCount = 10;
    public const int MaximumQuestionCount = 30;
    public const int MinimumPassLine = 1;
    public const int MinimumSchoolGrade = 1;
    public const int MaximumSchoolGrade = 9;

    public static LearningSessionSettings Default { get; } = new(20, 15, 1, false);

    public static LearningSessionSettings Normalize(
        int? questionCount,
        int? passLine,
        int? schoolGrade = null,
        bool? preferSchoolGrade = null)
    {
        var count = Math.Clamp(questionCount ?? Default.QuestionCount, MinimumQuestionCount, MaximumQuestionCount);
        var pass = Math.Clamp(passLine ?? Default.PassLine, MinimumPassLine, count);
        var grade = Math.Clamp(schoolGrade ?? Default.SchoolGrade, MinimumSchoolGrade, MaximumSchoolGrade);
        return new LearningSessionSettings(count, pass, grade, preferSchoolGrade ?? Default.PreferSchoolGrade);
    }

    public static string FormatSchoolGrade(int schoolGrade)
    {
        var grade = Math.Clamp(schoolGrade, MinimumSchoolGrade, MaximumSchoolGrade);
        return grade <= 6 ? $"小学{grade}年" : $"中学{grade - 6}年";
    }
}
