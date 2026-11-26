using EcclesiaCast.Presentation.Rendering;

namespace EcclesiaCast.RenderCompare;

/// <summary>What is compared, and how close it has to be.</summary>
public sealed record Criteria(double SizeTolerance, double RectTolerance)
{
    /// <summary>Section 7.2: size of the letters within 2 %, block within 1 % of the screen (19 px).</summary>
    public static Criteria Loose { get; } = new(0.02, 19);

    /// <summary>Windows against Mac with the typeface of the program: the same, exactly.</summary>
    public static Criteria Exact { get; } = new(0.0001, 0.01);
}

public enum Reason
{
    /// <summary>Same words, but a line is cut somewhere else.</summary>
    CutsElsewhere,

    /// <summary>Not the same number of lines (and the words are the same).</summary>
    LineCount,

    /// <summary>A text is drawn on one side and not on the other, or the words differ.</summary>
    MissingText,

    FontSize,
    Rectangle,
}

public static class ReasonNames
{
    public static string Name(this Reason reason) => reason switch
    {
        Reason.CutsElsewhere => "Un renglón corta en otra palabra",
        Reason.LineCount => "Distinta cantidad de renglones",
        Reason.MissingText => "Un texto falta de un lado o dice otra cosa",
        Reason.FontSize => "Tamaño de letra fuera del margen",
        Reason.Rectangle => "Rectángulo del texto fuera del margen",
        _ => reason.ToString(),
    };

    /// <summary>The criterion a reason belongs to: lines, size or rectangle.</summary>
    public static Criterion Kind(this Reason reason) => reason switch
    {
        Reason.FontSize => Criterion.Size,
        Reason.Rectangle => Criterion.Rectangle,
        _ => Criterion.Lines,
    };
}

public enum Criterion { Lines, Size, Rectangle }

/// <summary>One difference found in one text of one case.</summary>
public sealed record Difference(Reason Reason, string Text, string Words);

/// <summary>The result for one case in one theme.</summary>
public sealed class CaseResult
{
    public required string Case { get; init; }
    public required string Theme { get; init; }
    public string Key => RenderResult.FileKey(Case, Theme);
    public List<Difference> Differences { get; } = [];
    public bool Passes => Differences.Count == 0;
    public bool PassesIn(Criterion criterion) => Differences.All(d => d.Reason.Kind() != criterion);
}

/// <summary>One comparison: two folders of results, with names for each side.</summary>
public sealed class Comparison
{
    public required string Title { get; init; }
    public required string LeftName { get; init; }
    public required string RightName { get; init; }
    public required string LeftFolder { get; init; }
    public required string RightFolder { get; init; }
    public required Criteria Criteria { get; init; }

    /// <summary>Where the pictures of each side are (<c>case.theme.png</c>), when they are not next to the measures.</summary>
    public string? LeftImages { get; init; }
    public string? RightImages { get; init; }

    /// <summary>What to tell when the picture of the left side is not there.</summary>
    public string? LeftImageHint { get; init; }

    /// <summary>Which themes are compared; null for all of them.</summary>
    public string[]? Themes { get; init; }

    /// <summary>
    /// When set, only the texts drawn with this typeface on the left side are
    /// compared (Windows against Mac: the countdown is always Segoe UI, which
    /// a Mac does not have). The others are counted in <see cref="Skipped"/>.
    /// </summary>
    public string? OnlyFont { get; init; }

    /// <summary>The texts left out by <see cref="OnlyFont"/>, as «case.theme: text».</summary>
    public List<string> Skipped { get; } = [];

    /// <summary>Why nothing was compared (a folder is missing), or null.</summary>
    public string? NotRun { get; set; }

    public List<CaseResult> Results { get; } = [];

    /// <summary>Case-and-theme that one side has and the other does not.</summary>
    public List<string> Unpaired { get; } = [];

    public static Comparison Run(Comparison setup)
    {
        foreach (var (name, folder) in new[] { (setup.LeftName, setup.LeftFolder), (setup.RightName, setup.RightFolder) })
        {
            if (!Directory.Exists(folder) || Directory.GetFiles(folder, "*.json").Length == 0)
            {
                setup.NotRun = $"No hay resultados de «{name}» en {folder}.";
                return setup;
            }
        }

        var left = Load(setup.LeftFolder, setup.Themes);
        var right = Load(setup.RightFolder, setup.Themes);

        foreach (var key in left.Keys.Union(right.Keys).Order(StringComparer.Ordinal))
        {
            if (!left.TryGetValue(key, out var a) || !right.TryGetValue(key, out var b))
            {
                setup.Unpaired.Add($"{key} (sólo en {(left.ContainsKey(key) ? setup.LeftName : setup.RightName)})");
                continue;
            }

            var result = new CaseResult { Case = a.Case, Theme = a.Theme };
            CompareOne(a, b, setup, result);
            setup.Results.Add(result);
        }
        return setup;
    }

    private static Dictionary<string, RenderResult> Load(string folder, string[]? themes) =>
        Directory.GetFiles(folder, "*.json")
            .Select(f => RenderResult.Load(f))
            .Where(r => themes is null || themes.Contains(r.Theme))
            .ToDictionary(r => RenderResult.FileKey(r.Case, r.Theme));

    private static void CompareOne(RenderResult left, RenderResult right, Comparison setup, CaseResult result)
    {
        var criteria = setup.Criteria;
        var skipped = left.Texts
            .Where(a => setup.OnlyFont is { } font && !a.FontUsed.StartsWith(font, StringComparison.OrdinalIgnoreCase))
            .Select(a => a.Name)
            .ToHashSet();
        setup.Skipped.AddRange(skipped.Select(name => $"{result.Key}: {name}"));

        foreach (var a in left.Texts.Where(t => !skipped.Contains(t.Name)))
        {
            var b = right.Texts.FirstOrDefault(t => t.Name == a.Name);
            if (b is null)
            {
                result.Differences.Add(new Difference(Reason.MissingText, a.Name,
                    $"«{a.Name}» sólo se dibuja en {setup.LeftName}."));
                continue;
            }

            CompareLines(a, b, setup, result);

            if (Relative(a.FontSize, b.FontSize) > criteria.SizeTolerance)
            {
                result.Differences.Add(new Difference(Reason.FontSize, a.Name,
                    $"«{a.Name}»: letra de {a.FontSize:0.##} en {setup.LeftName} y de {b.FontSize:0.##} en {setup.RightName} ({Relative(a.FontSize, b.FontSize):P1} de diferencia)."));
            }

            var far = Math.Max(
                Math.Max(Math.Abs(a.Rect.X - b.Rect.X), Math.Abs(a.Rect.Y - b.Rect.Y)),
                Math.Max(Math.Abs(a.Rect.Width - b.Rect.Width), Math.Abs(a.Rect.Height - b.Rect.Height)));
            if (far > criteria.RectTolerance)
            {
                result.Differences.Add(new Difference(Reason.Rectangle, a.Name,
                    $"«{a.Name}»: el bloque está en ({a.Rect.X:0}, {a.Rect.Y:0}) de {a.Rect.Width:0}×{a.Rect.Height:0} en {setup.LeftName} y en ({b.Rect.X:0}, {b.Rect.Y:0}) de {b.Rect.Width:0}×{b.Rect.Height:0} en {setup.RightName}: {far:0} px de diferencia."));
            }
        }

        foreach (var b in right.Texts.Where(t => left.Texts.All(a => a.Name != t.Name) && !skipped.Contains(t.Name)))
        {
            result.Differences.Add(new Difference(Reason.MissingText, b.Name,
                $"«{b.Name}» sólo se dibuja en {setup.RightName}."));
        }
    }

    private static void CompareLines(RenderedText a, RenderedText b, Comparison setup, CaseResult result)
    {
        if (a.Lines.SequenceEqual(b.Lines))
            return;

        var same = Words(a.Lines).SequenceEqual(Words(b.Lines));
        if (!same)
        {
            result.Differences.Add(new Difference(Reason.MissingText, a.Name,
                $"«{a.Name}» dice otra cosa: {setup.LeftName} «{Join(a.Lines)}» y {setup.RightName} «{Join(b.Lines)}»."));
            return;
        }

        if (a.Lines.Count != b.Lines.Count)
        {
            result.Differences.Add(new Difference(Reason.LineCount, a.Name,
                $"«{a.Name}»: {a.Lines.Count} renglones en {setup.LeftName} y {b.Lines.Count} en {setup.RightName}. {FirstCut(a, b, setup)}"));
            return;
        }

        result.Differences.Add(new Difference(Reason.CutsElsewhere, a.Name, $"«{a.Name}»: {FirstCut(a, b, setup)}"));
    }

    /// <summary>Where the first line that differs ends on each side.</summary>
    private static string FirstCut(RenderedText a, RenderedText b, Comparison setup)
    {
        for (var i = 0; i < Math.Max(a.Lines.Count, b.Lines.Count); i++)
        {
            var x = i < a.Lines.Count ? a.Lines[i] : null;
            var y = i < b.Lines.Count ? b.Lines[i] : null;
            if (x == y)
                continue;

            return $"el renglón {i + 1} corta en «{LastWord(x)}» en {setup.LeftName} y en «{LastWord(y)}» en {setup.RightName}.";
        }
        return string.Empty;
    }

    private static string LastWord(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return line is null ? "(no hay renglón)" : "(renglón vacío)";
        var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words[^1];
    }

    private static string[] Words(List<string> lines) =>
        string.Join(' ', lines).Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries);

    private static string Join(List<string> lines) => string.Join(" / ", lines);

    private static double Relative(double a, double b)
    {
        var big = Math.Max(Math.Abs(a), Math.Abs(b));
        return big == 0 ? 0 : Math.Abs(a - b) / big;
    }
}
