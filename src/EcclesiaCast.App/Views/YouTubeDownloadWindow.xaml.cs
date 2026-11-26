using System.Windows;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.App.Views;

/// <summary>
/// Shows the progress of one yt-dlp download. The work is in
/// <see cref="YouTubeDownloadViewModel"/>.
/// </summary>
public partial class YouTubeDownloadWindow : Window
{
    private readonly YouTubeDownloadViewModel _download;

    public YouTubeDownloadWindow(YouTubeDownloadViewModel download)
    {
        InitializeComponent();
        _download = download;
        DataContext = download;

        download.CloseRequested += () => DialogResult = download.DownloadedPath is not null;
        Loaded += async (_, _) => await download.RunAsync();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => _download.PressClose();

    protected override void OnClosed(EventArgs e)
    {
        _download.Closed();
        base.OnClosed(e);
    }
}
