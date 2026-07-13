using KidsTraining.App.Domain.Updates;

namespace KidsTraining.App.Application.Updates;

internal interface IUpdateInstaller
{
    Task StartAsync(ReleaseAsset asset, Version releaseVersion, CancellationToken cancellationToken);
}
