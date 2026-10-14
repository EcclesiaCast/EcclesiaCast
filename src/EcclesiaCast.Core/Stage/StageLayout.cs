namespace EcclesiaCast.Core.Stage;

/// <summary>What one box of the stage screen shows.</summary>
public enum StageBoxKind
{
    /// <summary>The words that are on the projector now.</summary>
    LiveText,

    /// <summary>The slide after the live one, so the singers can prepare.</summary>
    NextSlide,

    /// <summary>Time of day.</summary>
    Clock,

    /// <summary>The service stopwatch: how long the service has been running.</summary>
    Timer,

    /// <summary>What is left of the countdown before the service (empty when none runs).</summary>
    Countdown,

    /// <summary>The preacher's outline, written by the operator.</summary>
    Notes,

    /// <summary>Name of what is live: «Coro», «Juan 3:16».</summary>
    LiveName,

    /// <summary>A text that never changes: a title, a reminder.</summary>
    FixedText,

    /// <summary>The footer notice, the same one the congregation reads (empty when none is up).</summary>
    Overlay,

    /// <summary>Exactly what the projector shows: the live slide with its background.</summary>
    Mirror,

    /// <summary>What is left of the section that is running (Alabanza, Prédica…), red once it is over.</summary>
    SectionTimer,
}

public enum StageAlign
{
    Left,
    Center,
    Right,
}

/// <summary>
/// One rectangle of the stage screen. Positions and sizes are in pixels over
/// a virtual 1920×1080 canvas, the same convention as the slides, so a layout
/// looks the same on any stage monitor.
/// </summary>
public sealed class StageBox
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

    public StageBoxKind Kind { get; set; } = StageBoxKind.FixedText;

    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; } = 400;
    public double Height { get; set; } = 120;

    /// <summary>Font size in pixels over the virtual canvas. Text that does not fit shrinks to fit.</summary>
    public double FontSize { get; set; } = 60;

    /// <summary>Text colour as #RRGGBB.</summary>
    public string Color { get; set; } = "#FFFFFF";

    /// <summary>
    /// Typeface by its system name («Segoe UI»). Null means the box keeps the
    /// default one, so layouts saved before this option look the same.
    /// </summary>
    public string? FontFamily { get; set; }

    public StageAlign Align { get; set; } = StageAlign.Center;

    public bool Bold { get; set; }

    /// <summary>The text of a <see cref="StageBoxKind.FixedText"/> box.</summary>
    public string Text { get; set; } = string.Empty;

    public StageBox Clone() => (StageBox)MemberwiseClone();

    /// <summary>The name the operator sees in lists and menus.</summary>
    public static string KindName(StageBoxKind kind) => kind switch
    {
        StageBoxKind.LiveText => "Letra en vivo",
        StageBoxKind.NextSlide => "Próxima diapositiva",
        StageBoxKind.Clock => "Hora",
        StageBoxKind.Timer => "Cronómetro",
        StageBoxKind.Countdown => "Cuenta regresiva (antes de empezar)",
        StageBoxKind.SectionTimer => "Temporizador (lo que queda de la sección)",
        StageBoxKind.Notes => "Notas para el predicador",
        StageBoxKind.LiveName => "Nombre de lo que está en vivo",
        StageBoxKind.FixedText => "Texto fijo",
        StageBoxKind.Overlay => "Aviso al pie",
        StageBoxKind.Mirror => "Igual a la pantalla principal",
        _ => kind.ToString(),
    };
}

/// <summary>A named arrangement of boxes: «Cantantes», «Predicador».</summary>
public sealed class StageLayout
{
    public const double CanvasWidth = 1920;
    public const double CanvasHeight = 1080;

    public string Name { get; set; } = "Escenario";

    public List<StageBox> Boxes { get; set; } = [];

    public StageLayout Clone() => new()
    {
        Name = Name,
        Boxes = [.. Boxes.Select(b => b.Clone())],
    };
}
