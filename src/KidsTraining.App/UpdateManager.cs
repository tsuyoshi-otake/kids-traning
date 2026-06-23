using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KidsTraining.App;

internal sealed class UpdateManager
{
    private const string LatestReleaseUrl = "https://api.github.com/repos/tsuyoshi-otake/kids-traning/releases/latest";
    private const string InstallerAssetName = "KidsTraining.msi";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient httpClient;

    public UpdateManager()
        : this(CreateHttpClient())
    {
    }

    private UpdateManager(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public static Version CurrentVersion =>
        NormalizeVersion(Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0, 0));

    public async Task<UpdateCheckResult> CheckAndInstallLatestAsync(CancellationToken cancellationToken)
    {
        try
        {
            AppPaths.EnsureRuntimeDirectories();

            var release = await GetLatestReleaseAsync(cancellationToken).ConfigureAwait(true);
            if (release is null)
            {
                return UpdateCheckResult.Failed("Could not fetch the latest release.");
            }

            if (release.Draft || release.Prerelease)
            {
                return UpdateCheckResult.NoUpdate($"Release {release.TagName} is draft or prerelease.");
            }

            if (!TryGetReleaseVersion(release.TagName, out var releaseVersion))
            {
                return UpdateCheckResult.NoUpdate($"Release tag {release.TagName} is not a version.");
            }

            if (!IsNewerVersion(releaseVersion, CurrentVersion))
            {
                return UpdateCheckResult.NoUpdate($"Current version {CurrentVersion} is up to date.");
            }

            var assets = release.Assets ?? [];
            var asset = assets.FirstOrDefault(static asset =>
                string.Equals(asset.Name, InstallerAssetName, StringComparison.OrdinalIgnoreCase)) ??
                assets.FirstOrDefault(static asset =>
                    asset.Name.EndsWith(".msi", StringComparison.OrdinalIgnoreCase));

            if (asset is null || string.IsNullOrWhiteSpace(asset.BrowserDownloadUrl))
            {
                return UpdateCheckResult.Failed($"Release {release.TagName} has no MSI asset.");
            }

            var installerPath = Path.Combine(AppPaths.UpdatesFolder, $"{Path.GetFileNameWithoutExtension(asset.Name)}-{releaseVersion}.msi");
            await DownloadInstallerAsync(asset.BrowserDownloadUrl, installerPath, cancellationToken).ConfigureAwait(true);

            var installer = new FileInfo(installerPath);
            if (!installer.Exists || installer.Length == 0)
            {
                return UpdateCheckResult.Failed("Downloaded MSI is empty.");
            }

            StartCopiedUpdateRunner(installerPath);
            return UpdateCheckResult.UpdateStarted($"Started installing release {release.TagName}.");
        }
        catch (Exception ex)
        {
            UpdateLogger.Error("Update check failed", ex);
            return UpdateCheckResult.Failed(ex.Message);
        }
    }

    public static bool TryGetReleaseVersion(string? tagName, out Version version)
    {
        version = new Version(0, 0, 0, 0);
        if (string.IsNullOrWhiteSpace(tagName))
        {
            return false;
        }

        var normalized = tagName.Trim();
        if (normalized.StartsWith('v') || normalized.StartsWith('V'))
        {
            normalized = normalized[1..];
        }

        var prereleaseMarker = normalized.IndexOf('-', StringComparison.Ordinal);
        if (prereleaseMarker >= 0)
        {
            normalized = normalized[..prereleaseMarker];
        }

        if (!Version.TryParse(normalized, out var parsed))
        {
            return false;
        }

        version = NormalizeVersion(parsed);
        return true;
    }

    public static bool IsNewerVersion(Version releaseVersion, Version currentVersion) =>
        NormalizeVersion(releaseVersion).CompareTo(NormalizeVersion(currentVersion)) > 0;

    private async Task<GitHubRelease?> GetLatestReleaseAsync(CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(LatestReleaseUrl, cancellationToken).ConfigureAwait(true);
        if (!response.IsSuccessStatusCode)
        {
            UpdateLogger.Info($"GitHub latest release returned {(int)response.StatusCode} {response.ReasonPhrase}");
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(true);
        return await JsonSerializer.DeserializeAsync<GitHubRelease>(stream, JsonOptions, cancellationToken).ConfigureAwait(true);
    }

    private async Task DownloadInstallerAsync(string url, string installerPath, CancellationToken cancellationToken)
    {
        var tempPath = installerPath + ".download";
        if (File.Exists(tempPath))
        {
            File.Delete(tempPath);
        }

        using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(true);
        response.EnsureSuccessStatusCode();

        await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(true))
        await using (var target = File.Create(tempPath))
        {
            await source.CopyToAsync(target, cancellationToken).ConfigureAwait(true);
        }

        if (File.Exists(installerPath))
        {
            File.Delete(installerPath);
        }

        File.Move(tempPath, installerPath);
        UpdateLogger.Info($"Downloaded installer to {installerPath}");
    }

    private static void StartCopiedUpdateRunner(string installerPath)
    {
        var currentExe = Application.ExecutablePath;
        File.Copy(currentExe, AppPaths.UpdateRunnerPath, overwrite: true);

        var arguments =
            $"--apply-update {QuoteArgument(installerPath)} --parent-pid {Environment.ProcessId} --restart {QuoteArgument(currentExe)}";

        var runner = Process.Start(new ProcessStartInfo
        {
            FileName = AppPaths.UpdateRunnerPath,
            Arguments = arguments,
            WorkingDirectory = AppPaths.UpdatesFolder,
            UseShellExecute = false,
            CreateNoWindow = true
        });

        if (runner is null)
        {
            throw new InvalidOperationException("Could not start the update runner.");
        }

        UpdateLogger.Info($"Started copied update runner: {AppPaths.UpdateRunnerPath}");
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("KidsTraining", CurrentVersion.ToString()));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        client.Timeout = TimeSpan.FromMinutes(5);
        return client;
    }

    private static Version NormalizeVersion(Version version) =>
        new(
            Math.Max(0, version.Major),
            Math.Max(0, version.Minor),
            Math.Max(0, version.Build),
            Math.Max(0, version.Revision));

    private static string QuoteArgument(string value) =>
        "\"" + value.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";

    private sealed record GitHubRelease(
        [property: JsonPropertyName("tag_name")] string TagName,
        [property: JsonPropertyName("draft")] bool Draft,
        [property: JsonPropertyName("prerelease")] bool Prerelease,
        [property: JsonPropertyName("assets")] GitHubAsset[]? Assets);

    private sealed record GitHubAsset(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("browser_download_url")] string BrowserDownloadUrl);
}

internal sealed record UpdateCheckResult(UpdateCheckStatus Status, string Message)
{
    public static UpdateCheckResult NoUpdate(string message) => new(UpdateCheckStatus.NoUpdate, message);

    public static UpdateCheckResult UpdateStarted(string message) => new(UpdateCheckStatus.UpdateStarted, message);

    public static UpdateCheckResult Failed(string message) => new(UpdateCheckStatus.Failed, message);
}

internal enum UpdateCheckStatus
{
    NoUpdate,
    UpdateStarted,
    Failed
}
