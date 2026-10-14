namespace EcclesiaCast.Core.Presentation;

/// <summary>A rectangle over the 1920×1080 canvas.</summary>
public readonly record struct BoxRect(double X, double Y, double W, double H)
{
    public double Right => X + W;

    public double Bottom => Y + H;
}

/// <summary>
/// Keeps the words of a slide clear of its extra text boxes. The area for the
/// lyrics knew about the caption but not about the boxes the operator drew, so
/// a long verse was painted right over a box sitting under it.
///
/// Only a box that runs across at least half of the area's width cuts into it
/// (a title strip, a translation line). A small box in a corner is meant to sit
/// beside the words and takes nothing from them.
/// </summary>
public static class TextAreaCarver
{
    /// <summary>Space left between the words and the box that cuts into their area.</summary>
    public const double Gap = 12;

    /// <summary>The words are never squeezed into less than this height.</summary>
    public const double MinHeight = 60;

    /// <summary>Share of the area's width a box must cover to cut into it.</summary>
    public const double WidthShare = 0.5;

    public static BoxRect Carve(BoxRect area, IEnumerable<BoxRect> boxes)
    {
        // Top to bottom, so each cut is worked out against what the previous
        // ones left, whatever order the boxes were drawn in.
        foreach (var box in boxes.OrderBy(b => b.Y).ThenBy(b => b.X))
        {
            if (!Crosses(area, box))
                continue;

            var belowCentre = box.Y + box.H / 2 >= area.Y + area.H / 2;
            if (belowCentre)
            {
                area = area with { H = Math.Max(MinHeight, box.Y - Gap - area.Y) };
            }
            else
            {
                // Keep the strip the words are left with at least MinHeight tall.
                var top = Math.Min(box.Bottom + Gap, area.Bottom - MinHeight);
                area = area with { Y = top, H = area.Bottom - top };
            }
        }

        return area;
    }

    /// <summary>True when the box overlaps the area and covers at least half of its width.</summary>
    private static bool Crosses(BoxRect area, BoxRect box)
    {
        var overlapW = Math.Min(area.Right, box.Right) - Math.Max(area.X, box.X);
        var overlapH = Math.Min(area.Bottom, box.Bottom) - Math.Max(area.Y, box.Y);
        return overlapW > 0 && overlapH > 0 && overlapW >= area.W * WidthShare;
    }
}
