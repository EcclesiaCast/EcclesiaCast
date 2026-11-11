using EcclesiaCast.Core.Diagnostics;

namespace EcclesiaCast.Core.Tests.Diagnostics;

public class DoublePressGateTests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 10, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void El_primer_toque_no_confirma()
    {
        var gate = new DoublePressGate(TimeSpan.FromSeconds(2));
        Assert.False(gate.Press(T0));
    }

    [Fact]
    public void El_segundo_toque_dentro_de_la_ventana_confirma()
    {
        var gate = new DoublePressGate(TimeSpan.FromSeconds(2));
        gate.Press(T0);
        Assert.True(gate.Press(T0.AddSeconds(1)));
    }

    [Fact]
    public void Pasada_la_ventana_vuelve_a_pedir_dos()
    {
        var gate = new DoublePressGate(TimeSpan.FromSeconds(2));
        gate.Press(T0);
        Assert.False(gate.Press(T0.AddSeconds(3)));
        Assert.True(gate.Press(T0.AddSeconds(4)));
    }

    [Fact]
    public void Despues_de_confirmar_el_siguiente_toque_es_primero()
    {
        var gate = new DoublePressGate(TimeSpan.FromSeconds(2));
        gate.Press(T0);
        gate.Press(T0.AddSeconds(1));
        Assert.False(gate.Press(T0.AddSeconds(1.5)));
    }
}
