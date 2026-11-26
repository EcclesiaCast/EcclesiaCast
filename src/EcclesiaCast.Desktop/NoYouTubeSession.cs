using EcclesiaCast.Presentation.Platform;

namespace EcclesiaCast.Desktop;

/// <summary>
/// No session to give yt-dlp: the Avalonia program has no browser of its own
/// signed in to YouTube (docs/plan-mac-fase-3.md).
/// </summary>
public sealed class NoYouTubeSession : IYouTubeSession
{
    public Task<string?> ExportCookiesAsync() => Task.FromResult<string?>(null);
}
