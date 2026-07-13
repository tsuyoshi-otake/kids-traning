namespace KidsTraining.App.Domain.Updates;

internal static class ReleaseVersion
{
    public static bool TryParse(string? tagName, out Version version)
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

        version = Normalize(parsed);
        return true;
    }

    public static bool IsNewer(Version releaseVersion, Version currentVersion) =>
        Normalize(releaseVersion).CompareTo(Normalize(currentVersion)) > 0;

    public static Version Normalize(Version version) =>
        new(
            Math.Max(0, version.Major),
            Math.Max(0, version.Minor),
            Math.Max(0, version.Build),
            Math.Max(0, version.Revision));
}
