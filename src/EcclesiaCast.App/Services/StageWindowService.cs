using EcclesiaCast.App.ViewModels;
using EcclesiaCast.App.Views;
using EcclesiaCast.Core.Displays;
using EcclesiaCast.Core.Stage;

namespace EcclesiaCast.App.Services;

public sealed class StageWindowService(ProjectionViewModel projectionViewModel) : IStageWindowService
{
    private StageWindow? _window;

    public bool IsVisible => _window?.IsVisible == true;

    public event EventHandler? VisibilityChanged;

    public void ShowOn(DisplayInfo display, StageLayout layout)
    {
        if (_window is null || !_window.IsLoaded)
        {
            // The service timer starts when the stage first comes on, as it
            // always has; the operator's ⟲ restarts it from there.
            if (_window is null)
                ResetTimer();

            _window = new StageWindow { DataContext = projectionViewModel };
            _window.IsVisibleChanged += (_, _) => VisibilityChanged?.Invoke(this, EventArgs.Empty);
            // Double-clicking the stage screen is the way out when it covers
            // the operator's own panel.
            _window.CloseRequested += (_, _) => Hide();
        }

        _window.ApplyLayout(layout);
        _window.ShowOn(display);
    }

    public void Hide() => _window?.Hide();

    public void ApplyLayout(StageLayout layout) => _window?.ApplyLayout(layout);

    public void ResetTimer() => projectionViewModel.StageTimerStart = DateTime.Now;
}
