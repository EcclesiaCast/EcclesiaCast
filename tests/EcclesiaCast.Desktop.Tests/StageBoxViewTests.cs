using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using EcclesiaCast.Core.Stage;
using EcclesiaCast.Desktop.Controls;

namespace EcclesiaCast.Desktop.Tests;

public sealed class StageBoxViewTests
{
    private static (StageBoxView View, SlideDrawing Drawing) Draw(StageBox box)
    {
        var view = new StageBoxView(box, null);
        var canvas = new Canvas { Width = SlideDrawing.Width, Height = SlideDrawing.Height };
        canvas.Children.Add(view);
        return (view, new SlideDrawing(canvas));
    }

    [AvaloniaTheory]
    [InlineData("escenario-letra")]
    [InlineData("escenario-texto-largo")]
    public void A_stage_box_is_cut_in_the_lines_of_wpf_and_shrunk_like_it(string id)
    {
        // The WPF reference is drawn with Segoe UI, which a Mac does not have.
        if (!OperatingSystem.IsWindows())
            Assert.Skip("La referencia de WPF usa Segoe UI, que la Mac no tiene: esta comparación corre en Windows.");

        var c = RenderCases.Load(id);
        var theme = c.Themes.Single(t => t.Key == "cancion");
        var wpf = RenderCases.Wpf(id, "cancion").Texts.Single(t => t.Name == "stage");

        var (view, drawing) = Draw(c.BuildStageBox(theme)!);
        using (drawing)
        {
            // The size the letters are seen at: the Viewbox may have shrunk them.
            var text = view.Text!;
            var shown = text.TranslatePoint(new Point(100, 0), view)!.Value.X - text.TranslatePoint(new Point(0, 0), view)!.Value.X;

            Assert.Equal(wpf.Lines, SlideView.LinesOf(text));
            Assert.Equal(wpf.FontSize, text.FontSize * shown / 100, 0);
        }
    }

    [AvaloniaFact]
    public void In_the_editor_a_box_shows_its_sample()
    {
        var (view, drawing) = Draw(new StageBox { Kind = StageBoxKind.Clock, Width = 400, Height = 200, FontSize = 80 });
        using (drawing)
            Assert.Equal("10:30", view.Text!.Text);
    }

    [AvaloniaFact]
    public void The_mirror_in_the_editor_says_it_is_the_main_screen()
    {
        var (view, drawing) = Draw(new StageBox { Kind = StageBoxKind.Mirror, Width = 640, Height = 360 });
        using (drawing)
        {
            Assert.Null(view.Text);
            Assert.Equal("Igual a la pantalla principal", ((TextBlock)((Border)view.Child!).Child!).Text);
        }
    }
}
