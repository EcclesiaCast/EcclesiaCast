using System.Globalization;
using System.Windows;
using System.Windows.Media;
using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Core.Themes;

namespace EcclesiaCast.App.Controls;

/// <summary>Where the main text goes and at what size, for one slide.</summary>
public readonly record struct SlideTextLayout(Thickness Margin, double Width, double Height, double FontSize, bool Readable);

/// <summary>
/// How <see cref="SlideView"/> fits the words into their box. It lives apart
/// from the control so the slide builder can ask the same question before a
/// slide exists: does this verse fit at a size people can read, or does it
/// need to be split over two slides?
/// </summary>
public static class SlideTextFit
{
    public const double CanvasWidth = 1920;
    public const double CanvasHeight = 1080;

    /// <summary>Gap between the two Bible versions.</summary>
    public const double SecondarySpacing = 50;

    /// <summary>Room kept for the caption when the text area comes from the margins.</summary>
    private const double CaptionBandFactor = 1.7;

    /// <summary>Space left between the caption and a text box that would touch it.</summary>
    private const double CaptionGap = 12;

    /// <summary>
    /// Below the theme's minimum the text still shrinks rather than run off
    /// the box: a smaller verse can be read, a cut one can't. This is only
    /// the point where shrinking stops making sense at all.
    /// </summary>
    private const double SmallestSize = 8;

    /// <summary>
    /// Where the main text lives and the largest size at which all of it
    /// (and the second version, if any) fits there. <see cref="SlideTextLayout.Readable"/>
    /// is false when that took going under the theme's minimum.
    /// </summary>
    public static SlideTextLayout Fit(SlideContent? slide, string? main, string? secondary, double pixelsPerDip)
    {
        var (style, margin, width, height) = Prepare(slide, pixelsPerDip);
        var maxSize = style.MaxFontSize;
        if (string.IsNullOrEmpty(main))
            return new SlideTextLayout(margin, width, height, maxSize, true);

        var minSize = MinimumSize(style);

        bool FitsAt(double size) => Fits(style, main, secondary, size, width, height, pixelsPerDip);

        // Sizes go down from the preferred one in steps of 2; a bigger size
        // never fits where a smaller one doesn't, so halving the range finds
        // the largest that fits in a handful of measures instead of dozens
        // (a whole chapter is measured each time one is opened).
        var steps = (int)Math.Floor((maxSize - minSize) / 2);
        if (FirstStepThatFits(steps, k => FitsAt(maxSize - 2 * k)) is int step)
            return new SlideTextLayout(margin, width, height, maxSize - 2 * step, true);

        var below = Math.Floor(minSize) - 1;
        var smaller = (int)(below - SmallestSize);
        if (smaller > 0 && FirstStepThatFits(smaller, k => FitsAt(below - k)) is int k)
            return new SlideTextLayout(margin, width, height, below - k, false);

        return new SlideTextLayout(margin, width, height, SmallestSize, false);
    }

    /// <summary>
    /// The smallest k in 0..last for which <paramref name="fits"/> holds,
    /// assuming once it holds it keeps holding; null when it never does.
    /// k counts steps down from the biggest size, so the first k is the biggest size.
    /// </summary>
    private static int? FirstStepThatFits(int last, Func<int, bool> fits)
    {
        if (last < 0 || !fits(last))
            return null;

        int low = 0, high = last;
        while (low < high)
        {
            var middle = (low + high) / 2;
            if (fits(middle))
                high = middle;
            else
                low = middle + 1;
        }
        return low;
    }

    /// <summary>
    /// True when the text fits its box at the theme's minimum size or
    /// bigger. One measure, against the handful of finding the exact size:
    /// the slide builder asks this of every verse in a chapter.
    /// </summary>
    public static bool IsReadable(SlideContent slide, string? main, string? secondary)
    {
        var (style, _, width, height) = Prepare(slide, 1.0);
        return string.IsNullOrEmpty(main) || style.FitToWidth
            || Fits(style, main, secondary, MinimumSize(style), width, height, 1.0);
    }

    private static (Style Style, Thickness Margin, double Width, double Height) Prepare(
        SlideContent? slide, double pixelsPerDip)
    {
        var style = new Style(slide);
        var hasCaption = style.Theme.ShowCaption && !string.IsNullOrEmpty(slide?.Caption);
        var (margin, width, height) = TextArea(style, hasCaption ? slide!.Caption : null, pixelsPerDip);
        return (style, margin, width, height);
    }

    /// <summary>
    /// The theme's minimum. Fit-to-width can shrink well below it, since its
    /// whole point is to keep long lines unbroken.
    /// </summary>
    private static double MinimumSize(Style style) =>
        style.FitToWidth ? SmallestSize : Math.Min(style.Theme.MinFontSize, style.MaxFontSize);

    /// <summary>
    /// Splits a text that can't be read in one slide into as few slides as
    /// make each one readable, cutting at the end of a sentence when possible.
    /// The second version, if any, is split in the same number of parts so
    /// the two keep walking together.
    /// </summary>
    public static IReadOnlyList<(string Main, string? Secondary)> SplitToFit(
        SlideContent slide, Func<string, string>? label = null, Func<string, string>? secondaryLabel = null,
        int maxParts = 6)
    {
        label ??= text => text;
        secondaryLabel ??= text => text;

        List<(string, string?)> parts = [];
        for (var count = 1; count <= maxParts; count++)
        {
            var mains = SplitEvenly(slide.MainText, count);
            var seconds = slide.SecondaryText is { } s ? SplitEvenly(s, count) : null;
            if (mains.Count == 0)
                mains = [slide.MainText];
            if (seconds is { Count: 0 })
                seconds = null;

            // A short text can come back in fewer pieces than asked for: the
            // leftovers of the longer one join its last piece, so no words
            // are lost and the two stay side by side.
            if (seconds is not null && seconds.Count > mains.Count)
                seconds = [.. seconds.Take(mains.Count - 1), string.Join(" ", seconds.Skip(mains.Count - 1))];
            else if (seconds is not null && mains.Count > seconds.Count)
                mains = [.. mains.Take(seconds.Count - 1), string.Join(" ", mains.Skip(seconds.Count - 1))];

            parts = mains
                .Select((text, i) => (label(text), seconds is null ? null : secondaryLabel(seconds[i])))
                .ToList();

            var readable = parts.All(p => IsReadable(slide, ApplyCase(p.Item1, slide), ApplyCase(p.Item2, slide)));
            if (readable)
                break;
        }

        return parts;
    }

    /// <summary>
    /// Cuts a text in <paramref name="count"/> pieces of about the same length,
    /// preferring the end of a sentence, then a comma or semicolon, then any
    /// space near each cut.
    /// </summary>
    public static IReadOnlyList<string> SplitEvenly(string text, int count)
    {
        if (count <= 1 || text.Length < count * 2)
            return [text];

        var pieces = new List<string>();
        var start = 0;
        for (var k = 1; k < count; k++)
        {
            var target = text.Length * k / count;
            var window = text.Length / (count * 3);
            var cut = BestCut(text, target, window, start);
            if (cut <= start)
                continue;
            pieces.Add(text[start..cut].Trim());
            start = cut;
        }
        pieces.Add(text[start..].Trim());
        return pieces.Where(p => p.Length > 0).ToList();
    }

    private static int BestCut(string text, int target, int window, int notBefore)
    {
        int? Nearest(Func<char, bool> endsHere)
        {
            int? best = null;
            for (var i = Math.Max(notBefore + 1, target - window); i < Math.Min(text.Length - 1, target + window); i++)
            {
                // A cut sits right after the punctuation and its space.
                if (text[i] == ' ' && endsHere(text[i - 1])
                    && (best is null || Math.Abs(i - target) < Math.Abs(best.Value - target)))
                    best = i + 1;
            }
            return best;
        }

        return Nearest(c => c is '.' or '?' or '!' or '»' or '"' or '”')
            ?? Nearest(c => c is ';' or ':' or ',')
            ?? Nearest(_ => true)
            ?? target;
    }

    /// <summary>The text as the theme asks to show it (ALL CAPS, Title…).</summary>
    public static string? ApplyCase(string? text, SlideContent? slide) =>
        ApplyCase(text, slide?.Override?.Case ?? (slide?.Theme ?? SlideTheme.Fallback).TextCase);

    public static string? ApplyCase(string? text, TextCase textCase)
    {
        if (text is null)
            return null;

        var culture = CultureInfo.CurrentCulture;
        return textCase switch
        {
            TextCase.Upper => text.ToUpper(culture),
            TextCase.Title => culture.TextInfo.ToTitleCase(text.ToLower(culture)),
            TextCase.Sentence => ToSentenceCase(text, culture),
            TextCase.Lower => text.ToLower(culture),
            _ => text,
        };
    }

    private static string ToSentenceCase(string text, CultureInfo culture)
    {
        var chars = text.ToCharArray();
        var startOfSentence = true;
        for (var i = 0; i < chars.Length; i++)
        {
            if (startOfSentence && char.IsLetter(chars[i]))
            {
                chars[i] = char.ToUpper(chars[i], culture);
                startOfSentence = false;
            }
            else if (chars[i] is '.' or '!' or '?' or '\n')
            {
                startOfSentence = true;
            }
        }
        return new string(chars);
    }

    /// <summary>
    /// Where the main text lives: the slide's own box if it has one, then the
    /// theme's box, else the theme margins. Either way it keeps clear of the
    /// caption, so the reference and the verse never sit on each other.
    /// </summary>
    private static (Thickness Margin, double Width, double Height) TextArea(Style style, string? caption, double pixelsPerDip)
    {
        var theme = style.Theme;
        var over = style.Override;

        double? bx = over?.BoxX ?? theme.BoxX;
        double? by = over?.BoxY ?? theme.BoxY;
        double? bw = over?.BoxWidth ?? theme.BoxWidth;
        double? bh = over?.BoxHeight ?? theme.BoxHeight;

        if (bx is not null && by is not null && bw is not null && bh is not null)
        {
            var x = Math.Clamp(bx.Value, 0, CanvasWidth - 100);
            var y = Math.Clamp(by.Value, 0, CanvasHeight - 60);
            var w = Math.Clamp(bw.Value, 100, CanvasWidth - x);
            var h = Math.Clamp(bh.Value, 60, CanvasHeight - y);

            if (caption is not null && CaptionBounds(theme, caption, pixelsPerDip) is var c
                && c.Left < x + w && c.Right > x && c.Top < y + h && c.Bottom > y)
            {
                // The box was drawn over the caption: give the caption its
                // strip and keep the text in what is left.
                if (IsCaptionOnTop(theme.CaptionPosition))
                {
                    var top = Math.Min(c.Bottom + CaptionGap, y + h - 60);
                    h -= top - y;
                    y = top;
                }
                else
                {
                    h = Math.Max(60, c.Top - CaptionGap - y);
                }
            }

            (x, y, w, h) = AvoidExtraBoxes(over, x, y, w, h);
            return (new Thickness(x, y, CanvasWidth - x - w, CanvasHeight - y - h), w, h);
        }

        var marginTop = theme.MarginVertical;
        var marginBottom = theme.MarginVertical;
        if (caption is not null)
        {
            var band = theme.CaptionFontSize * CaptionBandFactor;
            if (IsCaptionOnTop(theme.CaptionPosition))
                marginTop += band;
            else
                marginBottom += band;
        }

        var width = Math.Max(200, CanvasWidth - 2 * theme.MarginHorizontal);
        var height = Math.Max(150, CanvasHeight - marginTop - marginBottom);

        var (ax, ay, aw, ah) = AvoidExtraBoxes(over, theme.MarginHorizontal, marginTop, width, height);
        return (new Thickness(ax, ay, CanvasWidth - ax - aw, CanvasHeight - ay - ah), aw, ah);
    }

    /// <summary>
    /// Takes the area the slide's extra text boxes occupy out of the words'
    /// area (see <see cref="TextAreaCarver"/>): without it a long verse was
    /// drawn right over a box sitting under it. The boxes are measured at the
    /// size <see cref="SlideView"/> draws them.
    /// </summary>
    private static (double X, double Y, double W, double H) AvoidExtraBoxes(
        SlideOverride? over, double x, double y, double w, double h)
    {
        if (over is null || over.TextBoxes.Count == 0)
            return (x, y, w, h);

        var carved = TextAreaCarver.Carve(
            new BoxRect(x, y, w, h),
            over.TextBoxes.Select(b => new BoxRect(b.X, b.Y, Math.Max(20, b.Width), Math.Max(20, b.Height))));
        return (carved.X, carved.Y, carved.W, carved.H);
    }

    /// <summary>Where <see cref="SlideView"/> draws the caption, on the 1920×1080 canvas.</summary>
    private static Rect CaptionBounds(SlideTheme theme, string caption, double pixelsPerDip)
    {
        var text = new FormattedText(
            caption, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface(theme.CaptionFontFamily ?? theme.FontFamily),
            theme.CaptionFontSize, Brushes.White, pixelsPerDip);

        var inset = theme.MarginVertical * 0.5;
        var width = Math.Min(text.WidthIncludingTrailingWhitespace, CanvasWidth - 2 * theme.MarginHorizontal);
        var left = theme.CaptionPosition switch
        {
            CaptionPosition.TopLeft or CaptionPosition.BottomLeft => theme.MarginHorizontal,
            CaptionPosition.TopCenter or CaptionPosition.BottomCenter => (CanvasWidth - width) / 2,
            _ => CanvasWidth - theme.MarginHorizontal - width,
        };
        var top = IsCaptionOnTop(theme.CaptionPosition) ? inset : CanvasHeight - inset - text.Height;
        return new Rect(left, top, width, text.Height);
    }

    public static bool IsCaptionOnTop(CaptionPosition position) =>
        position is CaptionPosition.TopLeft or CaptionPosition.TopCenter or CaptionPosition.TopRight;

    private static bool Fits(Style style, string main, string? secondary, double size,
        double width, double height, double pixelsPerDip)
    {
        var mainHeight = MeasureHeight(style, main, size, width, pixelsPerDip);
        if (mainHeight > height)
            return false;
        if (style.FitToWidth && WidestLine(style, main, size, pixelsPerDip) > width)
            return false;

        if (string.IsNullOrEmpty(secondary))
            return true;

        var secondarySize = Math.Max(20, size * style.SecondaryScale);
        if (MeasureHeight(style, secondary, secondarySize, width, pixelsPerDip) + SecondarySpacing > height - mainHeight)
            return false;
        return !style.FitToWidth || WidestLine(style, secondary, secondarySize, pixelsPerDip) <= width;
    }

    /// <summary>Width of the longest line (split on hard breaks) at a given size, unwrapped.</summary>
    private static double WidestLine(Style style, string text, double fontSize, double pixelsPerDip)
    {
        var widest = 0d;
        foreach (var line in text.Split('\n'))
        {
            var formatted = Format(style, line.Length == 0 ? " " : line, fontSize, double.PositiveInfinity, pixelsPerDip);
            widest = Math.Max(widest, formatted.WidthIncludingTrailingWhitespace);
        }
        return widest;
    }

    private static double MeasureHeight(Style style, string text, double fontSize, double maxWidth, double pixelsPerDip) =>
        Format(style, text, fontSize, maxWidth, pixelsPerDip).Height;

    private static FormattedText Format(Style style, string text, double fontSize, double maxWidth, double pixelsPerDip)
    {
        var formatted = new FormattedText(
            text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            style.Typeface, fontSize, Brushes.White, pixelsPerDip);

        if (!double.IsPositiveInfinity(maxWidth))
            formatted.MaxTextWidth = maxWidth;

        // Same line height the TextBlock uses, or the measure and the screen
        // disagree as soon as a slide has its own line spacing.
        if (style.LineSpacing is double spacing && spacing > 0)
            formatted.LineHeight = fontSize * spacing;

        return formatted;
    }

    /// <summary>The theme with the slide's own changes applied.</summary>
    private sealed class Style(SlideContent? slide)
    {
        public SlideTheme Theme { get; } = slide?.Theme ?? SlideTheme.Fallback;
        public SlideOverride? Override { get; } = slide?.Override;

        public double MaxFontSize => Override?.FontSize ?? Theme.MaxFontSize;
        public bool FitToWidth => Override?.FitToWidth ?? Theme.FitToWidth;
        public double? LineSpacing => Override?.LineSpacing;
        public double SecondaryScale => Theme.SecondaryMatchesPrimary ? 1.0 : Theme.SecondaryScale;

        private Typeface? _typeface;

        public Typeface Typeface => _typeface ??= new(
            new FontFamily(Override?.FontFamily ?? Theme.FontFamily),
            (Override?.Italic ?? Theme.Italic) ? FontStyles.Italic : FontStyles.Normal,
            (Override?.Bold ?? Theme.Bold) ? FontWeights.SemiBold : FontWeights.Normal,
            FontStretches.Normal);
    }
}
