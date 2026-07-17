using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
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
    private readonly List<Thumb> _thumbs = [];

    private bool _loading;
    private bool _dragging;
    private Point _dragOffset;

    public SongDesignerWindow(Song song, SlideTheme theme, int selectIndex)
    {
        InitializeComponent();
        _song = song;
        _theme = theme;
        _overrides = song.Sections.Select(s => s.GetOverride()).ToList();

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
        return new SlideContent(_song.Sections[index].Text, null, null, _theme, _overrides[index]);
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
        if (Selected < 0)
            return;

        _overrides[Selected] = EnsureBox(_overrides[Selected]);
        LoadControls(_overrides[Selected]!);
        RefreshSelected();
    }

    private void LoadControls(SlideOverride over)
    {
        _loading = true;

        FontCombo.Text = over.FontFamily ?? _theme.FontFamily;
        SizeSlider.Value = over.FontSize ?? _theme.MaxFontSize;
        BoldToggle.IsChecked = over.Bold ?? _theme.Bold;
        ItalicToggle.IsChecked = over.Italic ?? _theme.Italic;
        UnderlineToggle.IsChecked = over.Underline ?? false;
        StrikeToggle.IsChecked = over.Strikethrough ?? false;
        ShadowCheck.IsChecked = over.Shadow ?? _theme.Shadow;
        ColorBox.Text = over.TextColor ?? _theme.TextColor;
        CaseCombo.SelectedIndex = (int)(over.Case ?? (_theme.Uppercase ? TextCase.Upper : TextCase.None));
        LineSlider.Value = over.LineSpacing is > 0 ? over.LineSpacing.Value : 1;

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

    // ── Escritura de cambios de formato ──────────────────────────

    private void Style_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading || Selected < 0)
            return;

        SizeValue.Text = $"{SizeSlider.Value:0}";
        LineValue.Text = $"{LineSlider.Value:0.0}";

        var alignH = HRight.IsChecked == true ? HAlign.Right
            : HLeft.IsChecked == true ? HAlign.Left : HAlign.Center;
        var alignV = VTop.IsChecked == true ? VAlign.Top
            : VBottom.IsChecked == true ? VAlign.Bottom : VAlign.Center;

        var box = _overrides[Selected]!;
        _overrides[Selected] = box with
        {
            FontFamily = string.IsNullOrWhiteSpace(FontCombo.Text) ? _theme.FontFamily : FontCombo.Text.Trim(),
            FontSize = Math.Round(SizeSlider.Value),
            Bold = BoldToggle.IsChecked == true,
            Italic = ItalicToggle.IsChecked == true,
            Underline = UnderlineToggle.IsChecked == true,
            Strikethrough = StrikeToggle.IsChecked == true,
            Shadow = ShadowCheck.IsChecked == true,
            TextColor = ColorBox.Text.Trim(),
            AlignH = alignH,
            AlignV = alignV,
            Case = (TextCase)Math.Clamp(CaseCombo.SelectedIndex, 0, 3),
            LineSpacing = LineSlider.Value,
        };

        RefreshSelected();
    }

    private void RefreshSelected()
    {
        if (Selected < 0)
            return;
        Preview.Slide = BuildSlide(Selected);
        _thumbs[Selected].Slide = BuildSlide(Selected);
        UpdateBoxInfo();
    }

    // ── Caja: colocar, arrastrar, redimensionar ──────────────────

    private void PlaceBox(SlideOverride over)
    {
        Canvas.SetLeft(BoxBorder, (over.BoxX ?? 0) * Scale);
        Canvas.SetTop(BoxBorder, (over.BoxY ?? 0) * Scale);
        BoxBorder.Width = (over.BoxWidth ?? 1920) * Scale;
        BoxBorder.Height = (over.BoxHeight ?? 1080) * Scale;
        UpdateBoxInfo();
    }

    private void CommitBox()
    {
        if (Selected < 0)
            return;

        _overrides[Selected] = _overrides[Selected]! with
        {
            BoxX = Math.Round(Canvas.GetLeft(BoxBorder) / Scale),
            BoxY = Math.Round(Canvas.GetTop(BoxBorder) / Scale),
            BoxWidth = Math.Round(BoxBorder.Width / Scale),
            BoxHeight = Math.Round(BoxBorder.Height / Scale),
        };
        RefreshSelected();
    }

    private void Box_MouseDown(object sender, MouseButtonEventArgs e)
    {
        // Don't start a drag when grabbing the resize handle.
        if (e.OriginalSource is System.Windows.Controls.Primitives.Thumb)
            return;
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

    private void BoxResize_DragDelta(object sender, DragDeltaEventArgs e)
    {
        var maxW = CanvasW - Canvas.GetLeft(BoxBorder);
        var maxH = CanvasH - Canvas.GetTop(BoxBorder);
        BoxBorder.Width = Math.Clamp(BoxBorder.Width + e.HorizontalChange, 60, maxW);
        BoxBorder.Height = Math.Clamp(BoxBorder.Height + e.VerticalChange, 40, maxH);
        UpdateBoxInfo();
        CommitBox();
    }

    private void UpdateBoxInfo()
    {
        BoxInfo.Text = $"X {Canvas.GetLeft(BoxBorder) / Scale:0} · Y {Canvas.GetTop(BoxBorder) / Scale:0} · " +
                       $"Ancho {BoxBorder.Width / Scale:0} · Alto {BoxBorder.Height / Scale:0}";
    }

    // ── Tamaño porcentual y alineación de la caja ────────────────

    private void SizePercent_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || Selected < 0 || SizePercentCombo.SelectedItem is not ComboBoxItem item)
            return;

        var percent = item.Content?.ToString()?.Replace("%", "").Trim() switch
        {
            "50" => 0.50, "60" => 0.60, "70" => 0.70, "75" => 0.75,
            "80" => 0.80, "90" => 0.90, _ => 1.0,
        };

        var w = 1920 * percent;
        var h = 1080 * percent;
        var x = (1920 - w) / 2;
        var y = (1080 - h) / 2;
        _overrides[Selected] = _overrides[Selected]! with { BoxX = x, BoxY = y, BoxWidth = w, BoxHeight = h };
        PlaceBox(_overrides[Selected]!);
        RefreshSelected();
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
            _thumbs[i].Slide = BuildSlide(i);
        }
        RefreshSelected();
    }

    private void ResetSlide_Click(object sender, RoutedEventArgs e)
    {
        if (Selected < 0)
            return;

        _overrides[Selected] = EnsureBox(null);
        LoadControls(_overrides[Selected]!);
        RefreshSelected();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        for (var i = 0; i < _song.Sections.Count; i++)
            _song.Sections[i].SetOverride(Normalize(_overrides[i]));

        Saved = true;
        DialogResult = true;
    }

    /// <summary>Drops the box and nulls any format field that matches the theme, so only real changes persist.</summary>
    private SlideOverride? Normalize(SlideOverride? over)
    {
        if (over is null)
            return null;

        var result = over with
        {
            FontFamily = Same(over.FontFamily, _theme.FontFamily) ? null : over.FontFamily,
            FontSize = over.FontSize is double fs && Math.Abs(fs - _theme.MaxFontSize) < 0.5 ? null : over.FontSize,
            Bold = over.Bold == _theme.Bold ? null : over.Bold,
            Italic = over.Italic == _theme.Italic ? null : over.Italic,
            Underline = over.Underline == true ? true : null,
            Strikethrough = over.Strikethrough == true ? true : null,
            Shadow = over.Shadow == _theme.Shadow ? null : over.Shadow,
            TextColor = Same(over.TextColor, _theme.TextColor) ? null : over.TextColor,
            AlignH = over.AlignH == _theme.AlignH ? null : over.AlignH,
            AlignV = over.AlignV == _theme.AlignV ? null : over.AlignV,
            Case = over.Case == (_theme.Uppercase ? TextCase.Upper : TextCase.None) ? null : over.Case,
            LineSpacing = over.LineSpacing is double ls && Math.Abs(ls - 1) < 0.01 ? null : over.LineSpacing,
        };

        return result.IsEmpty ? null : result;
    }

    private static bool Same(string? a, string? b) =>
        string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);
}
