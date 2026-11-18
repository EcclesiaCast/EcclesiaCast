using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Media;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.App.Views;

/// <summary>
/// The properties of a media item. What the fields mean, what is checked
/// when it saves and what the preview draws are in
/// <see cref="MediaInspectorViewModel"/>; this window places the controls and
/// moves the picture with the mouse and the arrow keys.
/// </summary>
public partial class MediaInspectorWindow : Window
{
    private readonly MediaInspectorViewModel _viewModel;
    private readonly MediaItem _item;

    /// <summary>False while the form is being filled in: the fields must not redraw the preview half-loaded.</summary>
    private bool _ready;

    private string _fillColor = "#000000";

    public MediaInspectorWindow(MediaInspectorViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _item = viewModel.Item;
        var item = _item;
        var form = viewModel.Initial();

        // Encuadre.
        ZoomField.SetSilently(form.ZoomPercent);
        OffsetXField.SetSilently(form.OffsetXPercent);
        OffsetYField.SetSilently(form.OffsetYPercent);
        FixedSizeCheck.IsChecked = form.FixedSize;
        FrameWidthField.SetSilently(form.FrameWidth);
        FrameHeightField.SetSilently(form.FrameHeight);
        SizeRow.IsEnabled = form.FixedSize;
        PresetBox.IsEnabled = form.FixedSize;
        PresetBox.SelectedIndex = 0;

        _fillColor = form.FillColor;
        UpdateFillSwatch();

        // Recorte.
        TrimStartField.SetSilently(form.TrimStart);
        TrimEndField.SetSilently(form.TrimEnd);
        TrimOptions.Visibility = viewModel.IsVideoFile ? Visibility.Visible : Visibility.Collapsed;

        FillOptionsBox.Fill(FillMediaBox, viewModel.FillOptions);
        FillMediaBox.SelectedItem = viewModel.FillOptionFor(form.FillMediaId);

        NameBox.Text = form.Name;
        TypeText.Text = viewModel.TypeText;
        VideoOptions.Visibility = viewModel.IsVideo ? Visibility.Visible : Visibility.Collapsed;
        AudioOptions.Visibility = viewModel.IsVideo ? Visibility.Visible : Visibility.Collapsed;
        ImageOptions.Visibility = viewModel.IsVideo ? Visibility.Collapsed : Visibility.Visible;

        NextMediaBox.ItemsSource = viewModel.Followers;
        NextMediaBox.SelectedItem = viewModel.FollowerFor(form.NextMediaId);

        ScalingBox.IsEnabled = viewModel.CanChangeScaling;

        CategoryBox.ItemsSource = viewModel.Categories;
        CategoryBox.Text = form.Category;

        BehaviorBox.SelectedIndex = form.BehaviorIndex;
        ScalingBox.SelectedIndex = form.ScalingIndex;
        EndBox.SelectedIndex = form.EndIndex;
        ShowSecondsField.SetSilently(form.ShowSeconds);
        ImageEndBox.SelectedIndex = form.ImageEndIndex;
        UpdateAfterOptions();
        MuteCheck.IsChecked = form.Muted;
        VolSlider.Value = form.Volume;
        BlurField.SetSilently(form.Blur);

        // Vencimiento: the day of the event and how long it stays after it.
        EventDatePicker.SelectedDate = form.EventDate;
        ArchiveDaysField.SetSilently(form.ArchiveDays);
        UpdateEventNote();

        // From here on the fields drive the preview.
        _ready = true;

        // Opens with the picture's frame picked, so the arrow keys work at once.
        Loaded += (_, _) => PreviewFrame.Focus();
        RefreshPreview();
    }

    public bool Saved { get; private set; }

    /// <summary>What the fields say right now.</summary>
    private MediaForm ReadForm() => new(
        EventDatePicker.SelectedDate?.Date,
        ArchiveDaysField.Value,
        NameBox.Text,
        CategoryBox.Text,
        BehaviorBox.SelectedIndex,
        ScalingBox.SelectedIndex,
        EndBox.SelectedIndex,
        ImageEndBox.SelectedIndex,
        ShowSecondsField.Value,
        (NextMediaBox.SelectedItem as MediaItem)?.Id,
        MuteCheck.IsChecked == true,
        VolSlider.Value,
        BlurField.Value,
        ZoomField.Value,
        OffsetXField.Value,
        OffsetYField.Value,
        FixedSizeCheck.IsChecked == true,
        FrameWidthField.Value,
        FrameHeightField.Value,
        TrimStartField.Value,
        TrimEndField.Value,
        _fillColor,
        (FillMediaBox.SelectedItem as MediaItem)?.Id);

    private void After_Changed(object sender, SelectionChangedEventArgs e) => UpdateAfterOptions();

    /// <summary>Shows the box that picks the next media only when «seguir con otro medio» is chosen.</summary>
    private void UpdateAfterOptions()
    {
        if (_viewModel is null || NextPanel is null || ImageEndBox is null || EndBox is null || BehaviorBox is null)
            return;

        var follows = _viewModel.FollowsNextMedia(EndBox.SelectedIndex, ImageEndBox.SelectedIndex);
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

    /// <summary>Says, under the date, what it will do.</summary>
    private void UpdateEventNote()
    {
        if (EventDateNote is null)
            return;

        EventDateNote.Text = MediaInspectorViewModel.EventNote(
            EventDatePicker.SelectedDate, ArchiveDaysField.Value, DateTime.Today);
    }

    private void Field_SelectionChanged(object sender, SelectionChangedEventArgs e) => RefreshPreview();

    /// <summary>Redraws the preview from what the fields say right now, before saving.</summary>
    private void RefreshPreview()
    {
        if (!_ready)
            return;

        var preview = _viewModel.PreviewOf(ReadForm());
        Preview.FillMedia = preview.FillMedia;
        Preview.Blur = preview.Blur;
        Preview.Media = preview.Media;
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
        if (FramePresets.SizeOf(PresetBox.SelectedIndex) is not { } chosen)
            return;

        FrameWidthField.SetSilently(chosen.Width);
        FrameHeightField.SetSilently(chosen.Height);
        FixedSizeCheck.IsChecked = true;
        RefreshPreview();
    }

    private void PickFillColor_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.PickFillColor(_fillColor) is { } picked)
        {
            _fillColor = picked;
            UpdateFillSwatch();
            RefreshPreview();
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
        _viewModel.Apply(ReadForm());

        Saved = true;
        DialogResult = true;
    }
}
