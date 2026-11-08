namespace EcclesiaCast.Core.Ui;

/// <summary>
/// One stop of the welcome tour: the part of the window it lights up
/// (<see cref="Target"/>, one of the <see cref="WelcomeTour"/> names; null for
/// the greeting and the goodbye, which light nothing) and what it says.
/// </summary>
public sealed record TourStep(string? Target, string Title, string Text);

/// <summary>A rectangle in window units, without WPF (Core has none).</summary>
public readonly record struct TourBox(double X, double Y, double Width, double Height)
{
    public double Right => X + Width;
    public double Bottom => Y + Height;
    public bool IsEmpty => Width <= 0 || Height <= 0;
}

/// <summary>
/// The tour shown the first time the program opens (and again from Ayuda →
/// «Recorrido del programa»): a greeting, then one part of the window at a
/// time, lit while the rest is darkened, with a short card that says what it
/// is for. What it says and where the card goes live here so they can be
/// tested; the window only finds the parts and draws.
/// </summary>
public static class WelcomeTour
{
    public const string Songs = "songs";
    public const string Bible = "bible";
    public const string Playlist = "playlist";
    public const string Slides = "slides";
    public const string Media = "media";
    public const string Tools = "tools";
    public const string Output = "output";
    public const string ScreenButtons = "screen-buttons";
    public const string GoLive = "go-live";

    /// <summary>Longest text a card may carry: it has to be read in a few seconds.</summary>
    public const int MaxTextLength = 230;

    public static IReadOnlyList<TourStep> Steps { get; } =
    [
        new(null, "Te damos la bienvenida a EcclesiaCast",
            "En un minuto te mostramos dónde está cada cosa. Podés saltar el recorrido cuando quieras."),
        new(Songs, "Canciones",
            "Buscá por el título o por un pedazo de la letra. Doble clic la proyecta. Con ➕ creás una, la buscás en internet o traés las de otro programa."),
        new(Bible, "Biblia",
            "Escribí «juan 3:16» o «sal 23» y Enter. Clic en una versión la elige; Ctrl+clic suma una segunda, que sale debajo. Con 📥 Biblias bajás más versiones."),
        new(Playlist, "La playlist del culto",
            "Arrastrá acá canciones, pasajes y medios en el orden de la reunión. Con las flechas pasás de uno al siguiente sin buscar nada."),
        new(Slides, "Diapositivas",
            "Las partes de lo que elegiste. Un clic pone una en pantalla, y las flechas avanzan y retroceden."),
        new(Media, "Medios",
            "Imágenes, videos, PDF y PowerPoint, ordenados en pestañas. Van de fondo detrás de la letra o solos en pantalla. Arrastrá archivos acá para sumarlos."),
        new(Tools, "Herramientas",
            "Texto rápido para escribir algo al momento, aviso al pie, resaltar palabras, notas para quien predica, música y los tiempos de la reunión."),
        new(Output, "LIVE y PREVIEW",
            "LIVE es lo que ve la congregación ahora. PREVIEW muestra lo que viene, y ESCENARIO lo que ven los músicos."),
        new(ScreenButtons, "Botones de pantalla",
            "Black tapa todo, Clear saca la letra y deja el fondo, Logo muestra el logo. 🎨 Temas cambia el diseño, 🎭 Escenario prende la pantalla de los músicos y 📱 lo manejás desde el celular."),
        new(GoLive, "Salir en vivo",
            "Elegí en «Salida» el proyector o la pantalla, y apretá «Poner en vivo». La tecla Esc apaga la salida."),
        new(null, "¡Listo!",
            "Lo podés volver a ver cuando quieras en Ayuda → «Recorrido del programa». ¡Que sea de bendición!"),
    ];

    /// <summary>
    /// The step after (<paramref name="direction"/> 1) or before (−1)
    /// <paramref name="from"/>, skipping the ones whose part of the window is
    /// not on show (a panel shrunk to icons, for instance). −1 when there is
    /// none: past the end the tour is over, before the start it stays put.
    /// </summary>
    public static int Next(IReadOnlyList<TourStep> steps, int from, int direction, Func<string, bool> isShown)
    {
        ArgumentNullException.ThrowIfNull(steps);
        ArgumentNullException.ThrowIfNull(isShown);
        if (direction == 0)
            throw new ArgumentOutOfRangeException(nameof(direction));

        for (var i = from + Math.Sign(direction); i >= 0 && i < steps.Count; i += Math.Sign(direction))
        {
            if (steps[i].Target is not { } target || isShown(target))
                return i;
        }

        return -1;
    }

    /// <summary>
    /// The lit rectangle: the part of the window with a little room around
    /// it, kept inside the window.
    /// </summary>
    public static TourBox Spot(TourBox target, TourBox area, double padding)
    {
        var x = Math.Max(area.X, target.X - padding);
        var y = Math.Max(area.Y, target.Y - padding);
        var right = Math.Min(area.Right, target.Right + padding);
        var bottom = Math.Min(area.Bottom, target.Bottom + padding);
        return right <= x || bottom <= y ? default : new TourBox(x, y, right - x, bottom - y);
    }

    /// <summary>
    /// Where the card goes: beside the lit part (right, then left), else
    /// under or over it, never on top of it while there is room. With no lit
    /// part, or no room anywhere, it goes in the middle of the window (a lit
    /// part that fills the window gets the card over its lower part).
    /// </summary>
    public static (double X, double Y) PlaceCard(TourBox spot, TourBox area, double width, double height, double gap)
    {
        double ClampX(double x) => Math.Clamp(x, area.X + gap, Math.Max(area.X + gap, area.Right - width - gap));
        double ClampY(double y) => Math.Clamp(y, area.Y + gap, Math.Max(area.Y + gap, area.Bottom - height - gap));

        var middle = (area.X + (area.Width - width) / 2, area.Y + (area.Height - height) / 2);
        if (spot.IsEmpty)
            return middle;

        if (spot.Right + gap + width + gap <= area.Right)
            return (spot.Right + gap, ClampY(spot.Y));
        if (spot.X - gap - width - gap >= area.X)
            return (spot.X - gap - width, ClampY(spot.Y));
        if (spot.Bottom + gap + height + gap <= area.Bottom)
            return (ClampX(spot.X), spot.Bottom + gap);
        if (spot.Y - gap - height - gap >= area.Y)
            return (ClampX(spot.X), spot.Y - gap - height);

        return (middle.Item1, ClampY(spot.Bottom - height - gap * 2));
    }
}
