using EcclesiaCast.App.Views;

namespace EcclesiaCast.App.Services;

public sealed class YouTubeBrowserService : IYouTubeBrowser
{
    public IReadOnlyList<string> Browse()
    {
        var window = new YouTubeBrowserWindow
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };
        window.ShowDialog();
        return window.AddedVideoIds;
    }
}
