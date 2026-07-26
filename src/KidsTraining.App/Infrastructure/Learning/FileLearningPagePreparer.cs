using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using KidsTraining.App.Application.Learning;
using KidsTraining.App.Application.ParentControl;

namespace KidsTraining.App.Infrastructure.Learning;

internal sealed class FileLearningPagePreparer : ILearningPagePreparer
{
    private const int CacheSchemaVersion = 1;
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);
    private readonly object prepareGate = new();
    private readonly LearningPageBuilder pageBuilder;
    private readonly IParentPinProvider parentPinProvider;
    private readonly IUserProfileNameProvider profileNameProvider;
    private readonly LearningPagePreparationPaths paths;

    public FileLearningPagePreparer(
        LearningPageBuilder pageBuilder,
        IParentPinProvider parentPinProvider,
        IUserProfileNameProvider profileNameProvider,
        LearningPagePreparationPaths? paths = null)
    {
        this.pageBuilder = pageBuilder;
        this.parentPinProvider = parentPinProvider;
        this.profileNameProvider = profileNameProvider;
        this.paths = paths ?? LearningPagePreparationPaths.Default;
    }

    public LearningPagePreparationResult Prepare()
    {
        lock (prepareGate)
        {
            return PrepareCore();
        }
    }

    private LearningPagePreparationResult PrepareCore()
    {
        try
        {
            if (!File.Exists(paths.HtmlTemplatePath))
            {
                return LearningPagePreparationResult.Failed(
                    $"Learning HTML template was not found: {paths.HtmlTemplatePath}");
            }

            if (!File.Exists(paths.LearningAppDefinitionPath))
            {
                return LearningPagePreparationResult.Failed(
                    $"Learning app definition was not found: {paths.LearningAppDefinitionPath}");
            }

            var htmlTemplate = File.ReadAllText(paths.HtmlTemplatePath, Encoding.UTF8);
            var appDefinition = File.ReadAllText(paths.LearningAppDefinitionPath, Encoding.UTF8);
            var profileName = profileNameProvider.GetProfileName();
            var parentPin = parentPinProvider.GetCurrentPin();
            var inputHash = ComputeInputHash(htmlTemplate, appDefinition, profileName, parentPin.Value);
            var manifest = TryReadManifest(paths.CacheManifestPath);
            if (IsCacheHit(manifest, inputHash))
            {
                return LearningPagePreparationResult.PreparedFromCache(paths.RuntimeHtmlPath);
            }

            var runtimeHtml = pageBuilder.Build(htmlTemplate, appDefinition, profileName, parentPin);
            WriteAtomically(paths.RuntimeHtmlPath, runtimeHtml);
            var outputHash = ComputeFileHash(paths.RuntimeHtmlPath);
            var outputLength = new FileInfo(paths.RuntimeHtmlPath).Length;

            TryWriteManifest(paths.CacheManifestPath, new RuntimeCacheManifest(
                CacheSchemaVersion,
                inputHash,
                outputHash,
                outputLength,
                DateTime.UtcNow));
            return LearningPagePreparationResult.PreparedFresh(paths.RuntimeHtmlPath);
        }
        catch (Exception exception)
        {
            return LearningPagePreparationResult.Failed(exception.Message);
        }
    }

    private static string ComputeInputHash(
        string htmlTemplate,
        string appDefinition,
        string profileName,
        string parentPin)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        AppendHashValue(hash, CacheSchemaVersion.ToString(System.Globalization.CultureInfo.InvariantCulture));
        AppendHashValue(hash, Assembly.GetExecutingAssembly().ManifestModule.ModuleVersionId.ToString("D"));
        AppendHashValue(hash, htmlTemplate);
        AppendHashValue(hash, appDefinition);
        AppendHashValue(hash, profileName.Trim());
        AppendHashValue(hash, parentPin);
        return Convert.ToHexString(hash.GetHashAndReset());
    }

    private static void AppendHashValue(IncrementalHash hash, string value)
    {
        var bytes = Utf8NoBom.GetBytes(value);
        hash.AppendData(BitConverter.GetBytes(bytes.Length));
        hash.AppendData(bytes);
    }

    private bool IsCacheHit(RuntimeCacheManifest? manifest, string inputHash)
    {
        if (manifest is null ||
            manifest.SchemaVersion != CacheSchemaVersion ||
            !string.Equals(manifest.InputHash, inputHash, StringComparison.Ordinal) ||
            !File.Exists(paths.RuntimeHtmlPath))
        {
            return false;
        }

        var info = new FileInfo(paths.RuntimeHtmlPath);
        return info.Length == manifest.OutputLength &&
            string.Equals(ComputeFileHash(paths.RuntimeHtmlPath), manifest.OutputHash, StringComparison.Ordinal);
    }

    private static string ComputeFileHash(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private static RuntimeCacheManifest? TryReadManifest(string manifestPath)
    {
        try
        {
            if (!File.Exists(manifestPath))
            {
                return null;
            }

            return JsonSerializer.Deserialize<RuntimeCacheManifest>(
                File.ReadAllText(manifestPath, Encoding.UTF8));
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static void TryWriteManifest(string manifestPath, RuntimeCacheManifest manifest)
    {
        try
        {
            WriteAtomically(manifestPath, JsonSerializer.Serialize(manifest));
        }
        catch (Exception exception)
        {
            UpdateLogger.Error("Could not persist the learning runtime cache manifest", exception);
        }
    }

    private static void WriteAtomically(string targetPath, string content)
    {
        var directory = Path.GetDirectoryName(targetPath) ??
            throw new InvalidOperationException($"A parent directory is required: {targetPath}");
        Directory.CreateDirectory(directory);
        var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(targetPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllText(temporaryPath, content, Utf8NoBom);
            File.Move(temporaryPath, targetPath, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private sealed record RuntimeCacheManifest(
        int SchemaVersion,
        string InputHash,
        string OutputHash,
        long OutputLength,
        DateTime GeneratedAtUtc);
}

internal sealed record LearningPagePreparationPaths(
    string HtmlTemplatePath,
    string LearningAppDefinitionPath,
    string RuntimeHtmlPath,
    string CacheManifestPath)
{
    public static LearningPagePreparationPaths Default { get; } = new(
        AppPaths.HtmlTemplatePath,
        AppPaths.LearningAppDefinitionPath,
        AppPaths.RuntimeHtmlPath,
        AppPaths.LearningRuntimeCacheManifestPath);
}
