using Avalonia;
using EcclesiaCast.Core.Displays;
using EcclesiaCast.Core.Media;
using EcclesiaCast.Desktop.Platform;
using EcclesiaCast.Desktop.Views;
using EcclesiaCast.Presentation.Services;
using EcclesiaCast.Presentation.ViewModels;
using Serilog;

namespace EcclesiaCast.Desktop.Services;

/// <summary>
/// The output window, in Avalonia: what the WPF <c>ProjectionWindowService</c>
/// does, and two things more.
/// <list type="bullet">
/// <item>While the output is on, <see cref="IPowerGuard"/> keeps the system
/// awake and the program at its pace.</item>
/// <item>When the screen of the output is unplugged the window is hidden at
/// once (its video paused), the program hears the output went off, and
/// <see cref="OutputScreenLost"/> says so to warn the operator. Plugged in
/// again, projecting works as always.</item>
/// </list>
/// </summary>
public sealed class ProjectionWindowService : IProjectionWindowService
{
    private readonly ProjectionViewModel _projection;
    private readonly IOutputWindowNative _native;
    private readonly IOperatorScreen _operatorScreen;
    private readonly IPowerGuard _power;
    private readonly IScreenList _screens;
    private OutputWindow? _window;
    private bool _windowClosed;
    private bool _projecting;

    public ProjectionWindowService(ProjectionViewModel projection, IOutputWindowNative native,
        IOperatorScreen operatorScreen, IPowerGuard power, IScreenList screens)
    {
        _projection = projection;
        _native = native;
        _operatorScreen = operatorScreen;
        _power = power;
        _screens = screens;
        _screens.Changed += (_, _) => OnScreensChanged();
    }

    public bool IsOutputVisible => _window is { IsVisible: true } && !_windowClosed;

    public event EventHandler? VisibilityChanged;

    public event EventHandler? VideoEnded;

    /// <summary>Never raised: this output has no YouTube player yet (docs/plan-mac-fase-3.md).</summary>
    public event EventHandler<int>? YouTubeRefused
    {
        add { }
        remove { }
    }

    public event EventHandler? TurnOffRequested;

    /// <summary>The screen the output was on went away (its name); the output is already off.</summary>
    public event EventHandler<string>? OutputScreenLost;

    /// <summary>The window, for the tests.</summary>
    internal OutputWindow? Window => _window;

    public void EnsureVisible(DisplayInfo display)
    {
        if (_window is null || _windowClosed)
        {
            _window = new OutputWindow(_projection, _native, _operatorScreen);
            _windowClosed = false;
            var window = _window;
            window.PropertyChanged += (_, e) =>
            {
                if (e.Property == Visual.IsVisibleProperty && ReferenceEquals(window, _window))
                    OnVisibilityChanged();
            };
            window.Closed += (_, _) =>
            {
                if (!ReferenceEquals(window, _window))
                    return;
                _windowClosed = true;
                OnVisibilityChanged();
            };
            window.VideoEnded += (_, _) => VideoEnded?.Invoke(this, EventArgs.Empty);
            window.TurnOffRequested += (_, _) => TurnOffRequested?.Invoke(this, EventArgs.Empty);
        }

        // Still fading to black to switch off counts as hidden: its video was
        // already paused, and it has to pick up again.
        var wasHidden = !_window.IsVisible || _window.IsFadingOut;
        _window.ShowOn(display);

        // Coming back from "output off": pick the video up where it stopped.
        if (wasHidden)
            _window.ResumeAfterHiddenOutput();
    }

    /// <summary>Hides the output. The video is paused too, so its sound stops with it.</summary>
    public void HideOutput()
    {
        _window?.PauseForHiddenOutput();
        _window?.FadeOutAndHide();
    }

    // ── Transporte del video proyectado ──────────────────────────

    public PlaybackState Playback => _window?.PlaybackState ?? PlaybackState.None;

    public void TogglePlayPause() => _window?.TogglePlayPause();

    public void SeekTo(TimeSpan position) => _window?.SeekTo(position);

    public void Skip(TimeSpan delta) => _window?.Skip(delta);

    // ── Encendido y pantallas ────────────────────────────────────

    private void OnVisibilityChanged()
    {
        var on = IsOutputVisible;
        if (on != _projecting)
        {
            _projecting = on;
            if (on)
                _power.BeginProjecting();
            else
                _power.EndProjecting();
        }

        VisibilityChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnScreensChanged()
    {
        if (_window is not { IsVisible: true } window || _windowClosed || window.Display is not { } display)
            return;

        var now = _screens.GetDisplays();
        if (AvaloniaDisplayProvider.Find(display, now) is not { } same)
        {
            Log.Warning("Se desconectó la pantalla de la salida ({Display}): se apaga la salida", display.DeviceName);
            window.PauseForHiddenOutput();
            window.HideNow();
            OutputScreenLost?.Invoke(this, display.DeviceName);
            return;
        }

        // Back in another mode, or moved in the arrangement: it follows.
        if (same != display)
        {
            Log.Information("La pantalla de la salida cambió ({Display}): se vuelve a ubicar", same.DeviceName);
            window.ShowOn(same);
        }
    }
}
