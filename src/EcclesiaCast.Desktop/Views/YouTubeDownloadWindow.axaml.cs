using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Desktop.Views;

/// <summary>
/// Downloads one YouTube video while the operator waits. The work is in
/// <see cref="YouTubeDownloadViewModel"/>.
/// </summary>
public sealed partial class YouTubeDownloadWindow : Window
{
    private readonly YouTubeDownloadViewModel _download;

    public YouTubeDownloadWindow(YouTubeDownloadViewModel download)
    {
        InitializeComponent();
        _download = download;
        DataContext = download;

        download.CloseRequested += Close;
        // Posted: work that ends at once would close the window while it is still opening.
        Opened += (_, _) => Dispatcher.UIThread.Post(async () => await download.RunAsync());
    }

    private void Close_Click(object? sender, RoutedEventArgs e) => _download.PressClose();

    protected override void OnClosed(EventArgs e)
    {
        _download.Closed();
        base.OnClosed(e);
    }
}
