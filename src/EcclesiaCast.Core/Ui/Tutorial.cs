using System.Reflection;
using System.Text;

namespace EcclesiaCast.Core.Ui;

/// <summary>
/// One short guided tour over the real program (Ayuda → «Tutoriales…»): a card
/// in the list, and 5 to 8 steps that reuse the welcome tour's engine
/// (<see cref="TourStep"/>: what to light, and what the card says).
/// </summary>
public sealed record Tutorial(string Id, string Title, string Summary, string Icon, int Order, IReadOnlyList<TourStep> Steps);

/// <summary>
/// Reads tutorials from plain text files, so a new one is a new file and no
/// code. The format (UTF-8, one «clave: valor» per line):
/// <code>
/// # a line that starts with # is a comment
/// titulo: Crear una canción nueva
/// resumen: One line for the card.
/// icono: 🎵
/// orden: 1
///
/// [paso]
/// señalar: SongsAddButton
/// titulo: Title of the step
/// texto: What the card says. A line that is not «clave: valor»
/// goes on with the text above it.
/// </code>
/// «señalar» is the x:Name of the element of the main window to light; leave
/// it out and the card goes in the middle of the window. «abrir» (optional)
/// opens something before the card shows (see <see cref="TourActions"/>) and
/// «resaltar» points at an element of the window that «abrir» opened.
/// </summary>
public static class TutorialFile
{
    public const int MinSteps = 3;
    public const int MaxSteps = 10;

    /// <summary>The 8 that ship with the program are text files embedded here.</summary>
    private const string ResourcePrefix = "Tutorials/";

    public static bool TryParse(string id, string text, out Tutorial? tutorial, out string error)
    {
        ArgumentNullException.ThrowIfNull(text);
        tutorial = null;
        error = string.Empty;

        var head = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var blocks = new List<Dictionary<string, string>>();
        var current = head;
        string? lastKey = null;

        foreach (var raw in text.Replace("\r", string.Empty).Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line[0] == '#')
                continue;

            if (line.Equals("[paso]", StringComparison.OrdinalIgnoreCase))
            {
                current = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                blocks.Add(current);
                lastKey = null;
                continue;
            }

            var colon = line.IndexOf(':');
            var key = colon > 0 ? Normalize(line[..colon]) : null;
            if (key is "titulo" or "resumen" or "icono" or "orden" or "senalar" or "texto" or "abrir" or "resaltar" or "dibujo")
            {
                current[key] = line[(colon + 1)..].Trim();
                lastKey = key;
            }
            else if (lastKey is not null)
            {
                current[lastKey] = (current[lastKey] + " " + line).Trim();
            }
        }

        if (!head.TryGetValue("titulo", out var title) || title.Length == 0)
            return Fail("falta «titulo:» al principio.", out error);
        head.TryGetValue("resumen", out var summary);
        head.TryGetValue("icono", out var icon);
        var order = head.TryGetValue("orden", out var o) && int.TryParse(o, out var n) ? n : 1000;

        if (blocks.Count is < MinSteps or > MaxSteps)
            return Fail($"tiene {blocks.Count} pasos y tiene que tener entre {MinSteps} y {MaxSteps}.", out error);

        var steps = new List<TourStep>();
        for (var i = 0; i < blocks.Count; i++)
        {
            var b = blocks[i];
            if (!b.TryGetValue("titulo", out var stepTitle) || stepTitle.Length == 0
                || !b.TryGetValue("texto", out var body) || body.Length == 0)
                return Fail($"el paso {i + 1} necesita «titulo:» y «texto:».", out error);
            if (body.Length > WelcomeTour.MaxTextLength)
                return Fail($"el texto del paso {i + 1} tiene {body.Length} letras y el máximo es {WelcomeTour.MaxTextLength}.", out error);

            b.TryGetValue("senalar", out var target);
            b.TryGetValue("abrir", out var open);
            if (!string.IsNullOrWhiteSpace(open) && !TourActions.IsKnown(open.Trim()))
                return Fail($"el paso {i + 1} pide abrir «{open}», que el programa no conoce.", out error);
            b.TryGetValue("resaltar", out var highlight);
            b.TryGetValue("dibujo", out var picture);
            if (!string.IsNullOrWhiteSpace(picture) && !TourActions.Pictures.Contains(picture.Trim()))
                return Fail($"el paso {i + 1} pide el dibujo «{picture}», que el programa no tiene.", out error);
            steps.Add(new TourStep(
                string.IsNullOrWhiteSpace(target) ? null : target, stepTitle, body,
                string.IsNullOrWhiteSpace(open) ? null : open.Trim(),
                string.IsNullOrWhiteSpace(highlight) ? null : highlight.Trim(),
                picture is { Length: > 0 } ? picture.Trim() : null));
        }

        tutorial = new Tutorial(id, title, summary ?? string.Empty, string.IsNullOrWhiteSpace(icon) ? "📘" : icon!, order, steps);
        return true;
    }

    /// <summary>The tutorials that ship inside the program, in their order.</summary>
    public static IReadOnlyList<Tutorial> LoadBuiltIn()
    {
        var assembly = typeof(TutorialFile).Assembly;
        var found = new List<Tutorial>();
        foreach (var name in assembly.GetManifestResourceNames().Where(n => n.StartsWith(ResourcePrefix, StringComparison.Ordinal)))
        {
            using var stream = assembly.GetManifestResourceStream(name)!;
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var id = Path.GetFileNameWithoutExtension(name[ResourcePrefix.Length..]);
            if (TryParse(id, reader.ReadToEnd(), out var tutorial, out _) && tutorial is not null)
                found.Add(tutorial);
        }

        return Sort(found);
    }

    /// <summary>
    /// The built-in ones plus the <c>.txt</c> files in <paramref name="folder"/>
    /// (the church's own, or a translation): the same name replaces the
    /// built-in one. A file that does not read is left out, never an error.
    /// </summary>
    public static IReadOnlyList<Tutorial> LoadAll(string? folder)
    {
        var all = LoadBuiltIn().ToDictionary(t => t.Id, StringComparer.OrdinalIgnoreCase);
        if (folder is { Length: > 0 } && Directory.Exists(folder))
        {
            foreach (var path in Directory.EnumerateFiles(folder, "*.txt"))
            {
                try
                {
                    var id = Path.GetFileNameWithoutExtension(path);
                    if (TryParse(id, File.ReadAllText(path, Encoding.UTF8), out var tutorial, out _) && tutorial is not null)
                        all[id] = tutorial;
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }

        return Sort(all.Values);
    }

    private static List<Tutorial> Sort(IEnumerable<Tutorial> tutorials) =>
        tutorials.OrderBy(t => t.Order).ThenBy(t => t.Title, StringComparer.CurrentCultureIgnoreCase).ToList();

    private static string Normalize(string key) =>
        key.Trim().ToLowerInvariant().Replace('ñ', 'n');

    private static bool Fail(string message, out string error)
    {
        error = message;
        return false;
    }
}
