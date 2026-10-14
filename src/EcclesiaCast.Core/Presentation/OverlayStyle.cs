using System.Globalization;

namespace EcclesiaCast.Core.Presentation;

/// <summary>
/// The look of the lower-third announcement (the «aviso al pie»): its typeface
/// and size, which the operator chooses and the program remembers. Rules only;
/// the drawing is the slide view's.
/// </summary>
public static class OverlayStyle
{
    public const string FontSettingKey = "overlay.font";
    public const string SizeSettingKey = "overlay.size";

    public const string DefaultFont = "Segoe UI";
    public const double DefaultSize = 54;
    public const double MinSize = 20;
    public const double MaxSize = 160;

    /// <summary>The size held between <see cref="MinSize"/> and <see cref="MaxSize"/>; the default when it is not a number.</summary>
    public static double ClampSize(double size) =>
        double.IsNaN(size) || double.IsInfinity(size) ? DefaultSize : Math.Clamp(size, MinSize, MaxSize);

    /// <summary>Reads a saved size (a point for decimals, whatever the language of Windows).</summary>
    public static double ParseSize(string? saved) =>
        double.TryParse(saved, NumberStyles.Float, CultureInfo.InvariantCulture, out var size)
            ? ClampSize(size)
            : DefaultSize;

    public static string FormatSize(double size) =>
        ClampSize(size).ToString("0.##", CultureInfo.InvariantCulture);

    /// <summary>The typeface to draw with: the one chosen, or Segoe UI when none is.</summary>
    public static string FontOrDefault(string? font) =>
        string.IsNullOrWhiteSpace(font) ? DefaultFont : font.Trim();
}
