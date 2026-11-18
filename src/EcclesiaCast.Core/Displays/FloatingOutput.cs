using System.Globalization;

namespace EcclesiaCast.Core.Displays;

/// <summary>The shape of the picture in the floating output window.</summary>
public enum OutputShape
{
    /// <summary>16:9, a widescreen TV or projector, and what a stream normally is.</summary>
    Wide,

    /// <summary>4:3, the square projectors many churches still have.</summary>
    Classic,
}

/// <summary>A size of the picture, in pixels, without the window's border.</summary>
public readonly record struct PictureSize(int Width, int Height)
{
    public override string ToString() => $"{Width}×{Height}";
}

/// <summary>A rectangle in screen coordinates: left, top, right, bottom.</summary>
public readonly record struct ScreenRect(int Left, int Top, int Right, int Bottom)
{
    public int Width => Right - Left;
    public int Height => Bottom - Top;
}

/// <summary>
/// «⧉ En una ventana»: the output in an ordinary window that can be moved,
/// resized, minimized and shared (Discord, a call) — also handy on a notebook
/// with a single screen or to try things without a projector. It travels as a
/// <see cref="DisplayInfo"/> like any screen, the way «only the stream» does,
/// and <see cref="IsFloating"/> tells it apart.
/// </summary>
public static class FloatingOutput
{
    /// <summary>Not a Windows device name, so it can never match a real screen.</summary>
    public const string DeviceName = "ecclesiacast:ventana";

    public const string Label = "⧉ En una ventana";

    public static bool IsFloating(DisplayInfo? display) => display?.DeviceName == DeviceName;

    /// <summary>
    /// With a single screen the window is the first entry of the list: the full
    /// screen would cover the program itself.
    /// </summary>
    public static bool GoesFirst(IReadOnlyList<DisplayInfo> screens) => screens.Count <= 1;

    /// <summary>
    /// Which output to start on: the one remembered if it is still there; with a
    /// single screen and nothing remembered, the window; otherwise a screen that
    /// is not the main one (the projector), or the main one.
    /// </summary>
    /// <remarks>
    /// Someone who already went live on their only screen keeps it: that may be
    /// a projector set to «duplicate», where the full screen is what they want.
    /// </remarks>
    public static string StartingChoice(IReadOnlyList<DisplayInfo> screens, string? remembered)
    {
        if (remembered == DeviceName || screens.Any(s => s.DeviceName == remembered))
            return remembered!;
        if (GoesFirst(screens))
            return DeviceName;
        return (screens.FirstOrDefault(s => !s.IsPrimary) ?? screens[0]).DeviceName;
    }

    /// <summary>The entry for the list of screens, sized as a full-HD picture of the shape.</summary>
    public static DisplayInfo For(OutputShape shape)
    {
        var size = FullSize(shape);
        return new DisplayInfo(DeviceName, 0, 0, size.Width, size.Height, IsPrimary: false);
    }

    public static double RatioOf(OutputShape shape) => shape == OutputShape.Classic ? 4d / 3d : 16d / 9d;

    /// <summary>The three sizes offered for each shape: small, medium and full height.</summary>
    public static IReadOnlyList<PictureSize> QuickSizes(OutputShape shape) => shape == OutputShape.Classic
        ? [new(640, 480), new(1024, 768), new(1440, 1080)]
        : [new(640, 360), new(1280, 720), new(1920, 1080)];

    private static PictureSize FullSize(OutputShape shape) => QuickSizes(shape)[^1];

    /// <summary>The picture of the given width with the shape's proportion.</summary>
    public static PictureSize WithWidth(OutputShape shape, int width)
    {
        width = Math.Max(MinWidth, width);
        return new PictureSize(width, (int)Math.Round(width / RatioOf(shape)));
    }

    /// <summary>Smaller than this and the words cannot be read; the window does not go below it.</summary>
    public const int MinWidth = 240;

    /// <summary>
    /// The largest picture of that proportion that fits in the window's inside,
    /// centred: what does not fit is left as black bands, never stretched or cut.
    /// </summary>
    public static (double X, double Y, double Width, double Height) Fit(double width, double height, double ratio)
    {
        if (width <= 0 || height <= 0)
            return (0, 0, Math.Max(0, width), Math.Max(0, height));
        if (!(ratio > 0) || double.IsInfinity(ratio))
            ratio = AspectFit.DefaultRatio;

        var w = Math.Min(width, height * ratio);
        var h = w / ratio;
        return ((width - w) / 2, (height - h) / 2, w, h);
    }

    // ── Al arrastrar un borde ────────────────────────────────────

    /// <summary>The edge or corner being dragged, numbered as Windows numbers them (WMSZ_*).</summary>
    public enum Edge
    {
        Left = 1,
        Right = 2,
        Top = 3,
        TopLeft = 4,
        TopRight = 5,
        Bottom = 6,
        BottomLeft = 7,
        BottomRight = 8,
    }

    /// <summary>
    /// The window rectangle Windows proposes while an edge is dragged, changed so
    /// the inside keeps the shape. <paramref name="frameWidth"/> and
    /// <paramref name="frameHeight"/> are what the border and title bar take.
    /// Sides drive the other measure; corners go by the width and move the
    /// edge on the corner's side, so the opposite corner stays put.
    /// </summary>
    public static ScreenRect KeepShape(Edge edge, ScreenRect proposed, int frameWidth, int frameHeight, double ratio)
    {
        var inside = (W: Math.Max(1, proposed.Width - frameWidth), H: Math.Max(1, proposed.Height - frameHeight));

        if (edge is Edge.Top or Edge.Bottom)
        {
            var width = (int)Math.Round(inside.H * ratio) + frameWidth;
            return proposed with { Right = proposed.Left + width };
        }

        var height = (int)Math.Round(inside.W / ratio) + frameHeight;
        return edge is Edge.TopLeft or Edge.TopRight
            ? proposed with { Top = proposed.Bottom - height }
            : proposed with { Bottom = proposed.Top + height };
    }

    // ── Que la ventana se vea ────────────────────────────────────

    /// <summary>How much of the title bar must be on a screen to count as reachable.</summary>
    private const int ReachableWidth = 100;
    private const int ReachableHeight = 30;

    /// <summary>
    /// Where to put the window: where it was, unless that is off every screen
    /// (a monitor that was unplugged, a resolution that changed) — then centred
    /// on the main screen. Everything in the same units.
    /// </summary>
    public static (int Left, int Top) Rescue(ScreenRect window, IReadOnlyList<ScreenRect> screens, ScreenRect primary)
    {
        // The top strip is the title bar: the part needed to grab and move it.
        var titleBar = window with { Bottom = window.Top + ReachableHeight };
        foreach (var screen in screens)
        {
            var overlapWidth = Math.Min(titleBar.Right, screen.Right) - Math.Max(titleBar.Left, screen.Left);
            var overlapHeight = Math.Min(titleBar.Bottom, screen.Bottom) - Math.Max(titleBar.Top, screen.Top);
            if (overlapWidth >= Math.Min(ReachableWidth, window.Width) && overlapHeight >= Math.Min(ReachableHeight, window.Height))
                return (window.Left, window.Top);
        }

        return (primary.Left + (primary.Width - window.Width) / 2,
                primary.Top + Math.Max(0, (primary.Height - window.Height) / 2));
    }

    // ── Lo que se recuerda ───────────────────────────────────────

    public const string SettingKey = "output.window";

    /// <summary>The window as it was left. <see cref="Left"/> is NaN when it never opened.</summary>
    public sealed record Layout(OutputShape Shape, int PictureWidth, double Left, double Top, bool AlwaysOnTop)
    {
        public static Layout Default { get; } = new(OutputShape.Wide, 1280, double.NaN, double.NaN, false);

        public PictureSize Picture => WithWidth(Shape, PictureWidth);

        public string ToSetting() => string.Join(';',
            Shape == OutputShape.Classic ? "4:3" : "16:9",
            PictureWidth.ToString(CultureInfo.InvariantCulture),
            Left.ToString("0.#", CultureInfo.InvariantCulture),
            Top.ToString("0.#", CultureInfo.InvariantCulture),
            AlwaysOnTop ? "1" : "0");

        public static Layout Parse(string? text)
        {
            var parts = (text ?? "").Split(';');
            if (parts.Length != 5)
                return Default;

            var shape = parts[0] == "4:3" ? OutputShape.Classic : OutputShape.Wide;
            var width = int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var w) && w > 0
                ? Math.Clamp(w, MinWidth, 7680)
                : Default.PictureWidth;
            var left = double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var l) ? l : double.NaN;
            var top = double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var t) ? t : double.NaN;
            return new Layout(shape, width, left, top, parts[4] == "1");
        }
    }
}
