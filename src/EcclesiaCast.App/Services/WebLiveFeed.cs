using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace EcclesiaCast.App.Services;

/// <summary>
/// The page of the web panel, as a picture that keeps changing. Whoever
/// projects a web page (the output window, the operator's LIVE and PREVIEW)
/// shows <see cref="Source"/>: it is the very image the panel's browser draws
/// into, so every window shows the same moving picture without a copy of it.
///
/// Also carries the mouse pointer over the page, so the screen can draw an
/// arrow where the operator is pointing.
/// </summary>
public static class WebLiveFeed
{
    private static Image? _image;
    private static ImageSource? _announced;

    /// <summary>The browser's picture, or null while the browser does not exist yet.</summary>
    public static ImageSource? Source => _image?.Source;

    /// <summary>Raised when <see cref="Source"/> is a different picture (the browser was created).</summary>
    public static event EventHandler? SourceChanged;

    /// <summary>
    /// Hooks the image the browser draws into. Called once, when the panel
    /// creates the browser; the picture itself is read from it on demand.
    /// </summary>
    public static void Attach(Image image)
    {
        Detach();
        _image = image;
        DependencyPropertyDescriptor.FromProperty(Image.SourceProperty, typeof(Image))
            .AddValueChanged(image, OnImageSourceChanged);
        Announce();
    }

    private static void Detach()
    {
        if (_image is null)
            return;

        DependencyPropertyDescriptor.FromProperty(Image.SourceProperty, typeof(Image))
            .RemoveValueChanged(_image, OnImageSourceChanged);
        _image = null;
        Announce();
    }

    // The notification arrives with every frame of the page (the picture is
    // modified in place), so it is only passed on when the picture is another one.
    private static void OnImageSourceChanged(object? sender, EventArgs e) => Announce();

    private static void Announce()
    {
        var current = Source;
        if (ReferenceEquals(current, _announced))
            return;

        _announced = current;
        SourceChanged?.Invoke(null, EventArgs.Empty);
    }

    // ── El puntero del mouse ─────────────────────────────────────

    private static bool _showPointer = true;
    private static Point? _pointer;

    /// <summary>The «Mostrar el puntero» box: draw the arrow on the screen.</summary>
    public static bool ShowPointer
    {
        get => _showPointer;
        set
        {
            if (_showPointer == value)
                return;

            _showPointer = value;
            PointerChanged?.Invoke(null, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Where the mouse is over the page, as a fraction of its width and height
    /// (0 to 1), or null when it is not over it. A fraction, so the arrow
    /// lands in the same place of the page on any screen and any scale.
    /// </summary>
    public static Point? Pointer => _showPointer ? _pointer : null;

    public static void SetPointer(Point? fraction)
    {
        if (fraction is { } p)
            fraction = new Point(Math.Clamp(p.X, 0, 1), Math.Clamp(p.Y, 0, 1));

        if (_pointer == fraction)
            return;

        _pointer = fraction;
        if (_showPointer)
            PointerChanged?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>Raised when the arrow moves, appears, goes, or the box is changed.</summary>
    public static event EventHandler? PointerChanged;

    // ── Una foto del momento ─────────────────────────────────────

    /// <summary>
    /// A still copy of the page right now, <paramref name="width"/> wide. The
    /// operator's panels draw this instead of the moving picture on a slow
    /// computer: a small copy every couple of seconds costs a fraction of
    /// drawing the whole page every frame in three places.
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
