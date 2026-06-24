using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace KidsTraining.App;

internal sealed class ParentControlServer : IDisposable
{
    public const int DefaultPort = 44567;

    private const int PortProbeCount = 10;
    private const int MaxRequestBodyBytes = 4096;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly Action startTraining;
    private readonly Action returnToComputer;
    private readonly Func<bool> isTrainingActive;
    private readonly Func<string?, string?, PasswordChangeResult> changeParentPassword;
    private readonly CancellationTokenSource stop = new();
    private readonly SemaphoreSlim connectionSlots = new(4, 4);
    private readonly TcpListener listener;

    private Task? acceptTask;

    public ParentControlServer(
        Action startTraining,
        Action returnToComputer,
        Func<bool> isTrainingActive,
        Func<string?, string?, PasswordChangeResult> changeParentPassword)
    {
        this.startTraining = startTraining;
        this.returnToComputer = returnToComputer;
        this.isTrainingActive = isTrainingActive;
        this.changeParentPassword = changeParentPassword;

        listener = StartListener(out var port);
        Port = port;
        NetworkUrls = GetNetworkUrls(port);
        PrimaryUrl = NetworkUrls.FirstOrDefault(static url => !url.Contains("127.0.0.1", StringComparison.Ordinal)) ??
            NetworkUrls[0];
    }

    public int Port { get; }

    public IReadOnlyList<string> NetworkUrls { get; }

    public string PrimaryUrl { get; }

    public void Start()
    {
        acceptTask ??= Task.Run(() => AcceptLoopAsync(stop.Token));
    }

    public void Dispose()
    {
        stop.Cancel();
        listener.Stop();
        connectionSlots.Dispose();
        stop.Dispose();
    }

    public static bool IsAllowedRemoteAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            var bytes = address.GetAddressBytes();
            return (bytes[0] & 0xfe) == 0xfc;
        }

        if (address.AddressFamily != AddressFamily.InterNetwork)
        {
            return false;
        }

        var octets = address.GetAddressBytes();
        return octets[0] == 10 ||
            octets[0] == 127 ||
            octets[0] == 169 && octets[1] == 254 ||
            octets[0] == 172 && octets[1] is >= 16 and <= 31 ||
            octets[0] == 192 && octets[1] == 168;
    }

    public static string BuildParentPage(IReadOnlyList<string> urls, bool trainingActive)
    {
        var urlItems = string.Join(
            "",
            urls.Select(static url => $"<li><code>{WebUtility.HtmlEncode(url)}</code></li>"));
        var initialStatus = trainingActive ? "起動中" : "停止中";

        return $$"""
<!doctype html>
<html lang="ja">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Kids Training 保護者画面</title>
  <style>
    :root { color-scheme: light; font-family: system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif; }
    body { margin: 0; min-height: 100vh; background: #f5f7fb; color: #20242c; }
    main { width: min(920px, calc(100% - 32px)); margin: 0 auto; padding: 32px 0; }
    header { display: flex; justify-content: space-between; gap: 16px; align-items: flex-start; margin-bottom: 24px; }
    h1 { margin: 0; font-size: 28px; line-height: 1.25; }
    .status { border: 2px solid #d9e2f5; background: #fff; border-radius: 8px; padding: 10px 14px; font-weight: 800; white-space: nowrap; }
    .panel { background: #fff; border: 2px solid #d9e2f5; border-radius: 8px; padding: 20px; margin-bottom: 16px; }
    .actions { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 14px; }
    button { border: 0; border-radius: 8px; padding: 18px; font-size: 20px; font-weight: 900; cursor: pointer; color: #fff; min-height: 68px; }
    button:disabled { cursor: not-allowed; opacity: .55; }
    .start { background: #f47b20; }
    .return { background: #218f55; }
    .refresh { background: #4f6fb7; font-size: 16px; min-height: 48px; padding: 12px 16px; }
    .message { min-height: 26px; margin-top: 14px; font-weight: 700; color: #4f5b70; }
    .fields { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 12px; align-items: end; }
    label { display: grid; gap: 6px; font-size: 14px; font-weight: 800; color: #4f5b70; }
    input { height: 44px; border: 2px solid #d9e2f5; border-radius: 8px; padding: 0 12px; font: inherit; font-size: 20px; letter-spacing: 0; }
    .save { background: #5d59b3; font-size: 17px; min-height: 48px; padding: 12px 16px; margin-top: 14px; }
    ul { margin: 10px 0 0; padding-left: 22px; }
    li { margin: 8px 0; }
    code { background: #eef3ff; border: 1px solid #d9e2f5; border-radius: 6px; padding: 3px 6px; word-break: break-all; }
    @media (max-width: 640px) {
      main { width: min(100% - 20px, 920px); padding: 18px 0; }
      header { display: block; }
      .status { margin-top: 14px; display: inline-block; }
      .actions { grid-template-columns: 1fr; }
      .fields { grid-template-columns: 1fr; }
      h1 { font-size: 24px; }
      button { font-size: 18px; }
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
        <button class="return" id="return" type="button">パソコンの画面に戻す</button>
      </div>
      <div class="message" id="message" aria-live="polite"></div>
    </section>
    <section class="panel">
      <button class="refresh" id="refresh" type="button">状態を更新</button>
      <ul>{{urlItems}}</ul>
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
    const currentPassword = document.getElementById('currentPassword');
    const newPassword = document.getElementById('newPassword');
    const confirmPassword = document.getElementById('confirmPassword');

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
    }

    async function action(path, text) {
      startButton.disabled = true;
      returnButton.disabled = true;
      message.textContent = '処理中...';
      try {
        await request(path, { method: 'POST' });
        message.textContent = text;
      } catch (error) {
        message.textContent = error.message || '操作に失敗しました';
      } finally {
        await refresh().catch(() => {});
        startButton.disabled = false;
      }
    }

    function cleanPin(input) {
      input.value = input.value.replace(/\D/g, '').slice(0, 4);
    }

    [currentPassword, newPassword, confirmPassword].forEach(input => {
      input.addEventListener('input', () => cleanPin(input));
    });

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
    returnButton.addEventListener('click', () => action('/api/return', 'パソコンの画面に戻しました'));
    document.getElementById('refresh').addEventListener('click', () => refresh().catch(error => { message.textContent = error.message; }));
    document.getElementById('savePassword').addEventListener('click', changePassword);
    refresh().catch(error => { message.textContent = error.message; });
  </script>
</body>
</html>
""";
    }

    private static TcpListener StartListener(out int port)
    {
        for (var candidatePort = DefaultPort; candidatePort < DefaultPort + PortProbeCount; candidatePort++)
        {
            var candidate = new TcpListener(IPAddress.Any, candidatePort);
            try
            {
                candidate.Server.ExclusiveAddressUse = true;
                candidate.Start(16);
                port = candidatePort;
                return candidate;
            }
            catch (SocketException)
            {
                candidate.Stop();
            }
        }

        throw new InvalidOperationException($"Parent control server could not bind ports {DefaultPort}-{DefaultPort + PortProbeCount - 1}.");
    }

    private static IReadOnlyList<string> GetNetworkUrls(int port)
    {
        var urls = new List<string> { $"http://127.0.0.1:{port}/" };
        foreach (var address in GetPrivateAddresses())
        {
            urls.Add($"http://{address}:{port}/");
        }

        return urls.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static IEnumerable<string> GetPrivateAddresses()
    {
        foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (networkInterface.OperationalStatus != OperationalStatus.Up ||
                networkInterface.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
            {
                continue;
            }

            foreach (var address in networkInterface.GetIPProperties().UnicastAddresses)
            {
                if (address.Address.AddressFamily != AddressFamily.InterNetwork ||
                    !IsAllowedRemoteAddress(address.Address) ||
                    IPAddress.IsLoopback(address.Address))
                {
                    continue;
                }

                yield return address.Address.ToString();
            }
        }
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var client = await listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                _ = Task.Run(() => HandleClientSafelyAsync(client, cancellationToken), CancellationToken.None);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                UpdateLogger.Error("Parent control accept loop failed", ex);
            }
        }
    }

    private async Task HandleClientSafelyAsync(TcpClient client, CancellationToken cancellationToken)
    {
        var slotTaken = false;
        try
        {
            await connectionSlots.WaitAsync(cancellationToken).ConfigureAwait(false);
            slotTaken = true;
            await HandleClientAsync(client, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Shutdown owns cancellation.
        }
        catch (Exception ex)
        {
            UpdateLogger.Error("Parent control request failed", ex);
        }
        finally
        {
            if (slotTaken)
            {
                connectionSlots.Release();
            }

            client.Dispose();
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        client.ReceiveTimeout = 5000;
        client.SendTimeout = 5000;

        var remoteAddress = ((IPEndPoint?)client.Client.RemoteEndPoint)?.Address;
        await using var stream = client.GetStream();
        var request = await ReadRequestAsync(stream, cancellationToken).ConfigureAwait(false);
        if (request is null)
        {
            return;
        }

        if (remoteAddress is null || !IsAllowedRemoteAddress(remoteAddress))
        {
            await WriteJsonAsync(stream, HttpStatusCode.Forbidden, new ApiResult(false, "このネットワークからはアクセスできません。", isTrainingActive()), cancellationToken).ConfigureAwait(false);
            return;
        }

        switch (request)
        {
            case { Method: "GET", Path: "/" or "/index.html" }:
                await WriteHtmlAsync(stream, BuildParentPage(NetworkUrls, isTrainingActive()), cancellationToken).ConfigureAwait(false);
                break;
            case { Method: "GET", Path: "/favicon.ico" }:
                await WriteResponseAsync(stream, HttpStatusCode.NoContent, "image/x-icon", "", cancellationToken).ConfigureAwait(false);
                break;
            case { Method: "GET", Path: "/api/status" }:
                await WriteJsonAsync(stream, HttpStatusCode.OK, new ApiResult(true, "OK", isTrainingActive()), cancellationToken).ConfigureAwait(false);
                break;
            case { Method: "POST", Path: "/api/start" }:
                startTraining();
                await WriteJsonAsync(stream, HttpStatusCode.OK, new ApiResult(true, "学習画面を起動しました。", true), cancellationToken).ConfigureAwait(false);
                break;
            case { Method: "POST", Path: "/api/return" }:
                returnToComputer();
                await WriteJsonAsync(stream, HttpStatusCode.OK, new ApiResult(true, "パソコンの画面に戻しました。", false), cancellationToken).ConfigureAwait(false);
                break;
            case { Method: "POST", Path: "/api/password" }:
                PasswordChangeRequest? payload;
                try
                {
                    payload = JsonSerializer.Deserialize<PasswordChangeRequest>(request.Body, JsonOptions);
                }
                catch (JsonException)
                {
                    await WriteJsonAsync(stream, HttpStatusCode.BadRequest, new ApiResult(false, "入力を読み取れませんでした。", isTrainingActive()), cancellationToken).ConfigureAwait(false);
                    break;
                }

                var changeResult = changeParentPassword(payload?.CurrentPassword, payload?.NewPassword);
                await WriteJsonAsync(
                    stream,
                    changeResult.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest,
                    new ApiResult(changeResult.Success, changeResult.Message, isTrainingActive()),
                    cancellationToken).ConfigureAwait(false);
                break;
            default:
                await WriteJsonAsync(stream, HttpStatusCode.NotFound, new ApiResult(false, "Not found.", isTrainingActive()), cancellationToken).ConfigureAwait(false);
                break;
        }
    }

    private static async Task<HttpRequest?> ReadRequestAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: true);
        var requestLine = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(requestLine))
        {
            return null;
        }

        var parts = requestLine.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return new HttpRequest("", "", "");
        }

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string? line;
        while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)))
        {
            var separator = line.IndexOf(':', StringComparison.Ordinal);
            if (separator <= 0)
            {
                continue;
            }

            headers[line[..separator].Trim()] = line[(separator + 1)..].Trim();
        }

        var body = "";
        if (headers.TryGetValue("Content-Length", out var contentLengthValue) &&
            int.TryParse(contentLengthValue, out var contentLength) &&
            contentLength > 0)
        {
            if (contentLength > MaxRequestBodyBytes)
            {
                body = "";
            }
            else
            {
                var buffer = new char[contentLength];
                var read = 0;
                while (read < contentLength)
                {
                    var count = await reader.ReadAsync(buffer.AsMemory(read, contentLength - read), cancellationToken).ConfigureAwait(false);
                    if (count == 0)
                    {
                        break;
                    }

                    read += count;
                }

                body = new string(buffer, 0, read);
            }
        }

        var target = parts[1];
        var path = Uri.TryCreate("http://localhost" + target, UriKind.Absolute, out var uri)
            ? uri.AbsolutePath
            : target;
        return new HttpRequest(parts[0].ToUpperInvariant(), path, body);
    }

    private static Task WriteHtmlAsync(NetworkStream stream, string html, CancellationToken cancellationToken) =>
        WriteResponseAsync(stream, HttpStatusCode.OK, "text/html; charset=utf-8", html, cancellationToken);

    private static Task WriteJsonAsync(NetworkStream stream, HttpStatusCode status, ApiResult result, CancellationToken cancellationToken) =>
        WriteResponseAsync(stream, status, "application/json; charset=utf-8", JsonSerializer.Serialize(result, JsonOptions), cancellationToken);

    private static async Task WriteResponseAsync(NetworkStream stream, HttpStatusCode status, string contentType, string body, CancellationToken cancellationToken)
    {
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        var header =
            $"HTTP/1.1 {(int)status} {status}\r\n" +
            $"Content-Type: {contentType}\r\n" +
            $"Content-Length: {bodyBytes.Length}\r\n" +
            "Cache-Control: no-store\r\n" +
            "Connection: close\r\n" +
            "X-Content-Type-Options: nosniff\r\n" +
            "\r\n";

        var headerBytes = Encoding.ASCII.GetBytes(header);
        await stream.WriteAsync(headerBytes, cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(bodyBytes, cancellationToken).ConfigureAwait(false);
    }

    private sealed record HttpRequest(string Method, string Path, string Body);

    private sealed record PasswordChangeRequest(string? CurrentPassword, string? NewPassword);

    private sealed record ApiResult(bool Ok, string Message, bool TrainingActive);
}
