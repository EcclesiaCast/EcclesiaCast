using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
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

        // Dragging the logo in the preview changes the fields many times a
        // second: the preview follows at once, the library is written when it stops.
        _saveTimer.Tick += (_, _) => FlushSave();
        Closing += (_, _) => FlushSave();

        Reload(null);
    }

    private readonly DispatcherTimer _saveTimer = new() { Interval = TimeSpan.FromMilliseconds(400) };
    private Logo? _pendingSave;

    private void SaveSoon(Logo logo)
    {
        _pendingSave = logo;
        Preview.Logo = logo.Clone();
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private void FlushSave()
    {
        _saveTimer.Stop();
        if (_pendingSave is { } logo)
        {
            _pendingSave = null;
            Save(logo);
        }
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
        FlushSave();
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

        // Encuadre.
        ZoomField.SetSilently(Math.Round((logo.Zoom <= 0 ? 1 : logo.Zoom) * 100));
        OffsetXField.SetSilently(Math.Round(logo.OffsetX * 1000) / 10);
        OffsetYField.SetSilently(Math.Round(logo.OffsetY * 1000) / 10);
        var hasFrame = logo.FrameWidth is > 0 && logo.FrameHeight is > 0;
        FixedSizeCheck.IsChecked = hasFrame;
        FrameWidthField.SetSilently(logo.FrameWidth ?? 1920);
        FrameHeightField.SetSilently(logo.FrameHeight ?? 1080);
        SizeRow.IsEnabled = PresetBox.IsEnabled = hasFrame;
        PresetBox.SelectedIndex = 0;
        LoadFillOptions(logo.Kind, logo.FillMediaId);

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

        logo.Zoom = Math.Clamp(ZoomField.Value / 100, 0.1, 4);
        logo.OffsetX = Math.Clamp(OffsetXField.Value / 100, -MediaFraming.MaxOffset, MediaFraming.MaxOffset);
        logo.OffsetY = Math.Clamp(OffsetYField.Value / 100, -MediaFraming.MaxOffset, MediaFraming.MaxOffset);
        var fixedSize = FixedSizeCheck.IsChecked == true;
        logo.FrameWidth = fixedSize ? (int)Math.Clamp(FrameWidthField.Value, 16, 1920) : null;
        logo.FrameHeight = fixedSize ? (int)Math.Clamp(FrameHeightField.Value, 16, 1080) : null;

        // A video has no blurred copy of itself: switching the kind drops that choice.
        var chosenFill = (FillMediaBox.SelectedItem as MediaItem)?.Id is int fillId and not 0 ? fillId : (int?)null;
        LoadFillOptions(logo.Kind, chosenFill);
        logo.FillMediaId = (FillMediaBox.SelectedItem as MediaItem)?.Id is int kept and not 0 ? kept : null;

        UpdatePanels();
        SaveSoon(logo);
    }

    /// <summary>
    /// The choices for the borders: the colour alone, the logo itself blurred
    /// (for a video, a still of it: a moving copy would need a second player)
    /// or an image of the media library.
    /// </summary>
    private void LoadFillOptions(LogoKind kind, int? selectedId)
    {
        var options = new List<MediaItem> { new() { Id = 0, Name = "El color de fondo" } };
        if (kind == LogoKind.Image)
            options.Add(new MediaItem { Id = MediaItem.FillWithSelfId, Name = "La misma imagen, desenfocada" });
        else if (kind == LogoKind.Video)
            options.Add(new MediaItem { Id = MediaItem.FillWithSelfId, Name = "Una imagen fija del video, desenfocada" });
        options.AddRange(MediaLookup.All()
            .Where(m => m.Type == MediaType.Image)
            .OrderBy(m => m.Name));

        var wasLoading = _loading;
        _loading = true;
        FillMediaBox.ItemsSource = options;
        FillMediaBox.SelectedItem = options.FirstOrDefault(m => m.Id == (selectedId ?? 0)) ?? options[0];
        _loading = wasLoading;
    }

    // ── Encuadre ─────────────────────────────────────────────────

    private void ResetFraming_Click(object sender, RoutedEventArgs e)
    {
        _loading = true;
        ZoomField.SetSilently(100);
        OffsetXField.SetSilently(0);
        OffsetYField.SetSilently(0);
        FixedSizeCheck.IsChecked = false;
        SizeRow.IsEnabled = PresetBox.IsEnabled = false;
        _loading = false;
        Field_Changed(sender, e);
    }

    private void FixedSize_Changed(object sender, RoutedEventArgs e)
    {
        var fixedSize = FixedSizeCheck.IsChecked == true;
        SizeRow.IsEnabled = fixedSize;
        PresetBox.IsEnabled = fixedSize;
        Field_Changed(sender, e);
    }

    private void Preset_Changed(object sender, SelectionChangedEventArgs e)
    {
        (double w, double h)? size = PresetBox.SelectedIndex switch
        {
            1 => (1920, 1080),
            2 => (1280, 720),
            3 => (1440, 1080),
            4 => (1080, 1080),
            5 => (1080, 1920),
            _ => null,
        };
        if (size is not { } chosen || _loading)
            return;

        _loading = true;
        FrameWidthField.SetSilently(chosen.w);
        FrameHeightField.SetSilently(chosen.h);
        FixedSizeCheck.IsChecked = true;
        SizeRow.IsEnabled = PresetBox.IsEnabled = true;
        _loading = false;
        Field_Changed(sender, e);
    }

    /// <summary>The screen inside the preview box: the slide is drawn 16:9 and centred.</summary>
    private Size PreviewScreen()
    {
        var width = Math.Min(Preview.ActualWidth, Preview.ActualHeight * 16 / 9);
        return new Size(width, width * 9 / 16);
    }

    private bool CanFrame => Current is { Kind: LogoKind.Image or LogoKind.Video };

    private Point? _dragFrom;
    private double _dragStartX;
    private double _dragStartY;

    /// <summary>The arrow keys nudge the logo by 0.1 % of the screen, or 1 % with Shift.</summary>
    private void PreviewFrame_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!CanFrame)
            return;

        var step = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? 1.0 : 0.1;
        switch (e.Key)
        {
            case Key.Left: OffsetXField.Value -= step; break;
            case Key.Right: OffsetXField.Value += step; break;
            case Key.Up: OffsetYField.Value -= step; break;
            case Key.Down: OffsetYField.Value += step; break;
            default: return;
        }

        e.Handled = true;
    }

    private void PreviewFrame_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!CanFrame)
            return;

        PreviewFrame.Focus();
        _dragFrom = e.GetPosition(PreviewFrame);
        _dragStartX = OffsetXField.Value;
        _dragStartY = OffsetYField.Value;
        PreviewFrame.CaptureMouse();
        e.Handled = true;
    }

    /// <summary>Dragging moves the logo as far as the pointer moved, over the screen drawn in the box.</summary>
    private void PreviewFrame_MouseMove(object sender, MouseEventArgs e)
    {
        var screen = PreviewScreen();
        if (_dragFrom is not Point from || !PreviewFrame.IsMouseCaptured || screen.Width <= 0)
            return;

        var now = e.GetPosition(PreviewFrame);
        OffsetXField.Value = _dragStartX + (now.X - from.X) / screen.Width * 100;
        OffsetYField.Value = _dragStartY + (now.Y - from.Y) / screen.Height * 100;
    }

    private void PreviewFrame_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _dragFrom = null;
        PreviewFrame.ReleaseMouseCapture();
    }

    /// <summary>The wheel moves the zoom by 5 points a notch.</summary>
    private void PreviewFrame_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!CanFrame)
            return;

        ZoomField.Value += e.Delta > 0 ? 5 : -5;
        e.Handled = true;
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
        FlushSave();
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
        FlushSave();
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
        FlushSave();
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
        FlushSave();
        if (Current is not { } logo)
            return;

        var confirm = Notice.Confirm(
            $"¿Eliminar el logo \"{logo.Name}\"?",
            "Eliminar", destructive: true);
        if (!confirm)
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
        FlushSave();
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
