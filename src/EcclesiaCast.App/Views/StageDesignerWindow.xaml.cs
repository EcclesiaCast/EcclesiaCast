using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using EcclesiaCast.App.Controls;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Stage;

namespace EcclesiaCast.App.Views;

/// <summary>
/// Where the operator arranges the stage screen: boxes placed and stretched
/// over a 1920×1080 canvas, each one choosing what it shows, with several
/// saved arrangements (singers, preacher...) that can be told apart by name.
/// Works on copies; nothing reaches the stage until «Guardar».
/// </summary>
public partial class StageDesignerWindow : Window
{
    private const double CanvasW = StageLayout.CanvasWidth;
    private const double CanvasH = StageLayout.CanvasHeight;

    private sealed record LayoutChoice(StageLayout Layout, string Text);
    private sealed record BoxChoice(StageBox Box, string Text);
    private sealed record KindChoice(StageBoxKind Kind, string Text);

    private readonly List<StageLayout> _layouts;
    private readonly Dictionary<StageBox, StageBoxView> _views = [];
    private readonly List<KindChoice> _kinds =
        [.. Enum.GetValues<StageBoxKind>().Select(k => new KindChoice(k, StageBox.KindName(k)))];

    private StageLayout _inUse;
    private StageLayout _current;
    private StageBox? _selected;

    /// <summary>True while the panels are being filled in, so that does not count as an edit.</summary>
    private bool _loading;

    // Moving a box with the mouse.
    private bool _dragging;
    private Point _dragStart;
    private Point _boxStart;

    public StageDesignerWindow(IReadOnlyList<StageLayout> layouts, string activeName)
    {
        InitializeComponent();

        _layouts = [.. layouts.Select(l => l.Clone())];
        if (_layouts.Count == 0)
            _layouts.Add(new StageLayout { Name = StageLayoutStore.SingersName });

        _inUse = _layouts.FirstOrDefault(l => string.Equals(l.Name, activeName, StringComparison.OrdinalIgnoreCase))
                 ?? _layouts[0];
        _current = _inUse;

        KindBox.ItemsSource = _kinds;
        FontBox.ItemsSource = Fonts.SystemFontFamilies.Select(f => f.Source).OrderBy(n => n).ToList();
        BuildHandles();
        ReloadLayoutPicker();
    }

    /// <summary>The layouts as saved and the one in use; null when the window was cancelled.</summary>
    public StageLayoutState? Result { get; private set; }

    // ── Escenarios ───────────────────────────────────────────────

    private void ReloadLayoutPicker()
    {
        _loading = true;
        var choices = _layouts
            .Select(l => new LayoutChoice(l, ReferenceEquals(l, _inUse) ? $"{l.Name}  ·  en uso" : l.Name))
            .ToList();
        LayoutPicker.ItemsSource = choices;
        LayoutPicker.SelectedItem = choices.First(c => ReferenceEquals(c.Layout, _current));
        _loading = false;

        InUseText.Text = $"En la pantalla de escenario se usa: «{_inUse.Name}». Se cambia también desde el ▾ junto al botón Escenario.";
        UseButton.IsEnabled = !ReferenceEquals(_current, _inUse);
        ShowLayout(_current);
    }

    private void LayoutPicker_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || LayoutPicker.SelectedItem is not LayoutChoice choice)
            return;

        _current = choice.Layout;
        UseButton.IsEnabled = !ReferenceEquals(_current, _inUse);
        ShowLayout(_current);
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
        if (Ask("Nuevo escenario", "Nombre del escenario", "Escenario") is not { Length: > 0 } name)
            return;

        var layout = new StageLayout
        {
            Name = StageLayoutStore.UniqueName(_layouts.Select(l => l.Name), name),
            Boxes =
            [
                NewBox(StageBoxKind.LiveText),
            ],
        };
        _layouts.Add(layout);
        _current = layout;
        ReloadLayoutPicker();
    }

    private void DuplicateLayout_Click(object sender, RoutedEventArgs e)
    {
        var copy = _current.Clone();
        copy.Name = StageLayoutStore.UniqueName(_layouts.Select(l => l.Name), $"{_current.Name} copia");
        foreach (var box in copy.Boxes)
            box.Id = Guid.NewGuid().ToString("N")[..8];
        _layouts.Add(copy);
        _current = copy;
        ReloadLayoutPicker();
    }

    private void RenameLayout_Click(object sender, RoutedEventArgs e)
    {
        if (Ask("Cambiar el nombre", "Nombre del escenario", _current.Name) is not { Length: > 0 } name)
            return;

        _current.Name = StageLayoutStore.UniqueName(
            _layouts.Where(l => !ReferenceEquals(l, _current)).Select(l => l.Name), name);
        ReloadLayoutPicker();
    }

    private void DeleteLayout_Click(object sender, RoutedEventArgs e)
    {
        if (_layouts.Count <= 1)
        {
            MessageBox.Show(this, "Tiene que quedar al menos un escenario.", "EcclesiaCast",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (MessageBox.Show(this, $"¿Eliminar el escenario «{_current.Name}»?", "EcclesiaCast",
                MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes)
            return;

        var index = _layouts.IndexOf(_current);
        _layouts.Remove(_current);
        if (ReferenceEquals(_current, _inUse))
            _inUse = _layouts[Math.Min(index, _layouts.Count - 1)];
        _current = _layouts[Math.Min(index, _layouts.Count - 1)];
        ReloadLayoutPicker();
    }

    private void UseLayout_Click(object sender, RoutedEventArgs e)
    {
        _inUse = _current;
        ReloadLayoutPicker();
    }

    private string? Ask(string title, string message, string initial)
    {
        var window = new TextPromptWindow(title, message, initial) { Owner = this };
        return window.ShowDialog() == true ? window.Value : null;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        Result = new StageLayoutState(_layouts, _inUse.Name);
        DialogResult = true;
    }

    // ── Recuadros ────────────────────────────────────────────────

    private void ReloadBoxList()
    {
        var keep = _selected;
        _loading = true;
        var choices = _current.Boxes.Select(b => new BoxChoice(b, BoxTitle(b))).ToList();
        BoxList.ItemsSource = choices;
        BoxList.SelectedItem = choices.FirstOrDefault(c => ReferenceEquals(c.Box, keep));
        _loading = false;
    }

    private static string BoxTitle(StageBox box)
    {
        var name = StageBox.KindName(box.Kind);
        if (box.Kind != StageBoxKind.FixedText || string.IsNullOrWhiteSpace(box.Text))
            return name;

        var oneLine = box.Text.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return $"{name}: {(oneLine.Length > 22 ? oneLine[..22] + "…" : oneLine)}";
    }

    private void BoxList_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_loading)
            return;

        Select((BoxList.SelectedItem as BoxChoice)?.Box);
    }

    private static StageBox NewBox(StageBoxKind kind)
    {
        // Sizes that read well for each kind; the operator moves and stretches
        // them afterwards.
        var (w, h, size) = kind switch
        {
            StageBoxKind.LiveText => (1600d, 560d, 110d),
            StageBoxKind.NextSlide => (1600d, 200d, 48d),
            StageBoxKind.Clock or StageBoxKind.Timer or StageBoxKind.Countdown or StageBoxKind.SectionTimer => (460d, 150d, 120d),
            StageBoxKind.Notes => (1600d, 600d, 60d),
            StageBoxKind.Mirror => (960d, 540d, 60d),
            StageBoxKind.Overlay => (1600d, 70d, 36d),
            _ => (900d, 100d, 48d),
        };

        return new StageBox
        {
            Kind = kind,
            Width = w,
            Height = h,
            X = (CanvasW - w) / 2,
            Y = (CanvasH - h) / 2,
            FontSize = size,
            Align = kind is StageBoxKind.Notes ? StageAlign.Left : StageAlign.Center,
            Bold = kind is StageBoxKind.LiveText,
            Text = kind == StageBoxKind.FixedText ? "Texto" : string.Empty,
        };
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
            item.Click += (_, _) => AddBox(NewBox(kind.Kind));
            menu.Items.Add(item);
        }

        menu.IsOpen = true;
    }

    private void AddBox(StageBox box)
    {
        _current.Boxes.Add(box);
        var view = new StageBoxView(box, projection: null) { IsHitTestVisible = false };
        _views[box] = view;
        Surface.Children.Add(view);
        _selected = box;
        ReloadBoxList();
        Select(box);
    }

    private void DuplicateBox_Click(object sender, RoutedEventArgs e)
    {
        if (_selected is not { } box)
            return;

        var copy = box.Clone();
        copy.Id = Guid.NewGuid().ToString("N")[..8];
        copy.X = Math.Min(box.X + 40, CanvasW - copy.Width);
        copy.Y = Math.Min(box.Y + 40, CanvasH - copy.Height);
        AddBox(copy);
    }

    private void DeleteBox_Click(object sender, RoutedEventArgs e) => DeleteSelected();

    private void DeleteSelected()
    {
        if (_selected is not { } box)
            return;

        var index = _current.Boxes.IndexOf(box);
        _current.Boxes.Remove(box);
        if (_views.Remove(box, out var view))
            Surface.Children.Remove(view);

        _selected = null;
        ReloadBoxList();
        Select(_current.Boxes.Count == 0 ? null : _current.Boxes[Math.Min(index, _current.Boxes.Count - 1)]);
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

        KindHint.Text = box.Kind switch
        {
            StageBoxKind.LiveText => "Las palabras que están en pantalla ahora. Durante una cuenta regresiva muestra la cuenta.",
            StageBoxKind.NextSlide => "La diapositiva que sigue, para que los cantantes se preparen.",
            StageBoxKind.Clock => "La hora de la computadora.",
            StageBoxKind.Timer => "Cuánto lleva el servicio. Se maneja desde la pestaña Tiempos o el menú Proyección.",
            StageBoxKind.Countdown => "Lo que falta de la cuenta regresiva que se proyecta antes de empezar. Queda vacío si no hay ninguna corriendo.",
            StageBoxKind.SectionTimer => "Lo que le queda a la sección en marcha (pestaña Tiempos). Pasado el tiempo sigue contando en negativo y se pone en rojo.",
            StageBoxKind.Notes => "Las notas que escribís en la pestaña Notas. La congregación nunca las ve.",
            StageBoxKind.LiveName => "El nombre de lo que está en vivo: «Coro», «Juan 3:16».",
            StageBoxKind.FixedText => "Un texto que no cambia: un título o un recordatorio.",
            StageBoxKind.Overlay => "El aviso al pie, el mismo que ve la congregación. Sólo aparece mientras hay uno.",
            StageBoxKind.Mirror => "Exactamente lo que se proyecta, con su fondo. El tamaño de letra no cuenta acá.",
            _ => string.Empty,
        };
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

        box.Kind = choice.Kind;
        ShowKindSpecifics(box);
        Touch(listChanged: true);
    }

    private void FixedText_Changed(object sender, TextChangedEventArgs e)
    {
        if (_loading || _selected is not { } box)
            return;

        box.Text = FixedTextBox.Text;
        Touch(listChanged: true);
    }

    private void Font_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading || _selected is not { } box)
            return;

        var typed = FontBox.Text.Trim();
        box.FontFamily = string.IsNullOrWhiteSpace(typed) ? null : typed;
        Touch();
    }

    private void FontSize_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading || _selected is not { } box)
            return;

        box.FontSize = Math.Clamp(FontSizeField.Value, 8, 600);
        Touch();
    }

    private void Color_Click(object sender, RoutedEventArgs e)
    {
        if (_selected is not { } box)
            return;

        if (ColorPickerHelper.Pick(box.Color) is not { } hex)
            return;

        box.Color = hex;
        ShowColor(hex);
        Touch();
    }

    private void Align_Click(object sender, RoutedEventArgs e)
    {
        if (_selected is not { } box || sender is not RadioButton { Tag: string tag })
            return;

        box.Align = Enum.Parse<StageAlign>(tag);
        Touch();
    }

    private void Bold_Click(object sender, RoutedEventArgs e)
    {
        if (_selected is not { } box)
            return;

        box.Bold = BoldBox.IsChecked == true;
        Touch();
    }

    private void Geometry_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading || _selected is not { } box)
            return;

        var w = Math.Clamp(WField.Value, 20, CanvasW);
        var h = Math.Clamp(HField.Value, 20, CanvasH);
        box.Width = w;
        box.Height = h;
        box.X = Math.Clamp(XField.Value, 0, CanvasW - w);
        box.Y = Math.Clamp(YField.Value, 0, CanvasH - h);
        Touch();
    }

    // ── Mover y estirar con el mouse ─────────────────────────────

    private void Overlay_MouseDown(object sender, MouseButtonEventArgs e)
    {
        Overlay.Focus();
        var p = e.GetPosition(Overlay);

        // The topmost box under the pointer wins: later boxes are drawn on top.
        var hit = _current.Boxes.LastOrDefault(b =>
            p.X >= b.X && p.X <= b.X + b.Width && p.Y >= b.Y && p.Y <= b.Y + b.Height);

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
        box.X = Math.Round(Math.Clamp(_boxStart.X + p.X - _dragStart.X, 0, CanvasW - box.Width));
        box.Y = Math.Round(Math.Clamp(_boxStart.Y + p.Y - _dragStart.Y, 0, CanvasH - box.Height));
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
            case Key.Left: box.X = Math.Max(0, box.X - step); break;
            case Key.Right: box.X = Math.Min(CanvasW - box.Width, box.X + step); break;
            case Key.Up: box.Y = Math.Max(0, box.Y - step); break;
            case Key.Down: box.Y = Math.Min(CanvasH - box.Height, box.Y + step); break;
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

        var (x, y, w, h) = BoxGeometry.Resize(
            handle, box.X, box.Y, box.Width, box.Height,
            e.HorizontalChange, e.VerticalChange, CanvasW, CanvasH);

        box.X = Math.Round(x);
        box.Y = Math.Round(y);
        box.Width = Math.Round(w);
        box.Height = Math.Round(h);
        ShowGeometryWhileLoading(box);
        Touch();
    }
}
