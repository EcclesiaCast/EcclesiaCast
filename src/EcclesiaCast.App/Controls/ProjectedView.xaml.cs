using System.IO;
using System.Windows;
using System.Windows.Controls;
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

    private void OnBackgroundChanged()
    {
        var media = BackgroundMedia;

        // Images render directly; videos use their poster thumbnail here
        // (live playback is added with the LibVLC output layer).
        var path = media?.Type == MediaType.Image ? media.Path : media?.ThumbnailPath;

        if (path == _lastImagePath)
            return;
        _lastImagePath = path;

        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth = 1920;
                bitmap.EndInit();
                bitmap.Freeze();
                BackgroundImage.Source = bitmap;
                BackgroundImage.Visibility = Visibility.Visible;
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
