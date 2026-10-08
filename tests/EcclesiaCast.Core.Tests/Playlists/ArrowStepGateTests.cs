using EcclesiaCast.Core.Playlists;

namespace EcclesiaCast.Core.Tests.Playlists;

public class ArrowStepGateTests
{
    [Fact]
    public void Una_pulsacion_nueva_en_el_medio_del_item_avanza_de_diapositiva()
    {
        var gate = new ArrowStepGate();

        Assert.Equal(ArrowAction.Move, gate.Decide(atEdge: false, hasNeighbour: true, isRepeat: false));
    }

    [Fact]
    public void Una_pulsacion_nueva_en_el_borde_pasa_al_item_siguiente()
    {
        var gate = new ArrowStepGate();

        Assert.Equal(ArrowAction.Cross, gate.Decide(atEdge: true, hasNeighbour: true, isRepeat: false));
    }

    [Fact]
    public void La_repeticion_de_la_tecla_se_frena_en_el_borde()
    {
        var gate = new ArrowStepGate();

        Assert.Equal(ArrowAction.Hold, gate.Decide(atEdge: true, hasNeighbour: true, isRepeat: true));
        Assert.Equal(ArrowAction.Hold, gate.Decide(atEdge: true, hasNeighbour: true, isRepeat: true));
    }

    [Fact]
    public void La_repeticion_de_la_tecla_sigue_moviendo_dentro_del_item()
    {
        var gate = new ArrowStepGate();

        Assert.Equal(ArrowAction.Move, gate.Decide(atEdge: false, hasNeighbour: true, isRepeat: true));
    }

    [Fact]
    public void Despues_de_cruzar_se_ignora_el_resto_de_la_misma_pulsacion()
    {
        var gate = new ArrowStepGate();
        gate.Decide(atEdge: true, hasNeighbour: true, isRepeat: false);

        // Already inside the next item: the held key must not keep walking it.
        Assert.Equal(ArrowAction.Ignore, gate.Decide(atEdge: false, hasNeighbour: true, isRepeat: true));
        Assert.Equal(ArrowAction.Ignore, gate.Decide(atEdge: true, hasNeighbour: true, isRepeat: true));
    }

    [Fact]
    public void Una_pulsacion_nueva_despues_de_cruzar_vuelve_a_andar()
    {
        var gate = new ArrowStepGate();
        gate.Decide(atEdge: true, hasNeighbour: true, isRepeat: false);
        gate.Decide(atEdge: false, hasNeighbour: true, isRepeat: true);

        Assert.Equal(ArrowAction.Move, gate.Decide(atEdge: false, hasNeighbour: true, isRepeat: false));
        Assert.Equal(ArrowAction.Move, gate.Decide(atEdge: false, hasNeighbour: true, isRepeat: true));
    }

    [Fact]
    public void Sin_item_vecino_el_borde_no_frena_y_se_puede_seguir_leyendo()
    {
        var gate = new ArrowStepGate();

        Assert.Equal(ArrowAction.Move, gate.Decide(atEdge: true, hasNeighbour: false, isRepeat: false));
        Assert.Equal(ArrowAction.Move, gate.Decide(atEdge: true, hasNeighbour: false, isRepeat: true));
    }

    [Fact]
    public void Una_tecla_mantenida_desde_antes_del_borde_se_frena_y_otra_pulsacion_cruza()
    {
        var gate = new ArrowStepGate();

        // Held key: first press and repeats walk the verses...
        Assert.Equal(ArrowAction.Move, gate.Decide(false, true, isRepeat: false));
        Assert.Equal(ArrowAction.Move, gate.Decide(false, true, isRepeat: true));
        // ...reach the end and stop there, however long it is held.
        Assert.Equal(ArrowAction.Hold, gate.Decide(true, true, isRepeat: true));
        Assert.Equal(ArrowAction.Hold, gate.Decide(true, true, isRepeat: true));
        // Letting go and pressing again crosses.
        Assert.Equal(ArrowAction.Cross, gate.Decide(true, true, isRepeat: false));
    }
}
