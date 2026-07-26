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

    private readonly WebView2 webView = new();
    private readonly System.Windows.Forms.Timer lockTimer = new();
    private readonly ILearningPagePreparer learningPagePreparer;
    private readonly IParentPinProvider parentPinProvider;
    private readonly IUserProfileNameProvider profileNameProvider;
    private readonly ParentLearningSettingsService parentLearningSettingsService;
    private readonly ParentLearningResetService parentLearningResetService;
    private bool canExit;
    private bool webViewInitialized;

    public TrainingForm(
        ILearningPagePreparer learningPagePreparer,
        IParentPinProvider parentPinProvider,
        IUserProfileNameProvider profileNameProvider,
        ParentLearningSettingsService parentLearningSettingsService,
        ParentLearningResetService parentLearningResetService)
    {
        this.learningPagePreparer = learningPagePreparer;
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

        Load += async (_, _) => await InitializeWebViewAsync();
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

    private async Task InitializeWebViewAsync()
    {
        if (webViewInitialized)
        {
            return;
        }

        webViewInitialized = true;

        try
        {
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
            var preparation = learningPagePreparer.Prepare();
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
            var legacyStorage = await ReadLegacyLearningStorageAsync(
                webView.CoreWebView2,
                new Uri(preparation.RuntimePagePath).AbsoluteUri);
            await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
                BuildLegacyStorageMigrationScript(legacyStorage));
            var runtimePageName = Uri.EscapeDataString(Path.GetFileName(preparation.RuntimePagePath));
            await NavigateAsync(
                webView.CoreWebView2,
                $"https://{LearningVirtualHostName}/{runtimePageName}");
            webView.Visible = true;
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
        core.PermissionRequested += (_, args) =>
        {
            var isTrustedLearningPage =
                Uri.TryCreate(args.Uri, UriKind.Absolute, out var origin) &&
                string.Equals(origin.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(origin.Host, LearningVirtualHostName, StringComparison.OrdinalIgnoreCase);
            var allowCamera =
                args.PermissionKind == CoreWebView2PermissionKind.Camera &&
                args.IsUserInitiated &&
                isTrustedLearningPage;

            args.SavesInProfile = false;
            args.State = allowCamera
                ? CoreWebView2PermissionState.Allow
                : CoreWebView2PermissionState.Deny;
            args.Handled = true;
        };
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

    private static async Task<IReadOnlyDictionary<string, string>> ReadLegacyLearningStorageAsync(
        CoreWebView2 core,
        string legacyRuntimeUri)
    {
        try
        {
            await NavigateAsync(core, legacyRuntimeUri);
            var serialized = await core.ExecuteScriptAsync(
                "(() => { const data = {}; for (const key of ['kt_profiles_v1','kt_settings_v1','kt_muted_v1','kt_session_checkpoint_v1']) { const value = localStorage.getItem(key); if (value !== null) data[key] = value; } return data; })()");
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(serialized)
                ?? new Dictionary<string, string>(StringComparer.Ordinal);
        }
        catch (Exception exception)
        {
            UpdateLogger.Error("Could not read legacy file-origin learning storage", exception);
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }
    }

    private static string BuildLegacyStorageMigrationScript(IReadOnlyDictionary<string, string> legacyStorage)
    {
        var serialized = System.Text.Json.JsonSerializer.Serialize(legacyStorage);
        return
            "(() => { try { const legacy = " + serialized +
            "; for (const [key, value] of Object.entries(legacy)) { if (localStorage.getItem(key) === null && typeof value === 'string') localStorage.setItem(key, value); } } catch {} })();";
    }

    private static Task NavigateAsync(CoreWebView2 core, string uri)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler<CoreWebView2NavigationCompletedEventArgs>? handler = null;
        handler = (_, args) =>
        {
            core.NavigationCompleted -= handler;
            if (args.IsSuccess)
            {
                completion.TrySetResult();
            }
            else
            {
                completion.TrySetException(new InvalidOperationException(
                    $"WebView navigation failed with status {args.WebErrorStatus}: {uri}"));
            }
        };
        core.NavigationCompleted += handler;
        try
        {
            core.Navigate(uri);
        }
        catch (Exception exception)
        {
            core.NavigationCompleted -= handler;
            completion.TrySetException(exception);
        }

        return completion.Task;
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
}
