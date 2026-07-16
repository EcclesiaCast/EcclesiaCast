using EcclesiaCast.App.ViewModels;
using EcclesiaCast.App.Views;
using EcclesiaCast.Core.Displays;

namespace EcclesiaCast.App.Services;

public sealed class ProjectionWindowService(ProjectionViewModel projectionViewModel)
    : IProjectionWindowService
{
    private OutputWindow? _window;

    public bool IsOutputVisible => _window?.IsVisible == true;

    public event EventHandler? VisibilityChanged;

    public void EnsureVisible(DisplayInfo display)
    {
        if (_window is null || !_window.IsLoaded)
        {
            _window = new OutputWindow { DataContext = projectionViewModel };
            _window.IsVisibleChanged += (_, _) =>
                VisibilityChanged?.Invoke(this, EventArgs.Empty);
        }

        _window.ShowOn(display);
    }

    public void HideOutput() => _window?.Hide();
}
