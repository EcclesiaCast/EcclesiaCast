using EcclesiaCast.App.Views;
using EcclesiaCast.Core.Displays;

namespace EcclesiaCast.App.Services;

public sealed class ProjectionWindowService : IProjectionWindowService
{
    private OutputWindow? _window;

    public bool IsOutputVisible => _window?.IsVisible == true;

    public void ShowTest(DisplayInfo display)
    {
        if (_window is null || !_window.IsLoaded)
            _window = new OutputWindow();

        _window.ShowOn(display);
    }

    public void HideOutput() => _window?.Hide();
}
