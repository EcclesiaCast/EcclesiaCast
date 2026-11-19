using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.TextFormatting;
using Avalonia.VisualTree;
using EcclesiaCast.Core.Themes;
using EcclesiaCast.Desktop.Controls;
using EcclesiaCast.Presentation.Rendering;
using EcclesiaCast.RenderCompare;

namespace EcclesiaCast.Desktop.Tests;

/// <summary>
/// The Avalonia render bench: draws every case of tests/casos-de-render, in
/// its three themes and in a fourth one that only this bench has (<c>selawik</c>,
/// the song theme with the typeface that travels with the program), at
/// 1920×1080 in memory, and leaves picture and measures (the same
/// <see cref="RenderResult"/> as the WPF bench) in
/// artifacts/render/avalonia/&lt;windows|mac&gt;/. It fails only when a case
/// could not be drawn: the differences are the comparator's business.
/// </summary>
public sealed class RenderBenchTests
{
    /// <summary>The theme that exists only in this bench: songs, in Selawik.</summary>
    public const string SelawikKey = "selawik";

    /// <summary>Folder name of this computer's results: windows, mac or linux.</summary>
    public static string System =>
        OperatingSystem.IsMacOS() ? "mac" : OperatingSystem.IsWindows() ? "windows" : "linux";

    // The bench is drawn once per test run, by whichever test needs it first.
    private static readonly object Gate = new();
    private static (int Cases, List<string> Failures)? _ran;

    [AvaloniaFact]
    public void Every_case_is_drawn_in_every_theme()
    {
        var (cases, failures) = RunBench();
        Assert.True(cases >= 60, "Faltan casos de render.");
        Assert.True(failures.Count == 0, "No se pudieron dibujar:\n" + string.Join("\n", failures));
    }

    /// <summary>
    /// The comparator reads what the bench left and writes informe.html and
    /// resumen.txt in artifacts/render/comparacion/: WPF against Avalonia on
    /// Windows, and, when the results of a Mac are there, Windows against Mac in
    /// the selawik theme. It never fails over a difference: it reports them.
    /// </summary>
    [AvaloniaFact]
    public void The_comparator_writes_its_report()
    {
        RunBench();
        var root = Path.GetDirectoryName(RenderCases.OutputFolder("avalonia"))!;
        var avalonia = Path.Combine(root, "avalonia");

        var comparisons = new List<Comparison>
        {
            Comparison.Run(new Comparison
            {
                Title = "WPF contra Avalonia en Windows (canciones, Biblia e impacto)",
                LeftName = "WPF",
                RightName = "Avalonia",
                LeftFolder = Path.Combine(RenderCases.Folder, "referencia-wpf"),
                LeftImages = Path.Combine(root, "wpf"),
                LeftImageHint = "Correr el banco de WPF para ver la imagen.",
                RightFolder = Path.Combine(avalonia, "windows"),
                Criteria = Criteria.Loose,
                Themes = ["cancion", "biblia", "impact"],
            }),
            Comparison.Run(new Comparison
            {
                Title = "Avalonia en Windows contra Avalonia en Mac (tema selawik, tiene que ser idéntico)",
                LeftName = "Windows",
                RightName = "Mac",
                LeftFolder = Path.Combine(avalonia, "windows"),
                RightFolder = Path.Combine(avalonia, "mac"),
                Criteria = Criteria.Exact,
                Themes = [SelawikKey],
            }),
        };

        var output = Path.Combine(root, "comparacion");
        Report.Write(output, comparisons);

        Assert.True(File.Exists(Path.Combine(output, "informe.html")));
        Assert.True(File.Exists(Path.Combine(output, "resumen.txt")));
        Assert.True(comparisons[0].NotRun is null, comparisons[0].NotRun);
        Assert.NotEmpty(comparisons[0].Results);
    }

    private static (int Cases, List<string> Failures) RunBench()
    {
        lock (Gate)
            return _ran ??= Draw();
    }

    private static (int Cases, List<string> Failures) Draw()
    {
        var folder = Path.Combine(RenderCases.OutputFolder("avalonia"), System);
        Directory.CreateDirectory(folder);
        foreach (var old in Directory.GetFiles(folder, "*.png").Concat(Directory.GetFiles(folder, "*.json")))
            File.Delete(old);

        var cases = RenderCase.LoadAll(RenderCases.Folder);
        var failures = new List<string>();
        var drawn = 0;

        foreach (var c in cases)
        {
            foreach (var theme in ThemesOf(c))
            {
                var key = RenderResult.FileKey(c.Id, theme.Key);
                try
                {
                    DrawOne(c, theme, folder, key);
                    drawn++;
                }
                catch (Exception ex)
                {
                    failures.Add($"{key}: {ex.GetType().Name}: {ex.Message}");
                }
            }
        }

        File.WriteAllText(Path.Combine(folder, "resultado.txt"),
            $"Casos: {cases.Count}\nDibujados: {drawn}\nFallaron: {failures.Count}\n" + string.Join("\n", failures.Select(f => "ERROR " + f)) + "\n",
            new UTF8Encoding(false));

        return (cases.Count, failures);
    }

    /// <summary>The themes of the case plus <c>selawik</c>, a copy of the song one with that typeface.</summary>
    private static IEnumerable<RenderTheme> ThemesOf(RenderCase c)
    {
        foreach (var theme in c.Themes)
            yield return theme;

        var song = c.Themes.FirstOrDefault(t => t.Key == "cancion") ?? c.Themes[0];
        var copy = song.Theme.Clone();
        copy.FontFamily = "Selawik";
        yield return new RenderTheme { Key = SelawikKey, Theme = copy };
    }

    private static void DrawOne(RenderCase c, RenderTheme theme, string folder, string key)
    {
        var result = new RenderResult
        {
            Case = c.Id,
            Theme = theme.Key,
            Width = SlideDrawing.Width,
            Height = SlideDrawing.Height,
        };

        Control content;
        SlideView? slide = null;
        StageBoxView? stage = null;
        if (c.StageBox is not null)
        {
            stage = new StageBoxView(c.BuildStageBox(theme)!, null);
            var canvas = new Canvas { Width = SlideDrawing.Width, Height = SlideDrawing.Height };
            canvas.Children.Add(stage);
            content = canvas;
        }
        else
        {
            slide = SlideDrawing.Slide(c, theme);
            content = slide;
        }

        using var drawing = new SlideDrawing(content);
        drawing.Settle();

        if (stage?.Text is { } stageText)
            Add(result, drawing.Host, "stage", stageText);
        if (slide is not null)
        {
            Named(slide, result, drawing.Host, "main", "MainText");
            Named(slide, result, drawing.Host, "secondary", "SecondaryText");
            Named(slide, result, drawing.Host, "caption", "CaptionText");
            Named(slide, result, drawing.Host, "overlay", "OverlayText");
            Named(slide, result, drawing.Host, "countdown-heading", "CountdownHeading");
            Named(slide, result, drawing.Host, "countdown-clock", "CountdownClock");
            Named(slide, result, drawing.Host, "logo", "LogoText");

            var n = 1;
            foreach (var holder in slide.GetVisualDescendants().OfType<Canvas>().FirstOrDefault(x => x.Name == "ExtraBoxLayer")?.Children.OfType<Grid>() ?? [])
            {
                if (holder.Children.OfType<TextBlock>().FirstOrDefault() is { } box)
                    Add(result, drawing.Host, "extra" + n++, box);
            }
        }

        drawing.Save(folder, key);
        File.WriteAllText(Path.Combine(folder, key + ".json"), result.ToJson(), new UTF8Encoding(false));
    }

    private static void Named(SlideView view, RenderResult result, Grid host, string name, string element)
    {
        if (view.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault(t => t.Name == element) is { } text)
            Add(result, host, name, text);
    }

    /// <summary>Records a text that is on the screen; the ones hidden or empty are left out.</summary>
    private static void Add(RenderResult result, Grid host, string name, TextBlock text)
    {
        var empty = string.IsNullOrEmpty(text.Text) && text.Inlines is not { Count: > 0 };
        if (empty || !text.IsEffectivelyVisible)
            return;

        var matrix = text.TransformToVisual(host) ?? Matrix.Identity;
        var rect = new Rect(0, 0, text.Bounds.Width, text.Bounds.Height).TransformToAABB(matrix);
        var scale = text.Bounds.Width > 0 ? rect.Width / text.Bounds.Width : 1;

        result.Texts.Add(new RenderedText
        {
            Name = name,
            FontSize = Math.Round(text.FontSize * scale, 3),
            FontUsed = FontUsed(text),
            Lines = [.. SlideView.LinesOf(text)],
            Rect = new RenderRect
            {
                X = Math.Round(rect.X, 2),
                Y = Math.Round(rect.Y, 2),
                Width = Math.Round(rect.Width, 2),
                Height = Math.Round(rect.Height, 2),
            },
        });
    }

    /// <summary>The typeface the letters were really drawn with (not the asked one), from the shaped runs.</summary>
    private static string FontUsed(TextBlock text)
    {
        var names = new List<string>();
        foreach (var line in text.TextLayout.TextLines)
        {
            foreach (var run in line.TextRuns.OfType<ShapedTextRun>())
            {
                var face = run.GlyphRun.GlyphTypeface;
                // The family name of the file already says the weight (Selawik Semibold).
                var name = $"{face.FamilyName}{(face.Style == FontStyle.Normal ? "" : " " + face.Style)}";
                if (!names.Contains(name))
                    names.Add(name);
            }
        }
        return names.Count == 0 ? "(ninguna)" : string.Join(" + ", names);
    }
}
