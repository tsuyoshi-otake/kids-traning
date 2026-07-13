using KidsTraining.App.Domain.Updates;

namespace KidsTraining.App.Application.Updates;

internal interface IReleaseClient
{
    Task<ReleaseInfo?> GetLatestAsync(CancellationToken cancellationToken);
}
