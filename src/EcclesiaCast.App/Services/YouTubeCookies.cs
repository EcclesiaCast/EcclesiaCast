using System.IO;
using System.Windows.Interop;
using EcclesiaCast.Core.Media;
using Microsoft.Web.WebView2.Core;
using Serilog;

namespace EcclesiaCast.App.Services;

/// <summary>
/// Hands the church's YouTube session to yt-dlp. Chromium keeps its cookie
/// file locked while it runs (and encrypts it), so reading it from disk fails;
/// instead the browser inside the program is asked for its cookies and they
/// are written to a temporary text file that is deleted when the download ends.
/// </summary>
public static class YouTubeCookies
{
    private const int WsPopup = unchecked((int)0x80000000);

    /// <summary>
    /// Writes the YouTube and Google cookies to a temporary file and returns
    /// its path, or null when there is no session to hand over.
    /// </summary>
    public static async Task<string?> ExportAsync(CoreWebView2Environment? environment = null)
    {
        HwndSource? host = null;
        CoreWebView2Controller? controller = null;
        try
        {
            environment ??= await WebViewProfile.GetEnvironmentAsync();

            // The cookie manager belongs to a browser instance, so one is created
            // for the occasion. Its window has no WS_VISIBLE style: it is never shown.
            host = new HwndSource(new HwndSourceParameters("EcclesiaCastCookies")
            {
                Width = 1,
                Height = 1,
                PositionX = -32000,
                PositionY = -32000,
                WindowStyle = WsPopup,
            });
            controller = await environment.CreateCoreWebView2ControllerAsync(host.Handle);
            controller.IsVisible = false;

            var all = await controller.CoreWebView2.CookieManager.GetCookiesAsync(null);
            var cookies = all
                .Where(c => IsGoogleDomain(c.Domain))
                .Select(c => new BrowserCookie(
                    c.Domain, c.Path, c.Name, c.Value,
                    c.IsSession ? null : new DateTimeOffset(c.Expires.ToUniversalTime()),
                    c.IsSecure, c.IsHttpOnly))
                .ToList();

            // Without a login cookie there is no session; going anonymous is
            // better than sending a file of tracking cookies.
            if (!cookies.Any(c => c.Name is "SID" or "__Secure-1PSID" or "__Secure-3PSID"))
            {
                Log.Information("No hay sesión de YouTube en el navegador del programa; se descarga sin ella.");
                return null;
            }

            var path = Path.Combine(Path.GetTempPath(), $"ecclesiacast-{Guid.NewGuid():N}.txt");
            await File.WriteAllTextAsync(path, NetscapeCookies.Format(cookies, DateTimeOffset.UtcNow));
            Log.Debug("Sesión de YouTube exportada: {Count} cookies", cookies.Count);
            return path;
        }
        catch (Exception ex)
        {
            // Not being able to read the session must not stop the download.
            Log.Warning(ex, "No se pudo pedir la sesión de YouTube al navegador");
            return null;
        }
        finally
        {
            try { controller?.Close(); } catch (Exception) { /* ya cerrado */ }
            host?.Dispose();
        }
    }

    private static bool IsGoogleDomain(string domain)
    {
        var host = domain.TrimStart('.');
        return host.EndsWith("youtube.com", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith("google.com", StringComparison.OrdinalIgnoreCase);
    }
}
