namespace EcclesiaCast.Core.Ui;

/// <summary>HSV ↔ RGB, the way people think about color: hue, intensity (saturation) and lightness (value).</summary>
public static class ColorMath
{
    /// <summary>Hue 0–360, saturation and value 0–1 to RGB 0–255.</summary>
    public static (byte R, byte G, byte B) HsvToRgb(double hue, double saturation, double value)
    {
        hue = ((hue % 360) + 360) % 360;
        saturation = Math.Clamp(saturation, 0, 1);
        value = Math.Clamp(value, 0, 1);

        var c = value * saturation;
        var x = c * (1 - Math.Abs(hue / 60 % 2 - 1));
        var m = value - c;

        var (r, g, b) = (int)(hue / 60) switch
        {
            0 => (c, x, 0d),
            1 => (x, c, 0d),
            2 => (0d, c, x),
            3 => (0d, x, c),
            4 => (x, 0d, c),
            _ => (c, 0d, x),
        };

        return (ToByte(r + m), ToByte(g + m), ToByte(b + m));
    }

    /// <summary>RGB 0–255 to hue 0–360 (0 for greys), saturation and value 0–1.</summary>
    public static (double Hue, double Saturation, double Value) RgbToHsv(byte r, byte g, byte b)
    {
        double rf = r / 255d, gf = g / 255d, bf = b / 255d;
        var max = Math.Max(rf, Math.Max(gf, bf));
        var min = Math.Min(rf, Math.Min(gf, bf));
        var delta = max - min;

        double hue;
        if (delta == 0)
            hue = 0;
        else if (max == rf)
            hue = 60 * (((gf - bf) / delta % 6 + 6) % 6);
        else if (max == gf)
            hue = 60 * ((bf - rf) / delta + 2);
        else
            hue = 60 * ((rf - gf) / delta + 4);

        return (hue, max == 0 ? 0 : delta / max, max);
    }

    private static byte ToByte(double v) => (byte)Math.Round(Math.Clamp(v, 0, 1) * 255);
}
