namespace EcclesiaCast.Core.Themes;

/// <summary>A rectangle in the coordinates of the text (px over the 1920×1080 canvas).</summary>
public readonly record struct BandBox(double X, double Y, double Width, double Height)
{
    public double Right => X + Width;
    public double Bottom => Y + Height;
}

/// <summary>
/// Where the bands of <see cref="LineBand"/> go: the boxes of the letters are
/// gathered into lines, and each line gets a band a little wider than its
/// words (or from edge to edge), a little shorter than the line so two lines
/// never touch.
/// </summary>
public static class LineBandLayout
{
    /// <summary>Room at each side of a band, as a part of the font size.</summary>
    public const double SidePadding = 0.32;

    /// <summary>What is taken off the top and the bottom of each line, as a part of its height.</summary>
    public const double Inset = 0.07;

    /// <summary>
    /// The lines the letters make: boxes whose middles are on the same height
    /// belong to the same line, in the order they come.
    /// </summary>
    public static IReadOnlyList<BandBox> Lines(IEnumerable<BandBox> letters)
    {
        var lines = new List<BandBox>();
        foreach (var letter in letters)
        {
            if (letter.Width < 0 || letter.Height <= 0 || double.IsNaN(letter.X) || double.IsNaN(letter.Y))
                continue;

            var middle = letter.Y + letter.Height / 2;
            var index = lines.FindIndex(l => middle > l.Y && middle < l.Bottom);
            if (index < 0)
            {
                lines.Add(letter);
                continue;
            }

            var line = lines[index];
            var x = Math.Min(line.X, letter.X);
            var y = Math.Min(line.Y, letter.Y);
            lines[index] = new BandBox(x, y, Math.Max(line.Right, letter.Right) - x, Math.Max(line.Bottom, letter.Bottom) - y);
        }

        return lines;
    }

    /// <summary>
    /// The bands for those lines. <paramref name="screenLeft"/> and
    /// <paramref name="screenRight"/> are where the edges of the screen fall in
    /// the same coordinates, for <see cref="LineBand.FullWidth"/>.
    /// </summary>
    public static IReadOnlyList<BandBox> Bands(
        IReadOnlyList<BandBox> lines, LineBand kind, double fontSize, double screenLeft, double screenRight)
    {
        if (kind == LineBand.None || lines.Count == 0)
            return [];

        var side = Math.Max(0, fontSize) * SidePadding;
        var bands = new List<BandBox>(lines.Count);
        foreach (var line in lines)
        {
            var inset = line.Height * Inset;
            var top = line.Y + inset;
            var height = Math.Max(1, line.Height - 2 * inset);
            if (kind == LineBand.FullWidth)
                bands.Add(new BandBox(screenLeft, top, Math.Max(0, screenRight - screenLeft), height));
            else
                bands.Add(new BandBox(line.X - side, top, line.Width + 2 * side, height));
        }

        return bands;
    }
}
