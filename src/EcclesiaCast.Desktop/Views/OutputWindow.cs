using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using EcclesiaCast.Core.Displays;
using EcclesiaCast.Core.Media;
using EcclesiaCast.Desktop.Controls;
using EcclesiaCast.Desktop.Services;
using EcclesiaCast.Presentation.Services;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Desktop.Views;

/// <summary>
/// The projector's window, in Avalonia: the WPF <c>OutputWindow</c>. It holds
/// a <see cref="ProjectedView"/> (which brings its own three players: the
/// background video, its fill and the video logo) and, above it, the black
/// shutter of «apagar la salida» and the way back with a single screen. The
/// slide is laid out on 1920×1080 and scaled to the screen by the view.
/// </summary>
public sealed class OutputWindow : FullScreenWindow
{
    private readonly ProjectionViewModel _vm;
    private readonly IOperatorScreen _operatorScreen;
    private readonly Rectangle _shutter;
    private readonly Border _wayBack;

    public OutputWindow(ProjectionViewModel projection, IOutputWindowNative native, IOperatorScreen operatorScreen)
        : base(native)
    {
        _vm = projection;
        _operatorScreen = operatorScreen;
        Title = "EcclesiaCast — Salida";

        Projected = new ProjectedView { IsLiveOutput = true, AnimateTransitions = true };
        _shutter = new Rectangle { Fill = Brushes.Black, Opacity = 0, IsVisible = false, IsHitTestVisible = false };
        _wayBack = Hint("Para volver al programa: tecla Esc o doble clic en esta pantalla");

        var layers = new Grid();
        layers.Children.Add(Projected);
        layers.Children.Add(_shutter);
        layers.Children.Add(_wayBack);
        Content = layers;

        Projected.VideoEnded += (_, _) => VideoEnded?.Invoke(this, EventArgs.Empty);
        Projected.VideoFrameChanged += (_, frame) => _vm.LiveFrame = frame;
        Projected.LogoFrameChanged += (_, frame) => _vm.LiveLogoFrame = frame;
        Projected.VideoFrameWritten += (_, _) => LiveFrames.RaiseVideoWritten();
        Projected.LogoFrameWritten += (_, _) => LiveFrames.RaiseLogoWritten();

        Copy();
        _vm.PropertyChanged += OnProjectionChanged;

        DoubleTapped += (_, e) =>
        {
            if (!CoversOperator)
                return;
            e.Handled = true;
            TurnOffRequested?.Invoke(this, EventArgs.Empty);
        };
        // The window never takes the keyboard on purpose, so Esc reaches the
        // operator's window; should a click give it the keyboard all the same,
        // Esc works here too.
        KeyDown += (_, e) =>
        {
            if (e.Key != Key.Escape || !CoversOperator)
                return;
            e.Handled = true;
            TurnOffRequested?.Invoke(this, EventArgs.Empty);
        };
        // The frames per second and the memory, in the log while it is on.
        _report = new VideoReport(() => Projected.Video);
        PropertyChanged += (_, e) =>
        {
            if (e.Property != IsVisibleProperty)
                return;
            if (IsVisible)
                _report.Start();
            else
                _report.Stop();
        };

        Closed += (_, _) =>
        {
            _report.Stop();
            _vm.PropertyChanged -= OnProjectionChanged;
            Projected.StopVideos();
        };
    }

    private readonly VideoReport _report;

    /// <summary>The projected picture, with its players.</summary>
    public ProjectedView Projected { get; }

    /// <summary>Raised when the projected video finishes and shouldn't loop.</summary>
    public event EventHandler? VideoEnded;

    /// <summary>
    /// Raised when the operator double-clicks the output (or presses Esc on
    /// it) to switch it off. Only while it covers the operator panel: on a
    /// projector of its own a stray double click must never take the words
    /// off the screen.
    /// </summary>
    public event EventHandler? TurnOffRequested;

    private bool CoversOperator => Display is { } display && _operatorScreen.Holds(display);

    private void OnProjectionChanged(object? sender, PropertyChangedEventArgs e) => Copy();

    /// <summary>What is projected, from the projection's view model.</summary>
    private void Copy()
    {
        Projected.Slide = _vm.Slide;
        Projected.State = _vm.State;
        Projected.Overlay = _vm.Overlay;
        Projected.OverlayFontFamily = _vm.OverlayFontFamily;
        Projected.OverlayFontSize = _vm.OverlayFontSize;
        Projected.Countdown = _vm.Countdown;
        Projected.Highlight = _vm.Highlight;
        Projected.BackgroundMedia = _vm.Background;
        Projected.FillMedia = _vm.BackgroundFill;
        Projected.BackgroundBlur = _vm.BackgroundBlur;
        Projected.Logo = _vm.ActiveLogo;
    }

    /// <summary>Shows this window full screen on the given display.</summary>
    public void ShowOn(DisplayInfo display)
    {
        var wasHidden = !IsVisible || IsFadingOut;

        // Something was projected while the screen was still fading to black
        // to switch off: it stays on.
        CancelFadeOut();

        ShowOnDisplay(display);

        if (wasHidden && !s_wayBackShown && _operatorScreen.Holds(display))
        {
            s_wayBackShown = true;
            Flash(_wayBack);
        }
    }

    /// <summary>
    /// Once per run: congregations that project with a single screen (a
    /// notebook, or a projector mirroring it) may see it too, so it is not
    /// repeated every time the output comes on.
    /// </summary>
    private static bool s_wayBackShown;

    // ── Apagar la salida con fundido ─────────────────────────────

    private int _shutterVersion;

    /// <summary>True while the screen is fading to black before the window hides.</summary>
    public bool IsFadingOut { get; private set; }

    /// <summary>
    /// Switches the output off: fades to black over the time set in
    /// «Transiciones» (if any) and then hides the window.
    /// </summary>
    public async void FadeOutAndHide()
    {
        if (IsFadingOut)
            return;

        var fade = PerformanceMode.IsLow ? TimeSpan.Zero : TransitionSettings.Current.Output;
        if (fade <= TimeSpan.Zero || !IsVisible)
        {
            Hide();
            return;
        }

        var version = ++_shutterVersion;
        IsFadingOut = true;
        _shutter.IsVisible = true;
        await Fades.Fade(_shutter, 0, 1, fade);
        if (version != _shutterVersion)
            return;

        IsFadingOut = false;
        Hide();
        ResetShutter();
    }

    /// <summary>Its screen went away: hidden at once, without a fade.</summary>
    public void HideNow()
    {
        _shutterVersion++;
        IsFadingOut = false;
        Hide();
        ResetShutter();
    }

    private void CancelFadeOut()
    {
        if (!IsFadingOut && !_shutter.IsVisible)
            return;

        _shutterVersion++;
        IsFadingOut = false;
        ResetShutter();
    }

    private void ResetShutter()
    {
        Fades.Stop(_shutter, OpacityProperty);
        _shutter.Opacity = 0;
        _shutter.IsVisible = false;
    }

    // ── Transporte ───────────────────────────────────────────────

    public PlaybackState PlaybackState => Projected.PlaybackState;

    public void TogglePlayPause() => Projected.TogglePlayPause();

    public void SeekTo(TimeSpan position) => Projected.SeekTo(position);

    public void Skip(TimeSpan delta) => Projected.Skip(delta);

    public void PauseForHiddenOutput() => Projected.PauseForHiddenOutput();

    public void ResumeAfterHiddenOutput() => Projected.ResumeAfterHiddenOutput();
}
