using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Presentation.Tests;

public sealed class YouTubeDownloadViewModelTests
{
    /// <summary>A download the test ends when it wants, and that cancelling stops.</summary>
    private sealed class FakeDownload
    {
        private readonly TaskCompletionSource<string?> _end = new();

        public IProgress<(double Percent, string Message)>? Progress { get; private set; }

        public Task<string?> Run(IProgress<(double Percent, string Message)> progress, CancellationToken cancellation)
        {
            Progress = progress;
            cancellation.Register(() => _end.TrySetCanceled(cancellation));
            return _end.Task;
        }

        public void Finish(string? path) => _end.SetResult(path);

        public void Break() => _end.SetException(new InvalidOperationException("ERROR 403"));
    }

    private static (YouTubeDownloadViewModel Download, FakeDownload Work, Func<int> Closes) New(bool hasFfmpeg = true)
    {
        var work = new FakeDownload();
        var download = new YouTubeDownloadViewModel(
            "abc", "Himno", hasFfmpeg, work.Run, _ => "Probá de nuevo más tarde.");
        var closes = 0;
        download.CloseRequested += () => closes++;
        return (download, work, () => closes);
    }

    [Fact]
    public void It_says_what_quality_will_come_down()
    {
        Assert.Equal("Descargando «Himno»", New().Download.Title);
        Assert.Equal("Se baja la mejor calidad hasta 1080p.", New().Download.Quality);
        Assert.StartsWith("Sin ffmpeg instalado", New(hasFfmpeg: false).Download.Quality);
    }

    [Fact]
    public async Task The_file_closes_the_window_by_itself()
    {
        var (download, work, closes) = New();
        var running = download.RunAsync();

        work.Finish("/videos/himno.mp4");
        await running;

        Assert.Equal("/videos/himno.mp4", download.DownloadedPath);
        Assert.Equal("Listo.", download.Status);
        Assert.Equal(100, download.Percent);
        Assert.Equal(1, closes());
    }

    [Fact]
    public void The_advance_may_or_may_not_have_a_number()
    {
        var context = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(new Inline());
        try
        {
            var (download, work, _) = New();
            _ = download.RunAsync();

            work.Progress!.Report((37, "Descargando… 37 %"));
            Assert.False(download.IsIndeterminate);
            Assert.Equal(37, download.Percent);
            Assert.Equal("Descargando… 37 %", download.Status);

            work.Progress.Report((-1, "Uniendo audio y video…"));
            Assert.True(download.IsIndeterminate);
            Assert.Equal("Uniendo audio y video…", download.Status);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(context);
        }
    }

    [Fact]
    public async Task No_file_stays_with_the_reason_until_closed()
    {
        var (download, work, closes) = New();
        var running = download.RunAsync();

        work.Finish(null);
        await running;

        Assert.Equal("yt-dlp terminó pero no encontré el archivo descargado.", download.Status);
        Assert.Equal("Cerrar", download.CloseText);
        Assert.Equal(0, closes());

        download.PressClose();
        Assert.Equal(1, closes());
        Assert.Null(download.DownloadedPath);
    }

    [Fact]
    public async Task A_failure_is_explained_for_the_operator()
    {
        var (download, work, closes) = New();
        var running = download.RunAsync();

        work.Break();
        await running;

        Assert.Equal("Probá de nuevo más tarde.", download.Status);
        Assert.Equal("Cerrar", download.CloseText);
        Assert.False(download.IsIndeterminate);
        Assert.Equal(0, closes());
    }

    [Fact]
    public async Task Cancel_stops_the_download_and_waits_for_Cerrar()
    {
        var (download, _, closes) = New();
        var running = download.RunAsync();

        download.PressClose();
        await running;

        Assert.Equal("Descarga cancelada.", download.Status);
        Assert.Equal("Cerrar", download.CloseText);
        Assert.Equal(0, closes());
    }

    [Fact]
    public async Task Closing_with_the_X_cancels_without_closing_again()
    {
        var (download, _, closes) = New();
        var running = download.RunAsync();

        download.Closed();
        await running;

        Assert.Null(download.DownloadedPath);
        Assert.Equal(0, closes());
    }

    private sealed class Inline : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state) => d(state);
    }
}
