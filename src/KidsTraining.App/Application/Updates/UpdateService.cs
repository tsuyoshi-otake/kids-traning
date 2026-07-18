using KidsTraining.App.Domain.Updates;

namespace KidsTraining.App.Application.Updates;

internal sealed class UpdateService
{
    private const string InstallerAssetName = "KidsTraining.msi";

    private readonly IReleaseClient releaseClient;
    private readonly IUpdateInstaller updateInstaller;

    public UpdateService(
        Version currentVersion,
        IReleaseClient releaseClient,
        IUpdateInstaller updateInstaller)
    {
        CurrentVersion = ReleaseVersion.Normalize(currentVersion);
        this.releaseClient = releaseClient;
        this.updateInstaller = updateInstaller;
    }

    public Version CurrentVersion { get; }

    public async Task<UpdateCheckResult> CheckAndInstallLatestAsync(CancellationToken cancellationToken)
    {
        try
        {
            var release = await releaseClient.GetLatestAsync(cancellationToken).ConfigureAwait(true);
            if (release is null)
            {
                return UpdateCheckResult.Failed("Could not fetch the latest release.");
            }

            if (release.Draft || release.Prerelease)
            {
                return UpdateCheckResult.NoUpdate($"Release {release.TagName} is draft or prerelease.");
            }

            if (!ReleaseVersion.TryParse(release.TagName, out var releaseVersion))
            {
                return UpdateCheckResult.NoUpdate($"Release tag {release.TagName} is not a version.");
            }

            if (!ReleaseVersion.IsNewer(releaseVersion, CurrentVersion))
            {
                return UpdateCheckResult.NoUpdate($"Current version {CurrentVersion} is up to date.");
            }

            var asset = release.Assets.FirstOrDefault(static candidate =>
                string.Equals(candidate.Name, InstallerAssetName, StringComparison.OrdinalIgnoreCase)) ??
                release.Assets.FirstOrDefault(static candidate =>
                    candidate.Name.EndsWith(".msi", StringComparison.OrdinalIgnoreCase));
            if (asset is null || string.IsNullOrWhiteSpace(asset.DownloadUrl))
            {
                return UpdateCheckResult.Failed($"Release {release.TagName} has no MSI asset.");
            }

            await updateInstaller.StartAsync(asset, releaseVersion, cancellationToken).ConfigureAwait(true);
            return UpdateCheckResult.UpdateStarted($"Started installing release {release.TagName}.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return UpdateCheckResult.Cancelled("Update check was canceled.");
        }
        catch (Exception exception)
        {
            return UpdateCheckResult.Failed(exception.Message);
        }
    }
}
