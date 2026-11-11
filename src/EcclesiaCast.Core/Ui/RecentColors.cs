namespace EcclesiaCast.Core.Ui;

/// <summary>
/// The colors picked lately, the last one first, with no repeats and a cap.
/// Stored in the settings as one text: the hex codes separated by commas.
/// </summary>
public static class RecentColors
{
    public const string SettingKey = "colors.recent";
    public const int Max = 12;

    /// <summary>Puts <paramref name="hex"/> first; an equal one already there moves up. Invalid codes are ignored.</summary>
    public static IReadOnlyList<string> Add(IReadOnlyList<string> current, string? hex)
    {
        var normalized = Normalize(hex);
        if (normalized is null)
            return current;

        return new[] { normalized }.Concat(current.Where(c => c != normalized)).Take(Max).ToList();
    }

    /// <summary>Reads the stored text; bad pieces are dropped, repeats too, and the cap applies.</summary>
    public static IReadOnlyList<string> Parse(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored))
            return [];

        return stored.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Normalize)
            .OfType<string>()
            .Distinct()
            .Take(Max)
            .ToList();
    }

    public static string Serialize(IEnumerable<string> colors) => string.Join(",", colors);

    /// <summary>"#RRGGBB" in capitals, or null when it is not a six-digit hex color.</summary>
    public static string? Normalize(string? hex)
    {
        if (hex is null)
            return null;

        var t = hex.Trim();
        if (t.Length != 7 || t[0] != '#' || !t.Skip(1).All(Uri.IsHexDigit))
            return null;

        return t.ToUpperInvariant();
    }
}
