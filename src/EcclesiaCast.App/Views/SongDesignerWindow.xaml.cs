using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Core.Songs;
using EcclesiaCast.Core.Themes;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.App.Views;

/// <summary>
/// ProPresenter-style editor for a whole song: the slide list on the left, a
/// draggable/resizable text box over the 1920×1080 canvas in the center, and
/// a format panel on the right. Each slide keeps its own overrides; anything
/// left untouched follows the song's theme.
/// </summary>
public partial class SongDesignerWindow : Window
{
    private const double Scale = SongDesignerViewModel.Scale;
    private const double CanvasW = SongDesignerViewModel.CanvasW;
    private const double CanvasH = SongDesignerViewModel.CanvasH;

    /// <summary>A thumbnail row bound to the slide list.</summary>
    public sealed partial class Thumb(string label, SlideContent slide) : ObservableObject
    {
        public string Label { get; } = label;

        [ObservableProperty]
        private SlideContent _slide = slide;
    }

    private readonly SongDesignerViewModel _vm;
    private readonly System.Collections.ObjectModel.ObservableCollection<Thumb> _thumbs = [];

    /// <summary>What the box on the canvas is editing (-1 the song's words, 0 and up the extra boxes): the view model keeps it.</summary>
    private int _selectedBox
    {
        get => _vm.SelectedBox;
        set => _vm.SelectedBox = value;
    }

    private bool _loading;
    private bool _dragging;
    private Point _dragOffset;
    private double _zoom = 1;
    private bool _fitMode = true;
    private bool _editing;
    private int _editingIndex = -1;

    public SongDesignerWindow(SongDesignerViewModel viewModel, int selectIndex)
    {
        InitializeComponent();
        _vm = viewModel;

        FontCombo.ItemsSource = _vm.FontNames;

        for (var i = 0; i < _vm.SlideCount; i++)
            _thumbs.Add(new Thumb(_vm.SlideLabel(i), _vm.BuildSlide(i)));
        SlidesList.ItemsSource = _thumbs;

        SlidesList.SelectedIndex = Math.Clamp(selectIndex, 0, Math.Max(0, _thumbs.Count - 1));
    }

    private int Selected => SlidesList.SelectedIndex;

    // ── Selección de diapositiva ─────────────────────────────────

    private void SlidesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_editing)
            CommitEdit();

        if (Selected < 0)
            return;

        var over = _vm.SelectSlide(Selected);
        RebuildBoxPicker();
        LoadControls(over);
        RefreshSelected();
    }

    private void LoadControls(SlideOverride over)
    {
        _loading = true;
        var panel = _vm.Panel(over);

        // Editing one of the extra boxes: the panel shows that box's own few
        // settings instead of the song's.
        if (panel.IsExtraBox)
        {
            FontCombo.Text = panel.FontFamily;
            SizeField.SetSilently(panel.FontSize);
            BoldToggle.IsChecked = panel.Bold;
            ItalicToggle.IsChecked = panel.Italic;
            ColorBox.Text = panel.Color;
            ShowAlign(panel);

            PlaceBox(over);
            UpdateColorSwatch();
            EnableSongOnlyControls(false);
            _loading = false;
            return;
        }

        EnableSongOnlyControls(true);

        FontCombo.Text = panel.FontFamily;
        SizeField.SetSilently(panel.FontSize);
        BoldToggle.IsChecked = panel.Bold;
        ItalicToggle.IsChecked = panel.Italic;
        UnderlineToggle.IsChecked = panel.Underline;
        StrikeToggle.IsChecked = panel.Strikethrough;
        ShadowCheck.IsChecked = panel.Shadow;
        ShadowField.SetSilently(panel.ShadowPercent);
        OutlineField.SetSilently(panel.OutlineWidth);
        OutlineColorBox.Text = panel.OutlineColor;
        ColorBox.Text = panel.Color;
        CaseCombo.SelectedIndex = panel.CaseIndex;
        LineField.SetSilently(panel.LineSpacing);
        FitWidthCheck.IsChecked = panel.FitToWidth;
        ShowAlign(panel);

        PlaceBox(over);
        _loading = false;
    }

    private void ShowAlign(SongStylePanel panel)
    {
        HLeft.IsChecked = panel.AlignH == HAlign.Left;
        HCenter.IsChecked = panel.AlignH == HAlign.Center;
        HRight.IsChecked = panel.AlignH == HAlign.Right;
        VTop.IsChecked = panel.AlignV == VAlign.Top;
        VCenter.IsChecked = panel.AlignV == VAlign.Center;
        VBottom.IsChecked = panel.AlignV == VAlign.Bottom;
    }

    /// <summary>
    /// Greys out the settings that belong to the song's words alone. An extra
    /// box carries its own typeface, size, colour and alignment and nothing
    /// else, and a control that looks available but changes nothing is worse
    /// than one that is plainly off.
    /// </summary>
    private void EnableSongOnlyControls(bool enabled)
    {
        UnderlineToggle.IsEnabled = enabled;
        StrikeToggle.IsEnabled = enabled;
        CaseCombo.IsEnabled = enabled;
        LineField.IsEnabled = enabled;
        ShadowCheck.IsEnabled = enabled;
        ShadowField.IsEnabled = enabled;
        OutlineField.IsEnabled = enabled;
        OutlineColorBox.IsEnabled = enabled;
        FitWidthCheck.IsEnabled = enabled;
    }

    // ── Escritura de cambios de formato ──────────────────────────

    private void Style_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading || Selected < 0)
            return;

        var alignH = HRight.IsChecked == true ? HAlign.Right
            : HLeft.IsChecked == true ? HAlign.Left : HAlign.Center;
        var alignV = VTop.IsChecked == true ? VAlign.Top
            : VBottom.IsChecked == true ? VAlign.Bottom : VAlign.Center;

        _vm.ApplyStyle(Selected, new SongStyleForm(
            FontCombo.Text,
            SizeField.Value,
            BoldToggle.IsChecked == true,
            ItalicToggle.IsChecked == true,
            ColorBox.Text,
            alignH,
            alignV,
            UnderlineToggle.IsChecked == true,
            StrikeToggle.IsChecked == true,
            ShadowCheck.IsChecked == true,
            ShadowField.Value,
            OutlineField.Value,
            OutlineColorBox.Text,
            CaseCombo.SelectedIndex,
            LineField.Value,
            FitWidthCheck.IsChecked == true));

        RefreshSelected();
    }

    private void RefreshSelected()
    {
        if (Selected < 0)
            return;
        Preview.Slide = _vm.BuildSlide(Selected);
        _thumbs[Selected].Slide = _vm.BuildSlide(Selected);
        UpdateColorSwatch();
        UpdateBoxInfo();
        DrawGhosts();
    }

    private void UpdateColorSwatch()
    {
        ColorSwatch.Background = BrushFromHex(ColorBox.Text.Trim());
        OutlineSwatch.Background = BrushFromHex(OutlineColorBox.Text.Trim());
    }

    // ── Diapositiva nueva ────────────────────────────────────────

    /// <summary>
    /// «➕ Nueva diapositiva» (GitHub #32): an empty slide after the selected
    /// one, ready to write on. It looks like the theme until it is formatted;
    /// left empty, it is not saved.
    /// </summary>
    private void NewSlide_Click(object sender, RoutedEventArgs e)
    {
        if (_editing)
            CommitEdit();

        var at = _vm.InsertSlideAfter(Selected);
        _thumbs.Insert(at, new Thumb(_vm.SlideLabel(at), _vm.BuildSlide(at)));
        SlidesList.SelectedIndex = at;
        SlidesList.ScrollIntoView(SlidesList.SelectedItem);

        // Straight into writing, as a double click on the box would.
        Dispatcher.BeginInvoke(EnterEdit, System.Windows.Threading.DispatcherPriority.Input);
    }

    // ── Edición del texto sobre la propia diapositiva ────────────

    private void EnterEdit()
    {
        if (Selected < 0)
            return;

        _editing = true;
        _editingIndex = Selected;

        // An extra box edits its own words, on the spot, with its own look; the
        // box being edited is blanked on the preview behind, so the editable
        // copy is the only one showing.
        var edit = _vm.BeginEdit(Selected);
        InlineEditor.Text = edit.Text;
        InlineEditor.FontFamily = new FontFamily(edit.FontFamily);
        InlineEditor.FontSize = Math.Max(8, edit.FontSize * Scale);
        InlineEditor.FontWeight = edit.Bold ? FontWeights.SemiBold : FontWeights.Normal;
        InlineEditor.FontStyle = edit.Italic ? FontStyles.Italic : FontStyles.Normal;
        InlineEditor.Foreground = BrushFromHex(edit.Color);
        InlineEditor.TextAlignment = edit.AlignH switch
        {
            HAlign.Left => TextAlignment.Left,
            HAlign.Right => TextAlignment.Right,
            _ => TextAlignment.Center,
        };
        InlineEditor.VerticalContentAlignment = edit.AlignV switch
        {
            VAlign.Top => VerticalAlignment.Top,
            VAlign.Bottom => VerticalAlignment.Bottom,
            _ => VerticalAlignment.Center,
        };

        Preview.Slide = edit.PreviewMeanwhile;
        InlineEditor.Visibility = Visibility.Visible;
        InlineEditor.Focus();
        InlineEditor.SelectAll();
    }

    private void CommitEdit()
    {
        if (!_editing)
            return;

        _editing = false;
        var index = _editingIndex;
        _editingIndex = -1;
        InlineEditor.Visibility = Visibility.Collapsed;

        if (_vm.CommitEdit(index, InlineEditor.Text))
            _thumbs[index].Slide = _vm.BuildSlide(index);

        RefreshSelected();
    }

    // ── Cuadros de texto extra ───────────────────────────────────

    /// <summary>
    /// Adds a box to this slide and selects it, so the next drag, the panel
    /// and a double click all land on the new one.
    /// </summary>
    private void AddTextBox_Click(object sender, RoutedEventArgs e)
    {
        if (Selected < 0)
            return;

        if (_editing)
            CommitEdit();

        _vm.AddTextBox(Selected);

        RebuildBoxPicker();
        LoadControls(_vm.OverrideOf(Selected)!);
        RefreshSelected();
    }

    private void RemoveTextBox_Click(object sender, RoutedEventArgs e)
    {
        if (Selected < 0 || _selectedBox < 0)
            return;

        if (_editing)
            CommitEdit();

        if (!_vm.RemoveTextBox(Selected))
            return;

        RebuildBoxPicker();
        LoadControls(_vm.OverrideOf(Selected)!);
        RefreshSelected();
    }

    /// <summary>Fills the picker with the song's words plus one entry per extra box.</summary>
    private void RebuildBoxPicker()
    {
        var previous = _loading;
        _loading = true;

        BoxPicker.Items.Clear();
        var names = _vm.BoxNames(Selected);
        foreach (var name in names)
            BoxPicker.Items.Add(name);

        BoxPicker.SelectedIndex = Math.Clamp(_selectedBox + 1, 0, BoxPicker.Items.Count - 1);
        UpdateBoxButtons(names.Count - 1);

        _loading = previous;
    }

    /// <summary>Remove and order buttons only mean something on an extra box, and the order ones need a neighbour.</summary>
    private void UpdateBoxButtons(int extraCount)
    {
        var (remove, bringForward, sendBack) = _vm.BoxButtons(extraCount);
        RemoveBoxButton.IsEnabled = remove;
        BringForwardButton.IsEnabled = bringForward;
        SendBackButton.IsEnabled = sendBack;
    }

    private void BoxPicker_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || Selected < 0)
            return;

        if (_editing)
            CommitEdit();

        // A typeface typed but not confirmed yet belongs to the box being left.
        CommitPendingFont();

        _selectedBox = BoxPicker.SelectedIndex - 1;
        var over = _vm.OverrideOf(Selected)!;
        UpdateBoxButtons(over.TextBoxes.Count);
        LoadControls(over);
        RefreshSelected();
    }

    /// <summary>
    /// A typeface written in the box but not confirmed (no Enter, focus still
    /// in the combo) would be lost, or worse, land on the next box. It is
    /// applied to the box being left, and only when it really differs so that
    /// just looking at a slide never marks it as edited.
    /// </summary>
    private void CommitPendingFont()
    {
        if (_loading || Selected < 0)
            return;

        if (_vm.FontDiffers(Selected, FontCombo.Text))
            Style_Changed(FontCombo, new RoutedEventArgs());
    }

    /// <summary>Moves the chosen extra box one place in the drawing order: later boxes are drawn over earlier ones.</summary>
    private void MoveBoxOrder(int direction)
    {
        if (Selected < 0 || _selectedBox < 0)
            return;

        if (_editing)
            CommitEdit();

        if (!_vm.MoveBoxOrder(Selected, direction))
            return;

        RebuildBoxPicker();
        LoadControls(_vm.OverrideOf(Selected)!);
        RefreshSelected();
    }

    private void BringForward_Click(object sender, RoutedEventArgs e) => MoveBoxOrder(+1);

    private void SendBack_Click(object sender, RoutedEventArgs e) => MoveBoxOrder(-1);

    private void InlineEditor_LostFocus(object sender, RoutedEventArgs e) => CommitEdit();

    private void InlineEditor_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            BoxBorder.Focus(); // dispara LostFocus → CommitEdit
        }
    }

    private static Brush BrushFromHex(string? hex)
    {
        try
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        }
        catch
        {
            return Brushes.White;
        }
    }

    private void PickColor_Click(object sender, RoutedEventArgs e)
    {
        var picked = _vm.PickColor(ColorBox.Text);
        if (picked is not null)
            ColorBox.Text = picked; // dispara Style_Changed
    }

    private void PickOutlineColor_Click(object sender, RoutedEventArgs e)
    {
        var picked = _vm.PickColor(OutlineColorBox.Text);
        if (picked is not null)
            OutlineColorBox.Text = picked;
    }

    // ── Zoom ─────────────────────────────────────────────────────

    private void CanvasScroll_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_fitMode)
            FitZoom();
    }

    private void FitZoom()
    {
        var availW = CanvasScroll.ViewportWidth > 0 ? CanvasScroll.ViewportWidth : CanvasScroll.ActualWidth;
        var availH = CanvasScroll.ViewportHeight > 0 ? CanvasScroll.ViewportHeight : CanvasScroll.ActualHeight;
        if (availW <= 0 || availH <= 0)
            return;

        _zoom = Math.Max(0.1, Math.Min(availW / CanvasW, availH / CanvasH) * 0.97);
        ApplyZoom();
    }

    private void ApplyZoom()
    {
        ZoomTransform.ScaleX = _zoom;
        ZoomTransform.ScaleY = _zoom;
        ZoomLabel.Text = $"{_zoom * 100:0}%";
    }

    private void ZoomFit_Click(object sender, RoutedEventArgs e)
    {
        _fitMode = true;
        FitZoom();
    }

    private void ZoomIn_Click(object sender, RoutedEventArgs e)
    {
        _fitMode = false;
        _zoom = Math.Min(3, _zoom + 0.1);
        ApplyZoom();
    }

    private void ZoomOut_Click(object sender, RoutedEventArgs e)
    {
        _fitMode = false;
        _zoom = Math.Max(0.2, _zoom - 0.1);
        ApplyZoom();
    }

    // ── Caja: colocar, arrastrar, redimensionar ──────────────────

    private void PlaceBox(SlideOverride over)
    {
        var (x, y, w, h) = _vm.BoxRect(over);

        Canvas.SetLeft(BoxBorder, x * Scale);
        Canvas.SetTop(BoxBorder, y * Scale);
        BoxBorder.Width = w * Scale;
        BoxBorder.Height = h * Scale;
        UpdateBoxInfo();
        DrawGhosts();
    }

    /// <summary>
    /// Dotted outlines and a small name for every box that is not the chosen
    /// one. With several boxes on a slide the one on top used to hide where the
    /// others were; the outlines say what can be clicked and what each is called.
    /// </summary>
    private void DrawGhosts()
    {
        GhostLayer.Children.Clear();
        foreach (var ghost in _vm.Ghosts(Selected))
            Ghost(ghost.Label, ghost.X, ghost.Y, ghost.Width, ghost.Height);

        void Ghost(string label, double x, double y, double w, double h)
        {
            var outline = new System.Windows.Shapes.Rectangle
            {
                Width = w * Scale,
                Height = h * Scale,
                Stroke = new SolidColorBrush(Color.FromArgb(0x66, 0xFF, 0xFF, 0xFF)),
                StrokeThickness = 1,
                StrokeDashArray = [4, 3],
            };
            Canvas.SetLeft(outline, x * Scale);
            Canvas.SetTop(outline, y * Scale);
            GhostLayer.Children.Add(outline);

            var tag = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(0x99, 0x00, 0x00, 0x00)),
                Padding = new Thickness(3, 0, 3, 0),
                Child = new TextBlock
                {
                    Text = label,
                    FontSize = 9,
                    Foreground = new SolidColorBrush(Color.FromArgb(0xCC, 0xFF, 0xFF, 0xFF)),
                },
            };
            Canvas.SetLeft(tag, x * Scale);
            Canvas.SetTop(tag, y * Scale);
            GhostLayer.Children.Add(tag);
        }
    }

    /// <summary>The box was moved or stretched on the canvas: its place is handed to the view model.</summary>
    private void CommitBox()
    {
        if (Selected < 0)
            return;

        _vm.SetBoxGeometry(
            Selected,
            Math.Round(Canvas.GetLeft(BoxBorder) / Scale),
            Math.Round(Canvas.GetTop(BoxBorder) / Scale),
            Math.Round(BoxBorder.Width / Scale),
            Math.Round(BoxBorder.Height / Scale));

        RefreshSelected();
    }

    /// <summary>Makes the box under the pointer the one being edited; true when it changed.</summary>
    private bool SelectBoxAt(Point p)
    {
        if (_vm.BoxAt(Selected, p.X, p.Y) is not { } index || index == _selectedBox)
            return false;

        // Writing on one box and clicking another used to leave the operator
        // stuck in the first: the click is what ends the edit.
        if (_editing)
            CommitEdit();
        CommitPendingFont();

        SelectBox(index);
        return true;
    }

    /// <summary>Makes <paramref name="index"/> (-1 the words, 0 and up the extra boxes) the one being edited.</summary>
    private void SelectBox(int index)
    {
        _selectedBox = index;
        RebuildBoxPicker();
        LoadControls(_vm.OverrideOf(Selected)!);
        RefreshSelected();
    }

    private void Overlay_MouseDown(object sender, MouseButtonEventArgs e)
    {
        // Only the empty canvas lands here; the selected box handles its own clicks.
        if (e.OriginalSource != Overlay)
            return;

        // The canvas does not take the keyboard focus, so clicking it while
        // writing would leave the text editor open and the click unanswered.
        if (_editing)
            CommitEdit();

        if (SelectBoxAt(e.GetPosition(Overlay)) && e.ClickCount == 2)
            EnterEdit();

        e.Handled = true;
    }

    private void Box_MouseDown(object sender, MouseButtonEventArgs e)
    {
        // While editing text, let the inner TextBox handle the mouse.
        if (_editing)
            return;

        // The selected box may be sitting on top of a smaller one (the song's
        // words cover most of the slide): the clicked box is the one to move.
        if (e.OriginalSource is not System.Windows.Controls.Primitives.Thumb)
            SelectBoxAt(e.GetPosition(Overlay));

        // Double-click writes directly on the slide.
        if (e.ClickCount == 2)
        {
            EnterEdit();
            e.Handled = true;
            return;
        }

        // Don't start a drag when grabbing a resize handle.
        if (e.OriginalSource is System.Windows.Controls.Primitives.Thumb)
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
        UpdateBoxInfo();
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
        UpdateBoxInfo();
        CommitBox();
    }

    private void Box_KeyDown(object sender, KeyEventArgs e)
    {
        if (_editing)
            return;

        // Tab walks through the boxes of the slide (the words first), Shift+Tab backwards.
        if (e.Key == Key.Tab && Selected >= 0)
        {
            var next = _vm.NextBox(Selected, (Keyboard.Modifiers & ModifierKeys.Shift) != 0);

            CommitPendingFont();
            SelectBox(next);
            BoxBorder.Focus();
            e.Handled = true;
            return;
        }

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
        UpdateBoxInfo();
        CommitBox();
        e.Handled = true;
    }

    /// <summary>Mirrors the box the canvas shows into the numeric fields.</summary>
    private void UpdateBoxInfo()
    {
        var (x, y, w, h, percent) = SongDesignerViewModel.BoxInfo(
            Canvas.GetLeft(BoxBorder), Canvas.GetTop(BoxBorder), BoxBorder.Width, BoxBorder.Height);

        _syncingBox = true;
        BoxXField.SetSilently(x);
        BoxYField.SetSilently(y);
        BoxWField.SetSilently(w);
        BoxHField.SetSilently(h);
        BoxPercentField.SetSilently(percent);
        _syncingBox = false;
    }

    /// <summary>True while the box fields are being written from the canvas.</summary>
    private bool _syncingBox;

    // ── Tamaño porcentual y alineación de la caja ────────────────

    /// <summary>
    /// Resizes the box to a percentage of the screen, keeping its current
    /// centre so stepping 80 → 81 → 82 % grows it in place instead of
    /// snapping back to the middle of the slide.
    /// </summary>
    private void SizePercent_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading || _syncingBox || Selected < 0)
            return;

        var (left, top, w, h) = SongDesignerViewModel.ResizeToPercent(
            Canvas.GetLeft(BoxBorder), Canvas.GetTop(BoxBorder), BoxBorder.Width, BoxBorder.Height,
            BoxPercentField.Value);

        Canvas.SetLeft(BoxBorder, left);
        Canvas.SetTop(BoxBorder, top);
        BoxBorder.Width = w;
        BoxBorder.Height = h;
        CommitBox();
    }

    /// <summary>Typing an exact X / Y / width / height moves the box on the canvas.</summary>
    private void BoxField_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading || _syncingBox || Selected < 0)
            return;

        var (left, top, w, h) = SongDesignerViewModel.FromTypedFields(
            BoxXField.Value, BoxYField.Value, BoxWField.Value, BoxHField.Value);
        Canvas.SetLeft(BoxBorder, left);
        Canvas.SetTop(BoxBorder, top);
        BoxBorder.Width = w;
        BoxBorder.Height = h;
        CommitBox();
    }

    private void AlignBox_Click(object sender, RoutedEventArgs e)
    {
        if (Selected < 0 || sender is not FrameworkElement fe)
            return;

        var (x, y) = SongDesignerViewModel.AlignBox(
            fe.Tag as string, Canvas.GetLeft(BoxBorder), Canvas.GetTop(BoxBorder), BoxBorder.Width, BoxBorder.Height);

        Canvas.SetLeft(BoxBorder, x);
        Canvas.SetTop(BoxBorder, y);
        CommitBox();
    }

    // ── Acciones ─────────────────────────────────────────────────

    private void ApplyToAll_Click(object sender, RoutedEventArgs e)
    {
        if (Selected < 0)
            return;

        _vm.ApplyToAll(Selected);
        for (var i = 0; i < _thumbs.Count; i++)
            _thumbs[i].Slide = _vm.BuildSlide(i);
        RefreshSelected();
    }

    /// <summary>Puts the slide back under the theme's control, dropping its own design.</summary>
    private void ResetSlide_Click(object sender, RoutedEventArgs e)
    {
        if (Selected < 0)
            return;

        LoadControls(_vm.ResetSlide(Selected));
        RefreshSelected();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_editing)
            CommitEdit();

        _vm.Save();
        DialogResult = true;
    }
}
