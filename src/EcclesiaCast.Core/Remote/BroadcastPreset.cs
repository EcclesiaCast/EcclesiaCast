namespace EcclesiaCast.Core.Remote;

/// <summary>
/// A ready-made look for a layer of the stream: a plate with the colours and
/// the lettering that go with it, applied in one click and touched up after.
/// It leaves alone what belongs to the layer rather than to the look: where
/// the words sit, capitals, the label and the fixed background.
/// </summary>
public sealed record BroadcastPreset(
    string Name,
    string Description,
    BroadcastPlate Plate,
    string AccentColor,
    string PlateColor,
    int PlateOpacity,
    string FontFamily,
    double FontSize,
    bool Bold,
    int WidthPercent)
{
    /// <summary>The style with this look put on it.</summary>
    public BroadcastStyle ApplyTo(BroadcastStyle style) => (style with
    {
        FollowProjector = false,
        Plate = Plate,
        AccentColor = AccentColor,
        PlateColor = PlateColor,
        PlateOpacity = PlateOpacity,
        TextColor = "#FFFFFF",
        FontFamily = FontFamily,
        FontSize = FontSize,
        Bold = Bold,
        WidthPercent = WidthPercent,
    }).Clamp();

    /// <summary>True when the style already wears this look (the editor marks it).</summary>
    public bool Matches(BroadcastStyle style) => !style.FollowProjector && ApplyTo(style) == style.Clamp();
}

/// <summary>The looks that come with the program, in the order the editor shows them.</summary>
public static class BroadcastPresets
{
    public static IReadOnlyList<BroadcastPreset> All { get; } =
    [
        new("Moderna", "Placa oscura con pestaña y línea turquesa",
            BroadcastPlate.Tab, BroadcastStyle.DefaultAccentColor, BroadcastStyle.DefaultPlateColor, 85,
            "Segoe UI", 54, true, 70),
        new("Vidrio", "Placa translúcida que deja ver la cámara",
            BroadcastPlate.Glass, "#FFFFFF", "#1E2230", 55,
            "Segoe UI", 52, true, 70),
        new("Clásica", "Degradé oscuro desde el borde y letra con serifa",
            BroadcastPlate.Gradient, "#FFFFFF", "#000000", 80,
            "Georgia", 52, false, 90),
        new("Minimalista", "Sin placa, con un halo suave detrás de la letra",
            BroadcastPlate.Halo, "#FFFFFF", "#000000", 85,
            "Segoe UI Light", 58, false, 80),
        new("Noticiero", "Letra con sombra y una línea amarilla al costado",
            BroadcastPlate.Line, "#F5B301", "#0B0D12", 90,
            "Segoe UI", 48, true, 80),
        new("Elegante", "Línea dorada fina debajo de la letra",
            BroadcastPlate.Fillet, "#E8C34A", "#000000", 85,
            "Georgia", 54, false, 75),
    ];
}
