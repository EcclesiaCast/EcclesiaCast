using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using EcclesiaCast.App.Services;
using EcclesiaCast.App.ViewModels;
using EcclesiaCast.Core.Displays;
using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Core.Themes;

namespace EcclesiaCast.App.Views;

/// <summary>
/// The monitor the musicians and the preacher look at: the words that are on
/// the projector right now, what comes next, the time of day and how long the
/// service has been running. Deliberately plain — white on black, no
/// backgrounds — because it is read from a distance and under stage lights.
/// </summary>
public partial class StageWindow : Window
{
    private readonly DispatcherTimer _clock = new() { Interval = TimeSpan.FromSeconds(1) };
    private DisplayInfo? _display;

    /// <summary>When the service timer was last reset.</summary>
    private DateTime _timerStart = DateTime.Now;

    public StageWindow()
    {
        InitializeComponent();

        _clock.Tick += (_, _) => UpdateClocks();
        _clock.Start();
        UpdateClocks();

        DataContextChanged += OnDataContextChanged;
    }

    /// <summary>Which parts are shown; the operator picks them in Ajustes.</summary>
    public StageOptions Options { get; private set; } = new();

    public void ApplyOptions(StageOptions options)
    {
        Options = options;
        TopBar.Visibility = options.ShowClock || options.ShowTimer
            ? Visibility.Visible
            : Visibility.Collapsed;
        ClockText.Visibility = options.ShowClock ? Visibility.Visible : Visibility.Hidden;
        TimerText.Visibility = options.ShowTimer ? Visibility.Visible : Visibility.Hidden;
        NextPanel.Visibility = options.ShowNext ? Visibility.Visible : Visibility.Collapsed;
        CurrentText.FontSize = Math.Clamp(options.TextScale, 20, 400);
        Render();
    }

    /// <summary>Restarts the service timer (the operator's ⟲ button).</summary>
    public void ResetTimer()
    {
        _timerStart = DateTime.Now;
        UpdateClocks();
    }

    /// <summary>Raised when the operator double-clicks the stage screen to close it.</summary>
    public event EventHandler? CloseRequested;

    public void ShowOn(DisplayInfo display)
    {
        _display = display;
        Show();
        MoveToDisplay();
        ShowHelpHint();
    }

    /// <summary>
    /// Flashes the way out for a few seconds. The window takes no keyboard
    /// focus by design, so Esc from the operator panel can't reach it; a
    /// double click can, and it works even when this window covers the panel.
    /// </summary>
    private void ShowHelpHint()
    {
        HelpHint.Visibility = Visibility.Visible;
        HelpHint.Opacity = 1;

        var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(700))
        {
            BeginTime = TimeSpan.FromSeconds(6),
        };
        fade.Completed += (_, _) => HelpHint.Visibility = Visibility.Collapsed;
        HelpHint.BeginAnimation(OpacityProperty, fade);
    }

    private void Stage_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) =>
        CloseRequested?.Invoke(this, EventArgs.Empty);

    // ── Contenido ────────────────────────────────────────────────

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is INotifyPropertyChanged oldVm)
            oldVm.PropertyChanged -= OnProjectionChanged;
        if (e.NewValue is INotifyPropertyChanged newVm)
            newVm.PropertyChanged += OnProjectionChanged;
        Render();
    }

    private void OnProjectionChanged(object? sender, PropertyChangedEventArgs e) => Render();

    private void Render()
    {
        if (DataContext is not ProjectionViewModel vm)
            return;

        // Black and Clear hide the words from the congregation, but the stage
        // still needs to read them — that is the point of a stage display.
        var text = vm.Slide?.MainText ?? string.Empty;
        CurrentText.Text = Transform(text, vm.Slide?.Theme);

        // While a countdown runs it is what the congregation sees, so it is
        // what the band and whoever is preaching need to see too — they are
        // the ones who have to be in place when it hits zero.
        if (vm.Countdown is not null)
        {
            RenderCountdown(vm);
            return;
        }

        LabelText.Text = vm.State switch
        {
            OutputState.Black => "⏸  PANTALLA EN NEGRO",
            OutputState.Logo => "🖼  LOGO",
            OutputState.Clear => vm.Slide is null ? "—" : "👁  SIN TEXTO EN PANTALLA",
            _ => vm.SlideLabel ?? string.Empty,
        };

        NextText.Text = vm.NextSlide is { } next
            ? Transform(next.MainText, next.Theme)
            : "—";

        var hasOverlay = !string.IsNullOrWhiteSpace(vm.Overlay);
        OverlayPanel.Visibility = hasOverlay ? Visibility.Visible : Visibility.Collapsed;
        OverlayText.Text = vm.Overlay ?? string.Empty;
    }

    private void RenderCountdown(ProjectionViewModel vm)
    {
        if (vm.Countdown is not { } countdown)
            return;

        var finished = countdown.HasFinished(DateTimeOffset.Now);

        LabelText.Text = "⏱  CUENTA REGRESIVA";
        CurrentText.Text = countdown.Format(DateTimeOffset.Now);
        NextText.Text = finished || string.IsNullOrWhiteSpace(countdown.Heading)
            ? "—"
            : countdown.Heading;

        var hasOverlay = !string.IsNullOrWhiteSpace(vm.Overlay);
        OverlayPanel.Visibility = hasOverlay ? Visibility.Visible : Visibility.Collapsed;
        OverlayText.Text = vm.Overlay ?? string.Empty;
    }

    /// <summary>
    /// Matches the projector's casing so the singers read what the
    /// congregation reads; everything else about the look stays plain.
    /// </summary>
    private static string Transform(string text, SlideTheme? theme) =>
        (theme?.TextCase ?? TextCase.None) switch
        {
            TextCase.Upper => text.ToUpper(CultureInfo.CurrentCulture),
            TextCase.Lower => text.ToLower(CultureInfo.CurrentCulture),
            TextCase.Title => CultureInfo.CurrentCulture.TextInfo
                .ToTitleCase(text.ToLower(CultureInfo.CurrentCulture)),
            _ => text,
        };

    private void UpdateClocks()
    {
        ClockText.Text = DateTime.Now.ToString("HH:mm", CultureInfo.CurrentCulture);

        // The countdown ticks on its own; the clock timer is already running
        // every second, so it does the redrawing rather than a second timer.
        if (DataContext is ProjectionViewModel { Countdown: not null } counting)
            RenderCountdown(counting);

        var elapsed = DateTime.Now - _timerStart;
        TimerText.Text = elapsed.TotalHours >= 1
            ? elapsed.ToString(@"h\:mm\:ss")
            : elapsed.ToString(@"m\:ss");
    }

    // ── Posición en el monitor de escenario ──────────────────────

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var hwnd = new WindowInteropHelper(this).Handle;
        _ = SetWindowLong(hwnd, GWL_EXSTYLE, GetWindowLong(hwnd, GWL_EXSTYLE) | WS_EX_NOACTIVATE);
        MoveToDisplay();
    }

    protected override void OnClosed(EventArgs e)
    {
        _clock.Stop();
        base.OnClosed(e);
    }

    private void MoveToDisplay()
    {
        if (_display is null)
            return;

        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero)
            return;

        SetWindowPos(hwnd, IntPtr.Zero,
            _display.X, _display.Y, _display.Width, _display.Height,
            SWP_NOZORDER | SWP_FRAMECHANGED | SWP_SHOWWINDOW | SWP_NOACTIVATE);
    }

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_FRAMECHANGED = 0x0020;
    private const uint SWP_SHOWWINDOW = 0x0040;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
}
