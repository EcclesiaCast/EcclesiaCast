using EcclesiaCast.App.Views;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.App.Services;

public sealed class YouTubeDownloadDialogService : IYouTubeDownloadDialog
{
    public string? Download(string videoId, string name)
    {
        var download = YouTubeDownloadViewModel.For(videoId, name, () => YouTubeCookies.ExportAsync());
        var window = new YouTubeDownloadWindow(download)
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };

        return window.ShowDialog() == true ? download.DownloadedPath : null;
    }
}
