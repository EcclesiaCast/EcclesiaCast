using System.Net;
using System.Net.Sockets;
using System.Text;

namespace EcclesiaCast.Core.Tests.Updates;

/// <summary>What the test server sends back for one request.</summary>
/// <param name="DeclaredLength">The Content-Length to announce when it is not the real one (a download cut short).</param>
/// <param name="HangAfterBody">Keep the connection open without finishing, like a network that stops answering.</param>
internal sealed record ServerResponse(
    int Status,
    byte[] Body,
    long? DeclaredLength = null,
    bool HangAfterBody = false)
{
    public static ServerResponse Ok(byte[] body) => new(200, body);

    public static ServerResponse Ok(string text) => new(200, Encoding.UTF8.GetBytes(text));

    public static ServerResponse Status403() => new(403, Encoding.UTF8.GetBytes("{\"message\":\"API rate limit exceeded\"}"));
}

/// <summary>
/// A web server on this computer for the update tests, so that no test ever
/// goes to GitHub. It speaks just enough HTTP for HttpClient, and can also
/// cut an answer short or stop answering, which is how the network fails.
/// </summary>
internal sealed class LocalHttpServer : IDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly Func<string, ServerResponse> _handler;
    private readonly CancellationTokenSource _stop = new();
    private readonly List<string> _requests = [];

    public LocalHttpServer(Func<string, ServerResponse> handler)
    {
        _handler = handler;
        _listener.Start();
        _ = AcceptLoopAsync();
    }

    public string BaseUrl => $"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}";

    /// <summary>The headers of every request received so far, as text.</summary>
    public IReadOnlyList<string> Requests
    {
        get
        {
            lock (_requests)
                return [.. _requests];
        }
    }

    private async Task AcceptLoopAsync()
    {
        try
        {
            while (!_stop.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync(_stop.Token);
                _ = Task.Run(() => ServeAsync(client));
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException or SocketException)
        {
            // The test is over.
        }
    }

    private async Task ServeAsync(TcpClient client)
    {
        using (client)
        {
            try
            {
                var stream = client.GetStream();
                var header = new StringBuilder();
                var one = new byte[1];
                while (!header.ToString().EndsWith("\r\n\r\n", StringComparison.Ordinal))
                {
                    if (await stream.ReadAsync(one, _stop.Token) == 0)
                        return;
                    header.Append((char)one[0]);
                }

                lock (_requests)
                    _requests.Add(header.ToString());

                var path = header.ToString().Split(' ')[1];
                var response = _handler(path);

                var head = $"HTTP/1.1 {response.Status} X\r\n"
                    + $"Content-Length: {response.DeclaredLength ?? response.Body.Length}\r\n"
                    + "Content-Type: application/octet-stream\r\nConnection: close\r\n\r\n";
                await stream.WriteAsync(Encoding.ASCII.GetBytes(head), _stop.Token);
                await stream.WriteAsync(response.Body, _stop.Token);
                await stream.FlushAsync(_stop.Token);

                if (response.HangAfterBody)
                    await Task.Delay(Timeout.Infinite, _stop.Token);
            }
            catch (Exception ex) when (ex is OperationCanceledException or IOException or SocketException or ObjectDisposedException)
            {
                // The client gave up, or the test is over.
            }
        }
    }

    public void Dispose()
    {
        if (_stop.IsCancellationRequested)
            return;

        _stop.Cancel();
        _listener.Stop();
        _stop.Dispose();
    }
}
