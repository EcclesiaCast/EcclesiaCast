using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Presentation;

namespace EcclesiaCast.App.Views;

/// <summary>
/// «✏ Diseñar…» of the «Placas» tab: the church's name plate styles, with
/// every option and a preview that changes as they are touched. Nothing is
/// kept until «Guardar».
/// </summary>
public partial class NamePlateDesignerWindow : Window
{
    private readonly List<NamePlateStyle> _styles;
    private readonly NamePlate _sample;
    private int _index = -1;
    private bool _loading;

    /// <param name="sample">The plate written in the tab, to see each style with it.</param>
    public NamePlateDesignerWindow(NamePlate sample)
    {
        InitializeComponent();
        _sample = sample;
        Preview.Slide = new SlideContent("Cantad alegres al Señor,\nmortales todos por doquier");
        _styles = NamePlateStyleSettings.Current.ToList();

        FontBox.ItemsSource = Fonts.SystemFontFamilies.Select(f => f.Source).OrderBy(n => n).ToList();
        EnterBox.ItemsSource = NamePlateMedia.Animations.Select(a => a.Label).ToList();
        ExitBox.ItemsSource = NamePlateMedia.Exits.Select(a => a.Label).ToList();

        RefreshList();
        var start = _styles.FindIndex(s => string.Equals(s.Name, sample.StyleName, StringComparison.CurrentCultureIgnoreCase));
        StyleList.SelectedIndex = Math.Max(0, start);
    }

    /// <summary>The styles as they were left (read after «Guardar»).</summary>
    public IReadOnlyList<NamePlateStyle> Styles => _styles;

    /// <summary>The style selected when the window closed: the tab chooses it.</summary>
    public string ChosenStyle => _index >= 0 && _index < _styles.Count ? _styles[_index].Name : NamePlateStyle.Classic.Name;

    private NamePlateStyle Selected => _styles[_index];

    // ── La lista ─────────────────────────────────────────────────

    private void RefreshList()
    {
        var keep = _index;
        _loading = true;
        StyleList.ItemsSource = _styles.Select(s => s.Name).ToList();
        StyleList.SelectedIndex = Math.Clamp(keep, -1, _styles.Count - 1);
        _loading = false;
        DeleteButton.IsEnabled = _styles.Count > 1;
    }

    /// <summary>Shows style <paramref name="index"/> in the list, the options and the preview.</summary>
    private void Select(int index)
    {
        _index = index;
        RefreshList();
        LoadControls(Selected);
        ShowPreview(animate: false);
    }

    private void StyleList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || StyleList.SelectedIndex < 0)
            return;

        _index = StyleList.SelectedIndex;
        LoadControls(Selected);
        ShowPreview(animate: false);
    }

    private void Add(NamePlateStyle style)
    {
        _styles.Add(style with { Name = NamePlateStyleList.FreeName(_styles, style.Name) });
        Select(_styles.Count - 1);
        NameBox.Focus();
        NameBox.SelectAll();
    }

    private void New_Click(object sender, RoutedEventArgs e) => Add(NamePlateStyle.Classic with { Name = "Estilo nuevo" });

    private void Duplicate_Click(object sender, RoutedEventArgs e)
    {
        if (_index >= 0)
            Add(Selected);
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (_index < 0 || _styles.Count <= 1)
            return;

        if (!Notice.Confirm($"¿Borrar el estilo «{Selected.Name}»? Las placas que lo usan pasan a «{_styles.First(s => s != Selected).Name}».",
                "Borrar", title: "Borrar estilo", destructive: true))
            return;

        _styles.RemoveAt(_index);
        Select(Math.Min(_index, _styles.Count - 1));
    }

    /// <summary>Brings back the ready styles that were deleted (or renamed), next to the church's own.</summary>
    private void Restore_Click(object sender, RoutedEventArgs e)
    {
        var missing = NamePlateStyle.Presets
            .Where(p => _styles.All(s => !string.Equals(s.Name, p.Name, StringComparison.CurrentCultureIgnoreCase)))
            .ToList();
        if (missing.Count == 0)
        {
            Notice.Show("Ya están todos los estilos listos.", "Estilos de placa");
            return;
        }

        _styles.AddRange(missing);
        Select(_styles.Count - missing.Count);
    }

    // ── Los controles ────────────────────────────────────────────

    private void LoadControls(NamePlateStyle s)
    {
        _loading = true;
        NameBox.Text = s.Name;
        TargetBox.SelectedIndex = (int)s.Target;
        BackColorBox.Text = s.BackgroundColor;
        OpacitySlider.Value = s.BackgroundOpacity;
        CornerField.SetSilently(s.CornerRadius);
        WidthField.SetSilently(s.Width);
        HeightField.SetSilently(s.Height);
        AccentBox.SelectedIndex = (int)s.Accent;
        AccentColorBox.Text = s.AccentColor;
        AccentField.SetSilently(s.AccentThickness);
        NameOnAccentCheck.IsChecked = s.NameOnAccent;
        FontBox.Text = s.FontFamily;
        NameColorBox.Text = s.NameColor;
        NameSizeField.SetSilently(s.NameSize);
        RoleColorBox.Text = s.RoleColor;
        RoleSizeField.SetSilently(s.RoleSize);
        BoldCheck.IsChecked = s.NameBold;
        UpperCheck.IsChecked = s.Uppercase;
        ShadowCheck.IsChecked = s.TextShadow;
        AlignBox.SelectedIndex = (int)s.Align;
        BottomField.SetSilently(s.MarginBottom);
        SideField.SetSilently(s.MarginSide);
        EnterBox.SelectedIndex = NamePlateMedia.Animations.ToList().FindIndex(a => a.Animation == s.Enter);
        EnterMsField.SetSilently(s.EnterMs);
        ExitBox.SelectedIndex = NamePlateMedia.Exits.ToList().FindIndex(a => a.Exit == s.Exit);
        ExitMsField.SetSilently(s.ExitMs);
        _loading = false;
        PaintSwatches();
    }

    private NamePlateStyle ReadControls() => new NamePlateStyle
    {
        Name = NameBox.Text,
        Target = (NamePlateTarget)Math.Max(0, TargetBox.SelectedIndex),
        BackgroundColor = BackColorBox.Text,
        BackgroundOpacity = (int)Math.Round(OpacitySlider.Value),
        CornerRadius = (int)CornerField.Value,
        Width = (int)WidthField.Value,
        Height = (int)HeightField.Value,
        Accent = (NamePlateAccent)Math.Max(0, AccentBox.SelectedIndex),
        AccentColor = AccentColorBox.Text,
        AccentThickness = (int)AccentField.Value,
        NameOnAccent = NameOnAccentCheck.IsChecked == true,
        FontFamily = FontBox.Text,
        NameColor = NameColorBox.Text,
        NameSize = (int)NameSizeField.Value,
        RoleColor = RoleColorBox.Text,
        RoleSize = (int)RoleSizeField.Value,
        NameBold = BoldCheck.IsChecked == true,
        Uppercase = UpperCheck.IsChecked == true,
        TextShadow = ShadowCheck.IsChecked == true,
        Align = (NamePlateAlign)Math.Max(0, AlignBox.SelectedIndex),
        MarginBottom = (int)BottomField.Value,
        MarginSide = (int)SideField.Value,
        Enter = NamePlateMedia.Animations[Math.Max(0, EnterBox.SelectedIndex)].Animation,
        EnterMs = (int)EnterMsField.Value,
        Exit = NamePlateMedia.Exits[Math.Max(0, ExitBox.SelectedIndex)].Exit,
        ExitMs = (int)ExitMsField.Value,
    }.Clamped();

    /// <summary>Any option touched: the style takes it and the preview shows it.</summary>
    private void Any_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading || _index < 0)
            return;

        var style = ReadControls();
        var renamed = style.Name != Selected.Name;
        if (renamed && _styles.Where((_, i) => i != _index).Any(s => string.Equals(s.Name, style.Name, StringComparison.CurrentCultureIgnoreCase)))
            style = style with { Name = Selected.Name }; // two styles cannot share a name

        _styles[_index] = style;
        if (renamed)
            RefreshList();
        PaintSwatches();
        ShowPreview(animate: false);
    }

    private void Pick_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string boxName } || FindName(boxName) is not TextBox box)
            return;

        if (ColorPickerHelper.Pick(box.Text.Trim()) is { } picked)
            box.Text = picked; // runs Any_Changed
    }

    private void PaintSwatches()
    {
        static Brush Of(string hex)
        {
            try
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString(NamePlateStyle.Hex(hex, "#000000")));
            }
            catch (FormatException)
            {
                return Brushes.Black;
            }
        }

        BackSwatch.Background = Of(BackColorBox.Text);
        AccentSwatch.Background = Of(AccentColorBox.Text);
        NameSwatch.Background = Of(NameColorBox.Text);
        RoleSwatch.Background = Of(RoleColorBox.Text);
    }

    // ── La vista previa ──────────────────────────────────────────

    private void ShowPreview(bool animate)
    {
        if (_index < 0)
            return;

        Preview.AnimateTransitions = animate;
        Preview.NamePlate = _sample with { Style = Selected };
    }

    private void PlayIn_Click(object sender, RoutedEventArgs e)
    {
        Preview.AnimateTransitions = false;
        Preview.NamePlate = null;
        Dispatcher.BeginInvoke(() => ShowPreview(animate: true), System.Windows.Threading.DispatcherPriority.Background);
    }

    private void PlayOut_Click(object sender, RoutedEventArgs e)
    {
        Preview.AnimateTransitions = true;
        Preview.NamePlate = null;
        // And back, after it has gone, so the style stays in sight.
        var back = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(Selected.ExitMs + 900) };
        back.Tick += (_, _) =>
        {
            back.Stop();
            ShowPreview(animate: false);
        };
        back.Start();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
