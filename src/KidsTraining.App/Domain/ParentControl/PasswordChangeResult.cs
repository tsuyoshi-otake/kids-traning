namespace KidsTraining.App.Domain.ParentControl;

internal sealed record PasswordChangeResult(bool Success, string Message)
{
    public static PasswordChangeResult Ok(string message) => new(true, message);

    public static PasswordChangeResult Failed(string message) => new(false, message);
}
