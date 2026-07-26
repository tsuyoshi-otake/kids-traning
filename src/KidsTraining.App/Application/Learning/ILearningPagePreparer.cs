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
    public bool IsSuccess => Status != LearningPagePreparationStatus.Failed;

    public static LearningPagePreparationResult Prepared(string runtimePagePath) =>
        PreparedFresh(runtimePagePath);

    public static LearningPagePreparationResult PreparedFresh(string runtimePagePath) =>
        new(LearningPagePreparationStatus.PreparedFresh, runtimePagePath, null);

    public static LearningPagePreparationResult PreparedFromCache(string runtimePagePath) =>
        new(LearningPagePreparationStatus.PreparedFromCache, runtimePagePath, null);

    public static LearningPagePreparationResult Failed(string errorMessage) =>
        new(LearningPagePreparationStatus.Failed, null, errorMessage);
}

internal enum LearningPagePreparationStatus
{
    PreparedFresh,
    PreparedFromCache,
    Failed
}
