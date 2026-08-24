using EcclesiaCast.App.ViewModels;
using EcclesiaCast.App.Views;
using EcclesiaCast.Core.Displays;

namespace EcclesiaCast.App.Services;

public sealed class StageWindowService(ProjectionViewModel projectionViewModel) : IStageWindowService
{
    private StageWindow? _window;

    public bool IsVisible => _window?.IsVisible == true;

    public event EventHandler? VisibilityChanged;

    public void ShowOn(DisplayInfo display, StageOptions options)
    {
        if (_window is null || !_window.IsLoaded)
        {
            _window = new StageWindow { DataContext = projectionViewModel };
            _window.IsVisibleChanged += (_, _) => VisibilityChanged?.Invoke(this, EventArgs.Empty);
            // Double-clicking the stage screen is the way out when it covers
            // the operator's own panel.
            _window.CloseRequested += (_, _) => Hide();
        }

        _window.ApplyOptions(options);
        _window.ShowOn(display);
    }

    public void Hide() => _window?.Hide();

    public void ApplyOptions(StageOptions options) => _window?.ApplyOptions(options);

    public void ResetTimer() => _window?.ResetTimer();
}
