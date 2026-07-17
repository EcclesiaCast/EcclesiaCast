using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using EcclesiaCast.App.Services;
using EcclesiaCast.App.ViewModels;

namespace EcclesiaCast.App.Views;

public partial class ThemeManagerWindow : Window
{
    private const double Scale = 0.5;
    private const double CanvasW = 960;
    private const double CanvasH = 540;

    private readonly ThemeManagerViewModel _viewModel;
    private bool _dragging;
    private Point _dragOffset;

    public ThemeManagerWindow(ThemeManagerViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        FontCombo.ItemsSource = Fonts.SystemFontFamilies
            .Select(f => f.Source)
            .OrderBy(name => name)
            .ToList();

        // Reposition the box whenever the loaded theme's box changes.
        viewModel.PropertyChanged += OnViewModelChanged;
        Loaded += (_, _) => PlaceBox();
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Only reposition when a different theme loads — never while the
        // operator is editing the box (that caused the box to snap back).
        if (e.PropertyName == nameof(ThemeManagerViewModel.SelectedTheme))
            PlaceBox();
    }

    private void PlaceBox()
    {
        Canvas.SetLeft(BoxBorder, _viewModel.BoxX * Scale);
        Canvas.SetTop(BoxBorder, _viewModel.BoxY * Scale);
        BoxBorder.Width = _viewModel.BoxWidth * Scale;
        BoxBorder.Height = _viewModel.BoxHeight * Scale;
    }

    private void CommitBox()
    {
        _viewModel.BoxX = Math.Round(Canvas.GetLeft(BoxBorder) / Scale);
        _viewModel.BoxY = Math.Round(Canvas.GetTop(BoxBorder) / Scale);
        _viewModel.BoxWidth = Math.Round(BoxBorder.Width / Scale);
        _viewModel.BoxHeight = Math.Round(BoxBorder.Height / Scale);
    }

    private void Box_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is Thumb)
            return;
        BoxBorder.Focus(); // habilita el ajuste fino con flechas
        _dragging = true;
        var p = e.GetPosition(Overlay);
        _dragOffset = new Point(p.X - Canvas.GetLeft(BoxBorder), p.Y - Canvas.GetTop(BoxBorder));
        BoxBorder.CaptureMouse();
    }

    private void Box_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_dragging)
            return;
        var p = e.GetPosition(Overlay);
        Canvas.SetLeft(BoxBorder, Math.Clamp(p.X - _dragOffset.X, 0, CanvasW - BoxBorder.Width));
        Canvas.SetTop(BoxBorder, Math.Clamp(p.Y - _dragOffset.Y, 0, CanvasH - BoxBorder.Height));
    }

    private void Box_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_dragging)
            return;
        _dragging = false;
        BoxBorder.ReleaseMouseCapture();
        CommitBox();
    }

    private void Handle_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string handle })
            return;

        var (x, y, w, h) = BoxGeometry.Resize(handle,
            Canvas.GetLeft(BoxBorder), Canvas.GetTop(BoxBorder), BoxBorder.Width, BoxBorder.Height,
            e.HorizontalChange, e.VerticalChange, CanvasW, CanvasH);

        Canvas.SetLeft(BoxBorder, x);
        Canvas.SetTop(BoxBorder, y);
        BoxBorder.Width = w;
        BoxBorder.Height = h;
        CommitBox();
    }

    private void Box_KeyDown(object sender, KeyEventArgs e)
    {
        var step = (Keyboard.Modifiers & ModifierKeys.Control) != 0 ? 1 : 4;
        double dx = 0, dy = 0;
        switch (e.Key)
        {
            case Key.Left: dx = -step; break;
            case Key.Right: dx = step; break;
            case Key.Up: dy = -step; break;
            case Key.Down: dy = step; break;
            default: return;
        }

        Canvas.SetLeft(BoxBorder, Math.Clamp(Canvas.GetLeft(BoxBorder) + dx, 0, CanvasW - BoxBorder.Width));
        Canvas.SetTop(BoxBorder, Math.Clamp(Canvas.GetTop(BoxBorder) + dy, 0, CanvasH - BoxBorder.Height));
        CommitBox();
        e.Handled = true;
    }
}
