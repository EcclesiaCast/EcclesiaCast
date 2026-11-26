using EcclesiaCast.Presentation.Platform;

namespace EcclesiaCast.App.Services;

/// <summary>The session of the program's WebView2 browser (see <see cref="YouTubeCookies"/>).</summary>
public sealed class WpfYouTubeSession : IYouTubeSession
{
    public Task<string?> ExportCookiesAsync() => YouTubeCookies.ExportAsync();
}
