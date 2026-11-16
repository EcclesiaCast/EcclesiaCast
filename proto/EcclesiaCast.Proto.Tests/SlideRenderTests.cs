using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using EcclesiaCast.Core.Themes;

namespace EcclesiaCast.Proto.Tests;

/// <summary>
/// Draws a slide in memory with Skia — the same engine the Mac uses — and
/// keeps the picture (render/*.png next to the tests; GitHub uploads it).
/// </summary>
public sealed class SlideRenderTests
{
    private const string Lyrics = "Sublime gracia del Señor\nque a un pecador salvó;\nfui ciego mas hoy veo yo";

    [AvaloniaFact]
    public void Slide_draws_light_text_in_the_middle()
    {
        // Over a mid-grey «video», where outline and shadow are easy to see:
        // this is the picture to compare with Windows.
        var frame = Render(Theme(outline: 6, shadow: true), "diapositiva.png", overGrey: true);

        // The lyrics are white: plenty of light pixels around the centre.
        var light = Count(frame, 0.25, 0.75, 0.3, 0.7, (r, g, b) => r > 200 && g > 200 && b > 200);
        Assert.True(light > 2000, $"Casi no hay letra clara en el centro ({light} pixeles)");

        // And outline + shadow: a soft dark halo on a grey that has none.
        // Measured on 16/11/2026: WPF draws the same text with the same
        // effects at about 31 000 pixels per million darker than the grey by
        // more than 80; this control at about 30 000. Below 10 000 the halo
        // is gone.
        var halo = Count(frame, 0.1, 0.9, 0.2, 0.8, (r, g, b) => r + g + b < 0x7A + 0x86 + 0x96 - 80);
        Assert.True(halo > 10000, $"No se ve el contorno ni la sombra sobre el gris ({halo} pixeles oscuros)");
    }

    [AvaloniaFact]
    public void Outline_and_shadow_darken_around_the_letters()
    {
        // The empty slide is #10141E; outline and shadow are pure black, so
        // pixels darker than the background exist only where they are drawn.
        var plain = Render(Theme(outline: 0, shadow: false), "sin-efectos.png");
        var styled = Render(Theme(outline: 6, shadow: true), "contorno-y-sombra.png");

        static bool Darker(byte r, byte g, byte b) => r + g + b < 30;
        var darkPlain = Count(plain, 0.1, 0.9, 0.2, 0.8, Darker);
        var darkStyled = Count(styled, 0.1, 0.9, 0.2, 0.8, Darker);

        Assert.True(darkStyled > darkPlain + 1000,
            $"El contorno y la sombra no se ven (oscuros sin efectos {darkPlain}, con efectos {darkStyled})");
    }

    [AvaloniaFact]
    public void A_long_verse_shrinks_to_fit()
    {
        // John 3:16-17: two verses, too long for the Bible theme's 120.
        const string John316 = "Porque de tal manera amó Dios al mundo, que ha dado á su Hijo unigénito, "
            + "para que todo aquel que en él cree, no se pierda, mas tenga vida eterna. "
            + "Porque no envió Dios á su Hijo al mundo, para que condene al mundo, "
            + "mas para que el mundo sea salvo por él.";
        var theme = Theme(outline: 6, shadow: true);
        theme.MaxFontSize = 120;
        theme.MinFontSize = 36;

        var slide = new SlideView();
        slide.Show(John316, "Juan 3:16 (RV1909)", theme, overVideo: false);
        Assert.InRange(slide.FittedFontSize, 36, 119);

        // On screen, the text really fits inside the margins.
        var window = new Window { Width = 1920, Height = 1080, Content = slide };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        var text = slide.GetVisualDescendants().OfType<TextBlock>().First(t => t.Text == John316);
        Assert.True(text.Bounds.Height <= 1080 - 2 * theme.MarginVertical, $"No entra: {text.Bounds.Height}");
        var frame = window.CaptureRenderedFrame();
        frame?.Save(Path.Combine(AppContext.BaseDirectory, "render", "versiculo.png"), PngBitmapEncoderOptions.Default);
        window.Close();
    }

    private static SlideTheme Theme(double outline, bool shadow) => new()
    {
        Name = "Prueba",
        Kind = ThemeKind.Song,
        FontFamily = "Segoe UI",
        MaxFontSize = 92,
        Bold = true,
        OutlineWidth = outline,
        OutlineColor = "#000000",
        Shadow = shadow,
        ShadowBlur = 18,
        ShadowOpacity = 0.75,
    };

    private static WriteableBitmap Render(SlideTheme theme, string fileName, bool overGrey = false)
    {
        var slide = new SlideView();
        slide.Show(Lyrics, "Juan 3:16 (RV1909)", theme, overVideo: overGrey);

        var window = new Window
        {
            Width = 1280,
            Height = 720,
            Background = overGrey ? new SolidColorBrush(Color.Parse("#7A8696")) : Brushes.Black,
            Content = slide,
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();

        var frame = window.CaptureRenderedFrame() ?? throw new InvalidOperationException("No se dibujó nada");
        window.Close();

        var folder = Path.Combine(AppContext.BaseDirectory, "render");
        Directory.CreateDirectory(folder);
        frame.Save(Path.Combine(folder, fileName), PngBitmapEncoderOptions.Default);

        // Back to a WriteableBitmap to read its pixels.
        using var stream = new MemoryStream();
        frame.Save(stream, PngBitmapEncoderOptions.Default);
        stream.Position = 0;
        return WriteableBitmap.Decode(stream);
    }

    /// <summary>Counts the pixels of a region (fractions of the picture) that pass a test.</summary>
    private static unsafe int Count(WriteableBitmap bitmap, double left, double right, double top, double bottom,
        Func<byte, byte, byte, bool> test)
    {
        using var pixels = bitmap.Lock();
        var width = pixels.Size.Width;
        var height = pixels.Size.Height;
        var count = 0;
        for (var y = (int)(height * top); y < (int)(height * bottom); y++)
        {
            var row = (byte*)pixels.Address + (long)y * pixels.RowBytes;
            for (var x = (int)(width * left); x < (int)(width * right); x++)
            {
                var p = row + x * 4;
                // Bgra or Rgba: the tests only look at all three together.
                if (test(p[0], p[1], p[2]))
                    count++;
            }
        }

        return count;
    }
}
