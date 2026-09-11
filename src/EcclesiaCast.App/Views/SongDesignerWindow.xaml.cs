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

namespace EcclesiaCast.App.Views;

/// <summary>
/// ProPresenter-style editor for a whole song: the slide list on the left, a
/// draggable/resizable text box over the 1920×1080 canvas in the center, and
/// a format panel on the right. Each slide keeps its own overrides; anything
/// left untouched follows the song's theme.
/// </summary>
public partial class SongDesignerWindow : Window
{
    private const double Scale = 0.5;
    private const double CanvasW = 960;
    private const double CanvasH = 540;

    /// <summary>A thumbnail row bound to the slide list.</summary>
    public sealed partial class Thumb(string label, SlideContent slide) : ObservableObject
    {
        public string Label { get; } = label;

        [ObservableProperty]
        private SlideContent _slide = slide;
    }

    private readonly Song _song;
    private readonly SlideTheme _theme;
    private readonly List<SlideOverride?> _overrides;
    private readonly List<string> _texts;
    private readonly List<Thumb> _thumbs = [];

    /// <summary>
    /// Slides the operator actually changed in this session. Only these get
    /// their format written out in full; the rest keep whatever they had.
    ///
    /// Selecting a slide is not editing it — opening the designer gives every
    /// slide a box so it can be dragged, and treating that as an edit would
    /// freeze the whole song against its theme.
    /// </summary>
    private readonly HashSet<int> _touched = [];

    /// <summary>
    /// What the box on the canvas is editing: -1 is the song's own words,
    /// 0 and up are the extra boxes of the selected slide. One box with a
    /// changing target, rather than a canvas full of boxes, keeps dragging,
    /// the handles and the arrow keys working exactly as they did.
    /// </summary>
    private int _selectedBox = -1;

    private bool _loading;
    private bool _dragging;
    private Point _dragOffset;
    private double _zoom = 1;
    private bool _fitMode = true;
    private bool _editing;
    private int _editingIndex = -1;

    public SongDesignerWindow(Song song, SlideTheme theme, int selectIndex)
    {
        InitializeComponent();
        _song = song;
        _theme = theme;
        _overrides = song.Sections.Select(s => s.GetOverride()).ToList();
        _texts = song.Sections.Select(s => s.Text).ToList();

        FontCombo.ItemsSource = Fonts.SystemFontFamilies.Select(f => f.Source).OrderBy(n => n).ToList();

        for (var i = 0; i < song.Sections.Count; i++)
            _thumbs.Add(new Thumb(song.Sections[i].Label, BuildSlide(i)));
        SlidesList.ItemsSource = _thumbs;

        SlidesList.SelectedIndex = Math.Clamp(selectIndex, 0, Math.Max(0, _thumbs.Count - 1));
    }

    public bool Saved { get; private set; }

    private int Selected => SlidesList.SelectedIndex;

    // ── Construcción de contenido ────────────────────────────────

    private SlideContent BuildSlide(int index)
    {
        // Caption omitted in the editor so the box uses the full canvas.
        return new SlideContent(_texts[index], null, null, _theme, _overrides[index]);
    }

    private SlideOverride EnsureBox(SlideOverride? over)
    {
        if (over?.HasBox == true)
            return over;

        // Default box = the theme's margin area, made explicit for editing.
        var x = _theme.MarginHorizontal;
        var y = _theme.MarginVertical;
        var w = 1920 - 2 * _theme.MarginHorizontal;
        var h = 1080 - 2 * _theme.MarginVertical;
        return (over ?? new SlideOverride()) with { BoxX = x, BoxY = y, BoxWidth = w, BoxHeight = h };
    }

    // ── Selección de diapositiva ─────────────────────────────────

    private void SlidesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_editing)
            CommitEdit();

        if (Selected < 0)
            return;

        // Changing slide always goes back to the words: the extra boxes of
        // the slide you just left mean nothing on this one.
        _selectedBox = -1;
        _overrides[Selected] = EnsureBox(_overrides[Selected]);
        RebuildBoxPicker();
        LoadControls(_overrides[Selected]!);
        RefreshSelected();
    }

    private void LoadControls(SlideOverride over)
    {
        _loading = true;

        // Editing one of the extra boxes: the panel shows that box's own few
        // settings instead of the song's.
        if (_selectedBox >= 0 && _selectedBox < over.TextBoxes.Count)
        {
            var extra = over.TextBoxes[_selectedBox];
            FontCombo.Text = extra.FontFamily ?? _theme.FontFamily;
            SizeField.SetSilently(extra.FontSize);
            BoldToggle.IsChecked = extra.Bold;
            ItalicToggle.IsChecked = extra.Italic;
            ColorBox.Text = extra.Color;
            HLeft.IsChecked = extra.AlignH == HAlign.Left;
            HCenter.IsChecked = extra.AlignH == HAlign.Center;
            HRight.IsChecked = extra.AlignH == HAlign.Right;
            VTop.IsChecked = extra.AlignV == VAlign.Top;
            VCenter.IsChecked = extra.AlignV == VAlign.Center;
            VBottom.IsChecked = extra.AlignV == VAlign.Bottom;

            PlaceBox(over);
            UpdateColorSwatch();
            EnableSongOnlyControls(false);
            _loading = false;
            return;
        }

        EnableSongOnlyControls(true);

        FontCombo.Text = over.FontFamily ?? _theme.FontFamily;
        SizeField.SetSilently(over.FontSize ?? _theme.MaxFontSize);
        BoldToggle.IsChecked = over.Bold ?? _theme.Bold;
        ItalicToggle.IsChecked = over.Italic ?? _theme.Italic;
        UnderlineToggle.IsChecked = over.Underline ?? false;
        StrikeToggle.IsChecked = over.Strikethrough ?? false;
        ShadowCheck.IsChecked = over.Shadow ?? _theme.Shadow;
        ShadowField.SetSilently((over.ShadowOpacity ?? _theme.ShadowOpacity) * 100);
        OutlineField.SetSilently(over.OutlineWidth ?? _theme.OutlineWidth);
        OutlineColorBox.Text = over.OutlineColor ?? _theme.OutlineColor;
        ColorBox.Text = over.TextColor ?? _theme.TextColor;
        CaseCombo.SelectedIndex = (int)(over.Case ?? _theme.TextCase);
        LineField.SetSilently(over.LineSpacing is > 0 ? over.LineSpacing.Value : 1);
        FitWidthCheck.IsChecked = over.FitToWidth ?? _theme.FitToWidth;

        var alignH = over.AlignH ?? _theme.AlignH;
        HLeft.IsChecked = alignH == HAlign.Left;
        HCenter.IsChecked = alignH == HAlign.Center;
        HRight.IsChecked = alignH == HAlign.Right;

        var alignV = over.AlignV ?? _theme.AlignV;
        VTop.IsChecked = alignV == VAlign.Top;
        VCenter.IsChecked = alignV == VAlign.Center;
        VBottom.IsChecked = alignV == VAlign.Bottom;

        PlaceBox(over);
        _loading = false;
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

        _touched.Add(Selected);

        var alignH = HRight.IsChecked == true ? HAlign.Right
            : HLeft.IsChecked == true ? HAlign.Left : HAlign.Center;
        var alignV = VTop.IsChecked == true ? VAlign.Top
            : VBottom.IsChecked == true ? VAlign.Bottom : VAlign.Center;

        // An extra box carries only the handful of settings that make sense on
        // its own; the rest of the panel belongs to the song's words.
        if (_selectedBox >= 0)
        {
            _overrides[Selected] = ReplaceBox(_overrides[Selected]!, _selectedBox, b => b with
            {
                FontFamily = string.IsNullOrWhiteSpace(FontCombo.Text) ? null : FontCombo.Text.Trim(),
                FontSize = Math.Round(SizeField.Value),
                Bold = BoldToggle.IsChecked == true,
                Italic = ItalicToggle.IsChecked == true,
                Color = ColorBox.Text.Trim(),
                AlignH = alignH,
                AlignV = alignV,
            });
            RefreshSelected();
            return;
        }

        var box = _overrides[Selected]!;
        _overrides[Selected] = box with
        {
            FontFamily = string.IsNullOrWhiteSpace(FontCombo.Text) ? _theme.FontFamily : FontCombo.Text.Trim(),
            FontSize = Math.Round(SizeField.Value),
            Bold = BoldToggle.IsChecked == true,
            Italic = ItalicToggle.IsChecked == true,
            Underline = UnderlineToggle.IsChecked == true,
            Strikethrough = StrikeToggle.IsChecked == true,
            Shadow = ShadowCheck.IsChecked == true,
            ShadowOpacity = ShadowField.Value / 100,
            OutlineWidth = OutlineField.Value,
            OutlineColor = OutlineColorBox.Text.Trim(),
            TextColor = ColorBox.Text.Trim(),
            AlignH = alignH,
            AlignV = alignV,
            Case = (TextCase)Math.Clamp(CaseCombo.SelectedIndex, 0, 4),
            LineSpacing = LineField.Value,
            FitToWidth = FitWidthCheck.IsChecked == true,
        };

        RefreshSelected();
    }

    private void RefreshSelected()
    {
        if (Selected < 0)
            return;
        Preview.Slide = BuildSlide(Selected);
        _thumbs[Selected].Slide = BuildSlide(Selected);
        UpdateColorSwatch();
        UpdateBoxInfo();
    }

    private void UpdateColorSwatch()
    {
        ColorSwatch.Background = BrushFromHex(ColorBox.Text.Trim());
        OutlineSwatch.Background = BrushFromHex(OutlineColorBox.Text.Trim());
    }

    // ── Edición del texto sobre la propia diapositiva ────────────

    private void EnterEdit()
    {
        if (Selected < 0)
            return;

        _editing = true;
        _editingIndex = Selected;

        var over = _overrides[Selected]!;

        // An extra box edits its own words, on the spot, with its own look.
        if (_selectedBox >= 0 && _selectedBox < over.TextBoxes.Count)
        {
            var extra = over.TextBoxes[_selectedBox];
            InlineEditor.Text = extra.Text;
            InlineEditor.FontFamily = new FontFamily(extra.FontFamily ?? _theme.FontFamily);
            InlineEditor.FontSize = Math.Max(8, extra.FontSize * Scale);
            InlineEditor.FontWeight = extra.Bold ? FontWeights.SemiBold : FontWeights.Normal;
            InlineEditor.FontStyle = extra.Italic ? FontStyles.Italic : FontStyles.Normal;
            InlineEditor.Foreground = BrushFromHex(extra.Color);
            InlineEditor.TextAlignment = extra.AlignH switch
            {
                HAlign.Left => TextAlignment.Left,
                HAlign.Right => TextAlignment.Right,
                _ => TextAlignment.Center,
            };
            InlineEditor.VerticalContentAlignment = extra.AlignV switch
            {
                VAlign.Top => VerticalAlignment.Top,
                VAlign.Bottom => VerticalAlignment.Bottom,
                _ => VerticalAlignment.Center,
            };

            // The box being edited is blanked on the preview behind, so the
            // editable copy is the only one showing.
            Preview.Slide = new SlideContent(_texts[Selected], null, null, _theme,
                ReplaceBox(over, _selectedBox, b => b with { Text = string.Empty }));
            InlineEditor.Visibility = Visibility.Visible;
            InlineEditor.Focus();
            InlineEditor.SelectAll();
            return;
        }

        InlineEditor.Text = _texts[Selected];
        InlineEditor.FontFamily = new FontFamily(over.FontFamily ?? _theme.FontFamily);
        InlineEditor.FontSize = Math.Max(8, (over.FontSize ?? _theme.MaxFontSize) * Scale);
        InlineEditor.FontWeight = (over.Bold ?? _theme.Bold) ? FontWeights.SemiBold : FontWeights.Normal;
        InlineEditor.FontStyle = (over.Italic ?? _theme.Italic) ? FontStyles.Italic : FontStyles.Normal;
        InlineEditor.Foreground = BrushFromHex(over.TextColor ?? _theme.TextColor);
        InlineEditor.TextAlignment = (over.AlignH ?? _theme.AlignH) switch
        {
            HAlign.Left => TextAlignment.Left,
            HAlign.Right => TextAlignment.Right,
            _ => TextAlignment.Center,
        };
        InlineEditor.VerticalContentAlignment = (over.AlignV ?? _theme.AlignV) switch
        {
            VAlign.Top => VerticalAlignment.Top,
            VAlign.Bottom => VerticalAlignment.Bottom,
            _ => VerticalAlignment.Center,
        };

        // Hide the rendered text behind so only the editable copy shows.
        Preview.Slide = new SlideContent(string.Empty, null, null, _theme, over);
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

        if (index >= 0 && index < _texts.Count)
        {
            if (_selectedBox >= 0)
            {
                _touched.Add(index);
                _overrides[index] = ReplaceBox(_overrides[index]!, _selectedBox,
                    b => b with { Text = InlineEditor.Text });
            }
            else
            {
                _texts[index] = InlineEditor.Text;
            }

            _thumbs[index].Slide = BuildSlide(index);
        }

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

        var over = _overrides[Selected]!;
        var boxes = over.TextBoxes.ToList();
        boxes.Add(SlideTextBox.Fresh());

        _touched.Add(Selected);
        _overrides[Selected] = over with { Boxes = boxes };
        _selectedBox = boxes.Count - 1;

        RebuildBoxPicker();
        LoadControls(_overrides[Selected]!);
        RefreshSelected();
    }

    private void RemoveTextBox_Click(object sender, RoutedEventArgs e)
    {
        if (Selected < 0 || _selectedBox < 0)
            return;

        if (_editing)
            CommitEdit();

        var over = _overrides[Selected]!;
        var boxes = over.TextBoxes.ToList();
        if (_selectedBox >= boxes.Count)
            return;

        boxes.RemoveAt(_selectedBox);
        _touched.Add(Selected);
        _overrides[Selected] = over with { Boxes = boxes };
        _selectedBox = -1;

        RebuildBoxPicker();
        LoadControls(_overrides[Selected]!);
        RefreshSelected();
    }

    /// <summary>Fills the picker with the song's words plus one entry per extra box.</summary>
    private void RebuildBoxPicker()
    {
        var previous = _loading;
        _loading = true;

        BoxPicker.Items.Clear();
        BoxPicker.Items.Add("Letra de la canción");

        var count = Selected >= 0 ? _overrides[Selected]?.TextBoxes.Count ?? 0 : 0;
        for (var i = 0; i < count; i++)
            BoxPicker.Items.Add($"Cuadro {i + 1}");

        BoxPicker.SelectedIndex = Math.Clamp(_selectedBox + 1, 0, BoxPicker.Items.Count - 1);
        RemoveBoxButton.IsEnabled = _selectedBox >= 0;

        _loading = previous;
    }

    private void BoxPicker_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || Selected < 0)
            return;

        if (_editing)
            CommitEdit();

        _selectedBox = BoxPicker.SelectedIndex - 1;
        RemoveBoxButton.IsEnabled = _selectedBox >= 0;
        LoadControls(_overrides[Selected]!);
    }

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
        var picked = ColorPickerHelper.Pick(ColorBox.Text.Trim());
        if (picked is not null)
            ColorBox.Text = picked; // dispara Style_Changed
    }

    private void PickOutlineColor_Click(object sender, RoutedEventArgs e)
    {
        var picked = ColorPickerHelper.Pick(OutlineColorBox.Text.Trim());
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
        var (x, y, w, h) = _selectedBox < 0 || _selectedBox >= over.TextBoxes.Count
            ? (over.BoxX ?? 0, over.BoxY ?? 0, over.BoxWidth ?? 1920, over.BoxHeight ?? 1080)
            : (over.TextBoxes[_selectedBox].X, over.TextBoxes[_selectedBox].Y,
               over.TextBoxes[_selectedBox].Width, over.TextBoxes[_selectedBox].Height);

        Canvas.SetLeft(BoxBorder, x * Scale);
        Canvas.SetTop(BoxBorder, y * Scale);
        BoxBorder.Width = w * Scale;
        BoxBorder.Height = h * Scale;
        UpdateBoxInfo();
    }

    private void CommitBox()
    {
        if (Selected < 0)
            return;

        _touched.Add(Selected);

        var x = Math.Round(Canvas.GetLeft(BoxBorder) / Scale);
        var y = Math.Round(Canvas.GetTop(BoxBorder) / Scale);
        var w = Math.Round(BoxBorder.Width / Scale);
        var h = Math.Round(BoxBorder.Height / Scale);

        var over = _overrides[Selected]!;
        _overrides[Selected] = _selectedBox < 0
            ? over with { BoxX = x, BoxY = y, BoxWidth = w, BoxHeight = h }
            : ReplaceBox(over, _selectedBox, b => b with { X = x, Y = y, Width = w, Height = h });

        RefreshSelected();
    }

    /// <summary>Rewrites one extra box of an override, leaving the rest alone.</summary>
    private static SlideOverride ReplaceBox(SlideOverride over, int index, Func<SlideTextBox, SlideTextBox> change)
    {
        var boxes = over.TextBoxes.ToList();
        if (index < 0 || index >= boxes.Count)
            return over;

        boxes[index] = change(boxes[index]);
        return over with { Boxes = boxes };
    }

    private void Box_MouseDown(object sender, MouseButtonEventArgs e)
    {
        // While editing text, let the inner TextBox handle the mouse.
        if (_editing)
            return;

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
        var x = Canvas.GetLeft(BoxBorder) / Scale;
        var y = Canvas.GetTop(BoxBorder) / Scale;
        var w = BoxBorder.Width / Scale;
        var h = BoxBorder.Height / Scale;

        _syncingBox = true;
        BoxXField.SetSilently(Math.Round(x));
        BoxYField.SetSilently(Math.Round(y));
        BoxWField.SetSilently(Math.Round(w));
        BoxHField.SetSilently(Math.Round(h));
        BoxPercentField.SetSilently(Math.Round(w / CanvasW * Scale * 100));
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

        var percent = Math.Clamp(BoxPercentField.Value / 100, 0.1, 1);
        var w = CanvasW * percent;
        var h = CanvasH * percent;
        var centerX = Canvas.GetLeft(BoxBorder) + BoxBorder.Width / 2;
        var centerY = Canvas.GetTop(BoxBorder) + BoxBorder.Height / 2;

        Canvas.SetLeft(BoxBorder, Math.Clamp(centerX - w / 2, 0, CanvasW - w));
        Canvas.SetTop(BoxBorder, Math.Clamp(centerY - h / 2, 0, CanvasH - h));
        BoxBorder.Width = w;
        BoxBorder.Height = h;
        CommitBox();
    }

    /// <summary>Typing an exact X / Y / width / height moves the box on the canvas.</summary>
    private void BoxField_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading || _syncingBox || Selected < 0)
            return;

        var w = Math.Clamp(BoxWField.Value * Scale, 20, CanvasW);
        var h = Math.Clamp(BoxHField.Value * Scale, 20, CanvasH);
        Canvas.SetLeft(BoxBorder, Math.Clamp(BoxXField.Value * Scale, 0, CanvasW - w));
        Canvas.SetTop(BoxBorder, Math.Clamp(BoxYField.Value * Scale, 0, CanvasH - h));
        BoxBorder.Width = w;
        BoxBorder.Height = h;
        CommitBox();
    }

    private void AlignBox_Click(object sender, RoutedEventArgs e)
    {
        if (Selected < 0 || sender is not FrameworkElement fe)
            return;

        var w = BoxBorder.Width;
        var h = BoxBorder.Height;
        double x = Canvas.GetLeft(BoxBorder), y = Canvas.GetTop(BoxBorder);

        switch (fe.Tag as string)
        {
            case "HL": x = 0; break;
            case "HC": x = (CanvasW - w) / 2; break;
            case "HR": x = CanvasW - w; break;
            case "VT": y = 0; break;
            case "VC": y = (CanvasH - h) / 2; break;
            case "VB": y = CanvasH - h; break;
            case "CC": x = (CanvasW - w) / 2; y = (CanvasH - h) / 2; break;
        }

        Canvas.SetLeft(BoxBorder, x);
        Canvas.SetTop(BoxBorder, y);
        CommitBox();
    }

    // ── Acciones ─────────────────────────────────────────────────

    private void ApplyToAll_Click(object sender, RoutedEventArgs e)
    {
        if (Selected < 0)
            return;

        var source = _overrides[Selected];
        for (var i = 0; i < _overrides.Count; i++)
        {
            _overrides[i] = source;
            _touched.Add(i);
            _thumbs[i].Slide = BuildSlide(i);
        }
        RefreshSelected();
    }

    /// <summary>Puts the slide back under the theme's control, dropping its own design.</summary>
    private void ResetSlide_Click(object sender, RoutedEventArgs e)
    {
        if (Selected < 0)
            return;

        _overrides[Selected] = EnsureBox(null);
        _touched.Remove(Selected);
        LoadControls(_overrides[Selected]!);
        RefreshSelected();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_editing)
            CommitEdit();

        for (var i = 0; i < _song.Sections.Count; i++)
        {
            var text = _texts[i].Trim();
            if (text.Length > 0)
                _song.Sections[i].Text = text;

            // Slides the operator worked on keep exactly what the panel shows;
            // the rest are left alone so they keep following the theme.
            if (_touched.Contains(i))
                _song.Sections[i].SetOverride(Clean(_overrides[i]));
        }

        Saved = true;
        DialogResult = true;
    }

    /// <summary>
    /// Tidies an edited slide's design without second-guessing it. It used to
    /// null out every field matching the theme, which quietly threw away
    /// deliberate choices that happened to agree with it — pick the theme's own
    /// typeface in the designer and the slide was left with none, so changing
    /// the theme later moved a song that had been designed by hand.
    ///
    /// Only decorations that are off are dropped, since false and "not set"
    /// mean the same thing for them.
    /// </summary>
    private static SlideOverride? Clean(SlideOverride? over)
    {
        if (over is null)
            return null;

        var result = over with
        {
            Underline = over.Underline == true ? true : null,
            Strikethrough = over.Strikethrough == true ? true : null,
        };

        return result.IsEmpty ? null : result;
    }
}
