using System.Text;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using EcclesiaCast.Desktop.Controls;

namespace EcclesiaCast.Desktop.Tests;

/// <summary>
/// Five cases of tests/casos-de-render drawn at 1920×1080 in memory, left in
/// artifacts/render/avalonia-tanda-9/ to be looked at next to the WPF ones,
/// with the lines of both side by side (lineas.txt). The whole bench, with
/// every case, is batch 12.
/// </summary>
public sealed class RenderPicturesTests
{
    public static TheoryData<string, string> Cases => new()
    {
        { "cancion-4-renglones", "cancion" },
        { "biblia-dos-versiones", "biblia" },
        { "franja-texto-negra", "cancion" },
        { "efecto-contorno-y-sombra", "cancion" },
        { "cuenta-regresiva", "cancion" },
    };

    private static readonly string[] Texts =
        ["MainText", "SecondaryText", "CaptionText", "CountdownHeading", "CountdownClock"];

    [AvaloniaTheory]
    [MemberData(nameof(Cases))]
    public void The_case_is_drawn_as_a_slide(string id, string themeKey)
    {
        var c = RenderCases.Load(id);
        var view = SlideDrawing.Slide(c, c.Themes.Single(t => t.Key == themeKey));
        var folder = RenderCases.OutputFolder("avalonia-tanda-9");

        using var drawing = new SlideDrawing(view);
        using var frame = drawing.Capture();
        frame.Save(Path.Combine(folder, $"{id}.{themeKey}.png"), PngBitmapEncoderOptions.Default);

        // Something light is drawn in the middle of the screen: the words.
        Assert.True(LightPixels(frame) > 2000, "Casi no hay letra clara en el centro.");

        File.WriteAllText(Path.Combine(folder, $"{id}.{themeKey}.lineas.txt"), Compare(view, id, themeKey), new UTF8Encoding(false));
    }

    /// <summary>The lines of each text in Avalonia next to the ones the WPF bench saved.</summary>
    private static string Compare(SlideView view, string id, string themeKey)
    {
        var wpf = RenderCases.Wpf(id, themeKey).Texts;
        var report = new StringBuilder();
        foreach (var name in Texts)
        {
            var text = view.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Name == name);
            if (!text.IsEffectivelyVisible || (string.IsNullOrEmpty(text.Text) && text.Inlines is not { Count: > 0 }))
                continue;

            var key = name switch
            {
                "MainText" => "main",
                "SecondaryText" => "secondary",
                "CaptionText" => "caption",
                "CountdownHeading" => "countdown-heading",
                _ => "countdown-clock",
            };
            var theirs = wpf.FirstOrDefault(t => t.Name == key);
            var ours = SlideView.LinesOf(text);
            report.AppendLine($"== {key}: Avalonia {text.FontSize:0.##} / WPF {theirs?.FontSize:0.##}"
                              + (theirs is not null && theirs.Lines.SequenceEqual(ours) ? " (iguales)" : " (DISTINTOS)"));
            for (var i = 0; i < Math.Max(ours.Count, theirs?.Lines.Count ?? 0); i++)
                report.AppendLine($"  {(i < ours.Count ? ours[i] : ""),-50} | {(theirs is not null && i < theirs.Lines.Count ? theirs.Lines[i] : "")}");
        }
        return report.ToString();
    }

    private static unsafe int LightPixels(WriteableBitmap frame)
    {
        using var buffer = frame.Lock();
        var count = 0;
        for (var y = buffer.Size.Height / 4; y < buffer.Size.Height * 3 / 4; y++)
        {
            var row = (byte*)buffer.Address + y * buffer.RowBytes;
            for (var x = buffer.Size.Width / 8; x < buffer.Size.Width * 7 / 8; x++)
            {
                var p = row + x * 4;
                if (p[0] > 200 && p[1] > 200 && p[2] > 200)
                    count++;
            }
        }
        return count;
    }
}
