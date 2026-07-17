using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using EcclesiaCast.App.ViewModels;
using EcclesiaCast.Core.Displays;
using EcclesiaCast.Core.Media;
using LibVLCSharp.Shared;
using MediaType = EcclesiaCast.Core.Media.MediaType;

namespace EcclesiaCast.App.Views;

public partial class OutputWindow : Window
{
    private DisplayInfo? _display;
    private MediaPlayer? _player;
    private int _playingMediaId = -1;

    public OutputWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        DataContextChanged += OnDataContextChanged;
    }

    /// <summary>Shows this window fullscreen on the given display.</summary>
    public void ShowOn(DisplayInfo display)
    {
        _display = display;
        Show();
        MoveToDisplay();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (App.VideoEngine is { } engine && _player is null)
        {
            _player = new MediaPlayer(engine) { Mute = true, EnableHardwareDecoding = true };
            Video.MediaPlayer = _player;
            UpdateVideo();
        }
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is INotifyPropertyChanged oldVm)
            oldVm.PropertyChanged -= OnProjectionChanged;
        if (e.NewValue is INotifyPropertyChanged newVm)
            newVm.PropertyChanged += OnProjectionChanged;
        UpdateVideo();
    }

    private void OnProjectionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProjectionViewModel.Background))
            UpdateVideo();
    }

    // ── Reproducción del fondo de video ──────────────────────────

    private void UpdateVideo()
    {
        if (_player is null || App.VideoEngine is not { } engine)
            return;

        var background = (DataContext as ProjectionViewModel)?.Background;

        if (background is not { Type: MediaType.Video })
        {
            if (_playingMediaId != -1)
            {
                _player.Stop();
                _playingMediaId = -1;
            }
            return;
        }

        if (background.Id == _playingMediaId)
            return;

        _playingMediaId = background.Id;
        ApplyScaling(background.Scaling);
        _player.Mute = background.Muted;
        _player.Volume = Math.Clamp(background.Volume, 0, 100);

        using var media = new Media(engine, new Uri(background.Path));
        if (background.EndBehavior == VideoEndBehavior.Loop)
            media.AddOption(":input-repeat=65535");
        _player.Play(media);
    }

    private void ApplyScaling(MediaScaling scaling)
    {
        if (_player is null)
            return;

        switch (scaling)
        {
            case MediaScaling.Fill:   // recorta lo que sobra para llenar 16:9
                _player.AspectRatio = null;
                _player.CropGeometry = "16:9";
                break;
            case MediaScaling.Stretch: // deforma para llenar 16:9
                _player.CropGeometry = null;
                _player.AspectRatio = "16:9";
                break;
            default:                  // Fit: proporción original, con barras
                _player.CropGeometry = null;
                _player.AspectRatio = null;
                break;
        }
    }

    // ── Posición en el monitor de salida ─────────────────────────

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var hwnd = new WindowInteropHelper(this).Handle;
        _ = SetWindowLong(hwnd, GWL_EXSTYLE,
            GetWindowLong(hwnd, GWL_EXSTYLE) | WS_EX_NOACTIVATE);

        MoveToDisplay();
    }

    protected override void OnClosed(EventArgs e)
    {
        _player?.Stop();
        Video.MediaPlayer = null;
        _player?.Dispose();
        _player = null;
        base.OnClosed(e);
    }

    private void MoveToDisplay()
    {
        if (_display is null)
            return;

        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero)
            return;

        SetWindowPos(hwnd, IntPtr.Zero,
            _display.X, _display.Y, _display.Width, _display.Height,
            SWP_NOZORDER | SWP_FRAMECHANGED | SWP_SHOWWINDOW | SWP_NOACTIVATE);
    }

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_FRAMECHANGED = 0x0020;
    private const uint SWP_SHOWWINDOW = 0x0040;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd, IntPtr hWndInsertAfter,
        int x, int y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
}
