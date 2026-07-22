using System.Diagnostics;
using KidsTraining.App.Application.Learning;
using KidsTraining.App.Application.ParentControl;
using KidsTraining.App.Application.Updates;
using KidsTraining.App.Domain.ParentControl;
using KidsTraining.App.Domain.Updates;
using KidsTraining.App.Infrastructure.ParentControl;

namespace KidsTraining.App.Presentation.WinForms;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan WebViewSynchronizationTimeout = TimeSpan.FromSeconds(3);

    private readonly NotifyIcon notifyIcon;
    private readonly Control uiDispatcher = new();
    private readonly System.Windows.Forms.Timer startupTimer = new();
    private readonly System.Windows.Forms.Timer updateTimer = new();
    private readonly System.Windows.Forms.Timer autoTrainingTimer = new();
    private readonly CancellationTokenSource lifetimeCancellation = new();
    private readonly UpdateService updateService;
    private readonly ILearningPagePreparer learningPagePreparer;
    private readonly IParentPinProvider parentPinProvider;
    private readonly IUserProfileNameProvider profileNameProvider;
    private readonly ParentPasswordService parentPasswordService;
    private readonly ParentLearningSettingsService parentLearningSettingsService;
    private readonly ParentLearningResetService parentLearningResetService;

    private readonly ParentControlServer? parentControlServer;
    private TrainingForm? trainingForm;
    private Task<UpdateCheckResult>? activeUpdateCheck;
    private bool checkInProgress;
    private bool exitingForUpdate;
    private int trainingState = (int)TrainingSessionState.Inactive;
    private int exitStarted;

    public TrayApplicationContext(
        bool startTrainingOnLaunch,
        ILearningPagePreparer learningPagePreparer,
        IParentPinProvider parentPinProvider,
        IUserProfileNameProvider profileNameProvider,
        ParentPasswordService parentPasswordService,
        ParentLearningSettingsService parentLearningSettingsService,
        ParentLearningResetService parentLearningResetService,
        UpdateService updateService)
    {
        this.learningPagePreparer = learningPagePreparer;
        this.parentPinProvider = parentPinProvider;
        this.profileNameProvider = profileNameProvider;
        this.parentPasswordService = parentPasswordService;
        this.parentLearningSettingsService = parentLearningSettingsService;
        this.parentLearningResetService = parentLearningResetService;
        this.updateService = updateService;
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

        UpdateLogger.Info($"Tray started. Current version: {updateService.CurrentVersion}");
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
        menu.Items.Add("終了", null, async (_, _) => await ExitTrayAsync().ConfigureAwait(true));
        return menu;
    }

    private void StartTraining()
    {
        _ = TryStartTraining();
    }

    public void RequestTraining()
    {
        _ = RequestTrainingAsync();
    }

    private async Task RequestTrainingAsync()
    {
        try
        {
            var started = await InvokeOnUiThreadAsync(TryStartTraining, CancellationToken.None).ConfigureAwait(false);
            if (!started)
            {
                UpdateLogger.Info("A training request from another instance could not be completed.");
            }
        }
        catch (Exception exception)
        {
            UpdateLogger.Error("Could not handle a training request from another instance", exception);
        }
    }

    private bool TryStartTraining()
    {
        if (trainingForm is { IsDisposed: false })
        {
            SetTrainingState(TrainingSessionState.Active);
            trainingForm.WindowState = FormWindowState.Maximized;
            trainingForm.Activate();
            return true;
        }

        SetTrainingState(TrainingSessionState.Starting);
        try
        {
            var form = new TrainingForm(
                learningPagePreparer,
                parentPinProvider,
                profileNameProvider,
                parentLearningSettingsService,
                parentLearningResetService);
            trainingForm = form;
            form.FormClosed += (_, _) =>
            {
                if (ReferenceEquals(trainingForm, form))
                {
                    trainingForm = null;
                    SetTrainingState(TrainingSessionState.Inactive);
                }
            };
            form.Show();
            SetTrainingState(TrainingSessionState.Active);
            return true;
        }
        catch (Exception exception)
        {
            trainingForm?.Dispose();
            trainingForm = null;
            SetTrainingState(TrainingSessionState.Inactive);
            UpdateLogger.Error("Could not start the training window", exception);
            return false;
        }
    }

    private async Task<bool> ReturnToComputerAsync()
    {
        if (trainingForm is { IsDisposed: false } form)
        {
            SetTrainingState(TrainingSessionState.Stopping);
            try
            {
                var returned = await form.ReturnToComputerAsync().ConfigureAwait(true);
                if (!returned)
                {
                    SetTrainingState(TrainingSessionState.Active);
                    return false;
                }
                if (form.IsDisposed)
                {
                    trainingForm = null;
                    SetTrainingState(TrainingSessionState.Inactive);
                }

                return true;
            }
            catch (Exception exception)
            {
                SetTrainingState(TrainingSessionState.Active);
                UpdateLogger.Error("Could not close the training window", exception);
                return false;
            }
        }

        trainingForm = null;
        SetTrainingState(TrainingSessionState.Inactive);
        return true;
    }

    private bool IsTrainingActive() =>
        (TrainingSessionState)Volatile.Read(ref trainingState) != TrainingSessionState.Inactive;

    private void SetTrainingState(TrainingSessionState state) =>
        Volatile.Write(ref trainingState, (int)state);

    private ParentControlServer? StartParentControlServer()
    {
        ParentControlServer? server = null;
        try
        {
            server = new ParentControlServer(
                StartTrainingFromParentControl,
                ReturnToComputerFromParentControl,
                PauseTrainingFromParentControl,
                IsTrainingActive,
                ChangeParentPasswordFromParentControl,
                parentLearningSettingsService.GetCurrentSettings,
                ChangeLearningSettingsFromParentControl,
                RequestLearningResetFromParentControl);
            server.Start();
            UpdateLogger.Info($"Parent control server started: {string.Join(", ", server.NetworkUrls)}");
            return server;
        }
        catch (Exception ex)
        {
            try
            {
                server?.Dispose();
            }
            catch (Exception disposeException)
            {
                UpdateLogger.Error("Partially started parent control server could not be disposed", disposeException);
            }

            UpdateLogger.Error("Parent control server could not start", ex);
            return null;
        }
    }

    private Task<bool> StartTrainingFromParentControl(CancellationToken cancellationToken) =>
        InvokeOnUiThreadAsync(TryStartTraining, cancellationToken);

    private Task<bool> ReturnToComputerFromParentControl(CancellationToken cancellationToken) =>
        InvokeOnUiThreadAsync(ReturnToComputerAsync, cancellationToken);

    private Task<bool> PauseTrainingFromParentControl(CancellationToken cancellationToken) =>
        InvokeOnUiThreadAsync(async () =>
        {
            if (trainingForm is not { IsDisposed: false } form)
            {
                trainingForm = null;
                SetTrainingState(TrainingSessionState.Inactive);
                return false;
            }

            SetTrainingState(TrainingSessionState.Stopping);
            var paused = await form.PauseLearningAsync().ConfigureAwait(true);
            SetTrainingState(paused ? TrainingSessionState.Inactive : TrainingSessionState.Active);
            if (paused && form.IsDisposed)
            {
                trainingForm = null;
            }

            return paused;
        }, cancellationToken);

    private async Task<PasswordChangeResult> ChangeParentPasswordFromParentControl(
        string? currentPassword,
        string? newPassword,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = parentPasswordService.Change(currentPassword, newPassword);
        if (result.Success)
        {
            var savedPassword = parentPasswordService.GetCurrentPin().Value;
            var synchronized = await SynchronizeActiveTrainingAsync(
                form => form.SetParentPasswordAsync(savedPassword)).ConfigureAwait(false);
            if (!synchronized)
            {
                return result with
                {
                    Message = "パスワードは保存しましたが、現在の学習画面には反映できませんでした。次回起動時に反映します。"
                };
            }
        }

        return result;
    }

    private async Task<LearningSessionSettingsUpdateResult> ChangeLearningSettingsFromParentControl(
        int? questionCount,
        int? passLine,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = parentLearningSettingsService.Update(questionCount, passLine);
        if (result.Success)
        {
            var synchronized = await SynchronizeActiveTrainingAsync(
                form => form.SetLearningSessionSettingsAsync(result.Settings)).ConfigureAwait(false);
            if (!synchronized)
            {
                return result with
                {
                    Message = "学習設定は保存しましたが、現在の学習画面には反映できませんでした。次回起動時に反映します。"
                };
            }
        }

        return result;
    }

    private async Task<LearningResetResult> RequestLearningResetFromParentControl(
        string? currentPassword,
        string? resetMode,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = parentLearningResetService.Request(currentPassword, resetMode);
        if (!result.Success)
        {
            return result;
        }

        var applied = await SynchronizeActiveTrainingAsync(
            form => form.ApplyLearningResetAsync(result.Mode),
            succeedWhenInactive: false).ConfigureAwait(false);
        if (!applied)
        {
            return result with
            {
                Message = result.Message + " 学習画面が閉じている場合は、次回起動時に反映します。"
            };
        }

        if (!parentLearningResetService.CompleteAppliedReset(result.Mode))
        {
            return result with
            {
                Message = "リセットは反映しましたが、予約状態を解除できませんでした。アプリを再起動する前にもう一度お試しください。"
            };
        }

        return result with
        {
            Message = result.Mode == LearningResetMode.HistoryOnly
                ? "学習履歴をリセットしました。レベル・XP・星は維持されています。"
                : "すべての学習データをリセットしました。",
            Pending = false
        };
    }

    private async Task<bool> SynchronizeActiveTrainingAsync(
        Func<TrainingForm, Task<bool>> synchronize,
        bool succeedWhenInactive = true)
    {
        try
        {
            var synchronization = InvokeOnUiThreadAsync(
                () => trainingForm is not { IsDisposed: false } form
                    ? Task.FromResult(succeedWhenInactive)
                    : synchronize(form),
                lifetimeCancellation.Token);
            return await synchronization
                .WaitAsync(WebViewSynchronizationTimeout, lifetimeCancellation.Token)
                .ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            UpdateLogger.Info(
                $"WebView synchronization exceeded {WebViewSynchronizationTimeout.TotalSeconds:0} seconds.");
            return false;
        }
        catch (OperationCanceledException) when (lifetimeCancellation.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            UpdateLogger.Error("Could not synchronize settings with the active training window", exception);
            return false;
        }
    }

    private Task<T> InvokeOnUiThreadAsync<T>(Func<T> action, CancellationToken cancellationToken) =>
        InvokeOnUiThreadAsync(() => Task.FromResult(action()), cancellationToken);

    private async Task<T> InvokeOnUiThreadAsync<T>(
        Func<Task<T>> action,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (uiDispatcher.IsDisposed)
        {
            throw new ObjectDisposedException(nameof(uiDispatcher));
        }

        if (!uiDispatcher.InvokeRequired)
        {
            return await action().ConfigureAwait(true);
        }

        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellationRegistration = cancellationToken.Register(
            static state =>
            {
                var (source, token) = ((TaskCompletionSource<T>, CancellationToken))state!;
                source.TrySetCanceled(token);
            },
            (completion, cancellationToken));
        try
        {
            uiDispatcher.BeginInvoke(new Action(async () =>
            {
                if (completion.Task.IsCompleted)
                {
                    return;
                }

                try
                {
                    completion.TrySetResult(await action().ConfigureAwait(true));
                }
                catch (Exception exception)
                {
                    completion.TrySetException(exception);
                }
            }));
        }
        catch (Exception exception)
        {
            completion.TrySetException(exception);
        }

        return await completion.Task.ConfigureAwait(false);
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
            if (showNoUpdate)
            {
                ShowBalloon("Kids Training", "更新確認はすでに実行中です。");
            }

            return;
        }

        if (trainingForm is { IsDisposed: false })
        {
            UpdateLogger.Info("Update check skipped because learning mode is active.");
            if (showNoUpdate)
            {
                ShowBalloon("Kids Training", "学習中は更新を確認できません。");
            }

            return;
        }

        checkInProgress = true;
        try
        {
            var updateCheck = updateService.CheckAndInstallLatestAsync(lifetimeCancellation.Token);
            activeUpdateCheck = updateCheck;
            var result = await updateCheck.ConfigureAwait(true);
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
                case UpdateCheckStatus.Cancelled:
                    UpdateLogger.Info("Update check reached the cancelled terminal state.");
                    break;
                case UpdateCheckStatus.Failed when showNoUpdate:
                    ShowBalloon("Kids Training", $"更新確認に失敗しました: {result.Message}", ToolTipIcon.Warning);
                    break;
            }
        }
        finally
        {
            activeUpdateCheck = null;
            checkInProgress = false;
        }
    }

    private async Task ExitTrayAsync()
    {
        if (trainingForm is { IsDisposed: false })
        {
            ShowBalloon("Kids Training", "学習中はトレイ常駐を終了できません。", ToolTipIcon.Warning);
            return;
        }

        lifetimeCancellation.Cancel();
        var updateCheck = activeUpdateCheck;
        if (updateCheck is not null)
        {
            try
            {
                var result = await updateCheck.ConfigureAwait(true);
                UpdateLogger.Info($"Update check completed during tray exit: {result.Status} {result.Message}");
            }
            catch (Exception exception)
            {
                UpdateLogger.Error("Update check did not reach a clean terminal state during tray exit", exception);
            }
        }

        ExitThread();
    }

    protected override void ExitThreadCore()
    {
        if (Interlocked.Exchange(ref exitStarted, 1) != 0)
        {
            return;
        }

        startupTimer.Stop();
        updateTimer.Stop();
        autoTrainingTimer.Stop();
        lifetimeCancellation.Cancel();
        try
        {
            parentControlServer?.Dispose();
        }
        catch (Exception exception)
        {
            UpdateLogger.Error("Parent control server did not stop cleanly", exception);
        }
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
            return Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath) ?? SystemIcons.Application;
        }
        catch
        {
            return SystemIcons.Application;
        }
    }

    private enum TrainingSessionState
    {
        Inactive,
        Starting,
        Active,
        Stopping
    }
}
