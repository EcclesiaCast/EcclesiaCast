namespace EcclesiaCast.Presentation.Platform;

/// <summary>A clock whose <see cref="Tick"/> runs on the screen's thread.</summary>
public interface IUiTimer
{
    TimeSpan Interval { get; set; }

    bool IsEnabled { get; }

    event EventHandler? Tick;

    void Start();

    void Stop();
}

/// <summary>Makes the clocks of <see cref="IUiTimer"/>.</summary>
public interface IUiTimerFactory
{
    /// <summary>
    /// A stopped clock that ticks every <paramref name="interval"/>. Call it on
    /// the screen's thread: the clock belongs to the thread that made it.
    /// </summary>
    IUiTimer Create(TimeSpan interval);
}
