using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using Serilog;

namespace EcclesiaCast.Proto;

/// <summary>
/// The phone remote, cut down to the bone: the same way as
/// RemoteControlServer in the program (straight on a TcpListener, one request
/// per connection), with a page holding «Anterior» and «Siguiente». No PIN:
/// this is only for the test.
/// </summary>
public sealed class RemoteServer : IDisposable
{
    public const int DefaultPort = 8765;

    private readonly Func<string, Task<string>> _onCommand;
    private TcpListener? _listener;
    private CancellationTokenSource? _cancellation;

    /// <param name="onCommand">
    /// Runs «anterior» or «siguiente» and returns what the slide now says.
    /// </param>
    public RemoteServer(Func<string, Task<string>> onCommand) => _onCommand = onCommand;

    public int Port { get; private set; }

    public string Address => $"http://{LocalAddress()}:{Port}";

    public bool Start(int port = DefaultPort)
    {
        try
        {
            var listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            _listener = listener;
            Port = port;
            _cancellation = new CancellationTokenSource();
            _ = AcceptLoopAsync(listener, _cancellation.Token);
            Log.Information("Control remoto escuchando en {Address}", Address);
            return true;
        }
        catch (SocketException ex)
        {
            Log.Error(ex, "No se pudo abrir el puerto {Port} para el control remoto", port);
            return false;
        }
    }

    public void Dispose()
    {
        _cancellation?.Cancel();
        try { _listener?.Stop(); } catch (SocketException) { /* closing anyway */ }
        _listener = null;
    }

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

            _ = HandleAsync(client, cancellation);
        }
    }

    private async Task HandleAsync(TcpClient client, CancellationToken cancellation)
    {
        using (client)
        {
            try
            {
                var received = Stopwatch.GetTimestamp();
                client.ReceiveTimeout = 5000;
                client.SendTimeout = 5000;
                var caller = (client.Client.RemoteEndPoint as IPEndPoint)?.Address.ToString() ?? "?";

                await using var stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.UTF8, false, 1024, leaveOpen: true);

                var requestLine = await reader.ReadLineAsync(cancellation);
                if (string.IsNullOrWhiteSpace(requestLine))
                    return;

                var parts = requestLine.Split(' ');
                if (parts.Length < 2)
                    return;

                // Headers are read and ignored; no request here has a body.
                while (await reader.ReadLineAsync(cancellation) is { Length: > 0 })
                {
                }

                var path = parts[1].Split('?')[0];
                switch (path)
                {
                    case "/":
                        Log.Information("Celular {Caller}: abrió la página", caller);
                        await WriteAsync(stream, 200, "text/html; charset=utf-8", Page, cancellation);
                        break;

                    case "/anterior":
                    case "/siguiente":
                        var command = path[1..];
                        Log.Information("Celular {Caller}: pidió {Command} a las {Time:HH:mm:ss.fff}", caller, command, DateTime.Now);
                        var shown = await _onCommand(command);
                        var elapsed = Stopwatch.GetElapsedTime(received);
                        Log.Information("Celular: {Command} cambió la diapositiva a las {Time:HH:mm:ss.fff}, {Ms:0} ms después del pedido",
                            command, DateTime.Now, elapsed.TotalMilliseconds);
                        await WriteAsync(stream, 200, "text/plain; charset=utf-8", shown, cancellation);
                        break;

                    case "/tiempo":
                        // The phone reports how long the whole round trip took.
                        var ms = parts[1].Contains("ms=") ? parts[1][(parts[1].IndexOf("ms=", StringComparison.Ordinal) + 3)..] : "?";
                        Log.Information("Celular {Caller}: ida y vuelta {Ms} ms", caller, Uri.UnescapeDataString(ms));
                        await WriteAsync(stream, 200, "text/plain", "ok", cancellation);
                        break;

                    default:
                        await WriteAsync(stream, 404, "text/plain", "No existe", cancellation);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error atendiendo un pedido del control remoto");
            }
        }
    }

    private static async Task WriteAsync(NetworkStream stream, int status, string contentType, string content, CancellationToken cancellation)
    {
        var payload = Encoding.UTF8.GetBytes(content);
        var header = Encoding.ASCII.GetBytes(
            $"HTTP/1.1 {status} {(status == 200 ? "OK" : "Not Found")}\r\n"
            + $"Content-Type: {contentType}\r\n"
            + $"Content-Length: {payload.Length}\r\n"
            + "Cache-Control: no-store\r\n"
            + "Connection: close\r\n\r\n");

        await stream.WriteAsync(header, cancellation);
        await stream.WriteAsync(payload, cancellation);
        await stream.FlushAsync(cancellation);
    }

    /// <summary>
    /// The machine's address on the church network: an interface that is up,
    /// not loopback, with a gateway; failing that, any IPv4 that is up.
    /// </summary>
    public static string LocalAddress()
    {
        try
        {
            var up = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up
                    && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .ToList();

            string? Pick(bool needGateway) => up
                .Select(n => n.GetIPProperties())
                .Where(p => !needGateway || p.GatewayAddresses.Any(g => g.Address.AddressFamily == AddressFamily.InterNetwork))
                .SelectMany(p => p.UnicastAddresses)
                .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a.Address))
                .Select(a => a.Address.ToString())
                .FirstOrDefault();

            return Pick(true) ?? Pick(false) ?? "127.0.0.1";
        }
        catch (Exception ex) when (ex is NetworkInformationException or PlatformNotSupportedException)
        {
            Log.Debug(ex, "No se pudo saber la dirección de red local");
            return "127.0.0.1";
        }
    }

    /// <summary>Lists the network interfaces in the log, to understand an odd address on the Mac.</summary>
    public static void LogInterfaces()
    {
        try
        {
            foreach (var n in NetworkInterface.GetAllNetworkInterfaces())
            {
                var p = n.GetIPProperties();
                Log.Information("Red: {Name} ({Type}, {Status}) direcciones {Addresses} puertas {Gateways}",
                    n.Name, n.NetworkInterfaceType, n.OperationalStatus,
                    string.Join(", ", p.UnicastAddresses.Select(a => a.Address)),
                    string.Join(", ", p.GatewayAddresses.Select(g => g.Address)));
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "No se pudo listar la red");
        }
    }

    private const string Page = """
        <!doctype html>
        <html lang="es"><head><meta charset="utf-8">
        <meta name="viewport" content="width=device-width, initial-scale=1">
        <title>EcclesiaCast Proto</title>
        <style>
          body { font-family: -apple-system, system-ui, sans-serif; margin: 0; padding: 16px; background: #10141E; color: #fff; }
          button { display: block; width: 100%; font-size: 32px; padding: 32px 0; margin: 0 0 16px; border: 0; border-radius: 16px; }
          #ant { background: #3a4660; color: #fff; }
          #sig { background: #2f6fec; color: #fff; }
          #estado { font-size: 18px; opacity: .85; min-height: 3em; }
        </style></head>
        <body>
          <button id="ant">◀ Anterior</button>
          <button id="sig">Siguiente ▶</button>
          <div id="estado">Tocá un botón.</div>
        <script>
          async function mandar(que) {
            const t0 = performance.now();
            try {
              const r = await fetch('/' + que, { method: 'POST', cache: 'no-store' });
              const texto = await r.text();
              const ms = Math.round(performance.now() - t0);
              document.getElementById('estado').textContent = 'Tardó ' + ms + ' ms. ' + texto.slice(0, 60);
              fetch('/tiempo?ms=' + ms, { cache: 'no-store' });
            } catch (e) {
              document.getElementById('estado').textContent = 'No llegó: ' + e;
            }
          }
          document.getElementById('ant').onclick = () => mandar('anterior');
          document.getElementById('sig').onclick = () => mandar('siguiente');
        </script>
        </body></html>
        """;
}
