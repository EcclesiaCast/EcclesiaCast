using System.Text.Json;
using System.Text.Json.Serialization;
using EcclesiaCast.Core.Abstractions;

namespace EcclesiaCast.Core.Stage;

/// <summary>The saved layouts and which one the stage is showing.</summary>
public sealed record StageLayoutState(IReadOnlyList<StageLayout> Layouts, string ActiveName)
{
    public StageLayout Active =>
        Layouts.FirstOrDefault(l => string.Equals(l.Name, ActiveName, StringComparison.OrdinalIgnoreCase))
        ?? Layouts[0];
}

/// <summary>
/// Reads and writes the stage layouts (as JSON, in two settings), builds the
/// three that ship with the program and turns the old fixed stage options into
/// a layout so nobody loses what they had set up.
/// </summary>
public static class StageLayoutStore
{
    public const string LayoutsKey = "stage.layouts";
    public const string ActiveKey = "stage.layout.active";

    public const string SingersName = "Cantantes";
    public const string PreacherName = "Predicador";
    public const string MirrorName = "Igual a la pantalla principal";

    // The keys the fixed stage used before layouts existed.
    private const string LegacyClockKey = "stage.clock";
    private const string LegacyTimerKey = "stage.timer";
    private const string LegacyNextKey = "stage.next";
    private const string LegacyNotesKey = "stage.notes";
    private const string LegacyScaleKey = "stage.scale";

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = false,
    };

    // ── JSON ─────────────────────────────────────────────────────

    public static string Serialize(IEnumerable<StageLayout> layouts) =>
        JsonSerializer.Serialize(layouts, Json);

    /// <summary>
    /// Layouts from saved JSON. Damaged or empty text gives an empty list
    /// rather than an error: a stage screen must never stop the program from
    /// starting. Values are pulled back inside the canvas.
    /// </summary>
    public static IReadOnlyList<StageLayout> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            var layouts = JsonSerializer.Deserialize<List<StageLayout>>(json, Json) ?? [];
            layouts.RemoveAll(l => l is null);
            foreach (var layout in layouts)
            {
                layout.Name = string.IsNullOrWhiteSpace(layout.Name) ? "Escenario" : layout.Name.Trim();
                layout.Boxes ??= [];
                layout.Boxes.RemoveAll(b => b is null);
                foreach (var box in layout.Boxes)
                    Sanitize(box);
            }

            return layouts;
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static void Sanitize(StageBox box)
    {
        box.Width = Math.Clamp(box.Width, 20, StageLayout.CanvasWidth);
        box.Height = Math.Clamp(box.Height, 20, StageLayout.CanvasHeight);
        box.X = Math.Clamp(box.X, 0, StageLayout.CanvasWidth - box.Width);
        box.Y = Math.Clamp(box.Y, 0, StageLayout.CanvasHeight - box.Height);
        box.FontSize = Math.Clamp(box.FontSize, 8, 600);
        box.Text ??= string.Empty;
        box.FontFamily = string.IsNullOrWhiteSpace(box.FontFamily) ? null : box.FontFamily.Trim();
        if (string.IsNullOrWhiteSpace(box.Id))
            box.Id = Guid.NewGuid().ToString("N")[..8];
        if (string.IsNullOrWhiteSpace(box.Color))
            box.Color = "#FFFFFF";
    }

    // ── Settings ─────────────────────────────────────────────────

    /// <summary>
    /// What the stage should start with. Saved layouts win; otherwise the
    /// three that ship with the program, plus one made from the old options if
    /// the operator had changed any (that one is the active one, so the stage
    /// looks as it did).
    /// </summary>
    public static StageLayoutState Load(ISettingsStore settings)
    {
        var saved = Parse(settings.Get(LayoutsKey));
        if (saved.Count > 0)
            return new StageLayoutState(saved, settings.Get(ActiveKey) ?? saved[0].Name);

        var layouts = Defaults().ToList();
        var active = SingersName;

        var hadLegacy = new[] { LegacyClockKey, LegacyTimerKey, LegacyNextKey, LegacyNotesKey, LegacyScaleKey }
            .Any(key => settings.Get(key) is not null);
        if (hadLegacy)
        {
            var legacy = FromLegacy(
                settings.Get(LegacyClockKey) != "0",
                settings.Get(LegacyTimerKey) != "0",
                settings.Get(LegacyNextKey) != "0",
                settings.Get(LegacyNotesKey) != "0",
                double.TryParse(settings.Get(LegacyScaleKey), out var scale) && scale > 0 ? scale : 96);
            layouts.Insert(0, legacy);
            active = legacy.Name;
        }

        return new StageLayoutState(layouts, active);
    }

    public static void Save(ISettingsStore settings, IEnumerable<StageLayout> layouts, string activeName)
    {
        settings.Set(LayoutsKey, Serialize(layouts));
        settings.Set(ActiveKey, activeName);
    }

    /// <summary>The name with « 2», « 3»… added if another layout already has it.</summary>
    public static string UniqueName(IEnumerable<string> existing, string wanted)
    {
        var taken = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);
        var baseName = string.IsNullOrWhiteSpace(wanted) ? "Escenario" : wanted.Trim();
        if (!taken.Contains(baseName))
            return baseName;

        for (var n = 2; ; n++)
        {
            var candidate = $"{baseName} {n}";
            if (!taken.Contains(candidate))
                return candidate;
        }
    }

    // ── Factory layouts ──────────────────────────────────────────

    private const string Dim = "#7F8CA8";
    private const string Blue = "#4E7FD6";
    private const string Soft = "#9AA6BF";
    private const string Faint = "#4A5266";
    private const string Paper = "#C9D4E8";

    public static IReadOnlyList<StageLayout> Defaults() => [Singers(), Preacher(), Mirror()];

    private static StageBox Box(
        StageBoxKind kind, double x, double y, double w, double h, double size,
        string color = "#FFFFFF", StageAlign align = StageAlign.Center, bool bold = false, string text = "") =>
        new() { Kind = kind, X = x, Y = y, Width = w, Height = h, FontSize = size, Color = color, Align = align, Bold = bold, Text = text };

    /// <summary>Big live words and what comes next: what the band reads.</summary>
    private static StageLayout Singers() => new()
    {
        Name = SingersName,
        Boxes =
        [
            Box(StageBoxKind.LiveName, 60, 24, 1800, 64, 36, Blue, bold: true),
            Box(StageBoxKind.LiveText, 60, 100, 1800, 640, 120, bold: true),
            Box(StageBoxKind.FixedText, 60, 760, 400, 44, 24, Faint, StageAlign.Left, bold: true, text: "SIGUIENTE"),
            Box(StageBoxKind.NextSlide, 60, 806, 1800, 170, 48, Soft),
            Box(StageBoxKind.Overlay, 60, 990, 1800, 70, 36),
        ],
    };

    /// <summary>Time, how long the service has run and the outline: what the preacher needs.</summary>
    private static StageLayout Preacher() => new()
    {
        Name = PreacherName,
        Boxes =
        [
            Box(StageBoxKind.Clock, 60, 40, 480, 160, 130, Dim, StageAlign.Left),
            Box(StageBoxKind.Countdown, 600, 40, 720, 160, 130, Blue),
            Box(StageBoxKind.Timer, 1380, 40, 480, 160, 130, Dim, StageAlign.Right),
            Box(StageBoxKind.Notes, 60, 230, 1800, 740, 64, Paper, StageAlign.Left),
            Box(StageBoxKind.Overlay, 60, 990, 1800, 70, 36),
        ],
    };

    /// <summary>One box with the projector's own picture.</summary>
    private static StageLayout Mirror() => new()
    {
        Name = MirrorName,
        Boxes = [Box(StageBoxKind.Mirror, 0, 0, StageLayout.CanvasWidth, StageLayout.CanvasHeight, 60)],
    };

    /// <summary>
    /// The fixed stage of the earlier versions, as a layout: the same parts in
    /// the same places, with the ones the operator had turned off left out.
    /// </summary>
    public static StageLayout FromLegacy(bool clock, bool timer, bool next, bool notes, double textScale)
    {
        var layout = new StageLayout { Name = "Mi escenario" };

        if (clock)
            layout.Boxes.Add(Box(StageBoxKind.Clock, 28, 20, 300, 90, 44, Dim, StageAlign.Left));
        layout.Boxes.Add(Box(StageBoxKind.LiveName, 340, 20, 1240, 90, 30, Blue, bold: true));
        if (timer)
            layout.Boxes.Add(Box(StageBoxKind.Timer, 1592, 20, 300, 90, 44, Dim, StageAlign.Right));

        // The bottom grows upwards: the footer notice first, then the notes
        // and the next slide above it, and the live words take what is left.
        var bottom = 1060d;
        layout.Boxes.Add(Box(StageBoxKind.Overlay, 28, bottom - 70, 1864, 70, 34));
        bottom -= 80;

        if (notes)
        {
            layout.Boxes.Add(Box(StageBoxKind.Notes, 28, bottom - 220, 1864, 220, 34, Paper, StageAlign.Left));
            bottom -= 230;
        }

        if (next)
        {
            layout.Boxes.Add(Box(StageBoxKind.FixedText, 28, bottom - 170, 250, 44, 18, Faint, StageAlign.Left, bold: true, text: "SIGUIENTE"));
            layout.Boxes.Add(Box(StageBoxKind.NextSlide, 290, bottom - 170, 1602, 170, 40, Soft, StageAlign.Left));
            bottom -= 180;
        }

        layout.Boxes.Add(Box(StageBoxKind.LiveText, 28, 120, 1864, bottom - 120, Math.Clamp(textScale, 20, 400), bold: true));
        return layout;
    }
}
