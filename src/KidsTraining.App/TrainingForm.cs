using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace KidsTraining.App;

internal sealed class TrainingForm : Form
{
    private const string UnlockMessage = "kidsTraining.unlock";

    private readonly WebView2 webView = new();
    private readonly System.Windows.Forms.Timer lockTimer = new();
    private bool canExit;
    private bool webViewInitialized;

    public TrainingForm()
    {
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

            if (!File.Exists(AppPaths.HtmlPath))
            {
                MessageBox.Show(
                    $"学習HTMLが見つかりません。\n{AppPaths.HtmlPath}",
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
            var runtimeHtmlPath = RuntimeHtmlPreparer.Prepare();
            await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(BuildProfileStorageScript());
            await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(CompletionBridgeScript);
            webView.CoreWebView2.Navigate(new Uri(runtimeHtmlPath).AbsoluteUri);
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
            var icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
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

    private static string BuildProfileStorageScript()
    {
        var profileName = System.Text.Json.JsonSerializer.Serialize(RuntimeHtmlPreparer.PrimaryProfileName);
        return
            """
        (() => {
          const key = 'kt_profiles_v1';
          const settingsKey = 'kt_settings_v1';
          const profileName = __PROFILE_NAME__;
          const masteryKeys = ['add', 'sub', 'mul', 'clock', 'kokugo', 'hissan', 'moji'];
          const beginnerMastery = { add: .05, sub: .05, mul: .05, clock: .05, kokugo: .05, hissan: .05, moji: .05 };
          const defaultAvatar = { face: 'smile', clothes: 'red', accessory: 'none' };
          const beginnerSettings = {
            count: 10,
            pass: 8,
            topics: { add: true, sub: true, mul: true, clock: true, kokugo: true, hissan: true, moji: true }
          };
          const defaultProfile = {
            name: profileName,
            grade: 1,
            color: '#4ad991',
            streak: 0,
            stars: 0,
            xp: 0,
            avatarReady: false,
            avatar: { ...defaultAvatar },
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

          const normalizeAvatar = avatar => ({
            ...defaultAvatar,
            ...(avatar && typeof avatar === 'object' ? avatar : {})
          });

          const normalizeProfile = source => {
            const profile = source && typeof source === 'object' ? source : {};
            const mastery = profile.mastery && typeof profile.mastery === 'object' ? profile.mastery : {};
            const resetToBeginner = !hasMeaningfulProgress(profile);
            return {
              ...defaultProfile,
              ...profile,
              name: profileName,
              grade: resetToBeginner ? 1 : numberOrDefault(profile.grade, defaultProfile.grade),
              streak: numberOrDefault(profile.streak, defaultProfile.streak),
              stars: numberOrDefault(profile.stars, defaultProfile.stars),
              xp: numberOrDefault(profile.xp, defaultProfile.xp),
              avatarReady: profile.avatarReady === true,
              avatar: normalizeAvatar(profile.avatar),
              color: profile.color || defaultProfile.color,
              mastery: resetToBeginner ? { ...beginnerMastery } : { ...defaultProfile.mastery, ...mastery }
            };
          };

          try {
            const raw = localStorage.getItem(key);
            const parsed = raw ? JSON.parse(raw) : null;
            const source = Array.isArray(parsed) && parsed.length ? parsed[0] : parsed;
            const normalized = normalizeProfile(source);
            localStorage.setItem(key, JSON.stringify([normalized]));
            if (!localStorage.getItem(settingsKey) || !hasMeaningfulProgress(normalized)) {
              localStorage.setItem(settingsKey, JSON.stringify(beginnerSettings));
            }
          } catch {
            try {
              localStorage.setItem(key, JSON.stringify([defaultProfile]));
              localStorage.setItem(settingsKey, JSON.stringify(beginnerSettings));
            } catch {}
          }
        })();
        """.Replace("__PROFILE_NAME__", profileName, StringComparison.Ordinal);
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
