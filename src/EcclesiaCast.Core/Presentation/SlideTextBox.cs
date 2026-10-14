using EcclesiaCast.Core.Themes;

namespace EcclesiaCast.Core.Presentation;

/// <summary>
/// An extra block of text sitting on a slide, beside the words themselves:
/// the name of the series in a corner, a reference under the lyric, a line in
/// another language next to it.
///
/// Positions are in pixels over the same virtual 1920×1080 canvas the rest of
/// the slide uses, so a design looks identical in the operator's preview and
/// on the projector.
/// </summary>
public sealed record SlideTextBox(
    string Text,
    double X,
    double Y,
    double Width,
    double Height,
    double FontSize = 48,
    string? FontFamily = null,
    string Color = "#FFFFFF",
    bool Bold = false,
    bool Italic = false,
    HAlign AlignH = HAlign.Center,
    VAlign AlignV = VAlign.Center)
{
    /// <summary>A new box low on the canvas, out of the way of centred lyrics.</summary>
    public static SlideTextBox Fresh(string text = "Texto") =>
        new(text, X: 660, Y: 820, Width: 600, Height: 140);

    private const double CanvasWidth = 1920;
    private const double CanvasHeight = 1080;

    /// <summary>Where a new box is tried, in order: low centre first, the middle last.</summary>
    private static readonly (double X, double Y)[] Spots =
    [
        (660, 820), (660, 120), (60, 820), (1260, 820), (60, 120), (1260, 120), (660, 470),
    ];

    /// <summary>
    /// A new box that does not land on another one. <see cref="Fresh"/> always
    /// answers the same place, so the second box added to a slide sat exactly
    /// on the first and the two could not be told apart with the mouse. The
    /// spots are tried in a fixed order; when all are taken the box is shifted
    /// down and to the right of the last spot, staying inside the canvas.
    /// </summary>
    public static SlideTextBox FreshAvoiding(IReadOnlyList<SlideTextBox> others, string text = "Texto")
    {
        var template = Fresh(text);

        foreach (var (x, y) in Spots)
        {
            var candidate = template with { X = x, Y = y };
            if (!others.Any(candidate.Overlaps))
                return candidate;
        }

        // Every spot is taken: cascade from the middle one, 40 px right and
        // down at a time, never leaving the canvas. Once it reaches the corner
        // there is no room left and the last position is the answer.
        const double step = 40;
        var last = Spots[^1];
        var cascade = template with { X = last.X, Y = last.Y };
        for (var i = 1; ; i++)
        {
            var next = template with
            {
                X = Math.Min(last.X + step * i, CanvasWidth - template.Width),
                Y = Math.Min(last.Y + step * i, CanvasHeight - template.Height),
            };

            if (next.X == cascade.X && next.Y == cascade.Y)
                return cascade;

            cascade = next;
            if (!others.Any(cascade.Overlaps))
                return cascade;
        }
    }

    /// <summary>True when the two boxes share any area (touching edges do not count).</summary>
    public bool Overlaps(SlideTextBox other) =>
        X < other.X + other.Width && other.X < X + Width
        && Y < other.Y + other.Height && other.Y < Y + Height;
}
