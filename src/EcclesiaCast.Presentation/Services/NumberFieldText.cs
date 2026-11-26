using System.Globalization;

namespace EcclesiaCast.Presentation.Services;

/// <summary>
/// The numbers of the fields with − / + : how a value is held in its range,
/// how it is written and how what the operator types is read. Both
/// interfaces' NumericField use it.
/// </summary>
public static class NumberFieldText
{
    /// <summary>Rounded to the decimals shown and held between the two ends.</summary>
    public static double Clamp(double value, double minimum, double maximum, int decimals) =>
        Math.Clamp(Math.Round(value, Places(decimals)), minimum, maximum);

    public static string Format(double value, int decimals) =>
        value.ToString("F" + Places(decimals), CultureInfo.CurrentCulture);

    /// <summary>Accepts both decimal separators, so "1.15" and "1,15" both work.</summary>
    public static bool TryParse(string text, out double value)
    {
        text = text.Trim();
        return double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value)
            || double.TryParse(text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static int Places(int decimals) => Math.Clamp(decimals, 0, 6);
}
