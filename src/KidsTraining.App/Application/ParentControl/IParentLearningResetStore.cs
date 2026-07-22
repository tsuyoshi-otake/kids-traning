using KidsTraining.App.Domain.ParentControl;

namespace KidsTraining.App.Application.ParentControl;

internal interface IParentLearningResetStore
{
    LearningResetMode ReadPendingLearningReset();

    void WritePendingLearningReset(LearningResetMode mode);
}
