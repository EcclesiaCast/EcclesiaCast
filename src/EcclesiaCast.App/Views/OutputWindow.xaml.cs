using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using EcclesiaCast.App.Services;
using EcclesiaCast.App.ViewModels;
using EcclesiaCast.Core.Displays;
using EcclesiaCast.Core.Logos;
using EcclesiaCast.Core.Media;
using EcclesiaCast.Core.Presentation;
using MediaType = EcclesiaCast.Core.Media.MediaType;

namespace EcclesiaCast.App.Views;

public partial class OutputWindow : Window
{
    private DisplayInfo? _display;

    public OutputWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        YouTube.Ended += (_, _) => VideoEnded?.Invoke(this, EventArgs.Empty);
        Video.Ended += (_, _) => VideoEnded?.Invoke(this, EventArgs.Empty);
        Video.PreviewFrameChanged += (_, frame) => VideoFrameChanged?.Invoke(this, frame);
    }

    /// <summary>Raised when the projected video finishes and shouldn't loop.</summary>
    public event EventHandler? VideoEnded;

    /// <summary>Raised when the small copy of the video for the operator's preview changes.</summary>
    public event EventHandler<System.Windows.Media.ImageSource?>? VideoFrameChanged;

    /// <summary>Shows this window fullscreen on the given display.</summary>
    public void ShowOn(DisplayInfo display)
    {
        _display = display;
        Show();
        MoveToDisplay();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is INotifyPropertyChanged oldVm)
            oldVm.PropertyChanged -= OnProjectionChanged;
        if (e.NewValue is INotifyPropertyChanged newVm)
            newVm.PropertyChanged += OnProjectionChanged;
        UpdateVideo();
        UpdateLogoVideo();
    }

    private void OnProjectionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ProjectionViewModel.Background) or nameof(ProjectionViewModel.BackgroundFill))
            UpdateVideo();
        else if (e.PropertyName == nameof(ProjectionViewModel.BackgroundBlur))
            SetBlur((DataContext as ProjectionViewModel)?.BackgroundBlur ?? 0);
        else if (e.PropertyName is nameof(ProjectionViewModel.ActiveLogo) or nameof(ProjectionViewModel.State))
            UpdateLogoVideo();
    }

    /// <summary>
    /// A video logo plays on its own surface, above the background and below
    /// the text — so the lower-third announcement still shows over it.
    /// </summary>
    private void UpdateLogoVideo()
    {
        var vm = DataContext as ProjectionViewModel;
        var logo = vm?.ActiveLogo;
        var showing = vm?.State == OutputState.Logo
            && logo is { Kind: LogoKind.Video }
            && !string.IsNullOrWhiteSpace(logo.Path);

        LogoVideo.Visibility = showing ? Visibility.Visible : Visibility.Collapsed;
        LogoVideo.Show(showing ? AsLoopingVideo(logo!) : null);
    }

    /// <summary>Wraps a video logo as a media item so the VLC surface can play it.</summary>
    private static MediaItem AsLoopingVideo(Logo logo) => new()
    {
        // Negative ids never collide with the media library's own rows.
        Id = -1000 - logo.Id,
        Name = logo.Name,
        Path = logo.Path!,
        Type = MediaType.Video,
        Scaling = logo.Scaling,
        Muted = logo.Muted,
        EndBehavior = VideoEndBehavior.Loop,
    };

    private void UpdateVideo()
    {
        var background = (DataContext as ProjectionViewModel)?.Background;

        if (background is { Type: MediaType.YouTube })
        {
            Video.Show(null);
            YouTube.Visibility = Visibility.Visible;
            YouTube.Play(background);
        }
        else
        {
            YouTube.Clear();
            YouTube.Visibility = Visibility.Collapsed;
            Video.Show(background);
        }

        UpdateFill();
        SetBlur((DataContext as ProjectionViewModel)?.BackgroundBlur ?? 0);
    }

    /// <summary>
    /// Paints whatever the background does not cover. A church whose screen
    /// is not the shape of the projector's picture frames the background to
    /// the screen and fills the rest — with a colour, or with another item
    /// from the library (the usual trick is a blurred copy of the same
    /// video).
    /// </summary>
    private void UpdateFill()
    {
        var vm = DataContext as ProjectionViewModel;
        var background = vm?.Background;
        // Images are drawn by the layer above, which paints its own fill
        // right behind them; here we cover what the video surface leaves.
        var needsFill = background?.Type != MediaType.Image && MediaFraming.NeedsFill(background);
        var fill = needsFill ? vm?.BackgroundFill : null;

        FillColorLayer.Fill = MediaFraming.FillBrush(background);
        FillColorLayer.Visibility = needsFill ? Visibility.Visible : Visibility.Collapsed;

        var fillImage = fill?.Type == MediaType.Image ? fill.Path : null;
        FillImage.Source = fillImage is null ? null : LoadFillBitmap(fillImage);
        FillImage.Visibility = FillImage.Source is null ? Visibility.Collapsed : Visibility.Visible;

        // A video fill runs on its own surface — one VLC player each, which is
        // the arrangement that keeps them from tripping over one another.
        var fillVideo = fill?.Type == MediaType.Video ? fill : null;
        FillVideo.Visibility = fillVideo is null ? Visibility.Collapsed : Visibility.Visible;
        FillVideo.Show(fillVideo is null ? null : AsFillVideo(fillVideo));
    }

    /// <summary>
    /// The fill video always loops, is always silent and always covers the
    /// screen: it is scenery behind the real background, not the background.
    /// </summary>
    private static MediaItem AsFillVideo(MediaItem fill)
    {
        var copy = fill.Clone();
        copy.EndBehavior = VideoEndBehavior.Loop;
        copy.Muted = true;
        copy.Scaling = MediaScaling.Fill;
        copy.Zoom = 1;
        copy.OffsetX = copy.OffsetY = 0;
        copy.FrameWidth = copy.FrameHeight = null;
        return copy;
    }

    private static System.Windows.Media.Imaging.BitmapImage? LoadFillBitmap(string path)
    {
        if (!System.IO.File.Exists(path))
            return null;

        try
        {
            var bitmap = new System.Windows.Media.Imaging.BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(path);
            bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
            bitmap.DecodePixelWidth = 1920;
            bitmap.EndInit();
            if (bitmap.CanFreeze)
                bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    // ── Transporte y desenfoque del fondo ────────────────────────

    private bool IsYouTubeShowing => YouTube.Visibility == Visibility.Visible;

    /// <summary>Where the projected video is, whichever player is showing it.</summary>
    public PlaybackState PlaybackState => IsYouTubeShowing ? YouTube.State : Video.State;

    public void TogglePlayPause()
    {
        if (IsYouTubeShowing)
            YouTube.TogglePlayPause();
        else
            Video.TogglePlayPause();
    }

    public void SeekTo(TimeSpan position)
    {
        if (IsYouTubeShowing)
            YouTube.SeekTo(position);
        else
            Video.SeekTo(position);
    }

    public void Skip(TimeSpan delta)
    {
        if (IsYouTubeShowing)
            YouTube.Skip(delta);
        else
            Video.Skip(delta);
    }

    /// <summary>Switching the output off must stop the sound and the picture too.</summary>
    public void PauseForHiddenOutput()
    {
        YouTube.PauseForHiddenOutput();
        Video.PauseForHiddenOutput();
        LogoVideo.PauseForHiddenOutput();
        FillVideo.PauseForHiddenOutput();
    }

    public void ResumeAfterHiddenOutput()
    {
        YouTube.ResumeAfterHiddenOutput();
        Video.ResumeAfterHiddenOutput();
        LogoVideo.ResumeAfterHiddenOutput();
        FillVideo.ResumeAfterHiddenOutput();
    }

    /// <summary>Blurs the background layer (video, YouTube or image), 0–100.</summary>
    public void SetBlur(double amount)
    {
        Video.SetBlur(amount);
        YouTube.SetBlur(amount);
        Projected.BackgroundBlur = amount;
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
        Video.Stop();
        LogoVideo.Stop();
        YouTube.Clear();
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
