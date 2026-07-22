using KidsTraining.App.Domain.ParentControl;

namespace KidsTraining.App.Application.ParentControl;

internal sealed record LearningResetResult(
    bool Success,
    string Message,
    LearningResetMode Mode,
    bool Pending)
{
    public static LearningResetResult Failed(string message) =>
        new(false, message, LearningResetMode.None, false);
}
