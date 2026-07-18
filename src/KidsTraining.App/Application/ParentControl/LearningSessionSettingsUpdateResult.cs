using KidsTraining.App.Domain.ParentControl;

namespace KidsTraining.App.Application.ParentControl;

internal sealed record LearningSessionSettingsUpdateResult(
    bool Success,
    string Message,
    LearningSessionSettings Settings);
