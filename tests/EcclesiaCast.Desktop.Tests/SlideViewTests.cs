using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.VisualTree;
using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Desktop.Controls;

namespace EcclesiaCast.Desktop.Tests;

public sealed class SlideViewTests
{
    private static T Named<T>(SlideView view, string name) where T : Control =>
        view.GetVisualDescendants().OfType<T>().Single(c => c.Name == name);

    [AvaloniaFact]
    public void A_song_is_drawn_at_the_size_and_in_the_lines_of_wpf()
    {
        var c = RenderCases.Load("cancion-4-renglones");
        var theme = c.Themes.Single(t => t.Key == "cancion");
        var wpf = RenderCases.Wpf(c.Id, "cancion").Texts.Single(t => t.Name == "main");
        var view = SlideDrawing.Slide(c, theme);

        using var drawing = new SlideDrawing(view);

        Assert.Equal(wpf.FontSize, Named<TextBlock>(view, "MainText").FontSize);
        Assert.Equal(wpf.Lines, view.MainLines());
        Assert.True(Named<TextBlock>(view, "CaptionText").IsVisible);
    }

    [AvaloniaFact]
    public void A_theme_with_bands_draws_one_band_per_line_behind_the_text()
    {
        var c = RenderCases.Load("franja-texto-negra");
        var view = SlideDrawing.Slide(c, c.Themes.Single(t => t.Key == "cancion"));

        using var drawing = new SlideDrawing(view);

        var bands = Named<Canvas>(view, "MainBands").Children.OfType<Rectangle>().ToList();
        Assert.Equal(4, bands.Count);
        Assert.All(bands, b => Assert.True(b.Width > 100 && b.Height > 50));
    }

    [AvaloniaFact]
    public void The_outline_and_the_shadow_are_effects_as_in_wpf()
    {
        var c = RenderCases.Load("efecto-contorno-y-sombra");
        var view = SlideDrawing.Slide(c, c.Themes.Single(t => t.Key == "cancion"));

        using var drawing = new SlideDrawing(view);

        var outline = Assert.IsType<DropShadowEffect>(Named<TextBlock>(view, "MainText").Effect);
        Assert.Equal(0, outline.OffsetX);
        Assert.Equal(1, outline.Opacity);
        var shadow = Assert.IsType<DropShadowDirectionEffect>(Named<Grid>(view, "MainTextShadow").Effect);
        Assert.Equal(3, shadow.ShadowDepth);

        view.LowDetail = true;
        Assert.Null(Named<TextBlock>(view, "MainText").Effect);
    }

    [AvaloniaFact]
    public void Black_covers_the_words_and_the_announcement_stays_over_it()
    {
        var view = new SlideView { Slide = new SlideContent("Hola"), State = OutputState.Black, Overlay = "Aviso" };

        using var drawing = new SlideDrawing(view);

        Assert.Equal(1, Named<Rectangle>(view, "BlackLayer").Opacity);
        Assert.True(Named<Border>(view, "OverlayLayer").IsVisible);
        Assert.False(Named<Grid>(view, "TextLayer").IsVisible);
    }

    [AvaloniaFact]
    public void A_countdown_shows_its_clock_and_goes_when_it_is_taken_off()
    {
        var view = new SlideView { Countdown = Countdown.ForMinutes(5, DateTimeOffset.Now, "Empezamos en", "¡Bienvenidos!") };

        using var drawing = new SlideDrawing(view);

        Assert.True(Named<Grid>(view, "CountdownLayer").IsVisible);
        Assert.Matches(@"^[45]:\d\d$", Named<TextBlock>(view, "CountdownClock").Text);

        view.Countdown = null;
        drawing.Settle();
        Assert.False(Named<Grid>(view, "CountdownLayer").IsVisible);
    }

    [AvaloniaFact]
    public void The_logo_state_shows_the_wordmark_when_there_is_no_logo()
    {
        var view = new SlideView { State = OutputState.Logo };

        using var drawing = new SlideDrawing(view);

        Assert.True(Named<Grid>(view, "LogoLayer").IsVisible);
        Assert.Equal("EcclesiaCast", Named<TextBlock>(view, "LogoText").Text);
    }

    [AvaloniaFact]
    public void A_fade_takes_the_whole_time_and_ends_where_it_was_going()
    {
        var border = new Border { Opacity = 0 };
        using var drawing = new SlideDrawing(border);

        var fade = Fades.Fade(border, 1, TimeSpan.FromMilliseconds(280));
        Assert.Equal(1, border.Opacity);
        Assert.False(fade.IsCompleted);

        // A second fade of the same thing takes over from the first.
        var back = Fades.Fade(border, 0, TimeSpan.Zero);
        Assert.True(back.IsCompleted);
        Assert.Equal(0, border.Opacity);
    }

    [AvaloniaFact]
    public void A_right_aligned_line_that_wraps_ends_at_the_edge_as_in_wpf()
    {
        // «el labio mío loores te » used to be a space short of the edge:
        // Avalonia aligned it with the space it wrapped at, WPF without it.
        var c = RenderCases.Load("alineacion-abajo-derecha");
        var view = SlideDrawing.Slide(c, c.Themes.Single(t => t.Key == "biblia"));
        using var drawing = new SlideDrawing(view);
        var text = Named<TextBlock>(view, "MainText");
        var wpf = RenderCases.Wpf(c.Id, "biblia").Texts.Single(t => t.Name == "main");

        Assert.Equal(wpf.Lines, SlideView.LinesOf(text));
        foreach (var line in text.TextLayout.TextLines)
            Assert.Equal(line.Width, line.WidthIncludingTrailingWhitespace, 3);
    }

    [AvaloniaFact]
    public void With_tight_line_spacing_the_last_line_is_not_cut()
    {
        // At 0,85 the letters of the last line go past the text's frame; WPF
        // draws them whole, and an Avalonia TextBlock used to cut them there.
        var c = RenderCases.Load("interlineado-chico");
        var view = SlideDrawing.Slide(c, c.Themes.Single(t => t.Key == "cancion"));
        using var drawing = new SlideDrawing(view);
        var text = Named<TextBlock>(view, "MainText");
        var frame = text.TransformToVisual(drawing.Host)!.Value;
        var bottom = (int)Math.Ceiling(new Avalonia.Point(0, text.Bounds.Height).Transform(frame).Y);
        var left = (int)new Avalonia.Point(0, 0).Transform(frame).X;
        var right = (int)new Avalonia.Point(text.Bounds.Width, 0).Transform(frame).X;

        using var picture = drawing.Capture();
        using var pixels = picture.Lock();
        var white = 0;
        for (var y = bottom + 2; y < bottom + 20; y++)
        {
            for (var x = left; x < right; x++)
            {
                var at = pixels.Address + y * pixels.RowBytes + x * 4;
                if (System.Runtime.InteropServices.Marshal.ReadByte(at) > 200
                    && System.Runtime.InteropServices.Marshal.ReadByte(at, 1) > 200
                    && System.Runtime.InteropServices.Marshal.ReadByte(at, 2) > 200)
                    white++;
            }
        }

        Assert.True(white > 200, $"Debajo del renglón hay {white} puntos de letra: lo cortó.");
    }
}
