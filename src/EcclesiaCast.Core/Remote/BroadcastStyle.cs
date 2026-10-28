using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using EcclesiaCast.Core.Abstractions;

namespace EcclesiaCast.Core.Remote;

/// <summary>Where the words sit on a layer of the stream.</summary>
public enum BroadcastPosition { Top, Center, Bottom }

/// <summary>The plate drawn behind (or around) the words of a layer of the stream.</summary>
public enum BroadcastPlate { None, Tab, Glass, Solid, Gradient, Halo, Fillet, Line }

/// <summary>What sits behind the plate: nothing (transparent), a flat colour, or a fixed picture or video.</summary>
public enum BroadcastBackground { None, Color, File }

/// <summary>
/// How one layer of the stream (the Bible, the songs or the announcements)
/// looks: its own design, made for the camera, not copied from the projector.
/// Immutable; <see cref="Clamp"/> keeps every number inside its limits, and
/// <see cref="Parse"/> never throws.
/// </summary>
public sealed record BroadcastStyle
{
    public const string DefaultAccentColor = "#2DD4BF";
    public const string DefaultPlateColor = "#14161C";

    /// <summary>When true the layer behaves as it always did: it uses the theme of the slide.</summary>
    public bool FollowProjector { get; init; }

    public BroadcastPosition Position { get; init; } = BroadcastPosition.Bottom;

    public BroadcastPlate Plate { get; init; } = BroadcastPlate.Tab;

    public string AccentColor { get; init; } = DefaultAccentColor;

    public string PlateColor { get; init; } = DefaultPlateColor;

    /// <summary>0 to 100.</summary>
    public int PlateOpacity { get; init; } = 85;

    public string TextColor { get; init; } = "#FFFFFF";

    public string FontFamily { get; init; } = "Segoe UI";

    /// <summary>In points over a 1080-high screen, like <c>RemoteOutput.FontSize</c>.</summary>
    public double FontSize { get; init; } = 54;

    public bool Bold { get; init; } = true;

    public bool Uppercase { get; init; }

    /// <summary>The small label over the plate: the part of a song («CORO») or the reference of a verse.</summary>
    public bool ShowLabel { get; init; } = true;

    /// <summary>The widest the plate may get, as a percentage of the screen.</summary>
    public int WidthPercent { get; init; } = 70;

    /// <summary>Distance kept from the edge of the screen, as a percentage.</summary>
    public int MarginPercent { get; init; } = 6;

    public BroadcastBackground Background { get; init; } = BroadcastBackground.None;

    public string BackgroundColor { get; init; } = "#000000";

    /// <summary>Path of the picture or video, for <see cref="BroadcastBackground.File"/>.</summary>
    public string BackgroundFile { get; init; } = string.Empty;

    public const int MinFontSize = 20;
    public const int MaxFontSize = 120;
    public const int MinWidthPercent = 30;
    public const int MaxWidthPercent = 100;
    public const int MaxMarginPercent = 15;

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    /// <summary>The factory design of a layer: the announcements sit in the middle, the songs in capitals.</summary>
    public static BroadcastStyle Default(OutputChannel channel) => channel switch
    {
        OutputChannel.Songs => new BroadcastStyle { Uppercase = true },
        OutputChannel.Announcements or OutputChannel.All => new BroadcastStyle { Position = BroadcastPosition.Center },
        _ => new BroadcastStyle(),
    };

    /// <summary>The same style with every number held inside its limits and every colour readable.</summary>
    public BroadcastStyle Clamp() => this with
    {
        Position = Enum.IsDefined(Position) ? Position : BroadcastPosition.Bottom,
        Plate = Enum.IsDefined(Plate) ? Plate : BroadcastPlate.Tab,
        Background = Enum.IsDefined(Background) ? Background : BroadcastBackground.None,
        AccentColor = Color(AccentColor, DefaultAccentColor),
        PlateColor = Color(PlateColor, DefaultPlateColor),
        TextColor = Color(TextColor, "#FFFFFF"),
        BackgroundColor = Color(BackgroundColor, "#000000"),
        PlateOpacity = Math.Clamp(PlateOpacity, 0, 100),
        FontSize = double.IsNaN(FontSize) || double.IsInfinity(FontSize) ? 54 : Math.Clamp(FontSize, MinFontSize, MaxFontSize),
        WidthPercent = Math.Clamp(WidthPercent, MinWidthPercent, MaxWidthPercent),
        MarginPercent = Math.Clamp(MarginPercent, 0, MaxMarginPercent),
        FontFamily = string.IsNullOrWhiteSpace(FontFamily) ? "Segoe UI" : FontFamily.Trim(),
        BackgroundFile = BackgroundFile ?? string.Empty,
    };

    /// <summary>What to store: JSON, the same whatever the language of Windows.</summary>
    public string ToSetting() => JsonSerializer.Serialize(Clamp(), Options);

    /// <summary>
    /// Reads what <see cref="ToSetting"/> wrote. Anything empty or broken gives
    /// the factory design of the layer; a field that cannot be read falls back
    /// to its factory value alone, so a half-broken setting costs only that field.
    /// </summary>
    public static BroadcastStyle Parse(string? setting, OutputChannel channel)
    {
        var style = Default(channel);
        if (string.IsNullOrWhiteSpace(setting))
            return style;

        try
        {
            using var doc = JsonDocument.Parse(setting);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return style;

            foreach (var property in doc.RootElement.EnumerateObject())
                style = Apply(style, property.Name.ToLowerInvariant(), property.Value);

            return style.Clamp();
        }
        catch (JsonException)
        {
            return style;
        }
    }

    private static BroadcastStyle Apply(BroadcastStyle style, string name, JsonElement value) => name switch
    {
        "followprojector" => IsBool(value) ? style with { FollowProjector = value.GetBoolean() } : style,
        "position" => Enum.TryParse<BroadcastPosition>(Text(value), true, out var p) ? style with { Position = p } : style,
        "plate" => Enum.TryParse<BroadcastPlate>(Text(value), true, out var pl) ? style with { Plate = pl } : style,
        "background" => Enum.TryParse<BroadcastBackground>(Text(value), true, out var b) ? style with { Background = b } : style,
        "accentcolor" => style with { AccentColor = Text(value) ?? style.AccentColor },
        "platecolor" => style with { PlateColor = Text(value) ?? style.PlateColor },
        "textcolor" => style with { TextColor = Text(value) ?? style.TextColor },
        "backgroundcolor" => style with { BackgroundColor = Text(value) ?? style.BackgroundColor },
        "fontfamily" => style with { FontFamily = Text(value) ?? style.FontFamily },
        "backgroundfile" => style with { BackgroundFile = Text(value) ?? style.BackgroundFile },
        "plateopacity" => value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var o) ? style with { PlateOpacity = o } : style,
        "fontsize" => value.ValueKind == JsonValueKind.Number ? style with { FontSize = value.GetDouble() } : style,
        "widthpercent" => value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var w) ? style with { WidthPercent = w } : style,
        "marginpercent" => value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var m) ? style with { MarginPercent = m } : style,
        "bold" => IsBool(value) ? style with { Bold = value.GetBoolean() } : style,
        "uppercase" => IsBool(value) ? style with { Uppercase = value.GetBoolean() } : style,
        "showlabel" => IsBool(value) ? style with { ShowLabel = value.GetBoolean() } : style,
        _ => style,
    };

    private static bool IsBool(JsonElement value) => value.ValueKind is JsonValueKind.True or JsonValueKind.False;

    private static string? Text(JsonElement value) => value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    /// <summary>A colour as «#RRGGBB»; anything else becomes the fallback (the page would not draw it).</summary>
    private static string Color(string? value, string fallback)
    {
        var text = (value ?? string.Empty).Trim();
        return text.Length == 7 && text[0] == '#'
               && int.TryParse(text.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _)
            ? text.ToUpperInvariant()
            : fallback;
    }
}

/// <summary>The three designs, one per layer of the stream, and where each one is saved.</summary>
public sealed record BroadcastStyles(BroadcastStyle Bible, BroadcastStyle Songs, BroadcastStyle Announcements)
{
    public static BroadcastStyles Default => new(
        BroadcastStyle.Default(OutputChannel.Bible),
        BroadcastStyle.Default(OutputChannel.Songs),
        BroadcastStyle.Default(OutputChannel.Announcements));

    /// <summary>Setting that holds the style of a layer: «broadcast.style.biblia», «…canciones», «…anuncios».</summary>
    public static string SettingKey(OutputChannel channel) =>
        "broadcast.style." + (channel == OutputChannel.All ? "anuncios" : OutputChannels.Name(channel));

    public BroadcastStyle For(OutputChannel channel) => channel switch
    {
        OutputChannel.Bible => Bible,
        OutputChannel.Songs => Songs,
        _ => Announcements,
    };

    public BroadcastStyles With(OutputChannel channel, BroadcastStyle style) => channel switch
    {
        OutputChannel.Bible => this with { Bible = style },
        OutputChannel.Songs => this with { Songs = style },
        _ => this with { Announcements = style },
    };

    /// <summary>Reads the three saved designs; the factory one for any that was never saved.</summary>
    public static BroadcastStyles Load(ISettingsStore settings) => new(
        BroadcastStyle.Parse(settings.Get(SettingKey(OutputChannel.Bible)), OutputChannel.Bible),
        BroadcastStyle.Parse(settings.Get(SettingKey(OutputChannel.Songs)), OutputChannel.Songs),
        BroadcastStyle.Parse(settings.Get(SettingKey(OutputChannel.Announcements)), OutputChannel.Announcements));
}
