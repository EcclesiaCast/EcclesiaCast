using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace EcclesiaCast.App.Services;

/// <summary>
/// The captured window, as a picture that keeps changing: the twin of
/// <see cref="WebLiveFeed"/>. Whoever projects it (the output window, the
/// operator's LIVE and PREVIEW) draws <see cref="Source"/>, which the capture
/// rewrites in place, so every view shows the same moving picture without a
/// copy of it. A new picture object only appears when the window changes size.
/// </summary>
public static class WindowCaptureFeed
{
    private static ImageSource? _source;

    /// <summary>The window's picture, or null while nothing is being captured.</summary>
    public static ImageSource? Source => _source;

    /// <summary>Raised when <see cref="Source"/> is another picture (started, stopped, resized).</summary>
    public static event EventHandler? SourceChanged;

    /// <summary>Called by the capture, on the UI thread.</summary>
    internal static void Publish(ImageSource? source)
    {
        if (ReferenceEquals(_source, source))
            return;

        _source = source;
        SourceChanged?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>Lets a test put a picture in without a window being captured.</summary>
    public static void SetSourceForTest(ImageSource? source) => Publish(source);

    /// <summary>
    /// A still copy of the window right now, <paramref name="width"/> wide: what
    /// the operator's panels draw instead of the moving picture on a slow
    /// computer.
    /// </summary>
    public static BitmapSource? Snapshot(int width = 960)
    {
        if (Source is not { Width: > 0, Height: > 0 } picture)
            return null;

        var height = Math.Max(1, (int)Math.Round(width * picture.Height / picture.Width));
        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
            context.DrawImage(picture, new Rect(0, 0, width, height));

        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }
}
