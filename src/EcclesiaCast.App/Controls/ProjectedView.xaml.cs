using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using EcclesiaCast.Core.Media;
using EcclesiaCast.Core.Presentation;

namespace EcclesiaCast.App.Controls;

/// <summary>
/// The full projected picture: a global background layer (image now, video
/// later) with the slide's text and states composited on top. The output
/// window and the operator previews both use it, so what you preview is what
/// projects.
/// </summary>
public partial class ProjectedView : UserControl
{
    public static readonly DependencyProperty BackgroundMediaProperty =
        DependencyProperty.Register(nameof(BackgroundMedia), typeof(MediaItem), typeof(ProjectedView),
            new PropertyMetadata(null, (d, _) => ((ProjectedView)d).OnBackgroundChanged()));

    public static readonly DependencyProperty SlideProperty =
        DependencyProperty.Register(nameof(Slide), typeof(SlideContent), typeof(ProjectedView),
            new PropertyMetadata(null, (d, e) => ((ProjectedView)d).SlideView.Slide = (SlideContent?)e.NewValue));

    public static readonly DependencyProperty StateProperty =
        DependencyProperty.Register(nameof(State), typeof(OutputState), typeof(ProjectedView),
            new PropertyMetadata(OutputState.Content, (d, e) => ((ProjectedView)d).SlideView.State = (OutputState)e.NewValue));

    public static readonly DependencyProperty OverlayProperty =
        DependencyProperty.Register(nameof(Overlay), typeof(string), typeof(ProjectedView),
            new PropertyMetadata(null, (d, e) => ((ProjectedView)d).SlideView.Overlay = (string?)e.NewValue));

    public static readonly DependencyProperty HighlightProperty =
        DependencyProperty.Register(nameof(Highlight), typeof(string), typeof(ProjectedView),
            new PropertyMetadata(null, (d, e) => ((ProjectedView)d).SlideView.Highlight = (string?)e.NewValue));

    public static readonly DependencyProperty AnimateTransitionsProperty =
        DependencyProperty.Register(nameof(AnimateTransitions), typeof(bool), typeof(ProjectedView),
            new PropertyMetadata(false, (d, e) => ((ProjectedView)d).SlideView.AnimateTransitions = (bool)e.NewValue));

    /// <summary>
    /// True in the output window, where a real video plays behind this
    /// control — so a video background shows nothing here (the video shows
    /// through). False in previews, where the video's poster is shown.
    /// </summary>
    public static readonly DependencyProperty IsLiveOutputProperty =
        DependencyProperty.Register(nameof(IsLiveOutput), typeof(bool), typeof(ProjectedView),
            new PropertyMetadata(false, (d, _) => ((ProjectedView)d).OnBackgroundChanged()));

    private string? _lastImagePath;

    public ProjectedView()
    {
        InitializeComponent();
    }

    /// <summary>The underlying slide renderer, so callers can reach its members.</summary>
    public SlideView SlideView => SlideRenderer;

    public MediaItem? BackgroundMedia
    {
        get => (MediaItem?)GetValue(BackgroundMediaProperty);
        set => SetValue(BackgroundMediaProperty, value);
    }

    public SlideContent? Slide
    {
        get => (SlideContent?)GetValue(SlideProperty);
        set => SetValue(SlideProperty, value);
    }

    public OutputState State
    {
        get => (OutputState)GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

    public string? Overlay
    {
        get => (string?)GetValue(OverlayProperty);
        set => SetValue(OverlayProperty, value);
    }

    public string? Highlight
    {
        get => (string?)GetValue(HighlightProperty);
        set => SetValue(HighlightProperty, value);
    }

    public bool AnimateTransitions
    {
        get => (bool)GetValue(AnimateTransitionsProperty);
        set => SetValue(AnimateTransitionsProperty, value);
    }

    public bool IsLiveOutput
    {
        get => (bool)GetValue(IsLiveOutputProperty);
        set => SetValue(IsLiveOutputProperty, value);
    }

    private void OnBackgroundChanged()
    {
        var media = BackgroundMedia;

        // Let the slide layer know it is sitting on top of media, so the
        // theme's background colour doesn't paint over it.
        SlideRenderer.IsOverMedia = media is not null;

        // Images render directly. Videos show their poster in previews, but
        // in the live output the poster is hidden so the moving video (behind
        // this control) shows through.
        var path = media?.Type == MediaType.Image
            ? media.Path
            : IsLiveOutput ? null : media?.ThumbnailPath; // póster de video/YouTube en los previews

        BackgroundImage.Stretch = media?.Scaling switch
        {
            MediaScaling.Fit => System.Windows.Media.Stretch.Uniform,
            MediaScaling.Stretch => System.Windows.Media.Stretch.Fill,
            _ => System.Windows.Media.Stretch.UniformToFill,
        };

        if (path == _lastImagePath)
            return;
        _lastImagePath = path;

        // YouTube posters are remote URLs; local media are files on disk.
        var isRemote = path?.StartsWith("http", StringComparison.OrdinalIgnoreCase) == true;

        if (!string.IsNullOrWhiteSpace(path) && (isRemote || File.Exists(path)))
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth = 1920;
                bitmap.EndInit();
                // A remote poster (YouTube) is still downloading here, and
                // freezing it would throw and drop us into the black fallback.
                // WPF fills the Image in once the download finishes.
                if (bitmap.CanFreeze)
                    bitmap.Freeze();
                BackgroundImage.Source = bitmap;
                BackgroundImage.Visibility = Visibility.Visible;
                if (AnimateTransitions)
                    BackgroundImage.BeginAnimation(OpacityProperty,
                        new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(320)) { EasingFunction = new QuadraticEase() });
                return;
            }
            catch
            {
                // Ilegible: se cae al fondo negro.
            }
        }

        BackgroundImage.Source = null;
        BackgroundImage.Visibility = Visibility.Collapsed;
    }
}
