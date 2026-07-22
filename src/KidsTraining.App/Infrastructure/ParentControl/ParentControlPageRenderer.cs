using System.Net;
using KidsTraining.App.Application.ParentControl;
using KidsTraining.App.Domain.ParentControl;

namespace KidsTraining.App.Infrastructure.ParentControl;

internal static class ParentControlPageRenderer
{
    public static string Build(IReadOnlyList<string> urls, bool trainingActive, LearningSessionSettings? learningSettings = null)
    {
        var urlItems = string.Join(
            "",
            urls.Select(static url => $"<li><code>{WebUtility.HtmlEncode(url)}</code></li>"));
        var initialStatus = trainingActive ? "起動中" : "停止中";
        var inactiveDisabled = trainingActive ? string.Empty : " disabled";
        var settings = learningSettings ?? LearningSessionSettings.Default;

        return $$"""
<!doctype html>
<html lang="ja">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Kids Training 保護者画面</title>
  <style>
    :root {
      color-scheme: light;
      font-family: system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
      --page: #f5f7fb;
      --panel: #fff;
      --ink: #20242c;
      --muted: #4f5b70;
      --border: #d9e2f5;
      --primary: #4f6fb7;
      --primary-dark: #3f5d9e;
      --focus: #2458c6;
      --danger: #b42318;
      --space-1: 4px;
      --space-2: 8px;
      --space-3: 12px;
      --space-4: 16px;
      --space-6: 24px;
      --radius: 8px;
    }
    body { margin: 0; min-height: 100vh; background: #f5f7fb; color: #20242c; }
    main { width: min(920px, calc(100% - 32px)); margin: 0 auto; padding: 32px 0; }
    header { display: flex; justify-content: space-between; gap: 16px; align-items: flex-start; margin-bottom: 24px; }
    h1 { margin: 0; font-size: 28px; line-height: 1.25; }
    .status { border: 2px solid #d9e2f5; background: #fff; border-radius: 8px; padding: 10px 14px; font-weight: 800; white-space: nowrap; }
    .panel { background: #fff; border: 2px solid #d9e2f5; border-radius: 8px; padding: 20px; margin-bottom: 16px; }
    .actions { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 14px; }
    button { border: 0; border-radius: 8px; padding: 18px; font-size: 20px; font-weight: 900; cursor: pointer; color: #fff; min-height: 68px; }
    button:disabled { cursor: not-allowed; opacity: .55; }
    .start { background: #bd4e0a; }
    .return { background: #5d6677; }
    .pause { background: #287e4d; }
    .refresh { background: #4f6fb7; font-size: 16px; min-height: 48px; padding: 12px 16px; }
    .message { min-height: 26px; margin-top: 14px; font-weight: 700; color: #4f5b70; }
    .fields { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 12px; align-items: end; }
    h2 { margin: 0 0 var(--space-2); font-size: 20px; line-height: 1.4; }
    .settings-copy { margin: 0 0 var(--space-4); color: var(--muted); line-height: 1.65; text-wrap: pretty; }
    .learning-fields { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: var(--space-4); }
    .field-help { color: #667085; font-size: 13px; font-weight: 600; line-height: 1.5; }
    .field-error { min-height: 22px; margin-top: var(--space-2); color: var(--danger); font-weight: 800; }
    .message:empty, .field-error:empty { display: none; }
    .save-settings { background: var(--primary); font-size: 17px; min-height: 48px; padding: 12px 16px; margin-top: var(--space-4); }
    input:focus-visible, button:focus-visible { outline: 3px solid var(--focus); outline-offset: 3px; }
    input[aria-invalid="true"] { border-color: var(--danger); }
    button { transition: background-color .2s ease, transform .2s ease, box-shadow .2s ease; }
    button:not(:disabled):hover { filter: brightness(.94); box-shadow: 0 4px 12px rgba(32, 36, 44, .16); transform: translateY(-1px); }
    button:not(:disabled):active { transform: translateY(1px) scale(.99); box-shadow: none; }
    label { display: grid; gap: 6px; font-size: 14px; font-weight: 800; color: #4f5b70; }
    input { height: 44px; border: 2px solid #d9e2f5; border-radius: 8px; padding: 0 12px; font: inherit; font-size: 20px; letter-spacing: 0; }
    .save { background: #5d59b3; font-size: 17px; min-height: 48px; padding: 12px 16px; margin-top: 14px; }
    .reset-actions { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 12px; margin-top: 14px; }
    .reset-history { background: #8b5a21; font-size: 17px; min-height: 54px; padding: 12px 16px; }
    .reset-full { background: #b42318; font-size: 17px; min-height: 54px; padding: 12px 16px; }
    ul { margin: 10px 0 0; padding-left: 22px; }
    li { margin: 8px 0; }
    code { background: #eef3ff; border: 1px solid #d9e2f5; border-radius: 6px; padding: 3px 6px; word-break: break-all; }
    @media (max-width: 640px) {
      main { width: min(100% - 20px, 920px); padding: 18px 0; }
      header { display: block; }
      .status { margin-top: 14px; display: inline-block; }
      .actions { grid-template-columns: 1fr; }
      .fields { grid-template-columns: 1fr; }
      .learning-fields { grid-template-columns: 1fr; }
      .reset-actions { grid-template-columns: 1fr; }
      h1 { font-size: 24px; }
      button { font-size: 18px; }
    }
    @media (prefers-reduced-motion: reduce) {
      *, *::before, *::after { scroll-behavior: auto !important; transition-duration: .01ms !important; }
    }
  </style>
</head>
<body>
  <main>
    <header>
      <div>
        <h1>Kids Training 保護者画面</h1>
      </div>
      <div class="status">学習画面: <span id="state">{{WebUtility.HtmlEncode(initialStatus)}}</span></div>
    </header>
    <section class="panel">
      <div class="actions">
        <button class="start" id="start" type="button">勉強を開始</button>
        <button class="pause" id="pause" type="button"{{inactiveDisabled}}>一時停止して戻る</button>
        <button class="return" id="return" type="button"{{inactiveDisabled}}>パソコンの画面に戻す</button>
      </div>
      <div class="message" id="message" aria-live="polite"></div>
    </section>
    <section class="panel" aria-labelledby="resetHeading">
      <h2 id="resetHeading">学習データのリセット</h2>
      <p class="settings-copy">操作には現在の保護者パスワードが必要です。学習画面が閉じているときは、次回起動時にリセットします。</p>
      <label for="resetPassword">いまのパスワード
        <input id="resetPassword" inputmode="numeric" autocomplete="current-password" maxlength="4" type="password" aria-describedby="resetMessage">
      </label>
      <div class="reset-actions">
        <button class="reset-history" id="resetHistory" type="button">履歴のみリセット<br><small>レベル・XP・星は維持</small></button>
        <button class="reset-full" id="resetFull" type="button">すべてリセット<br><small>レベル・XP・星も削除</small></button>
      </div>
      <div class="message" id="resetMessage" aria-live="polite"></div>
    </section>
    <section class="panel">
      <button class="refresh" id="refresh" type="button">状態を更新</button>
      <ul>{{urlItems}}</ul>
    </section>
    <section class="panel" aria-labelledby="learningSettingsHeading">
      <h2 id="learningSettingsHeading">1回の学習設定</h2>
      <p class="settings-copy">次に始める学習から使う出題数と合格点を設定します。</p>
      <div class="learning-fields">
        <label for="questionCount">1回の出題数
          <input id="questionCount" type="number" inputmode="numeric" min="10" max="30" step="1" required aria-describedby="questionCountHelp settingsError" value="{{settings.QuestionCount}}">
          <small class="field-help" id="questionCountHelp">10〜30問</small>
        </label>
        <label for="passLine">合格点
          <input id="passLine" type="number" inputmode="numeric" min="1" max="{{settings.QuestionCount}}" step="1" required aria-describedby="passLineHelp settingsError" value="{{settings.PassLine}}">
          <small class="field-help" id="passLineHelp">1点以上、出題数以下</small>
        </label>
      </div>
      <button class="save-settings" id="saveLearningSettings" type="button">学習設定を保存</button>
      <div class="field-error" id="settingsError" role="alert"></div>
      <div class="message" id="settingsMessage" aria-live="polite"></div>
    </section>
    <section class="panel">
      <div class="fields">
        <label>いまのパスワード
          <input id="currentPassword" inputmode="numeric" autocomplete="current-password" maxlength="4" type="password">
        </label>
        <label>新しいパスワード
          <input id="newPassword" inputmode="numeric" autocomplete="new-password" maxlength="4" type="password">
        </label>
        <label>もう一度
          <input id="confirmPassword" inputmode="numeric" autocomplete="new-password" maxlength="4" type="password">
        </label>
      </div>
      <button class="save" id="savePassword" type="button">パスワードを変更</button>
      <div class="message" id="passwordMessage" aria-live="polite"></div>
    </section>
  </main>
  <script>
    const state = document.getElementById('state');
    const message = document.getElementById('message');
    const passwordMessage = document.getElementById('passwordMessage');
    const startButton = document.getElementById('start');
    const returnButton = document.getElementById('return');
    const pauseButton = document.getElementById('pause');
    const currentPassword = document.getElementById('currentPassword');
    const newPassword = document.getElementById('newPassword');
    const confirmPassword = document.getElementById('confirmPassword');
    const questionCount = document.getElementById('questionCount');
    const passLine = document.getElementById('passLine');
    const settingsError = document.getElementById('settingsError');
    const settingsMessage = document.getElementById('settingsMessage');
    const saveLearningSettingsButton = document.getElementById('saveLearningSettings');
    const resetPassword = document.getElementById('resetPassword');
    const resetMessage = document.getElementById('resetMessage');
    const resetHistoryButton = document.getElementById('resetHistory');
    const resetFullButton = document.getElementById('resetFull');

    async function request(path, options) {
      const response = await fetch(path, options);
      const data = await response.json();
      if (!response.ok || !data.ok) {
        throw new Error(data.message || '操作に失敗しました');
      }
      return data;
    }

    async function refresh() {
      const data = await request('/api/status', { cache: 'no-store' });
      state.textContent = data.trainingActive ? '起動中' : '停止中';
      returnButton.disabled = !data.trainingActive;
      pauseButton.disabled = !data.trainingActive;
      questionCount.value = data.questionCount;
      passLine.value = data.passLine;
      passLine.max = data.questionCount;
    }

    async function action(path, text) {
      const actionButton = path === '/api/start' ? startButton : path === '/api/pause' ? pauseButton : returnButton;
      const actionLabel = actionButton.textContent;
      const wasActive = !returnButton.disabled;
      let nextActive = wasActive;
      startButton.disabled = true;
      returnButton.disabled = true;
      pauseButton.disabled = true;
      actionButton.textContent = '処理中...';
      message.textContent = '処理中...';
      try {
        await request(path, { method: 'POST' });
        nextActive = path === '/api/start';
        message.textContent = text;
      } catch (error) {
        message.textContent = error.message || '操作に失敗しました';
      } finally {
        actionButton.textContent = actionLabel;
        startButton.disabled = false;
        try {
          await refresh();
        } catch {
          returnButton.disabled = !nextActive;
          pauseButton.disabled = !nextActive;
        }
      }
    }

    function showSettingsError(text, fieldId = '') {
      settingsError.textContent = text;
      const invalid = Boolean(text);
      questionCount.setAttribute('aria-invalid', String(invalid && fieldId === 'questionCount'));
      passLine.setAttribute('aria-invalid', String(invalid && fieldId === 'passLine'));
      if (invalid && fieldId) {
        document.getElementById(fieldId)?.focus();
      }
    }

    function cleanLearningSettings() {
      const count = Number(questionCount.value);
      const pass = Number(passLine.value);
      passLine.max = Number.isInteger(count) ? String(count) : '30';
      if (!Number.isInteger(count) || count < 10 || count > 30) {
        return { error: '1回の出題数は10〜30問にしてください。', fieldId: 'questionCount' };
      }
      if (!Number.isInteger(pass) || pass < 1 || pass > count) {
        return { error: '合格点は1点以上、出題数以下にしてください。', fieldId: 'passLine' };
      }
      return { questionCount: count, passLine: pass };
    }

    async function saveLearningSettings() {
      const values = cleanLearningSettings();
      settingsMessage.textContent = '';
      showSettingsError(values.error || '', values.fieldId || '');
      if (values.error) {
        return;
      }

      saveLearningSettingsButton.disabled = true;
      saveLearningSettingsButton.textContent = '保存中...';
      try {
        const data = await request('/api/settings', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(values)
        });
        questionCount.value = data.questionCount;
        passLine.value = data.passLine;
        passLine.max = data.questionCount;
        settingsMessage.textContent = data.message;
      } catch (error) {
        showSettingsError(error.message || '学習設定を保存できませんでした。');
      } finally {
        saveLearningSettingsButton.disabled = false;
        saveLearningSettingsButton.textContent = '学習設定を保存';
      }
    }

    function cleanPin(input) {
      input.value = input.value.replace(/\D/g, '').slice(0, 4);
    }

    [currentPassword, newPassword, confirmPassword].forEach(input => {
      input.addEventListener('input', () => cleanPin(input));
    });
    resetPassword.addEventListener('input', () => {
      cleanPin(resetPassword);
      resetPassword.setAttribute('aria-invalid', 'false');
      resetMessage.textContent = '';
    });

    async function resetLearning(mode) {
      cleanPin(resetPassword);
      if (resetPassword.value.length !== 4) {
        resetPassword.setAttribute('aria-invalid', 'true');
        resetMessage.textContent = 'いまのパスワードを4桁で入力してください';
        resetPassword.focus();
        return;
      }
      const historyOnly = mode === 'history';
      const warning = historyOnly
        ? '習熟度・復習予定・クリア履歴をリセットします。レベル・XP・星は残します。続けますか？'
        : 'レベル・XP・星を含むすべての学習データをリセットします。この操作は取り消せません。続けますか？';
      if (!window.confirm(warning)) return;

      resetPassword.setAttribute('aria-invalid', 'false');
      resetHistoryButton.disabled = true;
      resetFullButton.disabled = true;
      resetMessage.textContent = '処理中...';
      try {
        const data = await request('/api/reset', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ currentPassword: resetPassword.value, mode })
        });
        resetMessage.textContent = data.message;
        resetPassword.value = '';
      } catch (error) {
        resetPassword.setAttribute('aria-invalid', 'true');
        resetMessage.textContent = error.message || 'リセットできませんでした';
      } finally {
        resetHistoryButton.disabled = false;
        resetFullButton.disabled = false;
      }
    }

    async function changePassword() {
      cleanPin(currentPassword);
      cleanPin(newPassword);
      cleanPin(confirmPassword);
      if (newPassword.value.length !== 4) {
        passwordMessage.textContent = '新しいパスワードは4桁の数字にしてください';
        return;
      }
      if (newPassword.value !== confirmPassword.value) {
        passwordMessage.textContent = '新しいパスワードが一致しません';
        return;
      }

      passwordMessage.textContent = '保存中...';
      try {
        const data = await request('/api/password', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ currentPassword: currentPassword.value, newPassword: newPassword.value })
        });
        passwordMessage.textContent = data.message;
        currentPassword.value = '';
        newPassword.value = '';
        confirmPassword.value = '';
      } catch (error) {
        passwordMessage.textContent = error.message || '保存に失敗しました';
      }
    }

    startButton.addEventListener('click', () => action('/api/start', '勉強画面を起動しました'));
    pauseButton.addEventListener('click', () => action('/api/pause', '学習を一時停止してパソコンの画面に戻しました'));
    returnButton.addEventListener('click', () => action('/api/return', 'パソコンの画面に戻しました'));
    document.getElementById('refresh').addEventListener('click', () => refresh().catch(error => { message.textContent = error.message; }));
    document.getElementById('savePassword').addEventListener('click', changePassword);
    saveLearningSettingsButton.addEventListener('click', saveLearningSettings);
    resetHistoryButton.addEventListener('click', () => resetLearning('history'));
    resetFullButton.addEventListener('click', () => resetLearning('full'));
    questionCount.addEventListener('input', () => { showSettingsError(''); cleanLearningSettings(); });
    passLine.addEventListener('input', () => showSettingsError(''));
    refresh().catch(error => { message.textContent = error.message; });
  </script>
</body>
</html>
""";
    }
}
