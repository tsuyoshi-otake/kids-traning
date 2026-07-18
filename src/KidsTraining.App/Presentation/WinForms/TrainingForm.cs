using KidsTraining.App.Application.Learning;
using KidsTraining.App.Application.ParentControl;
using KidsTraining.App.Domain.ParentControl;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace KidsTraining.App.Presentation.WinForms;

internal sealed class TrainingForm : Form
{
    private const string UnlockMessage = "kidsTraining.unlock";

    private readonly WebView2 webView = new();
    private readonly System.Windows.Forms.Timer lockTimer = new();
    private readonly ILearningPagePreparer learningPagePreparer;
    private readonly IParentPinProvider parentPinProvider;
    private readonly IUserProfileNameProvider profileNameProvider;
    private readonly IParentLearningSettingsProvider parentLearningSettingsProvider;
    private bool canExit;
    private bool webViewInitialized;

    public TrainingForm(
        ILearningPagePreparer learningPagePreparer,
        IParentPinProvider parentPinProvider,
        IUserProfileNameProvider profileNameProvider,
        IParentLearningSettingsProvider parentLearningSettingsProvider)
    {
        this.learningPagePreparer = learningPagePreparer;
        this.parentPinProvider = parentPinProvider;
        this.profileNameProvider = profileNameProvider;
        this.parentLearningSettingsProvider = parentLearningSettingsProvider;
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

    public void ReturnToComputer()
    {
        ExitAfterUnlock();
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
              const next = { ...current, count: __QUESTION_COUNT__, pass: __PASS_LINE__ };
              localStorage.setItem(key, JSON.stringify(next));
              if (window.__kidsTrainingHost) {
                window.__kidsTrainingHost.questionCount = __QUESTION_COUNT__;
                window.__kidsTrainingHost.passLine = __PASS_LINE__;
              }
              return true;
            } catch {
              return false;
            }
            """
                .Replace("__QUESTION_COUNT__", settings.QuestionCount.ToString(), StringComparison.Ordinal)
                .Replace("__PASS_LINE__", settings.PassLine.ToString(), StringComparison.Ordinal);
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
            var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: AppPaths.WebViewUserDataFolder);
            await webView.EnsureCoreWebView2Async(environment);

            ConfigureWebView(webView.CoreWebView2);
            var preparation = learningPagePreparer.Prepare();
            if (!preparation.IsSuccess || preparation.RuntimePagePath is null)
            {
                throw new InvalidOperationException(
                    preparation.ErrorMessage ?? "Learning page preparation failed.");
            }

            await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(BuildProfileStorageScript());
            webView.CoreWebView2.Navigate(new Uri(preparation.RuntimePagePath).AbsoluteUri);
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

        core.WebMessageReceived += (_, args) =>
        {
            if (string.Equals(args.TryGetWebMessageAsString(), UnlockMessage, StringComparison.Ordinal))
            {
                ExitAfterUnlock();
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

    private string BuildProfileStorageScript()
    {
        var profileName = System.Text.Json.JsonSerializer.Serialize(profileNameProvider.GetProfileName());
        var parentPin = System.Text.Json.JsonSerializer.Serialize(parentPinProvider.GetCurrentPin().Value);
        var learningSettings = parentLearningSettingsProvider.GetCurrentSettings();
        return
            """
        (() => {
          window.__kidsTrainingHost = {
            profileName: __PROFILE_NAME__,
            parentPin: __PARENT_PIN__,
            questionCount: __QUESTION_COUNT__,
            passLine: __PASS_LINE__
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
                StringComparison.Ordinal);
    }
}
