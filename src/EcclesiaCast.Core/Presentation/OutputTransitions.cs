using System.Globalization;

namespace EcclesiaCast.Core.Presentation;

/// <summary>
/// How long the projector takes to fade between things that are not slide
/// text (the text has its own transition, chosen per theme): the logo coming
/// and going, one medium giving way to the next, the black screen, and the
/// whole output switching off. All in seconds, as the operator thinks of them.
/// </summary>
public sealed record OutputTransitions(
    double LogoSeconds = 0.5,
    double MediaSeconds = 0.4,
    double BlackSeconds = 0.25,
    double OutputSeconds = 0)
{
    /// <summary>Setting that holds the values, e.g. <c>logo=0.5;media=0.4;black=0.25;output=0</c>.</summary>
    public const string SettingKey = "output.transitions";

    public const double MaxSeconds = 10;

    public const double DefaultLogoSeconds = 0.5;
    public const double DefaultMediaSeconds = 0.4;
    public const double DefaultBlackSeconds = 0.25;
    public const double DefaultOutputSeconds = 0;

    public static OutputTransitions Default => new();

    /// <summary>The same values with each one held between 0 and <see cref="MaxSeconds"/>.</summary>
    public OutputTransitions Clamp() => new(
        ClampSeconds(LogoSeconds, DefaultLogoSeconds),
        ClampSeconds(MediaSeconds, DefaultMediaSeconds),
        ClampSeconds(BlackSeconds, DefaultBlackSeconds),
        ClampSeconds(OutputSeconds, DefaultOutputSeconds));

    /// <summary>What to store: always with a point for the decimals, whatever the language of Windows.</summary>
    public string ToSetting()
    {
        var c = Clamp();
        return $"logo={Format(c.LogoSeconds)};media={Format(c.MediaSeconds)};black={Format(c.BlackSeconds)};output={Format(c.OutputSeconds)}";
    }

    /// <summary>
    /// Reads what <see cref="ToSetting"/> wrote. Anything missing, unreadable
    /// or out of range falls back to the default (or the nearest allowed
    /// value) for that item alone, so a half-broken setting never costs the
    /// rest of it.
    /// </summary>
    public static OutputTransitions Parse(string? setting)
    {
        double logo = DefaultLogoSeconds, media = DefaultMediaSeconds,
            black = DefaultBlackSeconds, output = DefaultOutputSeconds;

        foreach (var part in (setting ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = part.Split('=', 2);
            if (pair.Length != 2
                || !double.TryParse(pair[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                continue;

            switch (pair[0].Trim().ToLowerInvariant())
            {
                case "logo": logo = value; break;
                case "media": media = value; break;
                case "black": black = value; break;
                case "output": output = value; break;
            }
        }

        return new OutputTransitions(logo, media, black, output).Clamp();
    }

    public TimeSpan Logo => TimeSpan.FromSeconds(ClampSeconds(LogoSeconds, DefaultLogoSeconds));

    public TimeSpan Media => TimeSpan.FromSeconds(ClampSeconds(MediaSeconds, DefaultMediaSeconds));

    public TimeSpan Black => TimeSpan.FromSeconds(ClampSeconds(BlackSeconds, DefaultBlackSeconds));

    public TimeSpan Output => TimeSpan.FromSeconds(ClampSeconds(OutputSeconds, DefaultOutputSeconds));

    private static double ClampSeconds(double value, double fallback) =>
        double.IsNaN(value) || double.IsInfinity(value) ? fallback : Math.Clamp(value, 0, MaxSeconds);

    private static string Format(double value) =>
        Math.Round(value, 2).ToString("0.##", CultureInfo.InvariantCulture);
}
