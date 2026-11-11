using EcclesiaCast.Core.Diagnostics;

namespace EcclesiaCast.Core.Tests.Diagnostics;

public class ErrorNoticeGateTests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 10, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void El_primer_error_avisa()
    {
        var gate = new ErrorNoticeGate(TimeSpan.FromSeconds(30));
        Assert.True(gate.ShouldNotify(T0));
    }

    [Fact]
    public void Un_error_repetido_dentro_del_intervalo_no_avisa()
    {
        var gate = new ErrorNoticeGate(TimeSpan.FromSeconds(30));
        gate.ShouldNotify(T0);
        Assert.False(gate.ShouldNotify(T0.AddSeconds(10)));
        Assert.False(gate.ShouldNotify(T0.AddSeconds(29)));
    }

    [Fact]
    public void Pasado_el_intervalo_vuelve_a_avisar()
    {
        var gate = new ErrorNoticeGate(TimeSpan.FromSeconds(30));
        gate.ShouldNotify(T0);
        Assert.True(gate.ShouldNotify(T0.AddSeconds(30)));
        Assert.False(gate.ShouldNotify(T0.AddSeconds(40)));
    }
}
