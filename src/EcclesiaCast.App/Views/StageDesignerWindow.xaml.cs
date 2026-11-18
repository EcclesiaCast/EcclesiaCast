using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using EcclesiaCast.App.Controls;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Stage;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.App.Views;

/// <summary>
/// Where the operator arranges the stage screen: boxes placed and stretched
/// over a 1920×1080 canvas, each one choosing what it shows, with several
/// saved arrangements (singers, preacher...) that can be told apart by name.
/// Works on copies; nothing reaches the stage until «Guardar».
/// </summary>
public partial class StageDesignerWindow : Window
{
    private sealed record LayoutChoice(StageLayout Layout, string Text);
    private sealed record BoxChoice(StageBox Box, string Text);
    private sealed record KindChoice(StageBoxKind Kind, string Text);

    private readonly StageDesignerViewModel _vm;
    private readonly Dictionary<StageBox, StageBoxView> _views = [];
    private readonly List<KindChoice> _kinds =
        [.. Enum.GetValues<StageBoxKind>().Select(k => new KindChoice(k, StageBox.KindName(k)))];

    /// <summary>The box chosen: the view model keeps it.</summary>
    private StageBox? _selected
    {
        get => _vm.Selected;
        set => _vm.Selected = value;
    }

    /// <summary>True while the panels are being filled in, so that does not count as an edit.</summary>
    private bool _loading;

    // Moving a box with the mouse.
    private bool _dragging;
    private Point _dragStart;
    private Point _boxStart;

    public StageDesignerWindow(StageDesignerViewModel viewModel)
    {
        InitializeComponent();
        _vm = viewModel;

        KindBox.ItemsSource = _kinds;
        FontBox.ItemsSource = _vm.FontNames;
        BuildHandles();
        ReloadLayoutPicker();
    }

    /// <summary>The layouts as saved and the one in use; null when the window was cancelled.</summary>
    public StageLayoutState? Result { get; private set; }

    // ── Escenarios ───────────────────────────────────────────────

    private void ReloadLayoutPicker()
    {
        _loading = true;
        var choices = _vm.Layouts
            .Select(l => new LayoutChoice(l, _vm.LayoutText(l)))
            .ToList();
        LayoutPicker.ItemsSource = choices;
        LayoutPicker.SelectedItem = choices.First(c => ReferenceEquals(c.Layout, _vm.Current));
        _loading = false;

        InUseText.Text = _vm.InUseText;
        UseButton.IsEnabled = _vm.CanUseCurrent;
        ShowLayout(_vm.Current);
    }

    private void LayoutPicker_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || LayoutPicker.SelectedItem is not LayoutChoice choice)
            return;

        _vm.OpenLayout(choice.Layout);
        UseButton.IsEnabled = _vm.CanUseCurrent;
        ShowLayout(_vm.Current);
    }

    private void ShowLayout(StageLayout layout)
    {
        Surface.Children.Clear();
        _views.Clear();
        foreach (var box in layout.Boxes)
        {
            var view = new StageBoxView(box, projection: null) { IsHitTestVisible = false };
            _views[box] = view;
            Surface.Children.Add(view);
        }

        ReloadBoxList();
        Select(layout.Boxes.LastOrDefault());
    }

    private void NewLayout_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.NewLayout())
            ReloadLayoutPicker();
    }

    private void DuplicateLayout_Click(object sender, RoutedEventArgs e)
    {
        _vm.DuplicateLayout();
        ReloadLayoutPicker();
    }

    private void RenameLayout_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.RenameLayout())
            ReloadLayoutPicker();
    }

    private void DeleteLayout_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.DeleteLayout())
            ReloadLayoutPicker();
    }

    private void UseLayout_Click(object sender, RoutedEventArgs e)
    {
        _vm.UseCurrent();
        ReloadLayoutPicker();
    }

    /// <summary>The text prompt, over this window.</summary>
    public string? Ask(string title, string message, string initial)
    {
        var window = new TextPromptWindow(title, message, initial) { Owner = this };
        return window.ShowDialog() == true ? window.Value : null;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        Result = _vm.Build();
        DialogResult = true;
    }

    // ── Recuadros ────────────────────────────────────────────────

    private void ReloadBoxList()
    {
        var keep = _selected;
        _loading = true;
        var choices = _vm.Current.Boxes.Select(b => new BoxChoice(b, StageDesignerViewModel.BoxTitle(b))).ToList();
        BoxList.ItemsSource = choices;
        BoxList.SelectedItem = choices.FirstOrDefault(c => ReferenceEquals(c.Box, keep));
        _loading = false;
    }

    private void BoxList_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_loading)
            return;

        Select((BoxList.SelectedItem as BoxChoice)?.Box);
    }

    private void AddBox_Click(object sender, RoutedEventArgs e)
    {
        var menu = new ContextMenu
        {
            PlacementTarget = (UIElement)sender,
            Placement = PlacementMode.Bottom,
        };

        foreach (var kind in _kinds)
        {
            var item = new MenuItem { Header = kind.Text };
            item.Click += (_, _) => AddBox(StageDesignerViewModel.NewBox(kind.Kind));
            menu.Items.Add(item);
        }

        menu.IsOpen = true;
    }

    private void AddBox(StageBox box)
    {
        _vm.AddBox(box);
        ShowAdded(box);
    }

    /// <summary>Draws a box the view model just added and selects it.</summary>
    private void ShowAdded(StageBox box)
    {
        var view = new StageBoxView(box, projection: null) { IsHitTestVisible = false };
        _views[box] = view;
        Surface.Children.Add(view);
        ReloadBoxList();
        Select(box);
    }

    private void DuplicateBox_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.DuplicateSelected() is { } copy)
            ShowAdded(copy);
    }

    private void DeleteBox_Click(object sender, RoutedEventArgs e) => DeleteSelected();

    private void DeleteSelected()
    {
        if (_vm.DeleteSelected() is not { } box)
            return;

        if (_views.Remove(box, out var view))
            Surface.Children.Remove(view);

        ReloadBoxList();
        Select(_vm.Selected);
    }

    // ── Selección y panel de propiedades ─────────────────────────

    private void Select(StageBox? box)
    {
        _selected = box;

        _loading = true;
        BoxList.SelectedItem = (BoxList.ItemsSource as IEnumerable<BoxChoice>)?.FirstOrDefault(c => ReferenceEquals(c.Box, box));
        Props.IsEnabled = box is not null;

        if (box is not null)
        {
            KindBox.SelectedItem = _kinds.First(k => k.Kind == box.Kind);
            FixedTextBox.Text = box.Text;
            FontBox.Text = box.FontFamily ?? "Segoe UI";
            FontSizeField.SetSilently(box.FontSize);
            ShowColor(box.Color);
            AlignLeft.IsChecked = box.Align == StageAlign.Left;
            AlignCenter.IsChecked = box.Align == StageAlign.Center;
            AlignRight.IsChecked = box.Align == StageAlign.Right;
            BoldBox.IsChecked = box.Bold;
            ShowGeometry(box);
            ShowKindSpecifics(box);
        }

        _loading = false;
        PlaceSelection();
    }

    private void ShowGeometry(StageBox box)
    {
        XField.SetSilently(Math.Round(box.X));
        YField.SetSilently(Math.Round(box.Y));
        WField.SetSilently(Math.Round(box.Width));
        HField.SetSilently(Math.Round(box.Height));
    }

    private void ShowColor(string hex)
    {
        ColorText.Text = hex;
        try
        {
            ColorSwatch.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        }
        catch (FormatException)
        {
            ColorSwatch.Background = Brushes.White;
        }
    }

    /// <summary>Text only means something for a fixed text; the mirror has no font at all.</summary>
    private void ShowKindSpecifics(StageBox box)
    {
        var fixedText = box.Kind == StageBoxKind.FixedText;
        TextLabel.Visibility = fixedText ? Visibility.Visible : Visibility.Collapsed;
        FixedTextBox.Visibility = fixedText ? Visibility.Visible : Visibility.Collapsed;

        KindHint.Text = StageDesignerViewModel.KindHint(box.Kind);
    }

    /// <summary>The frame and handles follow the selected box.</summary>
    private void PlaceSelection()
    {
        if (_selected is not { } box)
        {
            Selection.Visibility = Visibility.Collapsed;
            return;
        }

        Selection.Visibility = Visibility.Visible;
        Canvas.SetLeft(Selection, box.X);
        Canvas.SetTop(Selection, box.Y);
        Selection.Width = box.Width;
        Selection.Height = box.Height;
    }

    /// <summary>Redraws the selected box and its list line after any change.</summary>
    private void Touch(bool listChanged = false)
    {
        if (_selected is not { } box)
            return;

        if (_views.TryGetValue(box, out var view))
            view.Apply();

        if (listChanged)
            ReloadBoxList();

        PlaceSelection();
    }

    // ── Propiedades ──────────────────────────────────────────────

    private void Kind_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || _selected is not { } box || KindBox.SelectedItem is not KindChoice choice)
            return;

        _vm.SetKind(choice.Kind);
        ShowKindSpecifics(box);
        Touch(listChanged: true);
    }

    private void FixedText_Changed(object sender, TextChangedEventArgs e)
    {
        if (_loading || _selected is null)
            return;

        _vm.SetText(FixedTextBox.Text);
        Touch(listChanged: true);
    }

    private void Font_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading || _selected is null)
            return;

        _vm.SetFont(FontBox.Text);
        Touch();
    }

    private void FontSize_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading || _selected is null)
            return;

        _vm.SetFontSize(FontSizeField.Value);
        Touch();
    }

    private void Color_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.PickColor() is not { } hex)
            return;

        ShowColor(hex);
        Touch();
    }

    private void Align_Click(object sender, RoutedEventArgs e)
    {
        if (_selected is null || sender is not RadioButton { Tag: string tag })
            return;

        _vm.SetAlign(tag);
        Touch();
    }

    private void Bold_Click(object sender, RoutedEventArgs e)
    {
        if (_selected is null)
            return;

        _vm.SetBold(BoldBox.IsChecked == true);
        Touch();
    }

    private void Geometry_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading || _selected is null)
            return;

        _vm.SetGeometry(XField.Value, YField.Value, WField.Value, HField.Value);
        Touch();
    }

    // ── Mover y estirar con el mouse ─────────────────────────────

    private void Overlay_MouseDown(object sender, MouseButtonEventArgs e)
    {
        Overlay.Focus();
        var p = e.GetPosition(Overlay);

        // The topmost box under the pointer wins: later boxes are drawn on top.
        var hit = _vm.BoxAt(p.X, p.Y);

        if (!ReferenceEquals(hit, _selected))
            Select(hit);
        if (hit is null)
            return;

        _dragging = true;
        _dragStart = p;
        _boxStart = new Point(hit.X, hit.Y);
        Overlay.CaptureMouse();
        Overlay.Cursor = Cursors.SizeAll;
    }

    private void Overlay_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_dragging || _selected is not { } box)
            return;

        var p = e.GetPosition(Overlay);
        _vm.MoveTo(_boxStart.X + p.X - _dragStart.X, _boxStart.Y + p.Y - _dragStart.Y);
        ShowGeometryWhileLoading(box);
        Touch();
    }

    private void Overlay_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_dragging)
            return;

        _dragging = false;
        Overlay.ReleaseMouseCapture();
        Overlay.Cursor = Cursors.Arrow;
    }

    private void ShowGeometryWhileLoading(StageBox box)
    {
        _loading = true;
        ShowGeometry(box);
        _loading = false;
    }

    private void Overlay_KeyDown(object sender, KeyEventArgs e)
    {
        if (_selected is not { } box)
            return;

        var step = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? 10 : 1;
        switch (e.Key)
        {
            case Key.Left: _vm.Nudge(-1, 0, step); break;
            case Key.Right: _vm.Nudge(1, 0, step); break;
            case Key.Up: _vm.Nudge(0, -1, step); break;
            case Key.Down: _vm.Nudge(0, 1, step); break;
            case Key.Delete:
                DeleteSelected();
                e.Handled = true;
                return;
            default: return;
        }

        ShowGeometryWhileLoading(box);
        Touch();
        e.Handled = true;
    }

    /// <summary>
    /// The eight handles around the chosen box. They are made here, sized for
    /// the 1920-wide canvas (which is shown much smaller), because the usual
    /// 12-pixel handle would be a speck.
    /// </summary>
    private void BuildHandles()
    {
        const double size = 28;
        var spots = new (string Tag, HorizontalAlignment H, VerticalAlignment V, Cursor Cursor)[]
        {
            ("NW", HorizontalAlignment.Left, VerticalAlignment.Top, Cursors.SizeNWSE),
            ("N", HorizontalAlignment.Center, VerticalAlignment.Top, Cursors.SizeNS),
            ("NE", HorizontalAlignment.Right, VerticalAlignment.Top, Cursors.SizeNESW),
            ("W", HorizontalAlignment.Left, VerticalAlignment.Center, Cursors.SizeWE),
            ("E", HorizontalAlignment.Right, VerticalAlignment.Center, Cursors.SizeWE),
            ("SW", HorizontalAlignment.Left, VerticalAlignment.Bottom, Cursors.SizeNESW),
            ("S", HorizontalAlignment.Center, VerticalAlignment.Bottom, Cursors.SizeNS),
            ("SE", HorizontalAlignment.Right, VerticalAlignment.Bottom, Cursors.SizeNWSE),
        };

        foreach (var (tag, h, v, cursor) in spots)
        {
            var thumb = new Thumb
            {
                Style = (Style)FindResource("BoxHandle"),
                Width = size,
                Height = size,
                Tag = tag,
                Cursor = cursor,
                HorizontalAlignment = h,
                VerticalAlignment = v,
                Margin = new Thickness(
                    h == HorizontalAlignment.Left ? -size / 2 : 0,
                    v == VerticalAlignment.Top ? -size / 2 : 0,
                    h == HorizontalAlignment.Right ? -size / 2 : 0,
                    v == VerticalAlignment.Bottom ? -size / 2 : 0),
            };
            thumb.DragDelta += Handle_DragDelta;
            Selection.Children.Add(thumb);
        }
    }

    private void Handle_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (_selected is not { } box || sender is not Thumb { Tag: string handle })
            return;

        _vm.Resize(handle, e.HorizontalChange, e.VerticalChange);
        ShowGeometryWhileLoading(box);
        Touch();
    }
}
