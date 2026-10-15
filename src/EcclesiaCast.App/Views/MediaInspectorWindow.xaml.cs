using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Media;

namespace EcclesiaCast.App.Views;

public partial class MediaInspectorWindow : Window
{
    private readonly MediaItem _item;

    /// <summary>False while the form is being filled in: the fields must not redraw the preview half-loaded.</summary>
    private bool _ready;

    private string _fillColor = "#000000";

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

        // Recorte.
        TrimStartField.SetSilently(item.TrimStart);
        TrimEndField.SetSilently(item.TrimEnd);
        TrimOptions.Visibility = item.Type == MediaType.Video ? Visibility.Visible : Visibility.Collapsed;

        // "Sin nada" first, then everything except this very item — filling a
        // background with itself would be a mirror facing a mirror.
        var options = new List<MediaItem> { new() { Id = 0, Name = "(sólo el color)" } };

        // A picture can fill its own bars with a soft copy of itself, which makes
        // it look like one image with no border. A video would need a second player.
        if (item.Type == MediaType.Image)
            options.Add(new MediaItem { Id = MediaItem.FillWithSelfId, Name = "(la misma imagen, desenfocada)" });

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

        // Vencimiento: the day of the event and how long it stays after it.
        EventDatePicker.SelectedDate = item.EventDate?.Date;
        ArchiveDaysField.SetSilently(item.EventDate is null ? MediaExpiry.DefaultDays : item.ArchiveAfterDays);
        UpdateEventNote();

        // From here on the fields drive the preview.
        _ready = true;

        // Opens with the picture's frame picked, so the arrow keys work at once.
        Loaded += (_, _) => PreviewFrame.Focus();
        RefreshPreview();
    }

    public bool Saved { get; private set; }

    private void After_Changed(object sender, SelectionChangedEventArgs e) => UpdateAfterOptions();

    /// <summary>
    /// Shows the box that picks the next media only when «seguir con otro
    /// medio» is chosen. An image's seconds count whether it goes in front of
    /// the words or behind them.
    /// </summary>
    private void UpdateAfterOptions()
    {
        if (NextPanel is null || ImageEndBox is null || EndBox is null || BehaviorBox is null)
            return;

        var isVideo = _item.Type is MediaType.Video or MediaType.YouTube;
        var follows = isVideo ? EndBox.SelectedIndex == 3 : ImageEndBox.SelectedIndex == 2;
        NextPanel.Visibility = follows ? Visibility.Visible : Visibility.Collapsed;
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
        RefreshPreview();
    }

    // ── Vista previa en vivo ─────────────────────────────────────

    private void Field_Changed(object sender, RoutedEventArgs e) => RefreshPreview();

    private void EventDatePicker_SelectedDateChanged(object? sender, SelectionChangedEventArgs e) => UpdateEventNote();

    private void EventDate_Changed(object sender, RoutedEventArgs e) => UpdateEventNote();

    /// <summary>A click anywhere on the date opens the calendar: the date is chosen there, never typed.</summary>
    private void EventDatePicker_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!EventDatePicker.IsDropDownOpen)
            EventDatePicker.IsDropDownOpen = true;
    }

    private void ClearEventDate_Click(object sender, RoutedEventArgs e) => EventDatePicker.SelectedDate = null;

    /// <summary>Says, under the date, what it will do: «pasa a Eventos pasados el 27/10/2026».</summary>
    private void UpdateEventNote()
    {
        if (EventDateNote is null)
            return;

        if (EventDatePicker.SelectedDate is not { } date)
        {
            EventDateNote.Text = "Sin fecha: no vence. Con la fecha de un evento, pasa sola a «Eventos pasados» unos días después.";
            return;
        }

        var day = MediaExpiry.MovesOn(date, (int)ArchiveDaysField.Value)!.Value;
        EventDateNote.Text = day <= DateTime.Today
            ? $"Esa fecha ya pasó: al guardar se mueve a «{MediaExpiry.ArchiveTab}»."
            : $"Pasa a «{MediaExpiry.ArchiveTab}» el {MediaExpiry.Format(day)}.";
    }

    private void Field_SelectionChanged(object sender, SelectionChangedEventArgs e) => RefreshPreview();

    /// <summary>
    /// Redraws the preview from what the fields say right now — before
    /// saving — by handing it a copy of the item with those values on it.
    /// The real item is only touched by «Guardar».
    /// </summary>
    private void RefreshPreview()
    {
        if (!_ready)
            return;

        var shown = _item.Clone();
        shown.Scaling = (MediaScaling)Math.Max(0, ScalingBox.SelectedIndex);
        shown.Zoom = Math.Clamp(ZoomField.Value / 100, 0.1, 4);
        shown.OffsetX = Math.Clamp(OffsetXField.Value / 100, -MediaFraming.MaxOffset, MediaFraming.MaxOffset);
        shown.OffsetY = Math.Clamp(OffsetYField.Value / 100, -MediaFraming.MaxOffset, MediaFraming.MaxOffset);
        shown.FrameWidth = FixedSizeCheck.IsChecked == true ? (int)Math.Clamp(FrameWidthField.Value, 16, 1920) : null;
        shown.FrameHeight = FixedSizeCheck.IsChecked == true ? (int)Math.Clamp(FrameHeightField.Value, 16, 1080) : null;
        shown.FillColor = _fillColor;

        var chosenFill = FillMediaBox.SelectedItem as MediaItem;
        shown.FillMediaId = chosenFill is { Id: not 0 } ? chosenFill.Id : null;
        Preview.FillMedia = chosenFill is { Id: > 0 } fill ? fill : shown.FillsWithSelf ? shown : null;
        Preview.Blur = BlurField.Value;
        Preview.Media = shown;
    }

    private Point? _dragFrom;
    private double _dragStartX;
    private double _dragStartY;

    /// <summary>
    /// The arrow keys nudge the picture by 0.1 % of the screen (about two
    /// pixels on a 1920-wide one), or 1 % with Shift: the fine adjustment a
    /// mouse drag cannot give. They write the «Correr» fields, as the drag does.
    /// </summary>
    private void PreviewFrame_PreviewKeyDown(object sender, KeyEventArgs e)
    {
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
        // The arrow keys only work with the frame picked, so a click on it takes the focus.
        PreviewFrame.Focus();
        _dragFrom = e.GetPosition(PreviewFrame);
        _dragStartX = OffsetXField.Value;
        _dragStartY = OffsetYField.Value;
        PreviewFrame.CaptureMouse();
        e.Handled = true;
    }

    /// <summary>
    /// Dragging moves the picture by the same amount the pointer moved: the
    /// shift is a fraction of the screen, so a fraction of this frame is the
    /// same distance. It writes the «Correr» fields, which redraw the preview.
    /// </summary>
    private void PreviewFrame_MouseMove(object sender, MouseEventArgs e)
    {
        // The screen is a rectangle inside the frame, so its size, not the
        // frame's, is what a shift is a fraction of.
        if (_dragFrom is not Point from || !PreviewFrame.IsMouseCaptured
            || Preview.ScreenWidth <= 0 || Preview.ScreenHeight <= 0)
            return;

        var now = e.GetPosition(PreviewFrame);
        OffsetXField.Value = _dragStartX + (now.X - from.X) / Preview.ScreenWidth * 100;
        OffsetYField.Value = _dragStartY + (now.Y - from.Y) / Preview.ScreenHeight * 100;
    }

    private void PreviewFrame_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _dragFrom = null;
        PreviewFrame.ReleaseMouseCapture();
    }

    /// <summary>The wheel moves the «Zoom» field by its own step (5 points a notch).</summary>
    private void PreviewFrame_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        ZoomField.Value += e.Delta > 0 ? 5 : -5;
        e.Handled = true;
    }

    private void FixedSize_Changed(object sender, RoutedEventArgs e)
    {
        var fixedSize = FixedSizeCheck.IsChecked == true;
        SizeRow.IsEnabled = fixedSize;
        PresetBox.IsEnabled = fixedSize;
        RefreshPreview();
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
        RefreshPreview();
    }

    private void PickFillColor_Click(object sender, RoutedEventArgs e)
    {
        if (ColorPickerHelper.Pick(_fillColor) is { } picked)
        {
            _fillColor = picked;
            UpdateFillSwatch();
            RefreshPreview();
        }
    }

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
        _item.EventDate = EventDatePicker.SelectedDate?.Date;
        _item.ArchiveAfterDays = (int)Math.Clamp(ArchiveDaysField.Value, 0, 365);
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
        _item.OffsetX = Math.Clamp(OffsetXField.Value / 100, -MediaFraming.MaxOffset, MediaFraming.MaxOffset);
        _item.OffsetY = Math.Clamp(OffsetYField.Value / 100, -MediaFraming.MaxOffset, MediaFraming.MaxOffset);

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

        // Brillo y tinte se sacaron (15/10/2026): lo que hubiera quedado guardado se borra.
        _item.Brightness = 0;
        _item.Tint = null;
        _item.TintStrength = 0;
        _item.TrimStart = Math.Max(0, TrimStartField.Value);
        _item.TrimEnd = Math.Max(0, TrimEndField.Value);

        _item.FillColor = _fillColor;
        _item.FillMediaId = (FillMediaBox.SelectedItem as MediaItem)?.Id is int fillId and not 0 ? fillId : null;

        Saved = true;
        DialogResult = true;
    }
}
