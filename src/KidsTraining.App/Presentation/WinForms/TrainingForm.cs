using KidsTraining.App.Application.Learning;
using KidsTraining.App.Application.ParentControl;
using KidsTraining.App.Domain.ParentControl;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace KidsTraining.App.Presentation.WinForms;

internal sealed class TrainingForm : Form
{
    private const string UnlockMessage = "kidsTraining.unlock";
    private const string PauseMessage = "kidsTraining.pause";
    private const string ResetAppliedMessagePrefix = "kidsTraining.resetApplied:";
    private const string LearningSettingsMessagePrefix = "kidsTraining.settings:";
    private const string LearningVirtualHostName = "learning.kidstraining.local";
    private const string ExperimentalWebPlatformFeaturesArgument = "--enable-experimental-web-platform-features";
    private const int NavigationMaxAttempts = 2;
    private static readonly TimeSpan LegacyMigrationBudget = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan LegacyNavigationAttemptTimeout = TimeSpan.FromMilliseconds(1250);
    private static readonly TimeSpan MainNavigationAttemptTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan MigrationVerificationTimeout = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan NavigationRetryInitialDelay = TimeSpan.FromMilliseconds(125);

    private readonly WebView2 webView = new();
    private readonly System.Windows.Forms.Timer lockTimer = new();
    private readonly CancellationTokenSource initializationCancellation = new();
    private readonly ILearningPagePreparer learningPagePreparer;
    private readonly ILegacyLearningStorageMigrationStateStore legacyMigrationStateStore;
    private readonly IParentPinProvider parentPinProvider;
    private readonly IUserProfileNameProvider profileNameProvider;
    private readonly ParentLearningSettingsService parentLearningSettingsService;
    private readonly ParentLearningResetService parentLearningResetService;
    private bool canExit;
    private bool webViewInitialized;

    public TrainingForm(
        ILearningPagePreparer learningPagePreparer,
        ILegacyLearningStorageMigrationStateStore legacyMigrationStateStore,
        IParentPinProvider parentPinProvider,
        IUserProfileNameProvider profileNameProvider,
        ParentLearningSettingsService parentLearningSettingsService,
        ParentLearningResetService parentLearningResetService)
    {
        this.learningPagePreparer = learningPagePreparer;
        this.legacyMigrationStateStore = legacyMigrationStateStore;
        this.parentPinProvider = parentPinProvider;
        this.profileNameProvider = profileNameProvider;
        this.parentLearningSettingsService = parentLearningSettingsService;
        this.parentLearningResetService = parentLearningResetService;
        Text = "Kids Training";
        ApplyWindowIcon();
        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Maximized;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        KeyPreview = true;
        ShowInTaskbar = true;

        Controls.Add(webView);
        webView.Dock = DockStyle.Fill;

        Load += async (_, _) => await InitializeWebViewAsync(initializationCancellation.Token);
        Deactivate += (_, _) => EnforceLock();
        FormClosing += OnTrainingFormClosing;

        lockTimer.Interval = 1000;
        lockTimer.Tick += (_, _) => EnforceLock();
        lockTimer.Start();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (!canExit && (keyData == (Keys.Alt | Keys.F4) || keyData == Keys.Escape || keyData == (Keys.Control | Keys.W)))
        {
            EnforceLock();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        EnforceLock();
    }

    public async Task<bool> ReturnToComputerAsync()
    {
        if (webView.CoreWebView2 is not null)
        {
            try
            {
                var result = await webView.CoreWebView2.ExecuteScriptAsync(
                    "(() => { try { if (typeof window.__kidsTrainingDiscard === 'function') return window.__kidsTrainingDiscard() === true; localStorage.removeItem('kt_session_checkpoint_v1'); return true; } catch { return false; } })()");
                if (!string.Equals(result, "true", StringComparison.Ordinal))
                {
                    return false;
                }
            }
            catch (Exception exception)
            {
                UpdateLogger.Error("Could not discard the active learning session", exception);
                return false;
            }
        }

        ExitAfterUnlock();
        return true;
    }

    public async Task<bool> PauseLearningAsync()
    {
        if (webView.CoreWebView2 is null)
        {
            return false;
        }

        try
        {
            var result = await webView.CoreWebView2.ExecuteScriptAsync(
                "(() => typeof window.__kidsTrainingPause === 'function' && window.__kidsTrainingPause(false) === true)()");
            if (!string.Equals(result, "true", StringComparison.Ordinal))
            {
                return false;
            }

            ExitAfterUnlock();
            return true;
        }
        catch (Exception exception)
        {
            UpdateLogger.Error("Could not pause the active learning session", exception);
            return false;
        }
    }

    public async Task<bool> ApplyLearningResetAsync(LearningResetMode mode)
    {
        if (webView.CoreWebView2 is null || mode == LearningResetMode.None)
        {
            return false;
        }

        var encodedMode = System.Text.Json.JsonSerializer.Serialize(mode.ToWireValue());
        try
        {
            var result = await webView.CoreWebView2.ExecuteScriptAsync(
                $"(() => typeof window.__kidsTrainingReset === 'function' && window.__kidsTrainingReset({encodedMode}) === true)()");
            return string.Equals(result, "true", StringComparison.Ordinal);
        }
        catch (Exception exception)
        {
            UpdateLogger.Error("Could not apply a learning reset in the active WebView", exception);
            return false;
        }
    }

    public async Task<bool> SetParentPasswordAsync(string password)
    {
        if (webView.CoreWebView2 is null)
        {
            return false;
        }

        var encodedPassword = System.Text.Json.JsonSerializer.Serialize(password);
        try
        {
            var result = await webView.CoreWebView2.ExecuteScriptAsync(
                $$"""
                (() => {
                  try {
                    const password = {{encodedPassword}};
                    localStorage.setItem('kt_parent_pin_v1', password);
                    if (window.__kidsTrainingHost) {
                      window.__kidsTrainingHost.parentPin = password;
                    }
                    return true;
                  } catch {
                    return false;
                  }
                })()
                """);
            return string.Equals(result, "true", StringComparison.Ordinal);
        }
        catch (Exception exception)
        {
            UpdateLogger.Error("Could not synchronize the parent PIN into WebView storage", exception);
            return false;
        }
    }

    public async Task<bool> SetLearningSessionSettingsAsync(LearningSessionSettings settings)
    {
        if (webView.CoreWebView2 is null)
        {
            return false;
        }

        var script =
            """
            try {
              const key = 'kt_settings_v1';
              const raw = localStorage.getItem(key);
              const current = raw ? JSON.parse(raw) : {};
              const next = { ...current, count: __QUESTION_COUNT__, pass: __PASS_LINE__, schoolGrade: __SCHOOL_GRADE__, preferSchoolGrade: __PREFER_SCHOOL_GRADE__ };
              localStorage.setItem(key, JSON.stringify(next));
              if (window.__kidsTrainingHost) {
                window.__kidsTrainingHost.questionCount = __QUESTION_COUNT__;
                window.__kidsTrainingHost.passLine = __PASS_LINE__;
                window.__kidsTrainingHost.schoolGrade = __SCHOOL_GRADE__;
                window.__kidsTrainingHost.preferSchoolGrade = __PREFER_SCHOOL_GRADE__;
              }
              if (typeof window.__kidsTrainingApplySchoolGrade === 'function') {
                window.__kidsTrainingApplySchoolGrade(__SCHOOL_GRADE__);
              }
              return true;
            } catch {
              return false;
            }
            """
                .Replace("__QUESTION_COUNT__", settings.QuestionCount.ToString(), StringComparison.Ordinal)
                .Replace("__PASS_LINE__", settings.PassLine.ToString(), StringComparison.Ordinal)
                .Replace("__SCHOOL_GRADE__", settings.SchoolGrade.ToString(), StringComparison.Ordinal)
                .Replace(
                    "__PREFER_SCHOOL_GRADE__",
                    settings.PreferSchoolGrade ? "true" : "false",
                    StringComparison.Ordinal);
        try
        {
            var result = await webView.CoreWebView2.ExecuteScriptAsync($"(() => {{ {script} }})()");
            return string.Equals(result, "true", StringComparison.Ordinal);
        }
        catch (Exception exception)
        {
            UpdateLogger.Error("Could not synchronize learning settings into WebView storage", exception);
            return false;
        }
    }

    private async Task InitializeWebViewAsync(CancellationToken cancellationToken)
    {
        if (webViewInitialized)
        {
            return;
        }

        webViewInitialized = true;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            AppPaths.EnsureRuntimeDirectories();

            if (!File.Exists(AppPaths.HtmlTemplatePath) ||
                !File.Exists(AppPaths.LearningAppDefinitionPath))
            {
                MessageBox.Show(
                    $"学習コンテンツが見つかりません。\n{AppPaths.LearningAssetsFolder}",
                    "Kids Training",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                ExitAfterUnlock();
                return;
            }

            // Page preparation is CPU/file-I/O work and does not depend on WebView2. Start it before
            // the browser environment so the two longest independent startup spans overlap. The
            // wrapper converts every worker failure into an observed result, even when WebView2
            // initialization fails before this task is awaited.
            var preparationTask = Task.Run(PrepareLearningPage);

            CoreWebView2Environment.GetAvailableBrowserVersionString();
            var environmentOptions = new CoreWebView2EnvironmentOptions
            {
                AdditionalBrowserArguments = ExperimentalWebPlatformFeaturesArgument,
            };
            var environment = await CoreWebView2Environment.CreateAsync(
                userDataFolder: AppPaths.WebViewUserDataFolder,
                options: environmentOptions);
            await webView.EnsureCoreWebView2Async(environment);

            ConfigureWebView(webView.CoreWebView2);
            var preparation = await preparationTask.WaitAsync(cancellationToken);
            if (!preparation.IsSuccess || preparation.RuntimePagePath is null)
            {
                throw new InvalidOperationException(
                    preparation.ErrorMessage ?? "Learning page preparation failed.");
            }

            await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(BuildProfileStorageScript());
            var learningAssetsFolder = Path.GetDirectoryName(preparation.RuntimePagePath)
                ?? throw new InvalidOperationException("The learning assets folder could not be resolved.");
            webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                LearningVirtualHostName,
                learningAssetsFolder,
                CoreWebView2HostResourceAccessKind.DenyCors);
            webView.Visible = false;
            var runtimePageName = Uri.EscapeDataString(Path.GetFileName(preparation.RuntimePagePath));
            var secureRuntimeUri = $"https://{LearningVirtualHostName}/{runtimePageName}";
            string? legacyMigrationScriptId = null;
            var migrationPrepared = false;

            if (IsLegacyStorageMigrationRequired())
            {
                var legacyRead = await ReadLegacyLearningStorageAsync(
                    webView.CoreWebView2,
                    new Uri(preparation.RuntimePagePath).AbsoluteUri,
                    cancellationToken);
                if (legacyRead.IsSuccess)
                {
                    try
                    {
                        legacyMigrationScriptId = await webView.CoreWebView2
                            .AddScriptToExecuteOnDocumentCreatedAsync(
                                BuildLegacyStorageMigrationScript(legacyRead.Storage));
                        migrationPrepared = true;
                    }
                    catch (Exception exception)
                    {
                        UpdateLogger.Error(
                            "Could not prepare legacy learning-storage migration; continuing with the secure learning page",
                            exception);
                        DeferLegacyStorageMigration();
                    }
                }
                else
                {
                    DeferLegacyStorageMigration();
                }
            }

            try
            {
                await NavigateWithRetryAsync(
                    webView.CoreWebView2,
                    secureRuntimeUri,
                    MainNavigationAttemptTimeout,
                    cancellationToken);
                if (migrationPrepared)
                {
                    await CompleteLegacyStorageMigrationAsync(webView.CoreWebView2, cancellationToken);
                }

                webView.Visible = true;
            }
            finally
            {
                if (legacyMigrationScriptId is not null)
                {
                    try
                    {
                        webView.CoreWebView2.RemoveScriptToExecuteOnDocumentCreated(legacyMigrationScriptId);
                    }
                    catch (Exception exception)
                    {
                        UpdateLogger.Error("Could not remove the one-time legacy migration script", exception);
                    }
                }
            }
        }
        catch (WebView2RuntimeNotFoundException)
        {
            MessageBox.Show(
                "Microsoft Edge WebView2 Runtime が見つかりません。WebView2 Runtime をインストールしてから再実行してください。",
                "Kids Training",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            ExitAfterUnlock();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Closing the training window owns the cancelled initialization terminal state.
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"アプリを開始できませんでした。\n{ex.Message}",
                "Kids Training",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            ExitAfterUnlock();
        }
    }

    private LearningPagePreparationResult PrepareLearningPage()
    {
        try
        {
            return learningPagePreparer.Prepare();
        }
        catch (Exception exception)
        {
            UpdateLogger.Error("Learning page preparation failed on the startup worker", exception);
            return LearningPagePreparationResult.Failed(exception.Message);
        }
    }

    private void ConfigureWebView(CoreWebView2 core)
    {
        core.Settings.AreBrowserAcceleratorKeysEnabled = false;
        core.Settings.AreDefaultContextMenusEnabled = false;
        core.Settings.AreDevToolsEnabled = false;
        core.Settings.AreHostObjectsAllowed = false;
        core.Settings.IsGeneralAutofillEnabled = false;
        core.Settings.IsPasswordAutosaveEnabled = false;
        core.Settings.IsStatusBarEnabled = false;
        core.Settings.IsZoomControlEnabled = false;

        core.WebMessageReceived += async (_, args) =>
        {
            var message = args.TryGetWebMessageAsString();
            if (string.Equals(message, UnlockMessage, StringComparison.Ordinal) ||
                string.Equals(message, PauseMessage, StringComparison.Ordinal))
            {
                ExitAfterUnlock();
                return;
            }

            if (message.StartsWith(ResetAppliedMessagePrefix, StringComparison.Ordinal) &&
                LearningResetModeValues.TryParse(message[ResetAppliedMessagePrefix.Length..], out var appliedMode) &&
                !parentLearningResetService.CompleteAppliedReset(appliedMode))
            {
                UpdateLogger.Info("A pending learning reset was applied, but its completion marker could not be cleared.");
            }

            if (message.StartsWith(LearningSettingsMessagePrefix, StringComparison.Ordinal))
            {
                try
                {
                    var payload = System.Text.Json.JsonSerializer.Deserialize<LearningSettingsMessage>(
                        message[LearningSettingsMessagePrefix.Length..],
                        new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
                    var result = parentLearningSettingsService.Update(
                        payload?.QuestionCount,
                        payload?.PassLine,
                        payload?.SchoolGrade,
                        payload?.PreferSchoolGrade);
                    if (!result.Success)
                    {
                        await SetLearningSessionSettingsAsync(result.Settings).ConfigureAwait(true);
                    }
                }
                catch (System.Text.Json.JsonException exception)
                {
                    UpdateLogger.Error("Could not parse learning settings from the protected parent screen", exception);
                }
            }
        };

        core.NewWindowRequested += (_, args) => args.Handled = true;
    }

    private void ApplyWindowIcon()
    {
        try
        {
            var icon = Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath);
            if (icon is not null)
            {
                Icon = icon;
            }
        }
        catch
        {
            // The executable icon is cosmetic; startup should not fail if extraction fails.
        }
    }

    private void OnTrainingFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (canExit)
        {
            return;
        }

        e.Cancel = true;
        EnforceLock();
    }

    private void ExitAfterUnlock()
    {
        canExit = true;
        initializationCancellation.Cancel();
        lockTimer.Stop();
        TopMost = false;
        Close();
    }

    private void EnforceLock()
    {
        if (canExit || IsDisposed)
        {
            return;
        }

        if (WindowState != FormWindowState.Maximized)
        {
            WindowState = FormWindowState.Maximized;
        }

        if (!TopMost)
        {
            TopMost = true;
        }

        BeginInvoke(new Action(() =>
        {
            if (!canExit && !IsDisposed)
            {
                Activate();
                webView.Focus();
            }
        }));
    }

    private bool IsLegacyStorageMigrationRequired()
    {
        try
        {
            return legacyMigrationStateStore.Read().ShouldAttempt(DateTimeOffset.UtcNow);
        }
        catch (Exception exception)
        {
            UpdateLogger.Error(
                "Could not determine whether legacy learning storage was migrated; a bounded migration attempt will be made",
                exception);
            return true;
        }
    }

    private static async Task<LegacyLearningStorageReadResult> ReadLegacyLearningStorageAsync(
        CoreWebView2 core,
        string legacyRuntimeUri,
        CancellationToken cancellationToken)
    {
        using var migrationCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        migrationCancellation.CancelAfter(LegacyMigrationBudget);
        try
        {
            await NavigateWithRetryAsync(
                core,
                legacyRuntimeUri,
                LegacyNavigationAttemptTimeout,
                migrationCancellation.Token);
            var serialized = await core.ExecuteScriptAsync(
                    "(() => { const data = {}; for (const key of ['kt_profiles_v1','kt_settings_v1','kt_muted_v1','kt_session_checkpoint_v1']) { const value = localStorage.getItem(key); if (value !== null) data[key] = value; } return data; })()")
                .WaitAsync(migrationCancellation.Token);
            var storage = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(serialized)
                ?? new Dictionary<string, string>(StringComparer.Ordinal);
            return LegacyLearningStorageReadResult.Succeeded(storage);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            UpdateLogger.Info(
                "Legacy file-origin learning-storage migration exceeded its startup budget; continuing with the secure learning page.");
            return LegacyLearningStorageReadResult.Failed;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            UpdateLogger.Error(
                "Could not read legacy file-origin learning storage; continuing with the secure learning page",
                exception);
            return LegacyLearningStorageReadResult.Failed;
        }
    }

    private static string BuildLegacyStorageMigrationScript(IReadOnlyDictionary<string, string> legacyStorage)
    {
        var serialized = System.Text.Json.JsonSerializer.Serialize(legacyStorage);
        return
            "(() => { const result = { success: false }; try { const legacy = " + serialized +
            "; for (const [key, value] of Object.entries(legacy)) { if (typeof value !== 'string') continue; " +
            "if (localStorage.getItem(key) === null) localStorage.setItem(key, value); " +
            "if (localStorage.getItem(key) === null) throw new Error('Legacy storage could not be preserved: ' + key); } " +
            "result.success = true; } catch (error) { result.error = String(error); } " +
            "window.__kidsTrainingLegacyStorageMigration = result; })();";
    }

    private async Task CompleteLegacyStorageMigrationAsync(
        CoreWebView2 core,
        CancellationToken cancellationToken)
    {
        try
        {
            var verified = await core.ExecuteScriptAsync(
                    "(() => window.__kidsTrainingLegacyStorageMigration?.success === true)()")
                .WaitAsync(MigrationVerificationTimeout, cancellationToken);
            if (!string.Equals(verified, "true", StringComparison.Ordinal))
            {
                UpdateLogger.Info(
                    "Legacy learning-storage migration could not be verified; it will be retried on a later startup.");
                DeferLegacyStorageMigration();
                return;
            }

            if (legacyMigrationStateStore.TryMarkCompleted())
            {
                UpdateLogger.Info(
                    "Legacy file-origin learning storage migration completed; later startups will skip file navigation.");
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            UpdateLogger.Error(
                "Could not verify legacy learning-storage migration; continuing with the secure learning page",
                exception);
            DeferLegacyStorageMigration();
        }
    }

    private void DeferLegacyStorageMigration()
    {
        var now = DateTimeOffset.UtcNow;
        if (!legacyMigrationStateStore.TryMarkDeferred(now))
        {
            UpdateLogger.Info("Legacy learning-storage migration retry state could not be saved.");
            return;
        }

        var retryAfter = legacyMigrationStateStore.Read().RetryAfterUtc;
        UpdateLogger.Info(
            $"Legacy learning-storage migration was deferred until {retryAfter?.ToString("O") ?? "a later startup"}.");
    }

    private static async Task NavigateWithRetryAsync(
        CoreWebView2 core,
        string uri,
        TimeSpan attemptTimeout,
        CancellationToken cancellationToken)
    {
        Exception? lastFailure = null;
        for (var attempt = 1; attempt <= NavigationMaxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await NavigateAsync(core, uri, attemptTimeout, cancellationToken);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                lastFailure = exception;
                if (attempt == NavigationMaxAttempts)
                {
                    break;
                }

                var backoff = TimeSpan.FromMilliseconds(
                    NavigationRetryInitialDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
                await Task.Delay(backoff, cancellationToken);
            }
        }

        throw new InvalidOperationException(
            $"WebView navigation failed after {NavigationMaxAttempts} bounded attempts: {uri}",
            lastFailure);
    }

    private static async Task NavigateAsync(
        CoreWebView2 core,
        string uri,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var completion = new TaskCompletionSource<CoreWebView2NavigationCompletedEventArgs>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        ulong? navigationId = null;
        EventHandler<CoreWebView2NavigationStartingEventArgs>? startingHandler = null;
        EventHandler<CoreWebView2NavigationCompletedEventArgs>? completedHandler = null;
        startingHandler = (_, args) => navigationId ??= args.NavigationId;
        completedHandler = (_, args) =>
        {
            if (navigationId is not null && args.NavigationId == navigationId.Value)
            {
                completion.TrySetResult(args);
            }
        };

        core.NavigationStarting += startingHandler;
        core.NavigationCompleted += completedHandler;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            core.Navigate(uri);
            var result = await completion.Task.WaitAsync(timeout, cancellationToken);
            if (!result.IsSuccess)
            {
                throw new InvalidOperationException(
                    $"WebView navigation failed with status {result.WebErrorStatus}: {uri}");
            }
        }
        catch (TimeoutException exception)
        {
            TryStopNavigation(core);
            throw new TimeoutException($"WebView navigation timed out after {timeout}: {uri}", exception);
        }
        catch (OperationCanceledException)
        {
            TryStopNavigation(core);
            throw;
        }
        finally
        {
            core.NavigationStarting -= startingHandler;
            core.NavigationCompleted -= completedHandler;
        }
    }

    private static void TryStopNavigation(CoreWebView2 core)
    {
        try
        {
            core.Stop();
        }
        catch (Exception exception)
        {
            UpdateLogger.Error("Could not stop a timed-out or cancelled WebView navigation", exception);
        }
    }

    private string BuildProfileStorageScript()
    {
        var profileName = System.Text.Json.JsonSerializer.Serialize(profileNameProvider.GetProfileName());
        var parentPin = System.Text.Json.JsonSerializer.Serialize(parentPinProvider.GetCurrentPin().Value);
        var learningSettings = parentLearningSettingsService.GetCurrentSettings();
        var pendingLearningReset = System.Text.Json.JsonSerializer.Serialize(
            parentLearningResetService.GetPendingReset().ToWireValue());
        return
            """
        (() => {
          window.__kidsTrainingHost = {
            profileName: __PROFILE_NAME__,
            parentPin: __PARENT_PIN__,
            questionCount: __QUESTION_COUNT__,
            passLine: __PASS_LINE__,
            schoolGrade: __SCHOOL_GRADE__,
            preferSchoolGrade: __PREFER_SCHOOL_GRADE__,
            pendingLearningReset: __PENDING_LEARNING_RESET__
          };
        })();
        """
            .Replace("__PROFILE_NAME__", profileName, StringComparison.Ordinal)
            .Replace("__PARENT_PIN__", parentPin, StringComparison.Ordinal)
            .Replace(
                "__QUESTION_COUNT__",
                learningSettings.QuestionCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                StringComparison.Ordinal)
            .Replace(
                "__PASS_LINE__",
                learningSettings.PassLine.ToString(System.Globalization.CultureInfo.InvariantCulture),
                StringComparison.Ordinal)
            .Replace(
                "__SCHOOL_GRADE__",
                learningSettings.SchoolGrade.ToString(System.Globalization.CultureInfo.InvariantCulture),
                StringComparison.Ordinal)
            .Replace(
                "__PREFER_SCHOOL_GRADE__",
                learningSettings.PreferSchoolGrade ? "true" : "false",
                StringComparison.Ordinal)
            .Replace("__PENDING_LEARNING_RESET__", pendingLearningReset, StringComparison.Ordinal);
    }

    private sealed record LearningSettingsMessage(
        int? QuestionCount,
        int? PassLine,
        int? SchoolGrade,
        bool? PreferSchoolGrade);

    private sealed record LegacyLearningStorageReadResult(
        bool IsSuccess,
        IReadOnlyDictionary<string, string> Storage)
    {
        public static LegacyLearningStorageReadResult Failed { get; } =
            new(false, new Dictionary<string, string>(StringComparer.Ordinal));

        public static LegacyLearningStorageReadResult Succeeded(
            IReadOnlyDictionary<string, string> storage) => new(true, storage);
    }
}
