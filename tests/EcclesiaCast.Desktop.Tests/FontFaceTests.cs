using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using EcclesiaCast.Desktop.Platform;
using EcclesiaCast.Presentation.Rendering;

namespace EcclesiaCast.Desktop.Tests;

public sealed class FontFaceTests
{
    private static GlyphTypeface Drawn(TextBlock text)
    {
        text.Measure(Avalonia.Size.Infinity);
        return text.TextLayout.TextLines[0].TextRuns.OfType<ShapedTextRun>().First().GlyphRun.GlyphTypeface;
    }

    [AvaloniaFact]
    public void Medium_is_semibold_even_after_the_regular_face_was_drawn()
    {
        // Avalonia alone answers Medium with whichever face it loaded first:
        // after a Regular it drew the notice at the foot in Regular.
        var catalog = AvaloniaFontCatalog.Current;
        Drawn(new TextBlock { Text = "Hoy", FontFamily = catalog.Resolve("Segoe UI"), FontSize = 54 });

        var notice = new TextBlock { Text = "Hoy", FontSize = 54 };
        catalog.Face("Segoe UI", 500, false).ApplyTo(notice);

        Assert.Equal(FontWeight.SemiBold, Drawn(notice).Weight);
    }

    [AvaloniaFact]
    public void Impact_in_bold_is_thickened_and_widened_as_in_wpf()
    {
        // Impact (Anton on a Mac) has no bold: WPF thickens it and adds 2 % of
        // the size after each letter.
        var catalog = AvaloniaFontCatalog.Current;
        var face = catalog.Face("Impact", SlideTextFit.BoldWeight, false);
        var logo = new TextBlock { Text = "Vida", FontSize = 100 };
        face.ApplyTo(logo);

        Assert.True(face.SimulatedBold);
        Assert.Equal(FontSimulations.Bold, Drawn(logo).FontSimulations | Drawn(logo).PlatformTypeface.FontSimulations);
        Assert.Equal(2, logo.LetterSpacing, 3);

        logo.FontSize = 50;
        Assert.Equal(1, logo.LetterSpacing, 3);
    }

    [AvaloniaFact]
    public void The_measure_of_a_thickened_text_counts_the_wider_letters()
    {
        var metrics = AvaloniaTextMetrics.Current;

        var regular = metrics.Measure("Vida", "Impact", SlideTextFit.NormalWeight, false, 100, double.PositiveInfinity, null, 1);
        var bold = metrics.Measure("Vida", "Impact", SlideTextFit.BoldWeight, false, 100, double.PositiveInfinity, null, 1);

        Assert.Equal(regular.Width + 4 * 2, bold.Width, 1);
    }

    [AvaloniaFact]
    public void A_font_with_a_real_bold_is_not_thickened()
    {
        var face = AvaloniaFontCatalog.Current.Face("Segoe UI", SlideTextFit.BoldWeight, false);

        Assert.False(face.SimulatedBold);
        Assert.Equal(FontWeight.SemiBold, face.Weight);
    }
}
