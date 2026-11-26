using Avalonia;
using EcclesiaCast.Core.Displays;
using EcclesiaCast.Core.Stage;
using EcclesiaCast.Desktop.Platform;
using EcclesiaCast.Desktop.Views;
using EcclesiaCast.Presentation.Services;
using EcclesiaCast.Presentation.ViewModels;
using Serilog;

namespace EcclesiaCast.Desktop.Services;

/// <summary>
/// The stage window, in Avalonia: what the WPF <c>StageWindowService</c>
/// does. When its screen is unplugged it is hidden and
/// <see cref="StageScreenLost"/> says so; it can be put back on as always.
/// </summary>
public sealed class StageWindowService : IStageWindowService
{
    private readonly ProjectionViewModel _projection;
    private readonly IOutputWindowNative _native;
    private readonly IScreenList _screens;
    private StageWindow? _window;
    private bool _windowClosed;

    public StageWindowService(ProjectionViewModel projection, IOutputWindowNative native, IScreenList screens)
    {
        _projection = projection;
        _native = native;
        _screens = screens;
        _screens.Changed += (_, _) => OnScreensChanged();
    }

    public bool IsVisible => _window is { IsVisible: true } && !_windowClosed;

    public event EventHandler? VisibilityChanged;

    /// <summary>The screen the stage was on went away (its name); the stage is already off.</summary>
    public event EventHandler<string>? StageScreenLost;

    /// <summary>The window, for the tests.</summary>
    internal StageWindow? Window => _window;

    public void ShowOn(DisplayInfo display, StageLayout layout)
    {
        if (_window is null || _windowClosed)
        {
            // The service timer starts when the stage first comes on, as it
            // always has; the operator's ⟲ restarts it from there.
            if (_window is null)
                ResetTimer();

            _window = new StageWindow(_projection, _native);
            _windowClosed = false;
            var window = _window;
            window.PropertyChanged += (_, e) =>
            {
                if (e.Property == Visual.IsVisibleProperty && ReferenceEquals(window, _window))
                    VisibilityChanged?.Invoke(this, EventArgs.Empty);
            };
            window.Closed += (_, _) =>
            {
                if (!ReferenceEquals(window, _window))
                    return;
                _windowClosed = true;
                VisibilityChanged?.Invoke(this, EventArgs.Empty);
            };
            // Double-clicking the stage screen is the way out when it covers
            // the operator's own panel.
            window.CloseRequested += (_, _) => Hide();
        }

        _window.ApplyLayout(layout);
        _window.ShowOn(display);
    }

    public void Hide() => _window?.Hide();

    public void ApplyLayout(StageLayout layout) => _window?.ApplyLayout(layout);

    public void ResetTimer()
    {
        // Zero, and counting: the stopwatch of the service restarts from here.
        _projection.Clocks.Stopwatch.Reset();
        _projection.Clocks.Stopwatch.Start(DateTimeOffset.Now);
    }

    private void OnScreensChanged()
    {
        if (_window is not { IsVisible: true } window || _windowClosed || window.Display is not { } display)
            return;

        if (AvaloniaDisplayProvider.Find(display, _screens.GetDisplays()) is not { } same)
        {
            Log.Warning("Se desconectó la pantalla del escenario ({Display}): se apaga el escenario", display.DeviceName);
            window.Hide();
            StageScreenLost?.Invoke(this, display.DeviceName);
        }
        else if (same != display)
        {
            window.ShowOn(same);
        }
    }
}
