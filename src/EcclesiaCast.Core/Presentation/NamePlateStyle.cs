using System.Text.Json;
using System.Text.Json.Serialization;

namespace EcclesiaCast.Core.Presentation;

/// <summary>Where a name plate is seen.</summary>
public enum NamePlateTarget
{
    /// <summary>On the projector and on the stream (OBS).</summary>
    Everywhere,

    /// <summary>Only on the stream: the room keeps the clean picture.</summary>
    StreamOnly,

    /// <summary>Only on the projector.</summary>
    ProjectorOnly,
}

/// <summary>Which side of the card carries the coloured band.</summary>
public enum NamePlateAccent
{
    Left,
    Bottom,
    Top,
    None,
}

/// <summary>Where the card sits along the bottom of the screen.</summary>
public enum NamePlateAlign
{
    Left,
    Center,
    Right,
}

/// <summary>How the card leaves the screen.</summary>
public enum NamePlateExit
{
    /// <summary>Fades out where it is.</summary>
    Fade,

    /// <summary>Slides back out to the side while it fades.</summary>
    Slide,

    /// <summary>Drops a little while it fades.</summary>
    Drop,

    /// <summary>Closes from the right, like a blind.</summary>
    Wipe,
}

/// <summary>
/// The look of a name plate, saved with a name («Prédica», «Alabanza»…) so
/// several plates can share it: change the style and every plate that uses it
/// changes. Colours are «#RRGGBB»; sizes are in the 1920×1080 canvas.
/// </summary>
public sealed record NamePlateStyle
{
    public string Name { get; init; } = "Clásica";

    // ── Recuadro ──
    public string BackgroundColor { get; init; } = "#101420";

    /// <summary>0 (no box at all) to 100.</summary>
    public int BackgroundOpacity { get; init; } = 91;

    public int CornerRadius { get; init; }

    /// <summary>Fixed width of the card (canvas units); 0 fits the words.</summary>
    public int Width { get; init; }

    /// <summary>Fixed height of the card; 0 fits the words.</summary>
    public int Height { get; init; }

    // ── Franja de color ──
    public NamePlateAccent Accent { get; init; } = NamePlateAccent.Left;

    public string AccentColor { get; init; } = "#2F63C9";

    public int AccentThickness { get; init; } = 14;

    /// <summary>The name on a block of the accent colour, the role on the box below it («Noticias»).</summary>
    public bool NameOnAccent { get; init; }

    // ── Letras ──
    public string FontFamily { get; init; } = "Segoe UI";

    public string NameColor { get; init; } = "#FFFFFF";

    public string RoleColor { get; init; } = "#C9D3E6";

    public int NameSize { get; init; } = 66;

    public int RoleSize { get; init; } = 40;

    public bool NameBold { get; init; } = true;

    public bool Uppercase { get; init; }

    /// <summary>A soft shadow under the letters, for a style with no box.</summary>
    public bool TextShadow { get; init; }

    // ── Lugar ──
    public NamePlateAlign Align { get; init; } = NamePlateAlign.Left;

    /// <summary>Distance from the bottom edge (canvas units).</summary>
    public int MarginBottom { get; init; } = 110;

    /// <summary>Distance from the side edge (canvas units).</summary>
    public int MarginSide { get; init; } = 90;

    // ── Movimiento ──
    public NamePlateAnimation Enter { get; init; } = NamePlateAnimation.Slide;

    public NamePlateExit Exit { get; init; } = NamePlateExit.Fade;

    /// <summary>How long the way in lasts, in milliseconds.</summary>
    public int EnterMs { get; init; } = 420;

    public int ExitMs { get; init; } = 320;

    // ── Dónde ──
    public NamePlateTarget Target { get; init; } = NamePlateTarget.Everywhere;

    [JsonIgnore]
    public bool OnProjector => Target != NamePlateTarget.StreamOnly;

    [JsonIgnore]
    public bool OnStream => Target != NamePlateTarget.ProjectorOnly;

    /// <summary>Every value inside the range the designer allows, and colours that are colours.</summary>
    public NamePlateStyle Clamped() => this with
    {
        Name = string.IsNullOrWhiteSpace(Name) ? "Sin nombre" : Name.Trim(),
        BackgroundColor = Hex(BackgroundColor, "#101420"),
        BackgroundOpacity = Math.Clamp(BackgroundOpacity, 0, 100),
        CornerRadius = Math.Clamp(CornerRadius, 0, 80),
        Width = Width <= 0 ? 0 : Math.Clamp(Width, 300, 1800),
        Height = Height <= 0 ? 0 : Math.Clamp(Height, 80, 600),
        AccentColor = Hex(AccentColor, "#2F63C9"),
        AccentThickness = Math.Clamp(AccentThickness, 0, 60),
        FontFamily = string.IsNullOrWhiteSpace(FontFamily) ? "Segoe UI" : FontFamily.Trim(),
        NameColor = Hex(NameColor, "#FFFFFF"),
        RoleColor = Hex(RoleColor, "#C9D3E6"),
        NameSize = Math.Clamp(NameSize, 20, 160),
        RoleSize = Math.Clamp(RoleSize, 14, 120),
        MarginBottom = Math.Clamp(MarginBottom, 0, 900),
        MarginSide = Math.Clamp(MarginSide, 0, 800),
        Enter = Enum.IsDefined(Enter) ? Enter : NamePlateAnimation.Slide,
        Exit = Enum.IsDefined(Exit) ? Exit : NamePlateExit.Fade,
        EnterMs = Math.Clamp(EnterMs, 0, 3000),
        ExitMs = Math.Clamp(ExitMs, 0, 3000),
        Accent = Enum.IsDefined(Accent) ? Accent : NamePlateAccent.Left,
        Align = Enum.IsDefined(Align) ? Align : NamePlateAlign.Left,
        Target = Enum.IsDefined(Target) ? Target : NamePlateTarget.Everywhere,
    };

    /// <summary>«#RRGGBB» (or «#AARRGGBB») as typed; anything else gives <paramref name="fallback"/>.</summary>
    public static string Hex(string? value, string fallback)
    {
        var text = (value ?? string.Empty).Trim();
        if (!text.StartsWith('#'))
            text = "#" + text;
        return text.Length is 7 or 9 && text.Skip(1).All(Uri.IsHexDigit) ? text.ToUpperInvariant() : fallback;
    }

    // ── Estilos listos ───────────────────────────────────────────

    public static NamePlateStyle Classic { get; } = new();

    /// <summary>The styles that come with the program, to start from.</summary>
    public static IReadOnlyList<NamePlateStyle> Presets { get; } =
    [
        Classic,
        new()
        {
            Name = "Moderna", BackgroundColor = "#FFFFFF", BackgroundOpacity = 96, CornerRadius = 18,
            Accent = NamePlateAccent.Bottom, AccentColor = "#E8B931", AccentThickness = 10,
            NameColor = "#141821", RoleColor = "#4A5468", Enter = NamePlateAnimation.Rise,
            Exit = NamePlateExit.Drop,
        },
        new()
        {
            Name = "Noticias", NameOnAccent = true, AccentColor = "#C62E3B", BackgroundColor = "#0E1118",
            BackgroundOpacity = 94, Accent = NamePlateAccent.None, Uppercase = true, NameSize = 58, RoleSize = 36,
            Enter = NamePlateAnimation.Wipe, Exit = NamePlateExit.Wipe, MarginSide = 0, MarginBottom = 120,
        },
        new()
        {
            Name = "Vidrio", BackgroundColor = "#FFFFFF", BackgroundOpacity = 18, CornerRadius = 26,
            Accent = NamePlateAccent.Left, AccentColor = "#FFFFFF", AccentThickness = 6,
            NameColor = "#FFFFFF", RoleColor = "#E6ECF5", TextShadow = true, Enter = NamePlateAnimation.Fade,
        },
        new()
        {
            Name = "Mínima", BackgroundOpacity = 0, Accent = NamePlateAccent.Left, AccentColor = "#FFFFFF",
            AccentThickness = 5, TextShadow = true, NameSize = 72, RoleSize = 42,
        },
        new()
        {
            Name = "Centrada", Align = NamePlateAlign.Center, CornerRadius = 40, Accent = NamePlateAccent.None,
            BackgroundColor = "#141821", BackgroundOpacity = 88, Enter = NamePlateAnimation.Rise,
            Exit = NamePlateExit.Drop, MarginBottom = 90,
        },
    ];
}

/// <summary>The church's styles, kept in the settings. It always has at least the ready ones.</summary>
public static class NamePlateStyleList
{
    public const string SettingKey = "nameplates.styles";

    public static IReadOnlyList<NamePlateStyle> Parse(string? json)
    {
        List<NamePlateStyle> styles = [];
        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                styles = (JsonSerializer.Deserialize<List<NamePlateStyle>>(json) ?? [])
                    .OfType<NamePlateStyle>()
                    .Select(s => s.Clamped())
                    .ToList();
            }
            catch (JsonException)
            {
                styles = [];
            }
        }

        // Names are unique; the first one with a name wins.
        styles = styles.GroupBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase).Select(g => g.First()).ToList();
        return styles.Count > 0 ? styles : NamePlateStyle.Presets;
    }

    public static string ToSetting(IEnumerable<NamePlateStyle> styles) =>
        JsonSerializer.Serialize(styles.Select(s => s.Clamped()).ToList());

    /// <summary>The style with that name, or the first one (a plate whose style was deleted).</summary>
    public static NamePlateStyle Find(IReadOnlyList<NamePlateStyle> styles, string? name) =>
        styles.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.CurrentCultureIgnoreCase))
        ?? (styles.Count > 0 ? styles[0] : NamePlateStyle.Classic);

    /// <summary>A name not used yet: «Prédica», «Prédica 2»…</summary>
    public static string FreeName(IReadOnlyList<NamePlateStyle> styles, string wanted)
    {
        var name = string.IsNullOrWhiteSpace(wanted) ? "Estilo" : wanted.Trim();
        if (styles.All(s => !string.Equals(s.Name, name, StringComparison.CurrentCultureIgnoreCase)))
            return name;

        for (var i = 2; ; i++)
        {
            var candidate = $"{name} {i}";
            if (styles.All(s => !string.Equals(s.Name, candidate, StringComparison.CurrentCultureIgnoreCase)))
                return candidate;
        }
    }
}
