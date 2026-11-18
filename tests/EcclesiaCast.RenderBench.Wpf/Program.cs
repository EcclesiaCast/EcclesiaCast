using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using EcclesiaCast.App.Controls;
using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Presentation.Rendering;

namespace EcclesiaCast.RenderBench.Wpf;

/// <summary>
/// The WPF render bench. It draws every case of tests/casos-de-render in each
/// of its three themes at 1920×1080, without showing a window, and leaves in
/// artifacts/render/wpf the picture (case.theme.png) and the measures
/// (case.theme.json: size of the letters, text of each line as it was cut,
/// rectangle of the block, typeface really used). There is no console, so the
/// outcome is written to resultado.txt; the exit code is 0 when everything was
/// drawn.
///
/// Usage: RenderBench.Wpf [folder of the results] [part of a case id]
/// </summary>
public static class Program
{
    private const int Width = 1920;
    private const int Height = 1080;

    [STAThread]
    public static int Main(string[] args)
    {
        var started = Stopwatch.StartNew();
        var report = new StringBuilder();
        var failures = new List<string>();
        var drawn = 0;
        string? outFolder = null;

        try
        {
            var casesFolder = RenderCase.FindFolder(AppContext.BaseDirectory)
                ?? throw new DirectoryNotFoundException("tests/casos-de-render was not found above " + AppContext.BaseDirectory);
            var root = Directory.GetParent(Directory.GetParent(casesFolder)!.FullName)!.FullName;

            outFolder = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0])
                ? Path.GetFullPath(args[0])
                : Path.Combine(root, "artifacts", "render", "wpf");
            var filter = args.Length > 1 ? args[1] : null;

            Directory.CreateDirectory(outFolder);
            foreach (var old in Directory.GetFiles(outFolder, "*.png").Concat(Directory.GetFiles(outFolder, "*.json")))
                File.Delete(old);

            // An Application object (not the program's own App) is what registers
            // the pack:// addresses the controls load their XAML from. Nothing in
            // the slide and stage controls reads the resources of App.xaml.
            _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };

            var cases = RenderCase.LoadAll(casesFolder)
                .Where(c => filter is null || c.Id.Contains(filter, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var slowest = ("", 0d);
            foreach (var c in cases)
            {
                foreach (var theme in c.Themes)
                {
                    var key = RenderResult.FileKey(c.Id, theme.Key);
                    var one = Stopwatch.StartNew();
                    try
                    {
                        var (bitmap, result) = Draw(c, theme);
                        Save(bitmap, Path.Combine(outFolder, key + ".png"));
                        File.WriteAllText(Path.Combine(outFolder, key + ".json"), result.ToJson(), new UTF8Encoding(false));
                        drawn++;
                    }
                    catch (Exception ex)
                    {
                        failures.Add($"{key}: {ex.GetType().Name}: {ex.Message}");
                    }

                    if (one.Elapsed.TotalMilliseconds > slowest.Item2)
                        slowest = (key, one.Elapsed.TotalMilliseconds);
                }
            }

            report.AppendLine($"Casos: {cases.Count}");
            report.AppendLine($"Dibujados: {drawn} (casos por temas)");
            report.AppendLine($"Fallaron: {failures.Count}");
            report.AppendLine($"Tiempo total: {started.Elapsed.TotalSeconds:0.0} s");
            if (drawn > 0)
                report.AppendLine($"Promedio por imagen: {started.Elapsed.TotalMilliseconds / drawn:0} ms");
            report.AppendLine($"La más lenta: {slowest.Item1} ({slowest.Item2:0} ms)");
            if (cases.Count == 0)
                failures.Add("No hay casos para dibujar.");
        }
        catch (Exception ex)
        {
            failures.Add(ex.ToString());
        }

        foreach (var failure in failures)
            report.AppendLine("ERROR " + failure);
        report.Insert(0, failures.Count == 0 ? "OK\n" : "FALLÓ\n");

        try
        {
            outFolder ??= Path.Combine(AppContext.BaseDirectory, "render");
            Directory.CreateDirectory(outFolder);
            File.WriteAllText(Path.Combine(outFolder, "resultado.txt"), report.ToString(), new UTF8Encoding(false));
        }
        catch
        {
            // Nowhere to say it: the exit code still tells.
        }

        return failures.Count == 0 ? 0 : 1;
    }

    // ── Drawing ──────────────────────────────────────────────────

    private static (BitmapSource Picture, RenderResult Result) Draw(RenderCase c, RenderTheme theme)
    {
        // Black behind everything, as the projector has when no video is there.
        var host = new Grid { Width = Width, Height = Height, Background = Brushes.Black, ClipToBounds = true };
        var result = new RenderResult { Case = c.Id, Theme = theme.Key, Width = Width, Height = Height };

        if (c.StageBox is not null)
            DrawStageBox(c, theme, host, result);
        else
            DrawSlide(c, theme, host, result);

        host.Measure(new Size(Width, Height));
        host.Arrange(new Rect(0, 0, Width, Height));
        host.UpdateLayout();
        // The bands and the like are drawn when the sizes change.
        host.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { }));
        host.UpdateLayout();

        CollectTexts(c, host, result);

        var bitmap = new RenderTargetBitmap(Width, Height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(host);
        bitmap.Freeze();
        return (bitmap, result);
    }

    private static SlideView? _lastView;
    private static StageBoxView? _lastStage;

    private static void DrawSlide(RenderCase c, RenderTheme theme, Grid host, RenderResult result)
    {
        var view = new SlideView { AnimateTransitions = false };
        if (c.Overlay is { } overlay)
        {
            view.OverlayFontFamily = c.OverlayFont(theme);
            view.OverlayFontSize = overlay.FontSize;
            view.Overlay = overlay.Text;
        }

        view.Slide = c.BuildSlide(theme);
        view.Countdown = c.BuildCountdown(DateTimeOffset.Now);
        if (c.BuildLogo(theme) is { } logo)
            view.Logo = logo;
        view.State = c.State;

        host.Children.Add(view);
        _lastView = view;
        _lastStage = null;
    }

    private static void DrawStageBox(RenderCase c, RenderTheme theme, Grid host, RenderResult result)
    {
        var box = c.BuildStageBox(theme)!;
        var canvas = new Canvas { Width = Width, Height = Height };
        var view = new StageBoxView(box, null);
        canvas.Children.Add(view);
        host.Children.Add(canvas);
        _lastView = null;
        _lastStage = view;
    }

    // ── Measuring ────────────────────────────────────────────────

    private static void CollectTexts(RenderCase c, Grid host, RenderResult result)
    {
        if (_lastStage is { } stage)
        {
            if (stage.Child is Viewbox { Child: TextBlock text })
                Add(result, host, "stage", text);
            return;
        }

        var view = _lastView!;
        Named(view, result, host, "main", "MainText");
        Named(view, result, host, "secondary", "SecondaryText");
        Named(view, result, host, "caption", "CaptionText");
        Named(view, result, host, "overlay", "OverlayText");
        Named(view, result, host, "countdown-heading", "CountdownHeading");
        Named(view, result, host, "countdown-clock", "CountdownClock");
        Named(view, result, host, "logo", "LogoText");

        if (view.FindName("ExtraBoxLayer") is Canvas extras)
        {
            var n = 1;
            foreach (var holder in extras.Children.OfType<Grid>())
            {
                if (holder.Children.OfType<TextBlock>().FirstOrDefault() is { } box)
                    Add(result, host, "extra" + n++, box);
            }
        }
    }

    private static void Named(SlideView view, RenderResult result, Grid host, string name, string element)
    {
        if (view.FindName(element) is TextBlock text)
            Add(result, host, name, text);
    }

    /// <summary>Records a text that is on the screen; the ones hidden or empty are left out.</summary>
    private static void Add(RenderResult result, Grid host, string name, TextBlock text)
    {
        if (string.IsNullOrEmpty(new TextRange(text.ContentStart, text.ContentEnd).Text) || !IsShown(text))
            return;

        var rect = text.TransformToVisual(host).TransformBounds(new Rect(0, 0, text.ActualWidth, text.ActualHeight));
        var scale = text.ActualWidth > 0 ? rect.Width / text.ActualWidth : 1;

        result.Texts.Add(new RenderedText
        {
            Name = name,
            FontSize = Math.Round(text.FontSize * scale, 3),
            FontUsed = FontUsed(text),
            Lines = LinesOf(text),
            Rect = new RenderRect
            {
                X = Math.Round(rect.X, 2),
                Y = Math.Round(rect.Y, 2),
                Width = Math.Round(rect.Width, 2),
                Height = Math.Round(rect.Height, 2),
            },
        });
    }

    /// <summary>False when the text, or something that holds it, is collapsed or hidden.</summary>
    private static bool IsShown(DependencyObject element)
    {
        for (var node = element; node is not null; node = VisualTreeHelper.GetParent(node))
        {
            if (node is UIElement ui && ui.Visibility != Visibility.Visible)
                return false;
        }
        return true;
    }

    private static string FontUsed(TextBlock text)
    {
        var face = new Typeface(text.FontFamily, text.FontStyle, text.FontWeight, text.FontStretch);
        if (!face.TryGetGlyphTypeface(out var glyphs))
            return "(ninguna)";

        var english = CultureInfo.GetCultureInfo("en-US");
        var family = glyphs.FamilyNames.TryGetValue(english, out var f) ? f : glyphs.FamilyNames.Values.FirstOrDefault();
        var style = glyphs.FaceNames.TryGetValue(english, out var s) ? s : glyphs.FaceNames.Values.FirstOrDefault();
        return $"{family} {style}".Trim();
    }

    /// <summary>
    /// The lines as they were cut: every letter is asked where it is, and the
    /// letters are gathered by the height they sit at. A line break written in
    /// the text ends a line even when it is empty.
    /// </summary>
    private static List<string> LinesOf(TextBlock text)
    {
        var lines = new List<string>();
        var current = new StringBuilder();
        double? top = null;
        var pointer = text.ContentStart;
        var end = text.ContentEnd;

        while (pointer is not null && pointer.CompareTo(end) < 0)
        {
            if (pointer.GetPointerContext(LogicalDirection.Forward) != TextPointerContext.Text)
            {
                pointer = pointer.GetNextContextPosition(LogicalDirection.Forward);
                continue;
            }

            var run = pointer.GetTextInRun(LogicalDirection.Forward);
            for (var i = 0; i < run.Length; i++)
            {
                var ch = run[i];
                if (ch is '\r')
                    continue;
                if (ch is '\n')
                {
                    lines.Add(current.ToString().Trim());
                    current.Clear();
                    top = null;
                    continue;
                }

                var start = pointer.GetPositionAtOffset(i, LogicalDirection.Forward);
                var rect = start?.GetCharacterRect(LogicalDirection.Forward) ?? Rect.Empty;
                if (!rect.IsEmpty)
                {
                    // A new line starts when the letter is lower by more than half a line.
                    if (top is double previous && rect.Top - previous > rect.Height * 0.5)
                    {
                        lines.Add(current.ToString().Trim());
                        current.Clear();
                        top = rect.Top;
                    }
                    top ??= rect.Top;
                }

                current.Append(ch);
            }

            pointer = pointer.GetPositionAtOffset(run.Length, LogicalDirection.Forward)
                      ?? pointer.GetNextContextPosition(LogicalDirection.Forward);
        }

        if (current.Length > 0)
            lines.Add(current.ToString().Trim());
        return lines;
    }

    private static void Save(BitmapSource picture, string path)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(picture));
        using var file = File.Create(path);
        encoder.Save(file);
    }
}
