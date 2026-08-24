using System.Windows;
using EcclesiaCast.App.Services;
using Serilog;

namespace EcclesiaCast.App.Views;

/// <summary>Shows the progress of one yt-dlp download and hands back the file.</summary>
public partial class YouTubeDownloadWindow : Window
{
    private readonly string _videoId;
    private readonly string _name;
    private readonly CancellationTokenSource _cancellation = new();
    private bool _finished;

    public YouTubeDownloadWindow(string videoId, string name)
    {
        InitializeComponent();
        _videoId = videoId;
        _name = name;

        TitleText.Text = $"Descargando «{name}»";
        QualityText.Text = YtDlp.HasFfmpeg()
            ? "Se baja la mejor calidad hasta 1080p."
            : "Sin ffmpeg instalado se baja la mejor calidad en un solo archivo "
            + "(por lo general 720p). Instalando ffmpeg se llega a 1080p.";

        Loaded += async (_, _) => await RunAsync();
    }

    /// <summary>The downloaded file, or null if it failed or was cancelled.</summary>
    public string? DownloadedPath { get; private set; }

    private async Task RunAsync()
    {
        var progress = new Progress<(double Percent, string Message)>(report =>
        {
            if (report.Percent >= 0)
            {
                Progress.IsIndeterminate = false;
                Progress.Value = report.Percent;
            }
            else
            {
                Progress.IsIndeterminate = true;
            }
            StatusText.Text = report.Message;
        });

        try
        {
            DownloadedPath = await YtDlp.DownloadAsync(
                _videoId, YtDlp.DownloadsFolder, progress, _cancellation.Token);

            if (DownloadedPath is null)
            {
                StatusText.Text = "yt-dlp terminó pero no encontré el archivo descargado.";
                Log.Warning("yt-dlp no informó el archivo de {Id}", _videoId);
                Finish(success: false);
                return;
            }

            StatusText.Text = "Listo.";
            Progress.Value = 100;
            DialogResult = true;
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = "Descarga cancelada.";
            Finish(success: false);
        }
        catch (Exception ex)
        {
            // yt-dlp's own message is written for developers; turn the common
            // failures into something the operator can actually do.
            StatusText.Text = YtDlp.Explain(ex.Message);
            Log.Error(ex, "Falló la descarga de {Id} ({Name})", _videoId, _name);
            Finish(success: false);
        }
    }

    /// <summary>Leaves the window open with the message; the button now closes it.</summary>
    private void Finish(bool success)
    {
        _finished = true;
        Progress.IsIndeterminate = false;
        CloseButton.Content = "Cerrar";
        if (success)
            DialogResult = true;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        if (!_finished)
            _cancellation.Cancel();
        else
            DialogResult = DownloadedPath is not null;
    }

    protected override void OnClosed(EventArgs e)
    {
        _cancellation.Cancel();
        _cancellation.Dispose();
        base.OnClosed(e);
    }
}
