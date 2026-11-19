using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Core.Themes;
using EcclesiaCast.Presentation.Platform;
using EcclesiaCast.Presentation.Rendering;

namespace EcclesiaCast.Presentation.Tests;

/// <summary>
/// The fitting of the slide text, with a made-up measure (each letter is half
/// as wide as the size), so it runs the same on Windows and on a Mac. The real
/// measures are guarded by the render benches.
/// </summary>
public class SlideTextFitTests
{
    /// <summary>
    /// Every letter is <c>size × 0.5</c> wide; lines are cut at the spaces
    /// and each one is <c>size × 1.25</c> high (or the line height asked for).
    /// </summary>
    private sealed class FakeMetrics : ITextMetrics
    {
        public List<(string Text, string Family, int Weight, bool Italic, double Size, double? LineHeight)> Calls { get; } = [];

        public static double LineWidth(string line, double size) => line.Length * size * 0.5;

        public TextMeasure Measure(string text, string fontFamily, int weight, bool italic, double fontSize,
            double maxWidth, double? lineHeight, double pixelsPerDip)
        {
            Calls.Add((text, fontFamily, weight, italic, fontSize, lineHeight));
            var lines = new List<string>();
            foreach (var written in text.Split('\n'))
            {
                var current = "";
                foreach (var word in written.Split(' '))
                {
                    var joined = current.Length == 0 ? word : current + " " + word;
                    if (current.Length > 0 && LineWidth(joined, fontSize) > maxWidth)
                    {
                        lines.Add(current);
                        current = word;
                    }
                    else
                    {
                        current = joined;
                    }
                }
                lines.Add(current);
            }

            var width = lines.Max(l => LineWidth(l, fontSize));
            return new TextMeasure(width, lines.Count * (lineHeight ?? fontSize * 1.25));
        }
    }

    private static SlideContent Slide(string text, SlideTheme? theme = null, SlideOverride? over = null,
        string? caption = null, string? secondary = null) =>
        new(text, caption, secondary, theme ?? new SlideTheme(), over);

    [Fact]
    public void A_short_text_gets_the_preferred_size_inside_the_theme_margins()
    {
        var fit = SlideTextFit.Fit(new FakeMetrics(), Slide("Hola"), "Hola", null, 1.0);

        Assert.Equal(92, fit.FontSize);
        Assert.True(fit.Readable);
        Assert.Equal(new TextMargin(110, 80, 110, 80), fit.Margin);
        Assert.Equal(1700, fit.Width);
        Assert.Equal(920, fit.Height);
    }

    [Fact]
    public void A_long_text_gets_the_biggest_size_that_fits_going_down_in_steps_of_two()
    {
        var text = string.Join(" ", Enumerable.Repeat("palabra", 120));
        var metrics = new FakeMetrics();

        var fit = SlideTextFit.Fit(metrics, Slide(text), text, null, 1.0);

        Assert.True(fit.Readable);
        Assert.Equal(0, (92 - fit.FontSize) % 2);
        Assert.True(metrics.Measure(text, "x", 400, false, fit.FontSize, 1700, null, 1).Height <= 920);
        Assert.True(metrics.Measure(text, "x", 400, false, fit.FontSize + 2, 1700, null, 1).Height > 920);
    }

    [Fact]
    public void A_text_that_does_not_fit_at_the_minimum_goes_under_it_and_is_not_readable()
    {
        var text = string.Join(" ", Enumerable.Repeat("palabra", 900));

        var fit = SlideTextFit.Fit(new FakeMetrics(), Slide(text), text, null, 1.0);

        Assert.False(fit.Readable);
        Assert.True(fit.FontSize < 36);
        Assert.True(fit.FontSize >= 8);
        Assert.False(SlideTextFit.IsReadable(new FakeMetrics(), Slide(text), text, null));
    }

    [Fact]
    public void A_caption_at_the_bottom_takes_its_band_from_the_bottom_margin()
    {
        var fit = SlideTextFit.Fit(new FakeMetrics(), Slide("Hola", caption: "Juan 3:16"), "Hola", null, 1.0);

        Assert.Equal(80 + 40 * 1.7, fit.Margin.Bottom, 6);
        Assert.Equal(80, fit.Margin.Top);
    }

    [Fact]
    public void A_caption_on_top_takes_its_band_from_the_top_margin()
    {
        var theme = new SlideTheme { CaptionPosition = CaptionPosition.TopCenter };

        var fit = SlideTextFit.Fit(new FakeMetrics(), Slide("Hola", theme, caption: "Juan 3:16"), "Hola", null, 1.0);

        Assert.Equal(80 + 40 * 1.7, fit.Margin.Top, 6);
        Assert.Equal(80, fit.Margin.Bottom);
    }

    [Fact]
    public void A_box_drawn_over_the_caption_keeps_clear_of_it()
    {
        // The caption, bottom right, is measured in plain letters: 9 × 40 × 0.5 = 180 wide, 50 high.
        var over = new SlideOverride(BoxX: 100, BoxY: 100, BoxWidth: 1720, BoxHeight: 980);
        var metrics = new FakeMetrics();

        var fit = SlideTextFit.Fit(metrics, Slide("Hola", over: over, caption: "Juan 3:16"), "Hola", null, 1.0);

        var captionTop = 1080 - 40 - 50;
        Assert.Equal(captionTop - 12 - 100, fit.Height, 6);
        var caption = Assert.Single(metrics.Calls, c => c.Text == "Juan 3:16");
        Assert.Equal(SlideTextFit.NormalWeight, caption.Weight);
        Assert.False(caption.Italic);
    }

    [Fact]
    public void The_text_is_measured_with_the_slide_typeface_and_a_bold_theme_is_semibold()
    {
        var theme = new SlideTheme { FontFamily = "Anton", Bold = true, Italic = true };
        var metrics = new FakeMetrics();

        SlideTextFit.Fit(metrics, Slide("Hola", theme), "Hola", null, 1.0);

        Assert.All(metrics.Calls, c =>
        {
            Assert.Equal("Anton", c.Family);
            Assert.Equal(600, c.Weight);
            Assert.True(c.Italic);
        });
    }

    [Fact]
    public void The_slide_line_spacing_is_measured_as_the_screen_draws_it()
    {
        var metrics = new FakeMetrics();

        var fit = SlideTextFit.Fit(metrics, Slide("Hola", over: new SlideOverride(LineSpacing: 1.5)), "Hola", null, 1.0);

        Assert.Equal(fit.FontSize * 1.5, metrics.Calls[^1].LineHeight);
    }

    [Fact]
    public void Fit_to_width_shrinks_until_the_longest_written_line_fits_unbroken()
    {
        var line = new string('a', 60);
        var theme = new SlideTheme { FitToWidth = true };

        var fit = SlideTextFit.Fit(new FakeMetrics(), Slide(line, theme), line, null, 1.0);

        // 60 letters × size × 0.5 ≤ 1700 → size ≤ 56.6; in steps of 2 from 92 that is 56.
        Assert.Equal(56, fit.FontSize);
    }

    [Fact]
    public void The_second_version_takes_its_share_of_the_height()
    {
        var text = string.Join(" ", Enumerable.Repeat("palabra", 60));

        var alone = SlideTextFit.Fit(new FakeMetrics(), Slide(text), text, null, 1.0);
        var both = SlideTextFit.Fit(new FakeMetrics(), Slide(text, secondary: text), text, text, 1.0);

        Assert.True(both.FontSize < alone.FontSize);
    }

    [Fact]
    public void Split_evenly_cuts_at_the_end_of_a_sentence()
    {
        var parts = SlideTextFit.SplitEvenly("Uno dos tres cuatro. Cinco seis siete ocho nueve.", 2);

        Assert.Equal(["Uno dos tres cuatro.", "Cinco seis siete ocho nueve."], parts);
    }

    [Fact]
    public void Split_to_fit_splits_a_long_text_until_every_part_can_be_read()
    {
        var text = string.Join(" ", Enumerable.Repeat("palabra.", 500));
        var metrics = new FakeMetrics();

        var parts = SlideTextFit.SplitToFit(metrics, Slide(text));

        Assert.True(parts.Count > 1);
        Assert.All(parts, p => Assert.True(SlideTextFit.IsReadable(metrics, Slide(p.Main), p.Main, null)));
        Assert.Equal(text.Split(' ').Length, parts.Sum(p => p.Main.Split(' ').Length));
    }

    [Fact]
    public void The_case_of_the_theme_is_applied()
    {
        Assert.Equal("SEÑOR", SlideTextFit.ApplyCase("Señor", TextCase.Upper));
        Assert.Equal("Hola. Chau", SlideTextFit.ApplyCase("hola. chau", TextCase.Sentence));
        Assert.Null(SlideTextFit.ApplyCase(null, TextCase.Upper));
    }

    [Fact]
    public void Only_the_top_positions_put_the_caption_on_top()
    {
        Assert.True(SlideTextFit.IsCaptionOnTop(CaptionPosition.TopLeft));
        Assert.True(SlideTextFit.IsCaptionOnTop(CaptionPosition.TopRight));
        Assert.False(SlideTextFit.IsCaptionOnTop(CaptionPosition.BottomCenter));
    }
}
