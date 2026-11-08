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

    private readonly IReadOnlyList<TourStep> _steps = WelcomeTour.Steps;
    private Func<string, FrameworkElement?> _find = _ => null;
    private Action<string?>? _prepare;
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
    /// Starts at the greeting. <paramref name="content"/> is what gets
    /// blurred behind; <paramref name="find"/> gives the element a step
    /// lights; <paramref name="prepare"/> is told each step's part before it
    /// is measured, and null when the tour ends.
    /// </summary>
    public void Start(FrameworkElement content, Func<string, FrameworkElement?> find, Action<string?>? prepare)
    {
        _content = content;
        _find = find;
        _prepare = prepare;
        Visibility = Visibility.Visible;
        UpdateLayout();
        SetUpBlur();
        _spot = Rect.Empty;
        Show(0, animate: false);
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
        var next = WelcomeTour.Next(_steps, _index, direction, IsShown);
        if (next >= 0)
            Show(next, animate: true);
        else if (direction > 0)
            Stop();
    }

    private bool IsShown(string target) =>
        _find(target) is { IsVisible: true } element && element.ActualWidth > 24 && element.ActualHeight > 24;

    private void Show(int index, bool animate)
    {
        _index = index;
        var step = _steps[index];
        if (step.Target is { } target)
            _prepare?.Invoke(target);
        UpdateLayout();

        var area = new TourBox(0, 0, ActualWidth, ActualHeight);
        var spot = default(TourBox);
        if (step.Target is { } part && IsShown(part) && _find(part) is { } element)
        {
            var bounds = element.TransformToVisual(this).TransformBounds(new Rect(element.RenderSize));
            spot = WelcomeTour.Spot(new TourBox(bounds.X, bounds.Y, bounds.Width, bounds.Height), area, SpotPadding);
        }

        FillCard(index, step);
        Card.Measure(new Size(Card.Width, double.PositiveInfinity));
        var (x, y) = WelcomeTour.PlaceCard(spot, area, Card.Width, Card.DesiredSize.Height, CardGap);
        Canvas.SetLeft(Card, Math.Round(x));
        Canvas.SetTop(Card, Math.Round(y));
        if (animate)
            Card.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220)));

        GlideTo(spot.IsEmpty ? Rect.Empty : new Rect(spot.X, spot.Y, spot.Width, spot.Height), animate);
    }

    private void FillCard(int index, TourStep step)
    {
        var first = index == 0;
        var last = index == _steps.Count - 1;

        // «3 de 9», counting only the parts that are on show in this window.
        var parts = Enumerable.Range(0, _steps.Count)
            .Where(i => _steps[i].Target is { } t && IsShown(t))
            .ToList();
        var position = parts.IndexOf(index);
        StepCount.Text = position >= 0 ? $"{position + 1} de {parts.Count}" : string.Empty;
        StepCount.Visibility = position >= 0 ? Visibility.Visible : Visibility.Collapsed;

        TitleText.Text = step.Title;
        BodyText.Text = step.Text;
        NextButton.Content = first ? "Empezar el recorrido" : last ? "Terminar" : "Siguiente";
        BackButton.Visibility = first || last ? Visibility.Collapsed : Visibility.Visible;
        SkipButton.Content = first ? "Ahora no" : "Saltar el recorrido";
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
