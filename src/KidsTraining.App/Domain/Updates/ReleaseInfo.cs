namespace KidsTraining.App.Domain.Updates;

internal sealed record ReleaseInfo(
    string TagName,
    bool Draft,
    bool Prerelease,
    IReadOnlyList<ReleaseAsset> Assets);

internal sealed record ReleaseAsset(string Name, string DownloadUrl);
