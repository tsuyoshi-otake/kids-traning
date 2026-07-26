namespace KidsTraining.App.Domain.Learning;

internal sealed record LegacyLearningStorageMigrationState(
    bool IsCompleted,
    DateTimeOffset? CompletedAtUtc,
    int FailedAttempts,
    DateTimeOffset? RetryAfterUtc)
{
    public static LegacyLearningStorageMigrationState Pending { get; } = new(false, null, 0, null);

    public bool ShouldAttempt(DateTimeOffset nowUtc) =>
        !IsCompleted && (RetryAfterUtc is null || RetryAfterUtc <= nowUtc);
}
