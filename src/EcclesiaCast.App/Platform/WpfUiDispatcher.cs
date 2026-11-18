using System.Windows.Threading;

namespace EcclesiaCast.App.Platform;

/// <summary>The WPF thread of the program, behind <see cref="IUiDispatcher"/>.</summary>
public sealed class WpfUiDispatcher(Dispatcher dispatcher) : IUiDispatcher
{
    public bool CheckAccess() => dispatcher.CheckAccess();

    public void Post(Action action) => dispatcher.BeginInvoke(action);

    public Task InvokeAsync(Action action) => dispatcher.InvokeAsync(action).Task;

    public Task<T> InvokeAsync<T>(Func<T> function) => dispatcher.InvokeAsync(function).Task;
}
