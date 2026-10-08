using EcclesiaCast.Core.Diagnostics;

namespace EcclesiaCast.Core.Tests.Diagnostics;

public class FreezeWatchTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 8, 10, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Threshold = TimeSpan.FromSeconds(2);

    [Fact]
    public void Una_ventana_que_contesta_a_tiempo_no_genera_avisos()
    {
        var detector = new FreezeDetector(Threshold);

        detector.PingSent(T0);
        Assert.Null(detector.Check(T0.AddMilliseconds(500)));
        Assert.Null(detector.PingAnswered(T0.AddMilliseconds(600)));
    }

    [Fact]
    public void Pasados_dos_segundos_sin_respuesta_avisa_que_esta_congelada()
    {
        var detector = new FreezeDetector(Threshold);
        detector.PingSent(T0);

        Assert.Null(detector.Check(T0.AddSeconds(2)));
        var frozen = detector.Check(T0.AddMilliseconds(2500));

        Assert.NotNull(frozen);
        Assert.Equal(FreezeEventKind.Frozen, frozen.Value.Kind);
        Assert.Equal(TimeSpan.FromMilliseconds(2500), frozen.Value.Duration);
    }

    [Fact]
    public void El_aviso_de_congelada_sale_una_sola_vez()
    {
        var detector = new FreezeDetector(Threshold);
        detector.PingSent(T0);

        Assert.NotNull(detector.Check(T0.AddSeconds(3)));
        Assert.Null(detector.Check(T0.AddSeconds(4)));
        Assert.Null(detector.Check(T0.AddSeconds(5)));
    }

    [Fact]
    public void Al_volver_informa_cuanto_duro_en_total()
    {
        var detector = new FreezeDetector(Threshold);
        detector.PingSent(T0);
        detector.Check(T0.AddSeconds(3));

        var recovered = detector.PingAnswered(T0.AddSeconds(5.2));

        Assert.NotNull(recovered);
        Assert.Equal(FreezeEventKind.Recovered, recovered.Value.Kind);
        Assert.Equal(TimeSpan.FromSeconds(5.2), recovered.Value.Duration);
    }

    [Fact]
    public void Un_segundo_ping_no_reinicia_la_cuenta_del_primero()
    {
        var detector = new FreezeDetector(Threshold);
        detector.PingSent(T0);
        detector.PingSent(T0.AddSeconds(1.5));

        var frozen = detector.Check(T0.AddSeconds(2.5));

        Assert.NotNull(frozen);
        Assert.Equal(TimeSpan.FromSeconds(2.5), frozen.Value.Duration);
    }

    [Fact]
    public void Despues_de_recuperarse_puede_volver_a_avisar_otro_cuelgue()
    {
        var detector = new FreezeDetector(Threshold);
        detector.PingSent(T0);
        detector.Check(T0.AddSeconds(3));
        detector.PingAnswered(T0.AddSeconds(4));

        detector.PingSent(T0.AddSeconds(10));

        Assert.NotNull(detector.Check(T0.AddSeconds(13)));
    }

    [Fact]
    public void Si_el_vigia_estuvo_suspendido_se_perdona_el_ping_en_vuelo()
    {
        var detector = new FreezeDetector(Threshold);
        detector.PingSent(T0);

        detector.Forgive();

        Assert.False(detector.PingPending);
        Assert.Null(detector.Check(T0.AddMinutes(10)));
        Assert.Null(detector.PingAnswered(T0.AddMinutes(10)));
    }

    [Fact]
    public void Sin_ping_pendiente_no_hay_nada_que_revisar()
    {
        var detector = new FreezeDetector(Threshold);

        Assert.False(detector.PingPending);
        Assert.Null(detector.Check(T0.AddMinutes(5)));
    }

    [Fact]
    public void El_rastro_guarda_solo_las_ultimas_acciones()
    {
        var trail = new ActionTrail(capacity: 3);
        for (var i = 1; i <= 5; i++)
            trail.Note($"acción {i}", T0.AddSeconds(i));

        var entries = trail.Snapshot();

        Assert.Equal(["acción 3", "acción 4", "acción 5"], entries.Select(e => e.Text));
    }

    [Fact]
    public void El_rastro_se_describe_con_lo_mas_nuevo_primero_y_cuanto_hace()
    {
        var trail = new ActionTrail();
        trail.Note("Abrir capítulo Juan 8", T0);
        trail.Note("Proyectar «Juan 8:14»", T0.AddSeconds(6));

        var text = trail.Describe(T0.AddSeconds(9));

        Assert.Equal("hace 3.0 s: Proyectar «Juan 8:14» | hace 9.0 s: Abrir capítulo Juan 8", text);
    }

    [Fact]
    public void Un_rastro_vacio_lo_dice()
    {
        Assert.Contains("sin acciones", new ActionTrail().Describe(T0));
    }
}
