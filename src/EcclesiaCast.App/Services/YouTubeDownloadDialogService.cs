using EcclesiaCast.App.Views;

namespace EcclesiaCast.App.Services;

public sealed class YouTubeDownloadDialogService : IYouTubeDownloadDialog
{
    public string? Download(string videoId, string name)
    {
        var window = new YouTubeDownloadWindow(videoId, name)
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };

        return window.ShowDialog() == true ? window.DownloadedPath : null;
    }
}
