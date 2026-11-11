using EcclesiaCast.Core.Songs;

namespace EcclesiaCast.Core.Tests.Songs;

public class TrashPolicyTests
{
    private static readonly DateTime Now = new(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Lo_borrado_recien_tiene_treinta_dias()
        => Assert.Equal(30, TrashPolicy.DaysLeft(Now, Now));

    [Fact]
    public void Los_dias_que_quedan_bajan_con_el_tiempo()
        => Assert.Equal(20, TrashPolicy.DaysLeft(Now.AddDays(-10), Now));

    [Fact]
    public void Pasados_los_treinta_dias_quedan_cero()
        => Assert.Equal(0, TrashPolicy.DaysLeft(Now.AddDays(-45), Now));

    [Fact]
    public void El_limite_de_purga_es_hace_treinta_dias()
        => Assert.Equal(Now.AddDays(-30), TrashPolicy.PurgeLimit(Now));
}
