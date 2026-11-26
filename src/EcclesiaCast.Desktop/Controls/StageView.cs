using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using EcclesiaCast.Core.Stage;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Desktop.Controls;

/// <summary>
/// What the musicians and the preacher see, in Avalonia: the WPF
/// <c>StageView</c>. Each box of the chosen <see cref="StageLayout"/> is a
/// <see cref="StageBoxView"/>, placed on a 1920×1080 canvas that is scaled to
/// the size of the screen. The clock ticks once a second while it is shown.
/// </summary>
public sealed class StageView : UserControl
{
    public static readonly StyledProperty<StageLayout?> LayoutProperty =
        AvaloniaProperty.Register<StageView, StageLayout?>(nameof(Layout));

    private readonly DispatcherTimer _clock = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly List<StageBoxView> _boxes = [];
    private readonly Canvas _surface;
    private ProjectionViewModel? _vm;

    public StageView()
    {
        Background = Brushes.Black;
        _surface = new Canvas
        {
            Width = StageLayout.CanvasWidth,
            Height = StageLayout.CanvasHeight,
            ClipToBounds = true,
        };
        Content = new Viewbox { Stretch = Stretch.Uniform, Child = _surface };

        _clock.Tick += (_, _) =>
        {
            foreach (var box in _boxes)
                box.Tick();
        };

        // The clock only runs while the control is on screen somewhere.
        AttachedToVisualTree += (_, _) =>
        {
            _clock.Start();
            foreach (var box in _boxes)
                box.Tick();
        };
        DetachedFromVisualTree += (_, _) => _clock.Stop();
    }

    /// <summary>Which arrangement is drawn; the operator picks it under ▾ Escenario.</summary>
    public StageLayout? Layout
    {
        get => GetValue(LayoutProperty);
        set => SetValue(LayoutProperty, value);
    }

    /// <summary>The boxes drawn now, for the tests.</summary>
    internal IReadOnlyList<StageBoxView> Boxes => _boxes;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == LayoutProperty)
        {
            Build();
        }
        else if (change.Property == DataContextProperty)
        {
            if (_vm is not null)
                _vm.PropertyChanged -= OnProjectionChanged;
            _vm = DataContext as ProjectionViewModel;
            if (_vm is not null)
                _vm.PropertyChanged += OnProjectionChanged;

            // The boxes hold the projection they were built with.
            Build();
        }
    }

    private void Build()
    {
        _surface.Children.Clear();
        _boxes.Clear();

        if (Layout is not { } layout)
            return;

        foreach (var box in layout.Boxes)
        {
            var view = new StageBoxView(box, _vm);
            _boxes.Add(view);
            _surface.Children.Add(view);
        }
    }

    private void OnProjectionChanged(object? sender, PropertyChangedEventArgs e)
    {
        // The video frame changes on every frame and is drawn by the mirror
        // box on its own; redrawing the text for it would be waste.
        if (e.PropertyName is nameof(ProjectionViewModel.LiveFrame) or nameof(ProjectionViewModel.LiveLogoFrame))
            return;

        foreach (var box in _boxes)
            box.Refresh();
    }
}
