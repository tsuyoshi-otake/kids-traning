using KidsTraining.App.Domain.Learning;

namespace KidsTraining.App.Application.Learning;

internal interface ILegacyLearningStorageMigrationStateStore
{
    LegacyLearningStorageMigrationState Read();

    bool TryMarkCompleted();

    bool TryMarkDeferred(DateTimeOffset nowUtc);
}
