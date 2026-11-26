using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Ui;

namespace EcclesiaCast.App.Controls;

/// <summary>
/// The welcome tour drawn over the operator window: everything is blurred
/// and darkened except one part, lit through a rounded hole, and a card
/// beside it says what that part is for. The steps, and where the card goes,
/// come from <see cref="WelcomeTour"/>; the window says which element each
/// step lights and gets the chance to prepare it (the Biblia tab, say).
/// </summary>
public partial class WelcomeTourOverlay : UserControl
{
    private const double SpotPadding = 8;
    private const double CardGap = 16;
    private const double SpotRadius = 10;
    private static readonly TimeSpan Glide = TimeSpan.FromMilliseconds(320);

    private IReadOnlyList<TourStep> _steps = WelcomeTour.Steps;
    private bool _isWelcome = true;
    private Func<string, FrameworkElement?> _find = _ => null;
    private Action<TourStep?>? _prepare;
    private TourRun _run = TourRun.ForTutorial([]);
    private FrameworkElement? _content;
    private int _index = -1;

    // The lit rectangle as drawn now, and the glide from one part to the next.
    private Rect _spot = Rect.Empty;
    private Rect _glideFrom;
    private Rect _glideTo;
    private DateTime _glideStart;
    private bool _gliding;

    public WelcomeTourOverlay()
    {
        InitializeComponent();
        SizeChanged += (_, _) =>
        {
            if (!IsRunning)
                return;
            SizeBlur();
            Show(_index, animate: false);
        };
        PreviewKeyDown += (_, e) => HandleKey(e);
    }

    /// <summary>Raised once when the tour ends, finished or skipped.</summary>
    public event EventHandler? Finished;

    public bool IsRunning => _index >= 0;

    /// <summary>
    /// Starts at the greeting, or at the first step of <paramref name="steps"/> (a tutorial). <paramref name="content"/> is what gets
    /// blurred behind; <paramref name="find"/> gives the element a step
    /// lights; <paramref name="prepare"/> is told each step before it is
    /// measured (it opens what the step talks about), and null when the tour ends.
    /// Which steps there are is decided here, once (see <see cref="TourRun"/>).
    /// </summary>
    public void Start(FrameworkElement content, Func<string, FrameworkElement?> find, Action<TourStep?>? prepare,
        IReadOnlyList<TourStep>? steps = null)
    {
        _isWelcome = steps is null;
        _steps = steps ?? WelcomeTour.Steps;
        _content = content;
        _find = find;
        _prepare = prepare;
        Visibility = Visibility.Visible;
        UpdateLayout();
        SetUpBlur();
        _spot = Rect.Empty;
        _run = _isWelcome
            ? TourRun.ForWelcome(_steps, IsOnShow)
            : TourRun.ForTutorial(_steps);
        Show(Math.Max(0, _run.First), animate: false);
        Dispatcher.BeginInvoke(() => Keyboard.Focus(NextButton), DispatcherPriority.Input);
    }

    /// <summary>Takes the tour away and gives the window back as it was.</summary>
    public void Stop()
    {
        if (!IsRunning)
            return;

        _index = -1;
        StopGlide();
        Blurred.Fill = null;
        Blurred.Effect = null;
        Visibility = Visibility.Collapsed;
        _prepare?.Invoke(null);
        Finished?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Keys while the tour is on: arrows and Enter move, Esc closes. Every
    /// other key is swallowed too, so F1 or Esc never reach the projection
    /// behind the tour. The window passes its keys here while it runs.
    /// </summary>
    public void HandleKey(KeyEventArgs e)
    {
        if (!IsRunning || e.Handled)
            return;

        switch (e.Key)
        {
            case Key.Escape:
                Stop();
                break;
            case Key.Enter:
            case Key.Space:
                // On «Atrás» or «Saltar», the button itself answers.
                if (Keyboard.FocusedElement is Button button && Card.IsAncestorOf(button))
                    return;
                Move(1);
                break;
            case Key.Right:
                Move(1);
                break;
            case Key.Left:
                Move(-1);
                break;
            case Key.Tab:
                return; // moves between the card's buttons
        }

        e.Handled = true;
    }

    private void Next_Click(object sender, RoutedEventArgs e) => Move(1);

    private void Back_Click(object sender, RoutedEventArgs e) => Move(-1);

    private void Skip_Click(object sender, RoutedEventArgs e) => Stop();

    private void Move(int direction)
    {
        var next = _run.Next(_index, direction);
        if (next >= 0)
            Show(next, animate: true);
        else if (direction > 0)
            Stop();
    }

    /// <summary>
    /// Something that opens next to the lit part (a menu's drop-down): the lit
    /// rectangle grows to hold it, in this overlay's coordinates.
    /// </summary>
    public Func<TourStep, Rect?>? ExtraSpot { get; set; }

    /// <summary>
    /// Where another window sits over this one (a window the step opened): the
    /// card stays out of it, in this overlay's coordinates.
    /// </summary>
    public Func<TourStep, Rect?>? KeepClear { get; set; }

    /// <summary>Puts the keyboard back on the card, after a step opened a window that took it.</summary>
    public void FocusNext() => Keyboard.Focus(NextButton);

    // The welcome tour opens nothing: a part shrunk to icons is left out.
    private bool IsOnShow(string target) =>
        _find(target) is { IsVisible: true } element && element.ActualWidth > 24 && element.ActualHeight > 24;

    // Small on purpose: a one-line box or a row of buttons is barely 24 tall.
    private bool IsMeasurable(string target) =>
        _find(target) is { IsVisible: true } element && element.ActualWidth > 4 && element.ActualHeight > 4;

    private void Show(int index, bool animate)
    {
        _index = index;
        var step = _steps[index];
        _prepare?.Invoke(step);
        Place(index, animate);

        // What the step has just opened may finish laying itself out after
        // this (a list filling up, a column changing width): look again.
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
        {
            if (_index == index)
                Place(index, animate: false, onlyIfMoved: true);
        });
    }

    /// <summary>Measures the step's part, and puts the light and the card where they go.</summary>
    private void Place(int index, bool animate, bool onlyIfMoved = false)
    {
        var step = _steps[index];
        UpdateLayout();

        // A part inside a list with a scroll bar (the bottom of «Tiempos»)
        // is brought into view before it is measured.
        if (step.Target is { } inside && _find(inside) is { IsVisible: true } scrolled)
        {
            scrolled.BringIntoView();
            UpdateLayout();
        }

        var area = new TourBox(0, 0, ActualWidth, ActualHeight);
        var spot = default(TourBox);
        if (step.Target is { } part && IsMeasurable(part) && _find(part) is { } element)
        {
            var bounds = element.TransformToVisual(this).TransformBounds(new Rect(element.RenderSize));
            if (ExtraSpot?.Invoke(step) is { IsEmpty: false } extra)
                bounds.Union(extra);
            spot = WelcomeTour.Spot(new TourBox(bounds.X, bounds.Y, bounds.Width, bounds.Height), area, SpotPadding);
        }

        // The card keeps to the side of the window that another window left free.
        var cardArea = area;
        if (KeepClear?.Invoke(step) is { IsEmpty: false } busy)
        {
            cardArea = busy.X + busy.Width / 2 > area.Width / 2
                ? new TourBox(0, 0, Math.Max(Card.Width + 2 * CardGap, busy.X), area.Height)
                : new TourBox(Math.Min(busy.Right, area.Width - Card.Width - 2 * CardGap), 0,
                    area.Width - Math.Min(busy.Right, area.Width - Card.Width - 2 * CardGap), area.Height);
        }

        var target = spot.IsEmpty ? Rect.Empty : new Rect(spot.X, spot.Y, spot.Width, spot.Height);
        if (onlyIfMoved && target == (_gliding ? _glideTo : _spot))
            return;

        FillCard(index, step);
        Card.Measure(new Size(Card.Width, double.PositiveInfinity));
        var (x, y) = WelcomeTour.PlaceCard(spot, cardArea, Card.Width, Card.DesiredSize.Height, CardGap);
        Canvas.SetLeft(Card, Math.Round(x));
        Canvas.SetTop(Card, Math.Round(y));
        if (animate)
            Card.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220)));

        GlideTo(target, animate || (onlyIfMoved && _gliding));
    }

    private void FillCard(int index, TourStep step)
    {
        var first = _run.IsFirst(index);
        var last = _run.IsLast(index);

        // «3 de 9», with the total fixed since the tour started.
        var count = _run.Count(_steps, index);
        StepCount.Text = count is { } c ? $"{c.Number} de {c.Total}" : string.Empty;
        StepCount.Visibility = count is null ? Visibility.Collapsed : Visibility.Visible;

        TitleText.Text = step.Title;
        BodyText.Text = step.Text;
        PictureHost.Content = step.Picture is { } picture ? TutorialPicture.Create(picture) : null;
        NextButton.Content = _isWelcome && first ? "Empezar el recorrido" : last ? "Terminar" : "Siguiente";
        BackButton.Visibility = first || (_isWelcome && last) ? Visibility.Collapsed : Visibility.Visible;
        SkipButton.Content = _isWelcome && first ? "Ahora no" : _isWelcome ? "Saltar el recorrido" : "Salir del tutorial";
        SkipButton.Visibility = last ? Visibility.Collapsed : Visibility.Visible;
    }

    // ── El foco: se desliza de una parte a la otra ────────────────

    private void GlideTo(Rect target, bool animate)
    {
        if (!animate || target.IsEmpty)
        {
            StopGlide();
            ApplySpot(target);
            return;
        }

        // From nothing, the hole opens from the middle of the part.
        _glideFrom = _spot.IsEmpty
            ? new Rect(target.X + target.Width / 2, target.Y + target.Height / 2, 0, 0)
            : _spot;
        _glideTo = target;
        _glideStart = DateTime.UtcNow;
        if (!_gliding)
        {
            _gliding = true;
            CompositionTarget.Rendering += OnRendering;
        }
    }

    private void StopGlide()
    {
        if (!_gliding)
            return;
        _gliding = false;
        CompositionTarget.Rendering -= OnRendering;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        var t = Math.Clamp((DateTime.UtcNow - _glideStart).TotalMilliseconds / Glide.TotalMilliseconds, 0, 1);
        var ease = 1 - Math.Pow(1 - t, 3);
        double Lerp(double a, double b) => a + (b - a) * ease;
        ApplySpot(new Rect(
            Lerp(_glideFrom.X, _glideTo.X), Lerp(_glideFrom.Y, _glideTo.Y),
            Lerp(_glideFrom.Width, _glideTo.Width), Lerp(_glideFrom.Height, _glideTo.Height)));
        if (t >= 1)
            StopGlide();
    }

    private void ApplySpot(Rect spot)
    {
        _spot = spot;
        Geometry whole = new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight));
        var shape = spot.IsEmpty || spot.Width < 1 || spot.Height < 1
            ? whole
            : new CombinedGeometry(GeometryCombineMode.Exclude, whole, new RectangleGeometry(spot, SpotRadius, SpotRadius));
        shape.Freeze();
        Shade.Data = shape;
        Blurred.Clip = shape;

        if (spot.IsEmpty || spot.Width < 1)
        {
            Ring.Visibility = Visibility.Collapsed;
            return;
        }

        Canvas.SetLeft(Ring, spot.X);
        Canvas.SetTop(Ring, spot.Y);
        Ring.Width = spot.Width;
        Ring.Height = spot.Height;
        Ring.Visibility = Visibility.Visible;
    }

    // ── Lo de atrás, borroso ──────────────────────────────────────

    /// <summary>
    /// A live, blurred copy of the window under the dark layer. In the mode
    /// for slow computers there is no blur, just a darker layer.
    /// </summary>
    private void SetUpBlur()
    {
        if (PerformanceMode.IsLow || _content is null)
        {
            Blurred.Fill = null;
            Blurred.Effect = null;
            Shade.Fill = new SolidColorBrush(Color.FromArgb(0xD8, 0x09, 0x0B, 0x10));
            return;
        }

        Shade.Fill = new SolidColorBrush(Color.FromArgb(0xB0, 0x09, 0x0B, 0x10));
        Blurred.Fill = new VisualBrush(_content)
        {
            Stretch = Stretch.Fill,
            ViewboxUnits = BrushMappingMode.Absolute,
        };
        Blurred.Effect = new BlurEffect { Radius = 7, KernelType = KernelType.Gaussian, RenderingBias = RenderingBias.Performance };
        SizeBlur();
    }

    private void SizeBlur()
    {
        Blurred.Width = ActualWidth;
        Blurred.Height = ActualHeight;
        if (Blurred.Fill is VisualBrush brush && _content is not null)
        {
            var origin = _content.TransformToVisual(this).Transform(new Point(0, 0));
            brush.Viewbox = new Rect(0, 0, _content.ActualWidth, _content.ActualHeight);
            Canvas.SetLeft(Blurred, origin.X);
            Canvas.SetTop(Blurred, origin.Y);
            Blurred.Width = _content.ActualWidth;
            Blurred.Height = _content.ActualHeight;
        }
    }
}
