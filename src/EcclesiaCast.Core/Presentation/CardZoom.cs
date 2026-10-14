using System.Globalization;

namespace EcclesiaCast.Core.Presentation;

/// <summary>
/// The size of the cards in the slide grid and in the media bar, as the
/// operator sets it with the slider (or Ctrl + mouse wheel). A scale of 1.0 is
/// the size the cards always had. The value is kept as text in the database,
/// always with a dot as the decimal point: a Spanish-speaking computer writes
/// 1,5 and the stored value must read back the same on any of them.
/// </summary>
public static class CardZoom
{
    public const double SlidesMin = 0.6;
    public const double SlidesMax = 1.8;
    public const double MediaMin = 0.7;
    public const double MediaMax = 2.5;
    public const double Step = 0.1;
    public const double Default = 1.0;

    /// <summary>
    /// The stored value, clamped to the range. Anything missing or unreadable
    /// is the default size.
    /// </summary>
    public static double Parse(string? saved, double min, double max)
    {
        if (!double.TryParse(saved, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            || !double.IsFinite(value))
            return Default;

        return Math.Clamp(value, min, max);
    }

    public static string Format(double value) => value.ToString("0.0", CultureInfo.InvariantCulture);

    /// <summary>
    /// One click of the slider or the wheel: a step up (direction above zero)
    /// or down, rounded to a tenth so the slider never drifts to 1.1000000002.
    /// </summary>
    public static double Nudge(double current, int direction, double min, double max)
    {
        var next = Math.Round(current, 1) + Math.Sign(direction) * Step;
        return Math.Clamp(Math.Round(next, 1), min, max);
    }
}
