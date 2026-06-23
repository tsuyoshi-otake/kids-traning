using System.Diagnostics;

namespace KidsTraining.App;

internal static class UpdateInstaller
{
    public static int Run(string[] args)
    {
        try
        {
            var installerPath = GetValue(args, "--apply-update");
            var parentPidText = GetValue(args, "--parent-pid");
            var restartPath = GetValue(args, "--restart");

            if (string.IsNullOrWhiteSpace(installerPath) || !File.Exists(installerPath))
            {
                UpdateLogger.Info("Update installer was started without a valid MSI path.");
                return 2;
            }

            if (int.TryParse(parentPidText, out var parentPid) && parentPid > 0)
            {
                WaitForParentExit(parentPid);
            }

            var exitCode = InstallMsiWithRetry(installerPath);
            if (exitCode is 0 or 3010 && !string.IsNullOrWhiteSpace(restartPath) && File.Exists(restartPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = restartPath,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
            }

            return exitCode;
        }
        catch (Exception ex)
        {
            UpdateLogger.Error("Update installer failed", ex);
            return 1;
        }
    }

    private static int InstallMsiWithRetry(string installerPath)
    {
        var delays = new[] { TimeSpan.Zero, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(1) };
        var lastExitCode = 1;

        for (var attempt = 0; attempt < delays.Length; attempt++)
        {
            if (delays[attempt] > TimeSpan.Zero)
            {
                Thread.Sleep(delays[attempt]);
            }

            lastExitCode = RunMsi(installerPath);
            UpdateLogger.Info($"msiexec attempt {attempt + 1} exited with {lastExitCode}");

            if (lastExitCode is 0 or 3010)
            {
                return lastExitCode;
            }

            // 1618 means another Windows Installer transaction is already in progress.
            if (lastExitCode != 1618)
            {
                return lastExitCode;
            }
        }

        return lastExitCode;
    }

    private static int RunMsi(string installerPath)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "msiexec.exe",
            Arguments = $"/i {QuoteArgument(installerPath)} /qn /norestart",
            UseShellExecute = false,
            CreateNoWindow = true
        });

        if (process is null)
        {
            return 1;
        }

        process.WaitForExit();
        return process.ExitCode;
    }

    private static void WaitForParentExit(int parentPid)
    {
        try
        {
            using var parent = Process.GetProcessById(parentPid);
            if (!parent.HasExited)
            {
                parent.WaitForExit((int)TimeSpan.FromMinutes(2).TotalMilliseconds);
            }
        }
        catch (ArgumentException)
        {
            // The parent already exited.
        }
    }

    private static string? GetValue(string[] args, string name)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return i + 1 < args.Length ? args[i + 1] : null;
            }
        }

        return null;
    }

    private static string QuoteArgument(string value) =>
        "\"" + value.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
}
