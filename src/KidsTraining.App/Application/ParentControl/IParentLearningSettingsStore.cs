using KidsTraining.App.Domain.ParentControl;

namespace KidsTraining.App.Application.ParentControl;

internal interface IParentLearningSettingsStore
{
    LearningSessionSettings ReadLearningSettings();
    void WriteLearningSettings(LearningSessionSettings settings);
}
