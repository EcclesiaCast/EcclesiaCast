using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Logos;
using EcclesiaCast.Core.Media;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.App.Views;

/// <summary>
/// Manages the church's logos: one per kind of meeting (general, youth,
/// women's…), each an image, a looping video or plain text. Edits are written
/// as they happen, so the picker in the main window is always up to date.
/// What is decided and written is in <see cref="LogoManagerViewModel"/>; this
/// window places the controls and moves the logo in the preview.
/// </summary>
public partial class LogoManagerWindow : Window
{
    private readonly LogoManagerViewModel _viewModel;
    private bool _loading;

    public LogoManagerWindow(LogoManagerViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;

        FontCombo.ItemsSource = _viewModel.FontNames;

        // Dragging the logo in the preview changes the fields many times a
        // second: the preview follows at once, the library is written when it stops.
        _viewModel.PreviewChanged += logo => Preview.Logo = logo;
        _viewModel.Saved += logo =>
        {
            // The list shows the name and an icon per kind, so it has to redraw.
            LogosList.Items.Refresh();
            Preview.Logo = logo.Clone();
        };
        Closing += (_, _) => _viewModel.FlushSave();

        Reload(null);
    }

    private Logo? Current => LogosList.SelectedItem as Logo;

    private void Reload(int? keepId)
    {
        LogosList.ItemsSource = null;
        LogosList.ItemsSource = _viewModel.Items;
        LogosList.SelectedItem = _viewModel.Pick(keepId);

        // Nothing left to edit: blank the form out.
        if (Current is null)
            Fields.IsEnabled = false;
    }

    private void LogosList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _viewModel.FlushSave();
        if (Current is not { } logo)
        {
            Fields.IsEnabled = false;
            return;
        }

        Fields.IsEnabled = true;
        _loading = true;

        var form = LogoForm.Of(logo);
        NameBox.Text = form.Name;
        KindBox.SelectedIndex = form.KindIndex;
        PathBox.Text = form.Path;
        ScalingBox.SelectedIndex = form.ScalingIndex;
        MuteCheck.IsChecked = form.Muted;
        TextBoxField.Text = form.Text;
        FontCombo.Text = form.FontFamily;
        SizeField.SetSilently(form.FontSize);
        BoldCheck.IsChecked = form.Bold;
        TextColorBox.Text = form.TextColor;
        BackColorBox.Text = form.BackgroundColor;
        BlurField.SetSilently(form.BackgroundBlur);

        // Encuadre.
        ZoomField.SetSilently(form.ZoomPercent);
        OffsetXField.SetSilently(form.OffsetXPercent);
        OffsetYField.SetSilently(form.OffsetYPercent);
        FixedSizeCheck.IsChecked = form.FixedSize;
        FrameWidthField.SetSilently(form.FrameWidth);
        FrameHeightField.SetSilently(form.FrameHeight);
        SizeRow.IsEnabled = PresetBox.IsEnabled = form.FixedSize;
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

        var chosenFill = (FillMediaBox.SelectedItem as MediaItem)?.Id is int fillId and not 0 ? fillId : (int?)null;
        _viewModel.ApplyFields(logo, new LogoForm(
            NameBox.Text,
            KindBox.SelectedIndex,
            PathBox.Text,
            ScalingBox.SelectedIndex,
            MuteCheck.IsChecked == true,
            TextBoxField.Text,
            FontCombo.Text,
            SizeField.Value,
            BoldCheck.IsChecked == true,
            TextColorBox.Text,
            BackColorBox.Text,
            BlurField.Value,
            ZoomField.Value,
            OffsetXField.Value,
            OffsetYField.Value,
            FixedSizeCheck.IsChecked == true,
            FrameWidthField.Value,
            FrameHeightField.Value,
            chosenFill));

        // A video has no blurred copy of itself: switching the kind drops that choice.
        LoadFillOptions(logo.Kind, chosenFill);

        UpdatePanels();
    }

    /// <summary>Puts the choices for the borders in the box and selects the one in use.</summary>
    private void LoadFillOptions(LogoKind kind, int? selectedId)
    {
        var options = _viewModel.FillOptions(kind);

        var wasLoading = _loading;
        _loading = true;
        FillOptionsBox.Fill(FillMediaBox, options);
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
        if (LogoManagerViewModel.PresetSize(PresetBox.SelectedIndex) is not { } chosen || _loading)
            return;

        _loading = true;
        FrameWidthField.SetSilently(chosen.Width);
        FrameHeightField.SetSilently(chosen.Height);
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

    private bool CanFrame => LogoManagerViewModel.CanFrame(Current);

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

    // ── Archivo ──────────────────────────────────────────────────

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.FlushSave();
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

        var nameBefore = logo.Name;
        _viewModel.SetFile(logo, dialog.FileName);
        PathBox.Text = dialog.FileName;
        if (logo.Name != nameBefore)
            NameBox.Text = logo.Name;
    }

    private void PickTextColor_Click(object sender, RoutedEventArgs e)
    {
        var picked = _viewModel.PickColor(TextColorBox.Text.Trim());
        if (picked is not null)
            TextColorBox.Text = picked;
    }

    private void PickBackColor_Click(object sender, RoutedEventArgs e)
    {
        var picked = _viewModel.PickColor(BackColorBox.Text.Trim());
        if (picked is not null)
            BackColorBox.Text = picked;
    }

    // ── Alta, copia, baja y orden ────────────────────────────────

    private void New_Click(object sender, RoutedEventArgs e)
    {
        var created = _viewModel.CreateNew();
        Reload(created.Id);
        NameBox.Focus();
        NameBox.SelectAll();
    }

    private void Duplicate_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.FlushSave();
        if (Current is not { } logo)
            return;

        Reload(_viewModel.Duplicate(logo).Id);
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.FlushSave();
        if (Current is not { } logo)
            return;

        if (_viewModel.Delete(logo))
            Reload(null);
    }

    private void MoveUp_Click(object sender, RoutedEventArgs e) => Move(-1);

    private void MoveDown_Click(object sender, RoutedEventArgs e) => Move(+1);

    private void Move(int direction)
    {
        _viewModel.FlushSave();
        if (Current is not { } logo)
            return;

        if (_viewModel.Move(logo, direction))
            Reload(logo.Id);
    }

    private void Done_Click(object sender, RoutedEventArgs e) => Close();
}
