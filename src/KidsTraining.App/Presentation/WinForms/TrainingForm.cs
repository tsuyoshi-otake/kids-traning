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

    public void SetParentPassword(string password)
    {
        if (webView.CoreWebView2 is null)
        {
            return;
        }

        var encodedPassword = System.Text.Json.JsonSerializer.Serialize(password);
        _ = webView.CoreWebView2.ExecuteScriptAsync(
            $"try {{ localStorage.setItem('kt_parent_pin_v1', {encodedPassword}); }} catch {{ }}");
    }

    public void SetLearningSessionSettings(LearningSessionSettings settings)
    {
        if (webView.CoreWebView2 is null)
        {
            return;
        }

        var script =
            """
            try {
              const key = 'kt_settings_v1';
              const raw = localStorage.getItem(key);
              const current = raw ? JSON.parse(raw) : {};
              const next = { ...current, count: __QUESTION_COUNT__, pass: __PASS_LINE__ };
              localStorage.setItem(key, JSON.stringify(next));
            } catch {}
            """
                .Replace("__QUESTION_COUNT__", settings.QuestionCount.ToString(), StringComparison.Ordinal)
                .Replace("__PASS_LINE__", settings.PassLine.ToString(), StringComparison.Ordinal);
        _ = webView.CoreWebView2.ExecuteScriptAsync(script);
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
            await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(CompletionBridgeScript);
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
        var parentPassword = System.Text.Json.JsonSerializer.Serialize(parentPinProvider.GetCurrentPin().Value);
        var learningSettings = parentLearningSettingsProvider.GetCurrentSettings();
        return
            """
        (() => {
          const key = 'kt_profiles_v1';
          const settingsKey = 'kt_settings_v1';
          const parentPinKey = 'kt_parent_pin_v1';
          const profileName = __PROFILE_NAME__;
          const parentPassword = __PARENT_PASSWORD__;
          const parentQuestionCount = __QUESTION_COUNT__;
          const parentPassLine = __PASS_LINE__;
          const masteryKeys = ['add', 'sub', 'mul', 'clock', 'kokugo', 'hissan', 'moji', 'measure', 'kazu', 'shape', 'div', 'frac', 'chart', 'story', 'bun', 'goi', 'dokkai', 'eigo', 'money', 'groups', 'order'];
          const beginnerMastery = { add: .05, sub: .05, mul: .05, clock: .05, kokugo: .05, hissan: .05, moji: .05, measure: .05, kazu: .05, shape: .05, div: .05, frac: .05, chart: .05, story: .05, bun: .05, goi: .05, dokkai: .05, eigo: .05, money: .05, groups: .05, order: .05 };
          const beginnerSettings = {
            count: 20,
            pass: 15,
            topics: { add: true, sub: true, mul: true, clock: true, kokugo: true, hissan: true, moji: true, measure: true, kazu: true, shape: true, div: true, frac: true, chart: true, story: true, bun: true, goi: true, dokkai: true, eigo: true, money: true, groups: true, order: true }
          };
          const defaultProfile = {
            name: profileName,
            grade: 1,
            color: '#4ad991',
            streak: 0,
            stars: 0,
            xp: 0,
            mastery: { ...beginnerMastery }
          };

          const numberOrDefault = (value, fallback) => {
            const number = Number(value);
            return Number.isFinite(number) ? number : fallback;
          };

          const isDefaultishMastery = mastery => masteryKeys.every(key => {
            const value = Number(mastery && mastery[key]);
            return !Number.isFinite(value) ||
              Math.abs(value - .5) < .001 ||
              Math.abs(value - beginnerMastery[key]) < .001;
          });

          const hasMeaningfulProgress = profile =>
            numberOrDefault(profile.stars, 0) > 0 ||
            numberOrDefault(profile.streak, 0) > 0 ||
            numberOrDefault(profile.xp, 0) > 0 ||
            !isDefaultishMastery(profile.mastery);

          const normalizeProfile = source => {
            const profile = source && typeof source === 'object' ? source : {};
            const mastery = profile.mastery && typeof profile.mastery === 'object' ? profile.mastery : {};
            const resetToBeginner = !hasMeaningfulProgress(profile) && !profile.progressResetAt;
            return {
              ...defaultProfile,
              ...profile,
              name: profileName,
              grade: resetToBeginner ? 1 : numberOrDefault(profile.grade, defaultProfile.grade),
              streak: numberOrDefault(profile.streak, defaultProfile.streak),
              stars: numberOrDefault(profile.stars, defaultProfile.stars),
              xp: numberOrDefault(profile.xp, defaultProfile.xp),
              color: profile.color || defaultProfile.color,
              mastery: resetToBeginner ? { ...beginnerMastery } : { ...defaultProfile.mastery, ...mastery }
            };
          };

          try {
            localStorage.setItem(parentPinKey, parentPassword);
            const raw = localStorage.getItem(key);
            const parsed = raw ? JSON.parse(raw) : null;
            const source = Array.isArray(parsed) && parsed.length ? parsed[0] : parsed;
            const normalized = normalizeProfile(source);
            localStorage.setItem(key, JSON.stringify([normalized]));
            const rawSettings = localStorage.getItem(settingsKey);
            let parsedSettings = null;
            try { parsedSettings = rawSettings ? JSON.parse(rawSettings) : null; } catch {}
            const sourceSettings = parsedSettings && typeof parsedSettings === 'object' ? parsedSettings : {};
            const previousCount = numberOrDefault(sourceSettings.count, beginnerSettings.count);
            const migratedFromTen = previousCount <= 10;
            const normalizedSettings = {
              ...beginnerSettings,
              ...sourceSettings,
              topics: { ...beginnerSettings.topics, ...(sourceSettings.topics || {}) },
              count: Math.max(20, Math.min(40, parentQuestionCount)),
              pass: Math.max(1, Math.min(parentPassLine, Math.max(20, Math.min(40, parentQuestionCount))))
            };
            if (!hasMeaningfulProgress(normalized)) {
              normalizedSettings.count = parentQuestionCount;
              normalizedSettings.pass = parentPassLine;
            }
            localStorage.setItem(settingsKey, JSON.stringify(normalizedSettings));
          } catch {
            try {
              localStorage.setItem(parentPinKey, parentPassword);
              localStorage.setItem(key, JSON.stringify([defaultProfile]));
              localStorage.setItem(settingsKey, JSON.stringify({ ...beginnerSettings, count: parentQuestionCount, pass: parentPassLine }));
            } catch {}
          }
        })();
        """
            .Replace("__PROFILE_NAME__", profileName, StringComparison.Ordinal)
            .Replace("__PARENT_PASSWORD__", parentPassword, StringComparison.Ordinal)
            .Replace("__QUESTION_COUNT__", learningSettings.QuestionCount.ToString(), StringComparison.Ordinal)
            .Replace("__PASS_LINE__", learningSettings.PassLine.ToString(), StringComparison.Ordinal);
    }

    private const string CompletionBridgeScript =
        """
        (() => {
          const unlockMessage = 'kidsTraining.unlock';
          const pcText = '\u30d1\u30bd\u30b3\u30f3\u3092';
          const useText = '\u3064\u304b\u3046';
          let posted = false;

          document.addEventListener('click', event => {
            if (posted || !window.chrome || !window.chrome.webview) {
              return;
            }

            const target = event.target;
            const element = target && target.closest
              ? target.closest('button, a, div, span, [onclick]')
              : target;

            if (!element) {
              return;
            }

            const text = (element.innerText || element.textContent || '').replace(/\s+/g, ' ').trim();
            if (text.includes(pcText) && text.includes(useText)) {
              posted = true;
              window.chrome.webview.postMessage(unlockMessage);
            }
          }, true);
        })();
        """;
}
