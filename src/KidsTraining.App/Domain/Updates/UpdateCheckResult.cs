namespace KidsTraining.App.Domain.Updates;

internal sealed record UpdateCheckResult(UpdateCheckStatus Status, string Message)
{
    public static UpdateCheckResult NoUpdate(string message) => new(UpdateCheckStatus.NoUpdate, message);

    public static UpdateCheckResult UpdateStarted(string message) => new(UpdateCheckStatus.UpdateStarted, message);

    public static UpdateCheckResult Cancelled(string message) => new(UpdateCheckStatus.Cancelled, message);

    public static UpdateCheckResult Failed(string message) => new(UpdateCheckStatus.Failed, message);
}

internal enum UpdateCheckStatus
{
    NoUpdate,
    UpdateStarted,
    Cancelled,
    Failed
}
