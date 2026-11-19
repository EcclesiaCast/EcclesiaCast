using Avalonia.Headless.XUnit;
using EcclesiaCast.Desktop.Platform;
using EcclesiaCast.Presentation.Platform;
using EcclesiaCast.Presentation.Rendering;

namespace EcclesiaCast.Desktop.Tests;

public sealed class TextMetricsTests
{
    [AvaloniaFact]
    public void A_known_verse_is_cut_in_as_many_lines_as_in_wpf()
    {
        // John 3:16 in the Bible theme: WPF draws it at 120 in five lines.
        var c = RenderCases.Load("biblia-versiculo-medio");
        var theme = c.Themes.Single(t => t.Key == "biblia");
        var wpf = RenderCases.Wpf(c.Id, "biblia").Texts.Single(t => t.Name == "main");
        var metrics = AvaloniaTextMetrics.Current;

        var slide = c.BuildSlide(theme)!;
        var main = SlideTextFit.ApplyCase(slide.MainText, slide)!;
        var fit = SlideTextFit.Fit(metrics, slide, main, null, 1.0);
        var weight = theme.Theme.Bold ? SlideTextFit.BoldWeight : SlideTextFit.NormalWeight;
        var lines = metrics.Lines(main, theme.Theme.FontFamily, weight, theme.Theme.Italic, fit.FontSize, fit.Width, null);

        Assert.Equal(wpf.FontSize, fit.FontSize);
        Assert.Equal(wpf.Lines.Count, lines.Count);
    }

    [AvaloniaFact]
    public void Without_cutting_a_text_is_one_line_and_wider_when_longer()
    {
        var metrics = AvaloniaTextMetrics.Current;

        var shortOne = metrics.Measure("Aleluya", "Segoe UI", 400, false, 80, double.PositiveInfinity, null, 1);
        var longOne = metrics.Measure("Aleluya, aleluya", "Segoe UI", 400, false, 80, double.PositiveInfinity, null, 1);

        Assert.True(longOne.Width > shortOne.Width);
        Assert.Equal(shortOne.Height, longOne.Height, 3);
    }

    [AvaloniaFact]
    public void The_line_height_asked_for_is_the_height_of_each_line()
    {
        var metrics = AvaloniaTextMetrics.Current;

        var measured = metrics.Measure("uno\ndos\ntres", "Segoe UI", 600, false, 50, 1000, 100, 1);

        Assert.Equal(300, measured.Height, 1);
    }

    [AvaloniaFact]
    public void Selawik_on_a_mac_measures_like_segoe_ui_on_windows()
    {
        // Selawik was made with the measures of Segoe UI: the same verse at
        // the same size is cut in the same number of lines.
        var windows = new AvaloniaTextMetrics(new AvaloniaFontCatalog(() => ["Segoe UI"], PlatformSystem.Windows));
        var mac = new AvaloniaTextMetrics(new AvaloniaFontCatalog(() => ["Helvetica"], PlatformSystem.Mac));
        const string verse = "Porque de tal manera amó Dios al mundo, que ha dado a su Hijo unigénito, "
            + "para que todo aquel que en él cree, no se pierda, mas tenga vida eterna.";

        var onWindows = windows.Lines(verse, "Segoe UI", 600, false, 92, 1700, null);
        var onMac = mac.Lines(verse, "Segoe UI", 600, false, 92, 1700, null);

        Assert.Equal(onWindows.Count, onMac.Count);
    }
}
