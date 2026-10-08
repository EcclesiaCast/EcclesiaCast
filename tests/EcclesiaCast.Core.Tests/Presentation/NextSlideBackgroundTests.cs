using EcclesiaCast.Core.Media;
using EcclesiaCast.Core.Presentation;

namespace EcclesiaCast.Core.Tests.Presentation;

public class NextSlideBackgroundTests
{
    [Fact]
    public void Un_anuncio_en_primer_plano_no_queda_detras_de_la_proxima_diapositiva()
    {
        var announcement = new MediaItem { Name = "Anuncio", Behavior = MediaBehavior.Foreground };

        Assert.Null(NextSlideBackground.Resolve(announcement));
    }

    [Fact]
    public void Un_fondo_normal_sigue_detras_de_la_proxima_diapositiva()
    {
        var background = new MediaItem { Name = "Fondo", Behavior = MediaBehavior.Background };

        Assert.Same(background, NextSlideBackground.Resolve(background));
    }

    [Fact]
    public void Sin_fondo_el_preview_tampoco_lleva_fondo()
    {
        Assert.Null(NextSlideBackground.Resolve(null));
    }
}
