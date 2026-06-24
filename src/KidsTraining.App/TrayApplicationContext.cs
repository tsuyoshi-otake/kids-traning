using System.Diagnostics;

namespace KidsTraining.App;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);

    private readonly NotifyIcon notifyIcon;
    private readonly Control uiDispatcher = new();
    private readonly System.Windows.Forms.Timer startupTimer = new();
    private readonly System.Windows.Forms.Timer updateTimer = new();
    private readonly System.Windows.Forms.Timer autoTrainingTimer = new();
    private readonly UpdateManager updateManager = new();

    private readonly ParentControlServer? parentControlServer;
    private TrainingForm? trainingForm;
    private bool checkInProgress;
    private bool exitingForUpdate;
    private volatile bool trainingActive;

    public TrayApplicationContext(bool startTrainingOnLaunch)
    {
        AppPaths.EnsureRuntimeDirectories();
        uiDispatcher.CreateControl();
        _ = uiDispatcher.Handle;
        parentControlServer = StartParentControlServer();

        notifyIcon = new NotifyIcon
        {
            Icon = LoadIcon(),
            Text = "Kids Training",
            Visible = true,
            ContextMenuStrip = BuildContextMenu()
        };

        notifyIcon.DoubleClick += (_, _) => StartTraining();

        startupTimer.Interval = 30_000;
        startupTimer.Tick += async (_, _) =>
        {
            startupTimer.Stop();
            await CheckForUpdatesAsync(showNoUpdate: false).ConfigureAwait(true);
        };
        startupTimer.Start();

        updateTimer.Interval = (int)CheckInterval.TotalMilliseconds;
        updateTimer.Tick += async (_, _) => await CheckForUpdatesAsync(showNoUpdate: false).ConfigureAwait(true);
        updateTimer.Start();

        if (startTrainingOnLaunch)
        {
            autoTrainingTimer.Interval = 1000;
            autoTrainingTimer.Tick += (_, _) =>
            {
                autoTrainingTimer.Stop();
                StartTraining();
            };
            autoTrainingTimer.Start();
        }

        UpdateLogger.Info($"Tray started. Current version: {UpdateManager.CurrentVersion}");
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("学習を開始", null, (_, _) => StartTraining());
        if (parentControlServer is not null)
        {
            menu.Items.Add("保護者画面を開く", null, (_, _) => OpenParentControlPage());
            menu.Items.Add("保護者画面URLをコピー", null, (_, _) => CopyParentControlUrl());
        }

        menu.Items.Add("更新を確認", null, async (_, _) => await CheckForUpdatesAsync(showNoUpdate: true).ConfigureAwait(true));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("終了", null, (_, _) => ExitTray());
        return menu;
    }

    private void StartTraining()
    {
        if (trainingForm is { IsDisposed: false })
        {
            trainingActive = true;
            trainingForm.WindowState = FormWindowState.Maximized;
            trainingForm.Activate();
            return;
        }

        trainingForm = new TrainingForm();
        trainingActive = true;
        trainingForm.FormClosed += (_, _) =>
        {
            trainingActive = false;
            trainingForm = null;
        };
        trainingForm.Show();
    }

    private void ReturnToComputer()
    {
        if (trainingForm is { IsDisposed: false } form)
        {
            form.ReturnToComputer();
        }
        else
        {
            trainingActive = false;
        }
    }

    private ParentControlServer? StartParentControlServer()
    {
        try
        {
            var server = new ParentControlServer(
                StartTrainingFromParentControl,
                ReturnToComputerFromParentControl,
                () => trainingActive,
                ChangeParentPasswordFromParentControl);
            server.Start();
            UpdateLogger.Info($"Parent control server started: {string.Join(", ", server.NetworkUrls)}");
            return server;
        }
        catch (Exception ex)
        {
            UpdateLogger.Error("Parent control server could not start", ex);
            return null;
        }
    }

    private void StartTrainingFromParentControl()
    {
        trainingActive = true;
        InvokeOnUiThread(StartTraining);
    }

    private void ReturnToComputerFromParentControl()
    {
        trainingActive = false;
        InvokeOnUiThread(ReturnToComputer);
    }

    private PasswordChangeResult ChangeParentPasswordFromParentControl(string? currentPassword, string? newPassword)
    {
        var result = ParentSettings.ChangeParentPassword(currentPassword, newPassword);
        if (result.Success)
        {
            var savedPassword = ParentSettings.GetParentPassword();
            InvokeOnUiThread(() => trainingForm?.SetParentPassword(savedPassword));
        }

        return result;
    }

    private void InvokeOnUiThread(Action action)
    {
        if (uiDispatcher.IsDisposed)
        {
            return;
        }

        if (uiDispatcher.InvokeRequired)
        {
            uiDispatcher.BeginInvoke(action);
        }
        else
        {
            action();
        }
    }

    private void OpenParentControlPage()
    {
        if (parentControlServer is null)
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = parentControlServer.PrimaryUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            UpdateLogger.Error("Could not open parent control page", ex);
            ShowBalloon("Kids Training", "保護者画面を開けませんでした。", ToolTipIcon.Warning);
        }
    }

    private void CopyParentControlUrl()
    {
        if (parentControlServer is null)
        {
            return;
        }

        try
        {
            Clipboard.SetText(parentControlServer.PrimaryUrl);
            ShowBalloon("Kids Training", $"保護者画面URLをコピーしました: {parentControlServer.PrimaryUrl}");
        }
        catch (Exception ex)
        {
            UpdateLogger.Error("Could not copy parent control URL", ex);
            ShowBalloon("Kids Training", "保護者画面URLをコピーできませんでした。", ToolTipIcon.Warning);
        }
    }

    private async Task CheckForUpdatesAsync(bool showNoUpdate)
    {
        if (checkInProgress)
        {
            return;
        }

        if (trainingForm is { IsDisposed: false })
        {
            UpdateLogger.Info("Update check skipped because learning mode is active.");
            return;
        }

        checkInProgress = true;
        try
        {
            var result = await updateManager.CheckAndInstallLatestAsync(CancellationToken.None).ConfigureAwait(true);
            UpdateLogger.Info($"Update check result: {result.Status} {result.Message}");

            switch (result.Status)
            {
                case UpdateCheckStatus.UpdateStarted:
                    exitingForUpdate = true;
                    ExitThread();
                    break;
                case UpdateCheckStatus.NoUpdate when showNoUpdate:
                    ShowBalloon("Kids Training", "最新バージョンです。");
                    break;
                case UpdateCheckStatus.Failed when showNoUpdate:
                    ShowBalloon("Kids Training", $"更新確認に失敗しました: {result.Message}", ToolTipIcon.Warning);
                    break;
            }
        }
        finally
        {
            checkInProgress = false;
        }
    }

    private void ExitTray()
    {
        if (trainingForm is { IsDisposed: false })
        {
            ShowBalloon("Kids Training", "学習中はトレイ常駐を終了できません。", ToolTipIcon.Warning);
            return;
        }

        ExitThread();
    }

    protected override void ExitThreadCore()
    {
        startupTimer.Stop();
        updateTimer.Stop();
        autoTrainingTimer.Stop();
        parentControlServer?.Dispose();
        notifyIcon.Visible = false;
        notifyIcon.Dispose();
        uiDispatcher.Dispose();

        if (exitingForUpdate)
        {
            UpdateLogger.Info("Tray exiting for update installation.");
        }

        base.ExitThreadCore();
    }

    private void ShowBalloon(string title, string message, ToolTipIcon icon = ToolTipIcon.Info)
    {
        try
        {
            notifyIcon.ShowBalloonTip(5000, title, message, icon);
        }
        catch
        {
            // Balloon tips are cosmetic.
        }
    }

    private static Icon LoadIcon()
    {
        try
        {
            return Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
        }
        catch
        {
            return SystemIcons.Application;
        }
    }
}
