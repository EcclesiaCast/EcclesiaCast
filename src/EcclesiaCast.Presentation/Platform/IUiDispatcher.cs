namespace EcclesiaCast.Presentation.Platform;

/// <summary>
/// The thread the screens live on. View models and the screens are only
/// touched from it; work that finishes elsewhere (the network, the sound,
/// a download) comes back through here.
/// </summary>
public interface IUiDispatcher
{
    /// <summary>True when the caller already is on the screen's thread.</summary>
    bool CheckAccess();

    /// <summary>
    /// Queues <paramref name="action"/> on the screen's thread and returns at
    /// once, without waiting for it to run (WPF's <c>BeginInvoke</c>).
    /// </summary>
    void Post(Action action);

    /// <summary>Runs <paramref name="action"/> on the screen's thread; the task ends when it has run.</summary>
    Task InvokeAsync(Action action);

    /// <summary>Runs <paramref name="function"/> on the screen's thread and hands back its result.</summary>
    Task<T> InvokeAsync<T>(Func<T> function);
}
