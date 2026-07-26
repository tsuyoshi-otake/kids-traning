using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using KidsTraining.App.Application.ParentControl;
using KidsTraining.App.Domain.ParentControl;

namespace KidsTraining.App.Infrastructure.ParentControl;

internal sealed class ParentControlServer : IDisposable, IAsyncDisposable
{
    public const int DefaultPort = 44567;

    private const int PortProbeCount = 10;
    private const int MaxRequestBodyBytes = 4096;
    private const int MaxActiveClients = 4;
    private const int ListenBacklog = MaxActiveClients;
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan TimeoutResponseWriteLimit = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan InitialAcceptErrorDelay = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan MaximumAcceptErrorDelay = TimeSpan.FromSeconds(3);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly Func<CancellationToken, Task<bool>> startTraining;
    private readonly Func<CancellationToken, Task<bool>> returnToComputer;
    private readonly Func<CancellationToken, Task<bool>> pauseTraining;
    private readonly Func<bool> isTrainingActive;
    private readonly Func<string?, string?, CancellationToken, Task<PasswordChangeResult>> changeParentPassword;
    private readonly Func<LearningSessionSettings> getLearningSettings;
    private readonly Func<int?, int?, int?, bool?, CancellationToken, Task<LearningSessionSettingsUpdateResult>> changeLearningSettings;
    private readonly Func<string?, string?, CancellationToken, Task<LearningResetResult>> requestLearningReset;
    private readonly object lifecycleGate = new();
    private readonly object clientTasksGate = new();
    private readonly CancellationTokenSource stop = new();
    private readonly SemaphoreSlim connectionSlots = new(MaxActiveClients, MaxActiveClients);
    private readonly HashSet<Task> clientTasks = [];

    private TcpListener? listener;
    private Task? acceptTask;
    private Task? shutdownTask;

    public ParentControlServer(
        Func<CancellationToken, Task<bool>> startTraining,
        Func<CancellationToken, Task<bool>> returnToComputer,
        Func<CancellationToken, Task<bool>> pauseTraining,
        Func<bool> isTrainingActive,
        Func<string?, string?, CancellationToken, Task<PasswordChangeResult>> changeParentPassword,
        Func<LearningSessionSettings> getLearningSettings,
        Func<int?, int?, int?, bool?, CancellationToken, Task<LearningSessionSettingsUpdateResult>> changeLearningSettings,
        Func<string?, string?, CancellationToken, Task<LearningResetResult>> requestLearningReset)
    {
        ArgumentNullException.ThrowIfNull(startTraining);
        ArgumentNullException.ThrowIfNull(returnToComputer);
        ArgumentNullException.ThrowIfNull(pauseTraining);
        ArgumentNullException.ThrowIfNull(isTrainingActive);
        ArgumentNullException.ThrowIfNull(changeParentPassword);
        ArgumentNullException.ThrowIfNull(getLearningSettings);
        ArgumentNullException.ThrowIfNull(changeLearningSettings);
        ArgumentNullException.ThrowIfNull(requestLearningReset);

        this.startTraining = startTraining;
        this.returnToComputer = returnToComputer;
        this.pauseTraining = pauseTraining;
        this.isTrainingActive = isTrainingActive;
        this.changeParentPassword = changeParentPassword;
        this.getLearningSettings = getLearningSettings;
        this.changeLearningSettings = changeLearningSettings;
        this.requestLearningReset = requestLearningReset;
    }

    public int Port { get; private set; }

    public IReadOnlyList<string> NetworkUrls { get; private set; } = Array.Empty<string>();

    public string PrimaryUrl { get; private set; } = string.Empty;

    public void Start()
    {
        lock (lifecycleGate)
        {
            ObjectDisposedException.ThrowIf(shutdownTask is not null, this);
            if (acceptTask is not null)
            {
                return;
            }

            var startedListener = StartListener(out var port);
            try
            {
                var urls = GetNetworkUrls(port);
                listener = startedListener;
                Port = port;
                NetworkUrls = urls;
                PrimaryUrl = urls.FirstOrDefault(static url => !url.Contains("127.0.0.1", StringComparison.Ordinal)) ??
                    urls[0];
                acceptTask = AcceptLoopAsync(startedListener, stop.Token);
            }
            catch
            {
                startedListener.Stop();
                listener = null;
                throw;
            }
        }
    }

    public void Dispose()
    {
        GetOrCreateShutdownTask().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await GetOrCreateShutdownTask().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    private Task GetOrCreateShutdownTask()
    {
        lock (lifecycleGate)
        {
            if (shutdownTask is not null)
            {
                return shutdownTask;
            }

            stop.Cancel();
            listener?.Stop();
            shutdownTask = ShutdownCoreAsync(acceptTask);
            return shutdownTask;
        }
    }

    private async Task ShutdownCoreAsync(Task? activeAcceptTask)
    {
        if (activeAcceptTask is not null)
        {
            try
            {
                await activeAcceptTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                UpdateLogger.Error("Parent control accept loop did not stop cleanly", ex);
            }
        }

        Task[] pendingClientTasks;
        lock (clientTasksGate)
        {
            pendingClientTasks = [.. clientTasks];
        }

        try
        {
            await Task.WhenAll(pendingClientTasks).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            UpdateLogger.Error("Parent control requests did not drain cleanly", ex);
        }
        finally
        {
            connectionSlots.Dispose();
            stop.Dispose();
        }
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

    public static string BuildParentPage(IReadOnlyList<string> urls, bool trainingActive, LearningSessionSettings? learningSettings = null) =>
        ParentControlPageRenderer.Build(urls, trainingActive, learningSettings);


    private static TcpListener StartListener(out int port)
    {
        for (var candidatePort = DefaultPort; candidatePort < DefaultPort + PortProbeCount; candidatePort++)
        {
            var candidate = new TcpListener(IPAddress.Any, candidatePort);
            try
            {
                candidate.Server.ExclusiveAddressUse = true;
                candidate.Start(ListenBacklog);
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

    private async Task AcceptLoopAsync(TcpListener activeListener, CancellationToken cancellationToken)
    {
        var consecutiveFailures = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            var slotTaken = false;
            Exception? acceptFailure = null;
            try
            {
                await connectionSlots.WaitAsync(cancellationToken).ConfigureAwait(false);
                slotTaken = true;

                var client = await activeListener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                consecutiveFailures = 0;
                var clientTask = HandleClientSafelyAsync(client, cancellationToken);
                TrackClientTask(clientTask);
                slotTaken = false;
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (SocketException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                acceptFailure = ex;
                consecutiveFailures++;
            }
            finally
            {
                if (slotTaken)
                {
                    connectionSlots.Release();
                }
            }

            if (acceptFailure is not null)
            {
                UpdateLogger.Error("Parent control accept loop failed", acceptFailure);
                try
                {
                    await Task.Delay(GetAcceptErrorDelay(consecutiveFailures), cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    private async Task HandleClientSafelyAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using var requestTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        requestTimeout.CancelAfter(RequestTimeout);
        try
        {
            await HandleClientAsync(client, requestTimeout.Token, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Shutdown owns cancellation.
        }
        catch (OperationCanceledException)
        {
            UpdateLogger.Info($"Parent control request exceeded the {RequestTimeout.TotalSeconds:0}-second timeout.");
        }
        catch (Exception ex)
        {
            UpdateLogger.Error("Parent control request failed", ex);
        }
        finally
        {
            client.Dispose();
            connectionSlots.Release();
        }
    }

    private void TrackClientTask(Task clientTask)
    {
        lock (clientTasksGate)
        {
            clientTasks.Add(clientTask);
        }

        _ = clientTask.ContinueWith(
            static (completedTask, state) => ((ParentControlServer)state!).RemoveClientTask(completedTask),
            this,
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private void RemoveClientTask(Task clientTask)
    {
        lock (clientTasksGate)
        {
            clientTasks.Remove(clientTask);
        }
    }

    private static TimeSpan GetAcceptErrorDelay(int consecutiveFailures)
    {
        var exponent = Math.Min(Math.Max(consecutiveFailures - 1, 0), 5);
        var delayMilliseconds = Math.Min(
            MaximumAcceptErrorDelay.TotalMilliseconds,
            InitialAcceptErrorDelay.TotalMilliseconds * (1 << exponent));
        var jitterMilliseconds = Random.Shared.Next(0, Math.Max(1, (int)(delayMilliseconds / 4)));
        return TimeSpan.FromMilliseconds(Math.Min(
            MaximumAcceptErrorDelay.TotalMilliseconds,
            delayMilliseconds + jitterMilliseconds));
    }

    private async Task HandleClientAsync(
        TcpClient client,
        CancellationToken cancellationToken,
        CancellationToken shutdownToken)
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
                await WriteHtmlAsync(stream, BuildParentPage(NetworkUrls, isTrainingActive(), getLearningSettings()), cancellationToken).ConfigureAwait(false);
                break;
            case { Method: "GET", Path: "/favicon.ico" }:
                await WriteResponseAsync(stream, HttpStatusCode.NoContent, "image/x-icon", "", cancellationToken).ConfigureAwait(false);
                break;
            case { Method: "GET", Path: "/api/status" }:
                var statusSettings = getLearningSettings();
                await WriteJsonAsync(
                    stream,
                    HttpStatusCode.OK,
                    new ApiResult(
                        true,
                        "OK",
                        isTrainingActive(),
                        statusSettings.QuestionCount,
                        statusSettings.PassLine,
                        statusSettings.SchoolGrade,
                        statusSettings.PreferSchoolGrade),
                    cancellationToken).ConfigureAwait(false);
                break;
            case { Method: "POST", Path: "/api/start" }:
                await WriteControlActionResultAsync(
                    stream,
                    startTraining,
                    "学習画面を起動しました。",
                    "学習画面を起動できませんでした。",
                    cancellationToken,
                    shutdownToken).ConfigureAwait(false);
                break;
            case { Method: "POST", Path: "/api/return" }:
                await WriteControlActionResultAsync(
                    stream,
                    returnToComputer,
                    "パソコンの画面に戻しました。",
                    "パソコンの画面に戻せませんでした。",
                    cancellationToken,
                    shutdownToken).ConfigureAwait(false);
                break;
            case { Method: "POST", Path: "/api/pause" }:
                await WriteControlActionResultAsync(
                    stream,
                    pauseTraining,
                    "学習を一時停止してパソコンの画面に戻しました。",
                    "一時停止できませんでした。学習画面が起動しているか確認してください。",
                    cancellationToken,
                    shutdownToken).ConfigureAwait(false);
                break;
            case { Method: "POST", Path: "/api/settings" }:
                LearningSettingsRequest? settingsPayload;
                try
                {
                    settingsPayload = JsonSerializer.Deserialize<LearningSettingsRequest>(request.Body, JsonOptions);
                }
                catch (JsonException)
                {
                    var current = getLearningSettings();
                    await WriteJsonAsync(
                        stream,
                        HttpStatusCode.BadRequest,
                        new ApiResult(
                            false,
                            "入力を読み取れませんでした。",
                            isTrainingActive(),
                            current.QuestionCount,
                            current.PassLine,
                            current.SchoolGrade,
                            current.PreferSchoolGrade),
                        cancellationToken).ConfigureAwait(false);
                    break;
                }

                var settingsResult = await InvokeApiActionAsync(
                    stream,
                    token => changeLearningSettings(
                        settingsPayload?.QuestionCount,
                        settingsPayload?.PassLine,
                        settingsPayload?.SchoolGrade,
                        settingsPayload?.PreferSchoolGrade,
                        token),
                    "学習設定を保存できませんでした。",
                    cancellationToken,
                    shutdownToken).ConfigureAwait(false);
                if (settingsResult is null)
                {
                    break;
                }

                await WriteJsonAsync(
                    stream,
                    settingsResult.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest,
                    new ApiResult(
                        settingsResult.Success,
                        settingsResult.Message,
                        isTrainingActive(),
                        settingsResult.Settings.QuestionCount,
                        settingsResult.Settings.PassLine,
                        settingsResult.Settings.SchoolGrade,
                        settingsResult.Settings.PreferSchoolGrade),
                    cancellationToken).ConfigureAwait(false);
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

                var changeResult = await InvokeApiActionAsync(
                    stream,
                    token => changeParentPassword(
                        payload?.CurrentPassword,
                        payload?.NewPassword,
                        token),
                    "パスワードを変更できませんでした。",
                    cancellationToken,
                    shutdownToken).ConfigureAwait(false);
                if (changeResult is null)
                {
                    break;
                }

                await WriteJsonAsync(
                    stream,
                    changeResult.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest,
                    new ApiResult(changeResult.Success, changeResult.Message, isTrainingActive()),
                    cancellationToken).ConfigureAwait(false);
                break;
            case { Method: "POST", Path: "/api/reset" }:
                LearningResetRequest? resetPayload;
                try
                {
                    resetPayload = JsonSerializer.Deserialize<LearningResetRequest>(request.Body, JsonOptions);
                }
                catch (JsonException)
                {
                    await WriteJsonAsync(
                        stream,
                        HttpStatusCode.BadRequest,
                        new ApiResult(false, "入力を読み取れませんでした。", isTrainingActive()),
                        cancellationToken).ConfigureAwait(false);
                    break;
                }

                var resetResult = await InvokeApiActionAsync(
                    stream,
                    token => requestLearningReset(
                        resetPayload?.CurrentPassword,
                        resetPayload?.Mode,
                        token),
                    "学習データをリセットできませんでした。",
                    cancellationToken,
                    shutdownToken).ConfigureAwait(false);
                if (resetResult is null)
                {
                    break;
                }

                await WriteJsonAsync(
                    stream,
                    resetResult.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest,
                    new ApiResult(
                        resetResult.Success,
                        resetResult.Message,
                        isTrainingActive(),
                        Pending: resetResult.Pending),
                    cancellationToken).ConfigureAwait(false);
                break;
            default:
                await WriteJsonAsync(stream, HttpStatusCode.NotFound, new ApiResult(false, "Not found.", isTrainingActive()), cancellationToken).ConfigureAwait(false);
                break;
        }
    }

    private async Task WriteControlActionResultAsync(
        NetworkStream stream,
        Func<CancellationToken, Task<bool>> action,
        string successMessage,
        string failureMessage,
        CancellationToken cancellationToken,
        CancellationToken shutdownToken)
    {
        bool succeeded;
        try
        {
            succeeded = await action(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (shutdownToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            using var responseTimeout = CancellationTokenSource.CreateLinkedTokenSource(shutdownToken);
            responseTimeout.CancelAfter(TimeoutResponseWriteLimit);
            await WriteJsonAsync(
                stream,
                HttpStatusCode.GatewayTimeout,
                new ApiResult(false, "操作が時間内に完了しませんでした。", isTrainingActive()),
                responseTimeout.Token).ConfigureAwait(false);
            return;
        }
        catch (Exception ex)
        {
            UpdateLogger.Error("Parent control action failed", ex);
            await WriteJsonAsync(
                stream,
                HttpStatusCode.InternalServerError,
                new ApiResult(false, failureMessage, isTrainingActive()),
                cancellationToken).ConfigureAwait(false);
            return;
        }

        await WriteJsonAsync(
            stream,
            succeeded ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable,
            new ApiResult(succeeded, succeeded ? successMessage : failureMessage, isTrainingActive()),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<TResult?> InvokeApiActionAsync<TResult>(
        NetworkStream stream,
        Func<CancellationToken, Task<TResult>> action,
        string failureMessage,
        CancellationToken cancellationToken,
        CancellationToken shutdownToken)
        where TResult : class
    {
        try
        {
            return await action(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (shutdownToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            using var responseTimeout = CancellationTokenSource.CreateLinkedTokenSource(shutdownToken);
            responseTimeout.CancelAfter(TimeoutResponseWriteLimit);
            await WriteJsonAsync(
                stream,
                HttpStatusCode.GatewayTimeout,
                new ApiResult(false, "操作が時間内に完了しませんでした。", isTrainingActive()),
                responseTimeout.Token).ConfigureAwait(false);
            return null;
        }
        catch (Exception exception)
        {
            UpdateLogger.Error("Parent control action failed", exception);
            await WriteJsonAsync(
                stream,
                HttpStatusCode.InternalServerError,
                new ApiResult(false, failureMessage, isTrainingActive()),
                cancellationToken).ConfigureAwait(false);
            return null;
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

    private sealed record LearningSettingsRequest(
        int? QuestionCount,
        int? PassLine,
        int? SchoolGrade,
        bool? PreferSchoolGrade);

    private sealed record LearningResetRequest(string? CurrentPassword, string? Mode);

    private sealed record ApiResult(
        bool Ok,
        string Message,
        bool TrainingActive,
        int? QuestionCount = null,
        int? PassLine = null,
        int? SchoolGrade = null,
        bool? PreferSchoolGrade = null,
        bool? Pending = null);
}
