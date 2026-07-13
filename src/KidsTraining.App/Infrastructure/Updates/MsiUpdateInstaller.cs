using System.Diagnostics;
using KidsTraining.App.Application.Updates;
using KidsTraining.App.Domain.Updates;

namespace KidsTraining.App.Infrastructure.Updates;

internal sealed class MsiUpdateInstaller : IUpdateInstaller
{
    public async Task StartAsync(
        ReleaseAsset asset,
        Version releaseVersion,
        CancellationToken cancellationToken)
    {
        AppPaths.EnsureRuntimeDirectories();
        var installerPath = Path.Combine(
            AppPaths.UpdatesFolder,
            $"{Path.GetFileNameWithoutExtension(asset.Name)}-{releaseVersion}.msi");
        await DownloadInstallerAsync(asset.DownloadUrl, installerPath, cancellationToken).ConfigureAwait(true);

        var installer = new FileInfo(installerPath);
        if (!installer.Exists || installer.Length == 0)
        {
            throw new InvalidOperationException("Downloaded MSI is empty.");
        }

        StartCopiedUpdateRunner(installerPath);
    }

    private static async Task DownloadInstallerAsync(
        string url,
        string installerPath,
        CancellationToken cancellationToken)
    {
        var tempPath = installerPath + ".download";
        if (File.Exists(tempPath))
        {
            File.Delete(tempPath);
        }

        using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        using var response = await httpClient.GetAsync(
            url,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(true);
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
        var currentExecutable = System.Windows.Forms.Application.ExecutablePath;
        File.Copy(currentExecutable, AppPaths.UpdateRunnerPath, overwrite: true);

        var arguments =
            $"--apply-update {QuoteArgument(installerPath)} --parent-pid {Environment.ProcessId} --restart {QuoteArgument(currentExecutable)}";
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

    private static string QuoteArgument(string value) =>
        "\"" + value.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
}
