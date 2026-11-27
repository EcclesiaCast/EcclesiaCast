using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;

namespace EcclesiaCast.Presentation.ViewModels;

/// <summary>
/// One yt-dlp download while the operator waits: its progress, cancelling it
/// and the file it leaves. The window closes by itself when the file is there
/// (<see cref="CloseRequested"/>); otherwise it stays open with the reason
/// until the operator closes it.
/// </summary>
public sealed partial class YouTubeDownloadViewModel : ObservableObject
{
    private readonly string _videoId;
    private readonly string _name;
    private readonly Func<IProgress<(double Percent, string Message)>, CancellationToken, Task<string?>> _download;
    private readonly Func<Exception, string> _explain;
    private readonly CancellationTokenSource _cancellation = new();
    private bool _finished;
    private bool _closed;

    /// <param name="explain">Turns a failure into something the operator can act on.</param>
    public YouTubeDownloadViewModel(
        string videoId, string name, bool hasFfmpeg,
        Func<IProgress<(double Percent, string Message)>, CancellationToken, Task<string?>> download,
        Func<Exception, string>? explain = null)
    {
        _videoId = videoId;
        _name = name;
        _download = download;
        _explain = explain ?? YtDlp.Explain;
        Title = $"Descargando «{name}»";
        Quality = hasFfmpeg
            ? "Se baja la mejor calidad hasta 1080p."
            : "Sin ffmpeg instalado se baja la mejor calidad en un solo archivo "
            + "(por lo general 720p). Instalando ffmpeg se llega a 1080p.";
    }

    /// <summary>The one that really downloads the video into the downloads folder.</summary>
    /// <param name="exportCookies">The YouTube session for yt-dlp (see <see cref="IYouTubeSession"/>).</param>
    public static YouTubeDownloadViewModel For(string videoId, string name, Func<Task<string?>> exportCookies) =>
        new(videoId, name, YtDlp.HasFfmpeg(),
            (progress, cancellation) => YtDlp.DownloadAsync(
                videoId, YtDlp.DownloadsFolder, exportCookies, progress, cancellation));

    public string Title { get; }

    /// <summary>What quality will come down.</summary>
    public string Quality { get; }

    [ObservableProperty]
    private string _status = "Preparando…";

    [ObservableProperty]
    private bool _isIndeterminate = true;

    /// <summary>0 to 100.</summary>
    [ObservableProperty]
    private double _percent;

    [ObservableProperty]
    private string _closeText = "Cancelar";

    /// <summary>The window has to close now.</summary>
    public event Action? CloseRequested;

    /// <summary>The downloaded file, or null if it failed or was cancelled.</summary>
    public string? DownloadedPath { get; private set; }

    /// <summary>Downloads. Call it on the screen's thread, once the window is up.</summary>
    public async Task RunAsync()
    {
        // Closed before the download could start: there is nothing to do.
        if (_closed)
            return;

        var progress = new Progress<(double Percent, string Message)>(report =>
        {
            if (report.Percent >= 0)
            {
                IsIndeterminate = false;
                Percent = report.Percent;
            }
            else
            {
                IsIndeterminate = true;
            }
            Status = report.Message;
        });

        try
        {
            var path = await _download(progress, _cancellation.Token);

            // Closed while the last of it arrived: nobody is waiting for it.
            if (_closed)
                return;
            DownloadedPath = path;

            if (DownloadedPath is null)
            {
                Status = "yt-dlp terminó pero no encontré el archivo descargado.";
                Log.Warning("yt-dlp no informó el archivo de {Id}", _videoId);
                Finish();
                return;
            }

            Status = "Listo.";
            Percent = 100;
            CloseRequested?.Invoke();
        }
        catch (OperationCanceledException)
        {
            Status = "Descarga cancelada.";
            Finish();
        }
        catch (Exception ex)
        {
            // yt-dlp's own message is written for developers; turn the common
            // failures into something the operator can actually do.
            Status = _explain(ex);
            Log.Error(ex, "Falló la descarga de {Id} ({Name})", _videoId, _name);
            Finish();
        }
    }

    /// <summary>The one button: «Cancelar» while it downloads, «Cerrar» after it stopped.</summary>
    public void PressClose()
    {
        if (_finished)
            CloseRequested?.Invoke();
        else
            _cancellation.Cancel();
    }

    /// <summary>The window closed, however it did.</summary>
    public void Closed()
    {
        if (_closed)
            return;

        _closed = true;
        _cancellation.Cancel();
        _cancellation.Dispose();
    }

    /// <summary>Leaves the window open with the message; the button now closes it.</summary>
    private void Finish()
    {
        _finished = true;
        IsIndeterminate = false;
        CloseText = "Cerrar";
    }
}
