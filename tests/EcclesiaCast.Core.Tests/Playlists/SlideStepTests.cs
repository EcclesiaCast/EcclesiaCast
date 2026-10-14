using EcclesiaCast.Core.Playlists;

namespace EcclesiaCast.Core.Tests.Playlists;

public class SlideStepTests
{
    // A Bible chapter as the grid shows it: «ANTERIOR», three verses, «SIGUIENTE».
    private static readonly bool[] Chapter = [true, false, false, false, true];

    [Fact]
    public void Con_una_diapositiva_en_vivo_la_flecha_va_a_la_vecina()
    {
        Assert.Equal(3, SlideStep.Target(live: 2, preview: -1, direction: +1, Chapter));
        Assert.Equal(1, SlideStep.Target(live: 2, preview: -1, direction: -1, Chapter));
    }

    [Fact]
    public void Con_una_diapositiva_en_vivo_la_marca_pendiente_no_cambia_el_destino()
    {
        Assert.Equal(3, SlideStep.Target(live: 2, preview: 1, direction: +1, Chapter));
    }

    [Fact]
    public void Sin_nada_en_vivo_y_un_versiculo_marcado_la_flecha_derecha_proyecta_el_marcado()
    {
        Assert.Equal(2, SlideStep.Target(live: -1, preview: 2, direction: +1, Chapter));
    }

    [Fact]
    public void Sin_nada_en_vivo_y_un_versiculo_marcado_la_flecha_izquierda_va_al_anterior_al_marcado()
    {
        Assert.Equal(1, SlideStep.Target(live: -1, preview: 2, direction: -1, Chapter));
    }

    [Fact]
    public void Sin_nada_en_vivo_ni_marcado_la_flecha_derecha_salta_la_tarjeta_de_capitulo_anterior()
    {
        // Before, the target was index 0: the «ANTERIOR» card, one chapter back.
        Assert.Equal(1, SlideStep.Target(live: -1, preview: -1, direction: +1, Chapter));
    }

    [Fact]
    public void Sin_nada_en_vivo_ni_marcado_la_flecha_derecha_va_a_la_primera_si_no_hay_tarjetas_de_salto()
    {
        Assert.Equal(0, SlideStep.Target(live: -1, preview: -1, direction: +1, [false, false]));
    }

    [Fact]
    public void Sin_nada_en_vivo_ni_marcado_la_flecha_izquierda_queda_fuera_de_la_lista()
    {
        Assert.Equal(-1, SlideStep.Target(live: -1, preview: -1, direction: -1, Chapter));
    }

    [Fact]
    public void Con_la_lista_vacia_el_destino_queda_fuera_de_rango()
    {
        Assert.Equal(0, SlideStep.Target(live: -1, preview: -1, direction: +1, []));
        Assert.Equal(-1, SlideStep.Target(live: -1, preview: -1, direction: -1, []));
    }

    [Fact]
    public void Con_todo_tarjetas_de_salto_la_flecha_derecha_queda_fuera_de_rango()
    {
        Assert.Equal(2, SlideStep.Target(live: -1, preview: -1, direction: +1, [true, true]));
    }
}
