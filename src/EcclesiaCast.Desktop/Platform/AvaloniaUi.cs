using Avalonia.Threading;

namespace EcclesiaCast.Desktop.Platform;

/// <summary>The screen's thread, in Avalonia.</summary>
public sealed class AvaloniaUiDispatcher : IUiDispatcher
{
    public bool CheckAccess() => Dispatcher.UIThread.CheckAccess();

    public void Post(Action action) => Dispatcher.UIThread.Post(action);

    public void Post(Action action, UiPriority priority) => Dispatcher.UIThread.Post(
        action, priority == UiPriority.Loaded ? DispatcherPriority.Loaded : DispatcherPriority.Normal);

    public Task InvokeAsync(Action action) => Dispatcher.UIThread.InvokeAsync(action).GetTask();

    public Task<T> InvokeAsync<T>(Func<T> function) => Dispatcher.UIThread.InvokeAsync(function).GetTask();
}

/// <summary>A clock on the screen's thread, in Avalonia.</summary>
public sealed class AvaloniaUiTimer : IUiTimer
{
    private readonly DispatcherTimer _timer;

    public AvaloniaUiTimer(TimeSpan interval)
    {
        _timer = new DispatcherTimer { Interval = interval };
        _timer.Tick += (_, e) => Tick?.Invoke(this, e);
    }

    public TimeSpan Interval
    {
        get => _timer.Interval;
        set => _timer.Interval = value;
    }

    public bool IsEnabled => _timer.IsEnabled;

    public event EventHandler? Tick;

    public void Start() => _timer.Start();

    public void Stop() => _timer.Stop();
}

public sealed class AvaloniaUiTimerFactory : IUiTimerFactory
{
    public IUiTimer Create(TimeSpan interval) => new AvaloniaUiTimer(interval);
}
