using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using EcclesiaCast.App.Services;
using EcclesiaCast.App.ViewModels;
using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Core.Themes;

namespace EcclesiaCast.App.Controls;

/// <summary>
/// What the musicians and the preacher see: the words that are on the
/// projector right now, what comes next, the time of day and how long the
/// service has been running. Deliberately plain — white on black, no
/// backgrounds — because it is read from a distance and under stage lights.
/// The stage window shows it full screen; the operator's preview shows the
/// same control shrunk, so what the operator checks is what the stage gets.
/// </summary>
public partial class StageView : UserControl
{
    private readonly DispatcherTimer _clock = new() { Interval = TimeSpan.FromSeconds(1) };
    private ProjectionViewModel? _vm;

    public StageView()
    {
        InitializeComponent();

        _clock.Tick += (_, _) => UpdateClocks();

        // The clock only runs while the control is on screen somewhere.
        Loaded += (_, _) =>
        {
            _clock.Start();
            UpdateClocks();
        };
        Unloaded += (_, _) => _clock.Stop();

        DataContextChanged += OnDataContextChanged;
    }

    /// <summary>Which parts are shown; the operator picks them under ▾ Escenario.</summary>
    public static readonly DependencyProperty OptionsProperty =
        DependencyProperty.Register(nameof(Options), typeof(StageOptions), typeof(StageView),
            new PropertyMetadata(null, (d, _) => ((StageView)d).ApplyOptions()));

    public StageOptions? Options
    {
        get => (StageOptions?)GetValue(OptionsProperty);
        set => SetValue(OptionsProperty, value);
    }

    private StageOptions CurrentOptions => Options ?? new StageOptions();

    private void ApplyOptions()
    {
        var options = CurrentOptions;
        TopBar.Visibility = options.ShowClock || options.ShowTimer
            ? Visibility.Visible
            : Visibility.Collapsed;
        ClockText.Visibility = options.ShowClock ? Visibility.Visible : Visibility.Hidden;
        TimerText.Visibility = options.ShowTimer ? Visibility.Visible : Visibility.Hidden;
        NextPanel.Visibility = options.ShowNext ? Visibility.Visible : Visibility.Collapsed;
        CurrentText.FontSize = Math.Clamp(options.TextScale, 20, 400);
        Render();
    }

    // ── Contenido ────────────────────────────────────────────────

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_vm is not null)
            _vm.PropertyChanged -= OnProjectionChanged;
        _vm = e.NewValue as ProjectionViewModel;
        if (_vm is not null)
            _vm.PropertyChanged += OnProjectionChanged;
        Render();
        UpdateClocks();
    }

    private void OnProjectionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProjectionViewModel.StageTimerStart))
            UpdateClocks();
        else if (e.PropertyName != nameof(ProjectionViewModel.LiveFrame))
            Render();
    }

    private void Render()
    {
        if (_vm is not { } vm)
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

        RenderOverlay(vm);
        RenderNotes(vm);
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

        RenderOverlay(vm);
        RenderNotes(vm);
    }

    private void RenderOverlay(ProjectionViewModel vm)
    {
        var hasOverlay = !string.IsNullOrWhiteSpace(vm.Overlay);
        OverlayPanel.Visibility = hasOverlay ? Visibility.Visible : Visibility.Collapsed;
        OverlayText.Text = vm.Overlay ?? string.Empty;
    }

    /// <summary>
    /// The outline for whoever is preaching. It shows up only here — the
    /// congregation never sees it, which is the whole reason it exists.
    /// </summary>
    private void RenderNotes(ProjectionViewModel vm)
    {
        var hasNotes = CurrentOptions.ShowNotes && !string.IsNullOrWhiteSpace(vm.StageNotes);
        NotesPanel.Visibility = hasNotes ? Visibility.Visible : Visibility.Collapsed;
        NotesText.Text = vm.StageNotes ?? string.Empty;
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
        if (_vm is { Countdown: not null } counting)
            RenderCountdown(counting);

        var elapsed = DateTime.Now - (_vm?.StageTimerStart ?? DateTime.Now);
        TimerText.Text = elapsed.TotalHours >= 1
            ? elapsed.ToString(@"h\:mm\:ss")
            : elapsed.ToString(@"m\:ss");
    }
}
