using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Media;

namespace EcclesiaCast.App.Views;

public partial class MediaInspectorWindow : Window
{
    private readonly MediaItem _item;

    private string _fillColor = "#000000";
    private string _tint = "#2F63C9";

    public MediaInspectorWindow(MediaItem item, IReadOnlyList<string> categories, IReadOnlyList<MediaItem> library)
    {
        InitializeComponent();
        _item = item;

        // Encuadre.
        ZoomField.SetSilently(Math.Round((item.Zoom <= 0 ? 1 : item.Zoom) * 100));
        OffsetXField.SetSilently(Math.Round(item.OffsetX * 100));
        OffsetYField.SetSilently(Math.Round(item.OffsetY * 100));
        FixedSizeCheck.IsChecked = item.HasFrame;
        FrameWidthField.SetSilently(item.FrameWidth ?? 1920);
        FrameHeightField.SetSilently(item.FrameHeight ?? 1080);
        SizeRow.IsEnabled = item.HasFrame;
        PresetBox.IsEnabled = item.HasFrame;
        PresetBox.SelectedIndex = 0;

        _fillColor = string.IsNullOrWhiteSpace(item.FillColor) ? "#000000" : item.FillColor;
        UpdateFillSwatch();

        // Color y recorte.
        BrightnessField.SetSilently(item.Brightness);
        _tint = string.IsNullOrWhiteSpace(item.Tint) ? "#2F63C9" : item.Tint;
        TintField.SetSilently(item.TintStrength);
        UpdateTintSwatch();
        TrimStartField.SetSilently(item.TrimStart);
        TrimEndField.SetSilently(item.TrimEnd);
        TrimOptions.Visibility = item.Type == MediaType.Video ? Visibility.Visible : Visibility.Collapsed;

        // "Sin nada" first, then everything except this very item — filling a
        // background with itself would be a mirror facing a mirror.
        var options = new List<MediaItem> { new() { Id = 0, Name = "(sólo el color)" } };
        options.AddRange(library
            .Where(m => m.Id != item.Id && m.Type != MediaType.YouTube)
            .OrderBy(m => m.Name));
        FillMediaBox.ItemsSource = options;
        FillMediaBox.SelectedItem = options.FirstOrDefault(m => m.Id == (item.FillMediaId ?? 0)) ?? options[0];

        NameBox.Text = item.Name;
        TypeText.Text = item.Type switch
        {
            MediaType.Video => "Video",
            MediaType.YouTube => $"YouTube · {item.YouTubeId}",
            _ => "Imagen",
        };
        var isVideo = item.Type is MediaType.Video or MediaType.YouTube;
        VideoOptions.Visibility = isVideo ? Visibility.Visible : Visibility.Collapsed;
        AudioOptions.Visibility = isVideo ? Visibility.Visible : Visibility.Collapsed;
        ImageOptions.Visibility = isVideo ? Visibility.Collapsed : Visibility.Visible;

        // What can follow: any other media of the library.
        var followers = library.Where(m => m.Id != item.Id).OrderBy(m => m.Name).ToList();
        NextMediaBox.ItemsSource = followers;
        NextMediaBox.SelectedItem = followers.FirstOrDefault(m => m.Id == item.NextMediaId);

        // YouTube always fills the screen with its own player.
        ScalingBox.IsEnabled = item.Type != MediaType.YouTube;

        CategoryBox.ItemsSource = categories;
        CategoryBox.Text = item.Category;

        BehaviorBox.SelectedIndex = (int)item.Behavior;
        ScalingBox.SelectedIndex = (int)item.Scaling;
        EndBox.SelectedIndex = (int)item.EndBehavior;
        ShowSecondsField.SetSilently(item.ShowSeconds);
        // An image has no loop: the default of a new one (Loop) means "nothing".
        ImageEndBox.SelectedIndex = item.EndBehavior switch
        {
            VideoEndBehavior.Logo => 1,
            VideoEndBehavior.NextMedia => 2,
            _ => 0,
        };
        UpdateAfterOptions();
        MuteCheck.IsChecked = item.Muted;
        VolSlider.Value = item.Volume;
        BlurField.SetSilently(item.Blur);

        var poster = item.ThumbnailPath ?? (item.Type == MediaType.Image ? item.Path : null);
        if (!string.IsNullOrWhiteSpace(poster) && File.Exists(poster))
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(poster);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.DecodePixelWidth = 240;
                bmp.EndInit();
                // Remote posters (YouTube) are still downloading and can't be frozen.
                if (bmp.CanFreeze)
                    bmp.Freeze();
                Poster.Source = bmp;
            }
            catch { /* ignore */ }
        }
    }

    public bool Saved { get; private set; }

    private void After_Changed(object sender, SelectionChangedEventArgs e) => UpdateAfterOptions();

    /// <summary>
    /// Shows the box that picks the next media only when «seguir con otro
    /// medio» is chosen, and lets an image's seconds be set only when it goes
    /// on the whole screen: a background image never ends.
    /// </summary>
    private void UpdateAfterOptions()
    {
        if (NextPanel is null || ImageEndBox is null || EndBox is null || BehaviorBox is null)
            return;

        var isVideo = _item.Type is MediaType.Video or MediaType.YouTube;
        var follows = isVideo ? EndBox.SelectedIndex == 3 : ImageEndBox.SelectedIndex == 2;
        NextPanel.Visibility = follows ? Visibility.Visible : Visibility.Collapsed;
        ImageOptions.IsEnabled = BehaviorBox.SelectedIndex == 1;
    }

    private void Vol_Changed(object sender, RoutedPropertyChangedEventArgs<double> e) =>
        VolValue.Text = $"{VolSlider.Value:0}";

    private void OpenLocation_Click(object sender, RoutedEventArgs e)
    {
        if (!File.Exists(_item.Path))
            return;
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"/select,\"{_item.Path}\"",
            UseShellExecute = true,
        });
    }

    // ── Encuadre ─────────────────────────────────────────────────

    private void ResetFraming_Click(object sender, RoutedEventArgs e)
    {
        ZoomField.SetSilently(100);
        OffsetXField.SetSilently(0);
        OffsetYField.SetSilently(0);
        FixedSizeCheck.IsChecked = false;
    }

    private void FixedSize_Changed(object sender, RoutedEventArgs e)
    {
        var fixedSize = FixedSizeCheck.IsChecked == true;
        SizeRow.IsEnabled = fixedSize;
        PresetBox.IsEnabled = fixedSize;
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

        if (size is not { } chosen)
            return;

        FrameWidthField.SetSilently(chosen.w);
        FrameHeightField.SetSilently(chosen.h);
        FixedSizeCheck.IsChecked = true;
    }

    private void PickFillColor_Click(object sender, RoutedEventArgs e)
    {
        if (ColorPickerHelper.Pick(_fillColor) is { } picked)
        {
            _fillColor = picked;
            UpdateFillSwatch();
        }
    }

    private void PickTintColor_Click(object sender, RoutedEventArgs e)
    {
        if (ColorPickerHelper.Pick(_tint) is { } picked)
        {
            _tint = picked;
            UpdateTintSwatch();
        }
    }

    private void UpdateTintSwatch() => TintSwatch.Background = SwatchBrush(_tint);

    private static System.Windows.Media.Brush SwatchBrush(string hex)
    {
        try
        {
            return new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex));
        }
        catch (FormatException)
        {
            return System.Windows.Media.Brushes.Black;
        }
    }

    private void UpdateFillSwatch()
    {
        try
        {
            FillSwatch.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(_fillColor));
        }
        catch (FormatException)
        {
            FillSwatch.Background = System.Windows.Media.Brushes.Black;
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _item.Name = string.IsNullOrWhiteSpace(NameBox.Text) ? _item.Name : NameBox.Text.Trim();
        _item.Category = string.IsNullOrWhiteSpace(CategoryBox.Text) ? "Fondos" : CategoryBox.Text.Trim();
        _item.Behavior = (MediaBehavior)Math.Max(0, BehaviorBox.SelectedIndex);
        _item.Scaling = (MediaScaling)Math.Max(0, ScalingBox.SelectedIndex);
        var isVideo = _item.Type is MediaType.Video or MediaType.YouTube;
        _item.EndBehavior = isVideo
            ? (VideoEndBehavior)Math.Max(0, EndBox.SelectedIndex)
            : ImageEndBox.SelectedIndex switch
            {
                1 => VideoEndBehavior.Logo,
                2 => VideoEndBehavior.NextMedia,
                _ => VideoEndBehavior.Stop,
            };

        // Seconds and a following media only mean something with the choice that uses them.
        _item.ShowSeconds = isVideo ? 0 : (int)Math.Clamp(ShowSecondsField.Value, 0, 3600);
        var next = (NextMediaBox.SelectedItem as MediaItem)?.Id;
        if (_item.EndBehavior == VideoEndBehavior.NextMedia && next is null)
            _item.EndBehavior = VideoEndBehavior.Stop; // none chosen: nothing follows
        _item.NextMediaId = _item.EndBehavior == VideoEndBehavior.NextMedia ? next : null;
        _item.Muted = MuteCheck.IsChecked == true;
        _item.Volume = (int)VolSlider.Value;
        _item.Blur = BlurField.Value;

        _item.Zoom = Math.Clamp(ZoomField.Value / 100, 0.1, 4);
        _item.OffsetX = Math.Clamp(OffsetXField.Value / 100, -1, 1);
        _item.OffsetY = Math.Clamp(OffsetYField.Value / 100, -1, 1);

        if (FixedSizeCheck.IsChecked == true)
        {
            _item.FrameWidth = (int)Math.Clamp(FrameWidthField.Value, 16, 1920);
            _item.FrameHeight = (int)Math.Clamp(FrameHeightField.Value, 16, 1080);
        }
        else
        {
            _item.FrameWidth = null;
            _item.FrameHeight = null;
        }

        _item.Brightness = Math.Clamp(BrightnessField.Value, -100, 100);
        _item.Tint = TintField.Value > 0 ? _tint : null;
        _item.TintStrength = Math.Clamp(TintField.Value, 0, 100);
        _item.TrimStart = Math.Max(0, TrimStartField.Value);
        _item.TrimEnd = Math.Max(0, TrimEndField.Value);

        _item.FillColor = _fillColor;
        _item.FillMediaId = (FillMediaBox.SelectedItem as MediaItem)?.Id is int fillId and > 0 ? fillId : null;

        Saved = true;
        DialogResult = true;
    }
}
