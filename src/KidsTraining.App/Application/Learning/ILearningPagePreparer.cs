namespace KidsTraining.App.Application.Learning;

internal interface ILearningPagePreparer
{
    LearningPagePreparationResult Prepare();
}

internal sealed record LearningPagePreparationResult(
    LearningPagePreparationStatus Status,
    string? RuntimePagePath,
    string? ErrorMessage)
{
    public bool IsSuccess => Status == LearningPagePreparationStatus.Prepared;

    public static LearningPagePreparationResult Prepared(string runtimePagePath) =>
        new(LearningPagePreparationStatus.Prepared, runtimePagePath, null);

    public static LearningPagePreparationResult Failed(string errorMessage) =>
        new(LearningPagePreparationStatus.Failed, null, errorMessage);
}

internal enum LearningPagePreparationStatus
{
    Prepared,
    Failed
}
