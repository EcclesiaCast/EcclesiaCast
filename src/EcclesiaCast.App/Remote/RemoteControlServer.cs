using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Windows;
using EcclesiaCast.Core.Remote;
using Serilog;

namespace EcclesiaCast.App.Remote;

/// <summary>
/// A tiny web server so the operator can drive the projection from a phone on
/// the church's own wifi — useful when whoever is preaching wants to move the
/// slides themselves.
///
/// It is written straight on a <see cref="TcpListener"/> rather than
/// HttpListener because the latter needs an administrator-registered URL
/// reservation for anything beyond localhost, which no church volunteer is
/// going to set up. Only a handful of fixed routes exist, every one of them
/// behind a PIN.
/// </summary>
public sealed class RemoteControlServer : IDisposable
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IRemoteHost _host;

    // Requests are served in parallel, so the limiter — which is plain
    // single-threaded logic — is only ever touched under this lock.
    private readonly PinAttemptLimiter _limiter = new();
    private readonly object _limiterLock = new();

    private TcpListener? _listener;
    private CancellationTokenSource? _cancellation;

    public RemoteControlServer(IRemoteHost host) => _host = host;

    public bool IsRunning => _listener is not null;

    public int Port { get; private set; }

    /// <summary>Four digits the phone has to send with every request.</summary>
    public string Pin { get; private set; } = string.Empty;

    /// <summary>The address to type into the phone, e.g. http://192.168.1.40:8080.</summary>
    public string Address => $"http://{LocalAddress()}:{Port}";

    /// <summary>Starts listening. Returns false when no port could be opened.</summary>
    public bool Start(int preferredPort, string pin)
    {
        if (IsRunning)
            return true;

        Pin = pin;

        // Turning the remote off and on again is the way out for a volunteer
        // who locked their own phone out: it hands out a new PIN anyway.
        lock (_limiterLock)
            _limiter.Clear();

        // If the preferred port is taken (another copy, another program),
        // walk a few up rather than failing outright.
        for (var port = preferredPort; port < preferredPort + 10; port++)
        {
            try
            {
                var listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                _listener = listener;
                Port = port;
                break;
            }
            catch (SocketException)
            {
                // Puerto ocupado: probamos el siguiente.
            }
        }

        if (_listener is null)
        {
            Log.Error("No se pudo abrir ningún puerto para el control remoto desde {Port}", preferredPort);
            return false;
        }

        _cancellation = new CancellationTokenSource();
        _ = AcceptLoopAsync(_listener, _cancellation.Token);
        Log.Information("Control remoto escuchando en {Address}", Address);
        return true;
    }

    public void Stop()
    {
        _cancellation?.Cancel();
        try { _listener?.Stop(); } catch { /* ignore */ }
        _listener = null;
        _cancellation?.Dispose();
        _cancellation = null;
        Log.Information("Control remoto detenido");
    }

    public void Dispose() => Stop();

    private async Task AcceptLoopAsync(TcpListener listener, CancellationToken cancellation)
    {
        while (!cancellation.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await listener.AcceptTcpClientAsync(cancellation);
            }
            catch (Exception) when (cancellation.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Fallo al aceptar una conexión del control remoto");
                continue;
            }

            // One request per connection keeps the parser honest; phones poll
            // often but each poll is tiny.
            _ = HandleAsync(client, cancellation);
        }
    }

    private async Task HandleAsync(TcpClient client, CancellationToken cancellation)
    {
        using (client)
        {
            try
            {
                client.ReceiveTimeout = 5000;
                client.SendTimeout = 5000;

                // Wrong PINs are counted per phone, so the address has to come
                // from the socket: anything the request itself claims could be
                // made up by whoever is guessing.
                var caller = (client.Client.RemoteEndPoint as IPEndPoint)?.Address.ToString() ?? "?";

                await using var stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.UTF8, false, 1024, leaveOpen: true);

                var requestLine = await reader.ReadLineAsync(cancellation);
                if (string.IsNullOrWhiteSpace(requestLine))
                    return;

                var parts = requestLine.Split(' ');
                if (parts.Length < 2)
                    return;

                var method = parts[0];
                var target = parts[1];

                var contentLength = 0;
                while (await reader.ReadLineAsync(cancellation) is { } header && header.Length > 0)
                {
                    if (header.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase)
                        && int.TryParse(header[15..].Trim(), out var length))
                        contentLength = Math.Clamp(length, 0, 64 * 1024);
                }

                var body = string.Empty;
                if (contentLength > 0)
                {
                    var buffer = new char[contentLength];
                    var read = await reader.ReadBlockAsync(buffer, cancellation);
                    body = new string(buffer, 0, read);
                }

                await RespondAsync(stream, method, target, body, caller, cancellation);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error atendiendo una petición del control remoto");
            }
        }
    }

    private async Task RespondAsync(
        NetworkStream stream, string method, string target, string body, string caller,
        CancellationToken cancellation)
    {
        var path = target.Split('?')[0];

        if (method == "GET" && path is "/" or "/index.html")
        {
            await WriteAsync(stream, 200, "text/html; charset=utf-8", RemotePage.Html, cancellation);
            return;
        }

        if (path == "/api/state" && method == "GET")
        {
            var check = CheckPin(QueryValue(target, "pin"), caller);
            if (!check.Ok)
            {
                await WriteDeniedAsync(stream, check, cancellation);
                return;
            }

            var state = await OnUiThread(() => _host.GetState());
            await WriteAsync(stream, 200, "application/json; charset=utf-8",
                JsonSerializer.Serialize(state, Json), cancellation);
            return;
        }

        if (path == "/api/cmd" && method == "POST")
        {
            Command? command = null;
            try
            {
                command = JsonSerializer.Deserialize<Command>(body, Json);
            }
            catch (JsonException)
            {
                // Cuerpo ilegible: se responde 400 abajo.
            }

            if (command is null)
            {
                await WriteAsync(stream, 400, "application/json", """{"error":"body"}""", cancellation);
                return;
            }

            var check = CheckPin(command.Pin, caller);
            if (!check.Ok)
            {
                await WriteDeniedAsync(stream, check, cancellation);
                return;
            }

            var state = await OnUiThread(() =>
            {
                _host.Execute(command.Action ?? string.Empty, command.Index);
                return _host.GetState();
            });

            await WriteAsync(stream, 200, "application/json; charset=utf-8",
                JsonSerializer.Serialize(state, Json), cancellation);
            return;
        }

        await WriteAsync(stream, 404, "text/plain", "No encontrado", cancellation);
    }

    private sealed record Command(string? Pin, string? Action, int? Index);

    /// <summary>
    /// Checks the PIN and keeps score. A phone that keeps missing is turned
    /// away for a few minutes, which is what stops someone on the church wifi
    /// from walking through all ten thousand combinations.
    /// </summary>
    private PinCheck CheckPin(string? candidate, string caller)
    {
        var now = DateTimeOffset.UtcNow;

        lock (_limiterLock)
        {
            if (_limiter.RetryAfter(caller, now) is { } wait)
                return new PinCheck(false, (int)Math.Ceiling(wait.TotalSeconds));

            // Constant-time-ish comparison; the PIN is short and the network is local.
            var ok = !string.IsNullOrEmpty(Pin) && string.Equals(candidate, Pin, StringComparison.Ordinal);
            if (ok)
            {
                _limiter.RecordSuccess(caller);
                return new PinCheck(true, 0);
            }

            _limiter.RecordFailure(caller, now);

            if (_limiter.RetryAfter(caller, now) is { } lockout)
            {
                Log.Warning(
                    "Control remoto: {Caller} erró el PIN demasiadas veces; bloqueado por {Minutes} minutos",
                    caller, Math.Ceiling(lockout.TotalMinutes));
                return new PinCheck(false, (int)Math.Ceiling(lockout.TotalSeconds));
            }

            return new PinCheck(false, 0);
        }
    }

    /// <summary>A wrong PIN (<c>RetryAfterSeconds</c> 0) or a client serving its wait.</summary>
    private readonly record struct PinCheck(bool Ok, int RetryAfterSeconds);

    private static Task WriteDeniedAsync(NetworkStream stream, PinCheck check, CancellationToken cancellation) =>
        check.RetryAfterSeconds > 0
            ? WriteAsync(stream, 429, "application/json",
                $$"""{"error":"lockout","retryAfter":{{check.RetryAfterSeconds}}}""", cancellation,
                $"Retry-After: {check.RetryAfterSeconds}\r\n")
            : WriteAsync(stream, 403, "application/json", """{"error":"pin"}""", cancellation);

    private static string? QueryValue(string target, string key)
    {
        var split = target.Split('?', 2);
        if (split.Length < 2)
            return null;

        foreach (var pair in split[1].Split('&'))
        {
            var kv = pair.Split('=', 2);
            if (kv.Length == 2 && kv[0] == key)
                return Uri.UnescapeDataString(kv[1]);
        }

        return null;
    }

    /// <summary>
    /// The view model lives on the UI thread; every call hops onto it. Without
    /// a WPF application around (tests) the call runs where it stands.
    /// </summary>
    private static Task<T> OnUiThread<T>(Func<T> function) =>
        Application.Current?.Dispatcher is { } dispatcher
            ? dispatcher.InvokeAsync(function).Task
            : Task.FromResult(function());

    private static async Task WriteAsync(
        NetworkStream stream, int status, string contentType, string content, CancellationToken cancellation,
        string? extraHeaders = null)
    {
        var payload = Encoding.UTF8.GetBytes(content);
        var reason = status switch
        {
            200 => "OK",
            400 => "Bad Request",
            403 => "Forbidden",
            429 => "Too Many Requests",
            _ => "Not Found",
        };

        var header = Encoding.ASCII.GetBytes(
            $"HTTP/1.1 {status} {reason}\r\n"
            + $"Content-Type: {contentType}\r\n"
            + $"Content-Length: {payload.Length}\r\n"
            + "Cache-Control: no-store\r\n"
            + extraHeaders
            + "Connection: close\r\n\r\n");

        await stream.WriteAsync(header, cancellation);
        await stream.WriteAsync(payload, cancellation);
        await stream.FlushAsync(cancellation);
    }

    /// <summary>
    /// The machine's address on the church network. Picks an up, non-loopback
    /// interface with a gateway, so a virtual adapter (Docker, WSL, a VPN)
    /// doesn't get shown as the address to type.
    /// </summary>
    public static string LocalAddress()
    {
        try
        {
            var candidates = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up)
                .Where(n => n.NetworkInterfaceType is NetworkInterfaceType.Ethernet
                        or NetworkInterfaceType.Wireless80211)
                .Select(n => n.GetIPProperties())
                .Where(p => p.GatewayAddresses.Any(g => g.Address.AddressFamily == AddressFamily.InterNetwork))
                .SelectMany(p => p.UnicastAddresses)
                .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
                .Select(a => a.Address.ToString())
                .ToList();

            return candidates.FirstOrDefault() ?? "127.0.0.1";
        }
        catch (NetworkInformationException ex)
        {
            Log.Debug(ex, "No se pudo determinar la dirección de red local");
            return "127.0.0.1";
        }
    }
}
