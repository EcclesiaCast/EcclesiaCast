using System.Windows.Threading;

namespace EcclesiaCast.App.Platform;

/// <summary>A WPF <see cref="DispatcherTimer"/> behind <see cref="IUiTimer"/>.</summary>
public sealed class WpfUiTimer : IUiTimer
{
    private readonly DispatcherTimer _timer;

    /// <summary>Same as <c>new DispatcherTimer { Interval = interval }</c>: the thread that calls it, WPF's own priority for timers.</summary>
    public WpfUiTimer(TimeSpan interval)
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

public sealed class WpfUiTimerFactory : IUiTimerFactory
{
    public IUiTimer Create(TimeSpan interval) => new WpfUiTimer(interval);
}
