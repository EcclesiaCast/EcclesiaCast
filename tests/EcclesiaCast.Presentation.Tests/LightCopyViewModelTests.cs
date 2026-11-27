using EcclesiaCast.Presentation.Services;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Presentation.Tests;

public sealed class LightCopyViewModelTests
{
    /// <summary>A copy the test finishes when it wants, and that cancelling stops.</summary>
    private sealed class FakeWork
    {
        private readonly TaskCompletionSource<bool> _end = new();

        public IProgress<double>? Progress { get; private set; }

        public Task<bool> Run(IProgress<double> progress, CancellationToken cancellation)
        {
            Progress = progress;
            cancellation.Register(() => _end.TrySetCanceled(cancellation));
            return _end.Task;
        }

        public void Finish(bool ok) => _end.SetResult(ok);

        public void Break() => _end.SetException(new InvalidOperationException("ffmpeg"));
    }

    private static (LightCopyViewModel Copy, FakeWork Work, List<LightCopyResult> Closes) New()
    {
        var work = new FakeWork();
        var copy = new LightCopyViewModel("Olas", work.Run);
        var closes = new List<LightCopyResult>();
        copy.CloseRequested += () => closes.Add(copy.Result);
        return (copy, work, closes);
    }

    [Fact]
    public async Task A_finished_copy_closes_the_window_by_itself()
    {
        var (copy, work, closes) = New();
        Assert.Equal("Preparando la copia liviana de «Olas»", copy.Title);
        Assert.True(copy.IsIndeterminate);

        var running = copy.RunAsync();
        work.Finish(ok: true);
        await running;

        Assert.Equal([LightCopyResult.Done], closes);
    }

    [Fact]
    public void The_advance_is_told_as_a_percentage()
    {
        var context = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(new Inline());
        try
        {
            var (copy, work, _) = New();
            _ = copy.RunAsync();

            work.Progress!.Report(0.426);

            Assert.False(copy.IsIndeterminate);
            Assert.Equal(42.6, copy.Percent, 3);
            Assert.Equal("Preparando la copia… 43 %", copy.Status);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(context);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_copy_that_fails_stays_with_the_message_until_closed(bool throws)
    {
        var (copy, work, closes) = New();
        var running = copy.RunAsync();

        if (throws)
            work.Break();
        else
            work.Finish(ok: false);
        await running;

        Assert.Empty(closes);
        Assert.Equal("No se pudo hacer la copia. Se va a proyectar el original.", copy.Status);
        Assert.Equal("Cerrar", copy.CloseText);
        Assert.False(copy.IsIndeterminate);

        copy.PressClose();
        copy.Closed();

        Assert.Equal([LightCopyResult.Failed], closes);
        Assert.Equal(LightCopyResult.Failed, copy.Result);
    }

    [Fact]
    public async Task Cancel_stops_the_work_and_closes()
    {
        var (copy, _, closes) = New();
        var running = copy.RunAsync();

        copy.PressClose();
        await running;

        Assert.Equal([LightCopyResult.Cancelled], closes);
    }

    [Fact]
    public async Task Closing_with_the_X_cancels_without_closing_again()
    {
        var (copy, _, closes) = New();
        var running = copy.RunAsync();

        copy.Closed();
        await running;

        Assert.Empty(closes);
        Assert.Equal(LightCopyResult.Cancelled, copy.Result);
    }

    [Fact]
    public async Task A_window_closed_before_the_work_starts_starts_nothing()
    {
        var (copy, work, closes) = New();

        copy.Closed();
        await copy.RunAsync();

        Assert.Null(work.Progress);
        Assert.Empty(closes);
        Assert.Equal(LightCopyResult.Cancelled, copy.Result);
        Assert.StartsWith("Empezando", copy.Status);
    }

    private sealed class Inline : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state) => d(state);
    }
}
