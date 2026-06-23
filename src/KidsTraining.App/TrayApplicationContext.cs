namespace KidsTraining.App;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);

    private readonly NotifyIcon notifyIcon;
    private readonly System.Windows.Forms.Timer startupTimer = new();
    private readonly System.Windows.Forms.Timer updateTimer = new();
    private readonly UpdateManager updateManager = new();

    private TrainingForm? trainingForm;
    private bool checkInProgress;
    private bool exitingForUpdate;

    public TrayApplicationContext()
    {
        AppPaths.EnsureRuntimeDirectories();

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

        UpdateLogger.Info($"Tray started. Current version: {UpdateManager.CurrentVersion}");
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("学習を開始", null, (_, _) => StartTraining());
        menu.Items.Add("更新を確認", null, async (_, _) => await CheckForUpdatesAsync(showNoUpdate: true).ConfigureAwait(true));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("終了", null, (_, _) => ExitTray());
        return menu;
    }

    private void StartTraining()
    {
        if (trainingForm is { IsDisposed: false })
        {
            trainingForm.WindowState = FormWindowState.Maximized;
            trainingForm.Activate();
            return;
        }

        trainingForm = new TrainingForm();
        trainingForm.FormClosed += (_, _) => trainingForm = null;
        trainingForm.Show();
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
                    ShowBalloon("Kids Training", "更新をバックグラウンドでインストールします。");
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
        notifyIcon.Visible = false;
        notifyIcon.Dispose();

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
