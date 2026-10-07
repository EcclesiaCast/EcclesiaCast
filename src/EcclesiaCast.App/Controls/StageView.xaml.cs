using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using EcclesiaCast.App.ViewModels;
using EcclesiaCast.Core.Stage;

namespace EcclesiaCast.App.Controls;

/// <summary>
/// What the musicians and the preacher see, laid out as the operator drew it:
/// each box of the chosen <see cref="StageLayout"/> shows one thing (the live
/// words, what comes next, the time, the stopwatch...). Deliberately plain,
/// because it is read from a distance and under stage lights.
/// The stage window shows it full screen; the operator's preview shows the
/// same control shrunk, so what the operator checks is what the stage gets.
/// </summary>
public partial class StageView : UserControl
{
    private readonly DispatcherTimer _clock = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly List<StageBoxView> _boxes = [];
    private ProjectionViewModel? _vm;

    public StageView()
    {
        InitializeComponent();

        _clock.Tick += (_, _) =>
        {
            foreach (var box in _boxes)
                box.Tick();
        };

        // The clock only runs while the control is on screen somewhere.
        Loaded += (_, _) =>
        {
            _clock.Start();
            foreach (var box in _boxes)
                box.Tick();
        };
        Unloaded += (_, _) => _clock.Stop();

        DataContextChanged += OnDataContextChanged;
    }

    /// <summary>Which arrangement is drawn; the operator picks it under ▾ Escenario.</summary>
    public static readonly DependencyProperty LayoutProperty =
        DependencyProperty.Register(nameof(Layout), typeof(StageLayout), typeof(StageView),
            new PropertyMetadata(null, (d, _) => ((StageView)d).Build()));

    public StageLayout? Layout
    {
        get => (StageLayout?)GetValue(LayoutProperty);
        set => SetValue(LayoutProperty, value);
    }

    private void Build()
    {
        Surface.Children.Clear();
        _boxes.Clear();

        if (Layout is not { } layout)
            return;

        foreach (var box in layout.Boxes)
        {
            var view = new StageBoxView(box, _vm);
            _boxes.Add(view);
            Surface.Children.Add(view);
        }
    }

    // ── Contenido ────────────────────────────────────────────────

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_vm is not null)
            _vm.PropertyChanged -= OnProjectionChanged;
        _vm = e.NewValue as ProjectionViewModel;
        if (_vm is not null)
            _vm.PropertyChanged += OnProjectionChanged;

        // The boxes hold the projection they were built with.
        Build();
    }

    private void OnProjectionChanged(object? sender, PropertyChangedEventArgs e)
    {
        // The video frame changes on every frame and is drawn by the mirror
        // box through its own binding; redrawing the text for it would be waste.
        if (e.PropertyName == nameof(ProjectionViewModel.LiveFrame))
            return;

        foreach (var box in _boxes)
            box.Refresh();
    }
}
