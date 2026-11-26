using EcclesiaCast.Desktop.Views;
using EcclesiaCast.Presentation.Platform;
using EcclesiaCast.Presentation.Services;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Desktop.Services;

public sealed class AvaloniaYouTubeDownloadDialog(IYouTubeSession session) : IYouTubeDownloadDialog
{
    public string? Download(string videoId, string name)
    {
        var download = YouTubeDownloadViewModel.For(videoId, name, session.ExportCookiesAsync);
        Modal.Run(new YouTubeDownloadWindow(download));
        // Null when it failed or was cancelled, as WPF answers.
        return download.DownloadedPath;
    }
}
