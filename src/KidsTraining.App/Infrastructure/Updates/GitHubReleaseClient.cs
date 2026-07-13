using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using KidsTraining.App.Application.Updates;
using KidsTraining.App.Domain.Updates;

namespace KidsTraining.App.Infrastructure.Updates;

internal sealed class GitHubReleaseClient : IReleaseClient
{
    private const string LatestReleaseUrl = "https://api.github.com/repos/tsuyoshi-otake/kids-traning/releases/latest";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient httpClient;

    public GitHubReleaseClient(Version currentVersion)
    {
        httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(5)
        };
        httpClient.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("KidsTraining", ReleaseVersion.Normalize(currentVersion).ToString()));
        httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
    }

    public async Task<ReleaseInfo?> GetLatestAsync(CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(LatestReleaseUrl, cancellationToken).ConfigureAwait(true);
        if (!response.IsSuccessStatusCode)
        {
            UpdateLogger.Info($"GitHub latest release returned {(int)response.StatusCode} {response.ReasonPhrase}");
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(true);
        var release = await JsonSerializer.DeserializeAsync<GitHubRelease>(
            stream,
            JsonOptions,
            cancellationToken).ConfigureAwait(true);
        return release is null
            ? null
            : new ReleaseInfo(
                release.TagName,
                release.Draft,
                release.Prerelease,
                release.Assets?.Select(static asset => new ReleaseAsset(asset.Name, asset.BrowserDownloadUrl)).ToArray() ?? []);
    }

    private sealed record GitHubRelease(
        [property: JsonPropertyName("tag_name")] string TagName,
        [property: JsonPropertyName("draft")] bool Draft,
        [property: JsonPropertyName("prerelease")] bool Prerelease,
        [property: JsonPropertyName("assets")] GitHubAsset[]? Assets);

    private sealed record GitHubAsset(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("browser_download_url")] string BrowserDownloadUrl);
}
