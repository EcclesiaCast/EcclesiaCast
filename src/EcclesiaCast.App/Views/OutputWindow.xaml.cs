using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Animation;
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

    public OutputWindow() : this(floating: false)
    {
    }

    /// <param name="floating">
    /// An ordinary window («⧉ En una ventana») instead of a full screen on a display.
    /// </param>
    public OutputWindow(bool floating)
    {
        InitializeComponent();
        IsFloating = floating;
        if (floating)
            MakeFloating();
        DataContextChanged += OnDataContextChanged;
        YouTube.Ended += (_, _) => VideoEnded?.Invoke(this, EventArgs.Empty);
        YouTube.DoubleClicked += (_, _) => TurnOffForDoubleClick();
        YouTube.EscapePressed += (_, _) => OnYouTubeEscape();
        YouTube.MenuRequested += (_, _) => OpenFloatingMenu();
        Video.Ended += (_, _) => VideoEnded?.Invoke(this, EventArgs.Empty);

        // Changing media never goes through black: whatever was on screen stays
        // until the new picture is up, then fades away.
        // • Image to video: the image (kept by the projected view) goes when the
        //   video draws its first picture.
        Video.FirstFrame += (_, _) => Projected.ReleaseHeldImage(MediaFade);

        // The video logo's frames are drawn by the logo layer, not by its player.
        LogoVideo.FrameSourceChanged += (_, frame) =>
        {
            if (_logoVideoId is not null)
            {
                Projected.LogoVideoFrame = frame;
                LogoFrameChanged?.Invoke(this, frame);
            }
        };
        // • Video to image: the video's last picture (kept by the surface) goes
        //   when the image has finished fading in over it.
        Projected.BackgroundImageSettled += (_, _) => Video.ReleaseHold(TimeSpan.Zero);

        // The operator's LIVE panel gets a small copy of the video frames,
        // which costs a shrink and a bitmap write on every one of them. With
        // «bajo rendimiento» nobody listens, so none of that work is done
        // (the video's producer only prepares the copy while someone is listening).
        Loaded += (_, _) =>
        {
            PerformanceMode.Changed += OnPerformanceChanged;
            TransitionSettings.Changed += OnPerformanceChanged;
            ApplyPerformance();
        };
        Closed += (_, _) =>
        {
            PerformanceMode.Changed -= OnPerformanceChanged;
            TransitionSettings.Changed -= OnPerformanceChanged;
        };
    }

    /// <summary>How long one medium takes to give way to the next; zero in «bajo rendimiento».</summary>
    private static TimeSpan MediaFade =>
        PerformanceMode.IsLow ? TimeSpan.Zero : TransitionSettings.Current.Media;

    private bool _forwardingFrames;

    /// <summary>
    /// The background is a heavy video (4K, 50+ frames). The operator's LIVE
    /// panel shows its poster instead of a moving copy: the projector already
    /// has all it can do with that picture, and shrinking every frame for a
    /// small panel would take more of the same processor.
    /// </summary>
    private bool _backgroundIsHeavy;

    private void OnPreviewFrame(object? sender, System.Windows.Media.ImageSource? frame) =>
        VideoFrameChanged?.Invoke(this, frame);

    private void OnPerformanceChanged(object? sender, EventArgs e) => ApplyPerformance();

    /// <summary>
    /// Low-performance mode: transitions cut instead of animating on the
    /// projector, and the video stops being copied for the operator's panel.
    /// </summary>
    private void ApplyPerformance()
    {
        var low = PerformanceMode.IsLow;
        Projected.AnimateTransitions = !low;
        Video.HoldFade = LogoVideo.HoldFade = FillVideo.HoldFade = MediaFade;

        var forward = !low && !_backgroundIsHeavy;
        if (!forward && _forwardingFrames)
        {
            Video.PreviewFrameChanged -= OnPreviewFrame;
            _forwardingFrames = false;
            VideoFrameChanged?.Invoke(this, null);
        }
        else if (forward && !_forwardingFrames)
        {
            Video.PreviewFrameChanged += OnPreviewFrame;
            _forwardingFrames = true;
            Video.ResetPreview();
        }
    }

    /// <summary>Raised when the projected video finishes and shouldn't loop.</summary>
    public event EventHandler? VideoEnded;

    /// <summary>Raised when the small copy of the video for the operator's preview changes.</summary>
    public event EventHandler<System.Windows.Media.ImageSource?>? VideoFrameChanged;

    /// <summary>The frames of a video logo, copied to the operator's Live panel.</summary>
    public event EventHandler<System.Windows.Media.ImageSource?>? LogoFrameChanged;

    /// <summary>
    /// Raised when the operator double-clicks the output to switch it off.
    /// Only while it covers the operator panel: on a projector of its own a
    /// stray double click must never take the words off the screen.
    /// </summary>
    public event EventHandler? TurnOffRequested;

    /// <summary>Shows this window fullscreen on the given display.</summary>
    public void ShowOn(DisplayInfo display)
    {
        var wasHidden = !IsVisible || IsFadingOut;

        // Something was projected while the screen was still fading to black
        // to switch off: it stays on.
        CancelFadeOut();

        _display = display;
        if (IsFloating)
        {
            // Stays where the operator left it; back from «off» it comes up again
            // even if it had been minimized.
            Show();
            if (wasHidden && WindowState == WindowState.Minimized)
                WindowState = WindowState.Normal;
            return;
        }

        Show();
        MoveToDisplay();

        if (wasHidden && !s_wayBackShown && OperatorScreen.Holds(display))
        {
            s_wayBackShown = true;
            ShowWayBackHint();
        }
    }

    // ── Cómo volver al panel con una sola pantalla ───────────────

    /// <summary>
    /// Once per run: congregations that project with a single screen (a
    /// notebook, or a projector duplicating it) may see it too, so it is not
    /// repeated every time the output comes on.
    /// </summary>
    private static bool s_wayBackShown;

    /// <summary>The same, over a YouTube video: the first one projected on the operator's screen.</summary>
    private static bool s_youTubeWayBackShown;

    private const string WayBackText = "Para volver al programa: tecla Esc o doble clic en esta pantalla";

    /// <summary>
    /// Esc, if the keyboard ever ends up here instead of in the operator panel,
    /// does what it does there: switches the output off. A 1.4.0 user on a
    /// single screen was left behind a YouTube video with no way out.
    /// </summary>
    protected override void OnPreviewKeyDown(System.Windows.Input.KeyEventArgs e)
    {
        // The floating window is handled like any other: Esc there does nothing.
        if (e.Key == System.Windows.Input.Key.Escape && !IsFloating)
        {
            e.Handled = true;
            TurnOffRequested?.Invoke(this, EventArgs.Empty);
            Application.Current?.MainWindow?.Activate();
            return;
        }

        base.OnPreviewKeyDown(e);
    }

    /// <summary>
    /// With a single screen the output covers the operator panel, and someone
    /// new has no way to know that Esc brings it back: the first beta tester
    /// with a notebook was left stuck with a verse on screen. Flashes the way
    /// out for a few seconds, as the stage window does.
    /// </summary>
    private void ShowWayBackHint()
    {
        // A YouTube video already on screen would hide it (native window): the
        // page shows it over the video instead.
        if (YouTube.Visibility == Visibility.Visible && !s_youTubeWayBackShown)
        {
            s_youTubeWayBackShown = true;
            YouTube.ShowHint(WayBackText);
        }

        WayBackHint.BeginAnimation(OpacityProperty, null);
        WayBackHint.Visibility = Visibility.Visible;
        WayBackHint.Opacity = 1;

        var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(700))
        {
            BeginTime = TimeSpan.FromSeconds(6),
        };
        fade.Completed += (_, _) => WayBackHint.Visibility = Visibility.Collapsed;
        WayBackHint.BeginAnimation(OpacityProperty, fade);
    }

    private void Output_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) =>
        e.Handled = TurnOffForDoubleClick();

    /// <summary>A double click over the picture (or over a YouTube video, which reports its own).</summary>
    private bool TurnOffForDoubleClick()
    {
        if (_display is null || !OperatorScreen.Holds(_display))
            return false;

        TurnOffRequested?.Invoke(this, EventArgs.Empty);
        Application.Current?.MainWindow?.Activate();
        return true;
    }

    /// <summary>Esc pressed while the keyboard was in a YouTube video: the same as Esc anywhere else.</summary>
    private void OnYouTubeEscape()
    {
        if (IsFloating)
            return;

        TurnOffRequested?.Invoke(this, EventArgs.Empty);
        Application.Current?.MainWindow?.Activate();
    }

    // ── Apagar la salida con fundido ─────────────────────────────

    private int _shutterVersion;

    /// <summary>True while the screen is fading to black before the window closes.</summary>
    public bool IsFadingOut { get; private set; }

    /// <summary>
    /// Switches the output off: fades to black over the time set in
    /// «Transiciones» (if any) and then hides the window. YouTube, being a
    /// native window that WPF cannot fade, is hidden right away.
    /// </summary>
    public void FadeOutAndHide()
    {
        if (IsFadingOut)
            return;

        var fade = PerformanceMode.IsLow ? TimeSpan.Zero : TransitionSettings.Current.Output;
        if (fade <= TimeSpan.Zero || !IsVisible)
        {
            Hide();
            return;
        }

        var version = ++_shutterVersion;
        IsFadingOut = true;
        if (YouTube.Visibility == Visibility.Visible)
            YouTube.Visibility = Visibility.Hidden;

        ShutterLayer.Visibility = Visibility.Visible;
        var animation = new DoubleAnimation(0, 1, fade);
        animation.Completed += (_, _) =>
        {
            if (version != _shutterVersion)
                return;

            IsFadingOut = false;
            Hide();
            ResetShutter();
        };
        ShutterLayer.BeginAnimation(OpacityProperty, animation);
    }

    private void CancelFadeOut()
    {
        // The window is hidden with the shutter closed: open it again too.
        if (!IsFadingOut && ShutterLayer.Visibility != Visibility.Visible && YouTube.Visibility != Visibility.Hidden)
            return;

        _shutterVersion++;
        IsFadingOut = false;
        ResetShutter();
        UpdateYouTubeCover();
    }

    private void ResetShutter()
    {
        ShutterLayer.BeginAnimation(OpacityProperty, null);
        ShutterLayer.Opacity = 0;
        ShutterLayer.Visibility = Visibility.Collapsed;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is INotifyPropertyChanged oldVm)
            oldVm.PropertyChanged -= OnProjectionChanged;
        if (e.NewValue is INotifyPropertyChanged newVm)
            newVm.PropertyChanged += OnProjectionChanged;
        UpdateVideo();
        UpdateLogoVideo();
        UpdateLogoCover();
    }

    private void OnProjectionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ProjectionViewModel.Background) or nameof(ProjectionViewModel.BackgroundFill))
            UpdateVideo();
        else if (e.PropertyName == nameof(ProjectionViewModel.BackgroundBlur))
            SetBlur((DataContext as ProjectionViewModel)?.BackgroundBlur ?? 0);
        else if (e.PropertyName is nameof(ProjectionViewModel.ActiveLogo) or nameof(ProjectionViewModel.State))
        {
            UpdateLogoVideo();
            UpdateLogoCover();
            if (e.PropertyName == nameof(ProjectionViewModel.State))
                UpdateYouTubeCover();
        }
    }

    /// <summary>
    /// YouTube runs in a WebView2, a native window that Windows always draws
    /// above anything WPF paints — so the logo and the black screen, which are
    /// WPF, cannot cover it. While one of them is meant to be on screen the
    /// player is hidden (and paused, so it doesn't keep sounding behind it).
    /// </summary>
    private void UpdateYouTubeCover()
    {
        var vm = DataContext as ProjectionViewModel;
        if (vm?.Background is not { Type: MediaType.YouTube })
            return;

        if (vm.State is OutputState.Logo or OutputState.Black)
        {
            YouTube.Visibility = Visibility.Hidden;
            YouTube.PauseForCover();
        }
        else
        {
            YouTube.Visibility = Visibility.Visible;
            YouTube.ResumeAfterCover();
        }
    }


    /// <summary>
    /// With the logo or the black screen up nothing behind them needs to move: the background video
    /// and the video that fills its bars are paused (they pick up where they
    /// were when the logo goes), and the page or window that is live stops
    /// being redrawn (see <see cref="ProjectedView"/>). The logo's own video
    /// keeps playing, of course. YouTube has its own cover, above.
    /// </summary>
    private void UpdateLogoCover()
    {
        var covered = (DataContext as ProjectionViewModel)?.State is OutputState.Logo or OutputState.Black;
        Video.SetCovered(covered);
        FillVideo.SetCovered(covered);
    }

    /// <summary>
    /// A video logo is decoded by its own player, which is never on screen:
    /// its frames are drawn in the logo layer of the projected view, like an
    /// image logo, so no background (a theme colour, a picture) can cover it
    /// and it fades, frames and fills with everything an image logo has.
    /// </summary>
    private void UpdateLogoVideo()
    {
        var vm = DataContext as ProjectionViewModel;
        var logo = vm?.ActiveLogo;
        var showing = vm?.State == OutputState.Logo
            && logo is { Kind: LogoKind.Video }
            && !string.IsNullOrWhiteSpace(logo.Path);

        var version = ++_logoVideoVersion;
        if (showing)
        {
            // Another video: its poster shows until its first frame arrives.
            if (_logoVideoId != logo!.Id)
            {
                _logoVideoId = logo.Id;
                Projected.LogoVideoFrame = null;
                LogoFrameChanged?.Invoke(this, null);
            }

            LogoVideo.Show(AsLoopingVideo(logo));
            return;
        }

        void Remove()
        {
            // Asked for again in the meantime: leave it.
            if (version != _logoVideoVersion)
                return;

            LogoVideo.Show(null);
            _logoVideoId = null;
            Projected.LogoVideoFrame = null;
            LogoFrameChanged?.Invoke(this, null);
        }

        // The video keeps playing while the logo layer fades away.
        var fade = PerformanceMode.IsLow ? TimeSpan.Zero : TransitionSettings.Current.Logo;
        if (fade > TimeSpan.Zero && _logoVideoId is not null)
        {
            var timer = new System.Windows.Threading.DispatcherTimer { Interval = fade };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                Remove();
            };
            timer.Start();
        }
        else
        {
            Remove();
        }
    }

    private int? _logoVideoId;

    private int _logoVideoVersion;

    /// <summary>Wraps a video logo as a media item so the VLC surface can play it.</summary>
    private static MediaItem AsLoopingVideo(Logo logo) => logo.AsFramedMedia();

    private void UpdateVideo()
    {
        var background = (DataContext as ProjectionViewModel)?.Background;

        var heavy = background is not null && LightVideoCache.IsHeavy(background);
        if (heavy != _backgroundIsHeavy)
        {
            _backgroundIsHeavy = heavy;
            if (IsLoaded)
                ApplyPerformance();
        }

        if (background is { Type: MediaType.YouTube })
        {
            Video.Show(null);
            YouTube.Visibility = Visibility.Visible;
            YouTube.Play(background);
            UpdateYouTubeCover();

            // The WPF hint cannot be drawn over the video (it is a native
            // window), so with a single screen the page shows it itself.
            if (!s_youTubeWayBackShown && _display is not null && OperatorScreen.Holds(_display))
            {
                s_youTubeWayBackShown = true;
                YouTube.ShowHint(WayBackText);
            }
        }
        else
        {
            YouTube.Clear();
            YouTube.Visibility = Visibility.Collapsed;

            if (background is { Type: MediaType.Image })
            {
                // The image fades in over the video's last picture; when it is
                // fully up, BackgroundImageSettled lets that picture go.
                Video.Stop(holdLastFrame: true);
            }
            else if (background is null)
            {
                // Nothing replaces it: the last picture fades out instead of cutting.
                Video.Stop(holdLastFrame: true);
                Video.ReleaseHold(MediaFade);
            }
            else
            {
                Video.Show(background);
            }
        }

        UpdateFill();
        SetBlur((DataContext as ProjectionViewModel)?.BackgroundBlur ?? 0);

        // A video put on while the logo is up stays paused under it.
        UpdateLogoCover();
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
        // A web page and a captured window are drawn by that layer too, from
        // the web panel's browser and the window capture.
        var needsFill = background?.Type is not (MediaType.Image or MediaType.Web or MediaType.Window) && MediaFraming.NeedsFill(background);
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

    // Not the surface's Visibility: while the logo or black covers it the player
    // is hidden but still the one the play bar must talk to, not VLC.
    private bool IsYouTubeShowing =>
        (DataContext as ProjectionViewModel)?.Background?.Type == MediaType.YouTube;

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
        // The LIVE panel shows the logo's poster, still, while the output is off.
        LogoFrameChanged?.Invoke(this, null);
    }

    public void ResumeAfterHiddenOutput()
    {
        YouTube.ResumeAfterHiddenOutput();
        Video.ResumeAfterHiddenOutput();
        LogoVideo.ResumeAfterHiddenOutput();
        FillVideo.ResumeAfterHiddenOutput();
        if (_logoVideoId is not null)
            LogoFrameChanged?.Invoke(this, Projected.LogoVideoFrame);
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
        if (IsFloating)
        {
            // An ordinary window: it activates, and it is sized and placed as it was left.
            SetUpFloating(hwnd);
            return;
        }

        _ = SetWindowLong(hwnd, GWL_EXSTYLE,
            GetWindowLong(hwnd, GWL_EXSTYLE) | WS_EX_NOACTIVATE);

        MoveToDisplay();
    }

    protected override void OnClosed(EventArgs e)
    {
        // A closed window (swapped for the floating one, or back) stops
        // following the projection: its players are gone.
        if (DataContext is INotifyPropertyChanged vm)
            vm.PropertyChanged -= OnProjectionChanged;

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
