using System.Net;
using System.Text;

namespace EcclesiaCast.RenderCompare;

/// <summary>Writes <c>informe.html</c> and <c>resumen.txt</c> for one or more comparisons.</summary>
public static class Report
{
    public static void Write(string folder, IReadOnlyList<Comparison> comparisons)
    {
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "resumen.txt"), Summary(comparisons), new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(folder, "informe.html"), Html(folder, comparisons), new UTF8Encoding(false));
    }

    // ── resumen.txt ──────────────────────────────────────────────

    public static string Summary(IReadOnlyList<Comparison> comparisons)
    {
        var text = new StringBuilder();
        foreach (var c in comparisons)
        {
            text.AppendLine(c.Title);
            text.AppendLine(new string('=', c.Title.Length));
            if (c.NotRun is not null)
            {
                text.AppendLine("No se comparó. " + c.NotRun);
                text.AppendLine();
                continue;
            }

            var total = c.Results.Count;
            text.AppendLine($"Margen: letra {c.Criteria.SizeTolerance:P1}, rectángulo {c.Criteria.RectTolerance:0.##} px, renglones idénticos.");
            text.AppendLine($"Casos comparados (caso por tema): {total}");
            text.AppendLine($"Pasan en renglones:  {c.Results.Count(r => r.PassesIn(Criterion.Lines))} de {total}");
            text.AppendLine($"Pasan en tamaño:     {c.Results.Count(r => r.PassesIn(Criterion.Size))} de {total}");
            text.AppendLine($"Pasan en rectángulo: {c.Results.Count(r => r.PassesIn(Criterion.Rectangle))} de {total}");
            text.AppendLine($"Pasan en todo:       {c.Results.Count(r => r.Passes)} de {total}");
            if (c.Unpaired.Count > 0)
                text.AppendLine($"Sin pareja ({c.Unpaired.Count}): {string.Join(", ", c.Unpaired)}");
            if (c.Skipped.Count > 0)
                text.AppendLine($"No se comparan, porque no van en {c.OnlyFont} ({c.Skipped.Count}): {string.Join(", ", c.Skipped)}");

            foreach (var group in c.Results.Where(r => !r.Passes)
                         .SelectMany(r => r.Differences.Select(d => (r, d.Reason)).Distinct())
                         .GroupBy(x => x.Reason).OrderBy(g => g.Key))
            {
                text.AppendLine();
                text.AppendLine($"{group.Key.Name()}: {group.Count()}");
                foreach (var byTheme in group.GroupBy(x => x.r.Theme).OrderBy(g => g.Key))
                    text.AppendLine($"  [{byTheme.Key}] {string.Join(", ", byTheme.Select(x => x.r.Case))}");
            }
            text.AppendLine();
        }
        return text.ToString();
    }

    // ── informe.html ─────────────────────────────────────────────

    private static string Html(string folder, IReadOnlyList<Comparison> comparisons)
    {
        var html = new StringBuilder();
        html.AppendLine("<!doctype html><html lang=\"es\"><head><meta charset=\"utf-8\"><title>Informe de render</title>");
        html.AppendLine("<style>body{font-family:Segoe UI,sans-serif;margin:24px;background:#fafafa;color:#1b1b1b}"
                        + "h2{margin-top:40px}.caso{border:1px solid #ccc;background:#fff;margin:16px 0;padding:12px;border-radius:6px}"
                        + ".par{display:flex;gap:12px}.par figure{margin:0;flex:1}.par img{width:100%;border:1px solid #999;background:#000}"
                        + "figcaption{font-size:13px;color:#555}.falta{padding:40px 8px;border:1px dashed #999;color:#777;text-align:center}"
                        + "li{margin:2px 0}pre{background:#f0f0f0;padding:8px;overflow:auto}</style></head><body><h1>Informe de render</h1>");
        html.AppendLine("<pre>" + WebUtility.HtmlEncode(Summary(comparisons)) + "</pre>");

        foreach (var c in comparisons)
        {
            html.AppendLine($"<h2>{WebUtility.HtmlEncode(c.Title)}</h2>");
            if (c.NotRun is not null)
            {
                html.AppendLine($"<p>No se comparó. {WebUtility.HtmlEncode(c.NotRun)}</p>");
                continue;
            }

            var failing = c.Results.Where(r => !r.Passes).ToList();
            html.AppendLine($"<p>No pasan {failing.Count} de {c.Results.Count}.</p>");
            foreach (var r in failing)
            {
                html.AppendLine($"<div class=\"caso\"><h3>{WebUtility.HtmlEncode(r.Case)} <small>({WebUtility.HtmlEncode(r.Theme)})</small></h3><ul>");
                foreach (var d in r.Differences)
                    html.AppendLine($"<li><b>{WebUtility.HtmlEncode(d.Reason.Name())}.</b> {WebUtility.HtmlEncode(d.Words)}</li>");
                html.AppendLine("</ul><div class=\"par\">");
                html.AppendLine(Figure(folder, c.LeftName, c.LeftImages ?? c.LeftFolder, r.Key, c.LeftImageHint));
                html.AppendLine(Figure(folder, c.RightName, c.RightImages ?? c.RightFolder, r.Key, null));
                html.AppendLine("</div></div>");
            }
        }

        html.AppendLine("</body></html>");
        return html.ToString();
    }

    /// <summary>A picture linked (never copied); when it is not there, a note says so.</summary>
    private static string Figure(string reportFolder, string name, string imageFolder, string key, string? hint)
    {
        var path = Path.Combine(imageFolder, key + ".png");
        var caption = $"<figcaption>{WebUtility.HtmlEncode(name)}</figcaption>";
        if (!File.Exists(path))
        {
            var why = hint ?? "No está la imagen.";
            return $"<figure><div class=\"falta\">{WebUtility.HtmlEncode(why)}</div>{caption}</figure>";
        }

        var relative = Path.GetRelativePath(reportFolder, path).Replace('\\', '/');
        var link = string.Join('/', relative.Split('/').Select(s => s == ".." ? s : Uri.EscapeDataString(s)));
        return $"<figure><a href=\"{link}\"><img src=\"{link}\" loading=\"lazy\"></a>{caption}</figure>";
    }
}
