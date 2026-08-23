using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Logos;
using EcclesiaCast.Core.Media;

namespace EcclesiaCast.App.Views;

/// <summary>
/// Manages the church's logos: one per kind of meeting (general, youth,
/// women's…), each an image, a looping video or plain text. Edits are written
/// as they happen, so the picker in the main window is always up to date.
/// </summary>
public partial class LogoManagerWindow : Window
{
    private readonly ILogoRepository _logos;
    private readonly List<Logo> _items = [];
    private bool _loading;

    public LogoManagerWindow(ILogoRepository logos)
    {
        InitializeComponent();
        _logos = logos;

        FontCombo.ItemsSource = Fonts.SystemFontFamilies.Select(f => f.Source).OrderBy(n => n).ToList();

        Reload(null);
    }

    /// <summary>True if anything was added, edited or removed.</summary>
    public bool ChangesMade { get; private set; }

    private Logo? Current => LogosList.SelectedItem as Logo;

    private void Reload(int? keepId)
    {
        _items.Clear();
        _items.AddRange(_logos.GetAll());

        LogosList.ItemsSource = null;
        LogosList.ItemsSource = _items;
        LogosList.SelectedItem = _items.FirstOrDefault(l => l.Id == keepId) ?? _items.FirstOrDefault();

        // Nothing left to edit: blank the form out.
        if (Current is null)
            Fields.IsEnabled = false;
    }

    private void LogosList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Current is not { } logo)
        {
            Fields.IsEnabled = false;
            return;
        }

        Fields.IsEnabled = true;
        _loading = true;

        NameBox.Text = logo.Name;
        KindBox.SelectedIndex = (int)logo.Kind;
        PathBox.Text = logo.Path ?? string.Empty;
        ScalingBox.SelectedIndex = (int)logo.Scaling;
        MuteCheck.IsChecked = logo.Muted;
        TextBoxField.Text = logo.Text;
        FontCombo.Text = logo.FontFamily;
        SizeField.SetSilently(logo.FontSize);
        BoldCheck.IsChecked = logo.Bold;
        TextColorBox.Text = logo.TextColor;
        BackColorBox.Text = logo.BackgroundColor;
        BlurField.SetSilently(logo.BackgroundBlur);

        _loading = false;
        UpdatePanels();
        Preview.Logo = logo.Clone();
    }

    /// <summary>Only the fields that matter for the chosen kind are shown.</summary>
    private void UpdatePanels()
    {
        var kind = (LogoKind)Math.Clamp(KindBox.SelectedIndex, 0, 2);
        FilePanel.Visibility = kind == LogoKind.Text ? Visibility.Collapsed : Visibility.Visible;
        TextPanel.Visibility = kind == LogoKind.Text ? Visibility.Visible : Visibility.Collapsed;
        MuteCheck.Visibility = kind == LogoKind.Video ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Field_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading || Current is not { } logo)
            return;

        logo.Name = string.IsNullOrWhiteSpace(NameBox.Text) ? "Logo" : NameBox.Text.Trim();
        logo.Kind = (LogoKind)Math.Clamp(KindBox.SelectedIndex, 0, 2);
        logo.Scaling = (MediaScaling)Math.Max(0, ScalingBox.SelectedIndex);
        logo.Muted = MuteCheck.IsChecked == true;
        logo.Text = TextBoxField.Text;
        logo.FontFamily = string.IsNullOrWhiteSpace(FontCombo.Text) ? "Segoe UI" : FontCombo.Text.Trim();
        logo.FontSize = SizeField.Value;
        logo.Bold = BoldCheck.IsChecked == true;
        logo.TextColor = TextColorBox.Text.Trim();
        logo.BackgroundColor = BackColorBox.Text.Trim();
        logo.BackgroundBlur = BlurField.Value;

        UpdatePanels();
        Save(logo);
    }

    private void Save(Logo logo)
    {
        _logos.Save(logo);
        ChangesMade = true;

        // The list shows the name and an icon per kind, so it has to redraw.
        LogosList.Items.Refresh();
        Preview.Logo = logo.Clone();
    }

    // ── Archivo ──────────────────────────────────────────────────

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        if (Current is not { } logo)
            return;

        var isVideo = logo.Kind == LogoKind.Video;
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = isVideo ? "Video del logo" : "Imagen del logo",
            Filter = isVideo
                ? "Videos|*.mp4;*.mov;*.m4v;*.avi;*.mkv;*.wmv;*.webm"
                : "Imágenes|*.png;*.jpg;*.jpeg;*.bmp;*.webp;*.gif",
        };
        if (dialog.ShowDialog() != true)
            return;

        logo.Path = dialog.FileName;
        PathBox.Text = dialog.FileName;

        // A video logo needs a still for the operator's preview boxes, where
        // no video plays — the projector shows the real thing.
        MediaThumbnails.Delete(logo.PosterPath);
        logo.PosterPath = isVideo
            ? MediaThumbnails.Create(dialog.FileName, MediaType.Video, App.VideoEngine)
            : null;

        if (string.IsNullOrWhiteSpace(logo.Name) || logo.Name == "Logo nuevo")
        {
            logo.Name = Path.GetFileNameWithoutExtension(dialog.FileName);
            NameBox.Text = logo.Name;
        }

        Save(logo);
    }

    private void PickTextColor_Click(object sender, RoutedEventArgs e)
    {
        var picked = ColorPickerHelper.Pick(TextColorBox.Text.Trim());
        if (picked is not null)
            TextColorBox.Text = picked;
    }

    private void PickBackColor_Click(object sender, RoutedEventArgs e)
    {
        var picked = ColorPickerHelper.Pick(BackColorBox.Text.Trim());
        if (picked is not null)
            BackColorBox.Text = picked;
    }

    // ── Alta, copia, baja y orden ────────────────────────────────

    private void New_Click(object sender, RoutedEventArgs e)
    {
        var created = _logos.Save(new Logo
        {
            Name = "Logo nuevo",
            Kind = LogoKind.Image,
            Order = _items.Count,
        });
        ChangesMade = true;
        Reload(created.Id);
        NameBox.Focus();
        NameBox.SelectAll();
    }

    private void Duplicate_Click(object sender, RoutedEventArgs e)
    {
        if (Current is not { } logo)
            return;

        var copy = logo.Clone();
        copy.Id = 0;
        copy.Name = $"{logo.Name} (copia)";
        copy.Order = _items.Count;
        var created = _logos.Save(copy);
        ChangesMade = true;
        Reload(created.Id);
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (Current is not { } logo)
            return;

        var confirm = MessageBox.Show(
            $"¿Eliminar el logo \"{logo.Name}\"?",
            "EcclesiaCast", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes)
            return;

        MediaThumbnails.Delete(logo.PosterPath);
        _logos.Delete(logo.Id);
        ChangesMade = true;
        Reload(null);
    }

    private void MoveUp_Click(object sender, RoutedEventArgs e) => Move(-1);

    private void MoveDown_Click(object sender, RoutedEventArgs e) => Move(+1);

    private void Move(int direction)
    {
        if (Current is not { } logo)
            return;

        var index = _items.IndexOf(logo);
        var target = index + direction;
        if (index < 0 || target < 0 || target >= _items.Count)
            return;

        (_items[index], _items[target]) = (_items[target], _items[index]);
        for (var i = 0; i < _items.Count; i++)
        {
            _items[i].Order = i;
            _logos.Save(_items[i]);
        }

        ChangesMade = true;
        Reload(logo.Id);
    }

    private void Done_Click(object sender, RoutedEventArgs e) => Close();
}
