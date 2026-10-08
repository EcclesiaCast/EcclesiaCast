using EcclesiaCast.Core.Displays;

namespace EcclesiaCast.Core.Tests.Displays;

public class AspectFitTests
{
    private const double Sixteen9 = 16d / 9d;

    [Fact]
    public void Un_panel_ancho_no_agranda_el_cuadro_hacia_abajo()
    {
        var (width, height) = AspectFit.Compute(availableWidth: 600, maxHeight: 165, Sixteen9);

        Assert.Equal(165, height, 6);
        Assert.Equal(165 * Sixteen9, width, 6);
    }

    [Fact]
    public void Un_panel_angosto_achica_el_cuadro_entero_manteniendo_la_proporcion()
    {
        var (width, height) = AspectFit.Compute(availableWidth: 200, maxHeight: 165, Sixteen9);

        Assert.Equal(200, width, 6);
        Assert.Equal(200 / Sixteen9, height, 6);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(244)]
    [InlineData(293.33)]
    [InlineData(500)]
    [InlineData(2000)]
    public void La_proporcion_se_mantiene_con_cualquier_ancho(double available)
    {
        var (width, height) = AspectFit.Compute(available, 165, Sixteen9);

        Assert.Equal(Sixteen9, width / height, 6);
    }

    [Fact]
    public void La_proporcion_sale_de_la_pantalla_elegida()
    {
        var ratio = AspectFit.RatioOf(1920, 1280); // una pantalla 3:2

        var (width, height) = AspectFit.Compute(1000, 165, ratio);

        Assert.Equal(1.5, width / height, 6);
        Assert.Equal(165, height, 6);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1920, 0)]
    [InlineData(-5, 1080)]
    public void Una_pantalla_sin_medidas_usa_16_9(int width, int height)
    {
        Assert.Equal(16d / 9d, AspectFit.RatioOf(width, height), 6);
    }

    [Fact]
    public void Sin_limite_de_ancho_el_cuadro_tiene_el_alto_maximo()
    {
        var (width, height) = AspectFit.Compute(double.PositiveInfinity, 165, Sixteen9);

        Assert.Equal(165, height, 6);
        Assert.Equal(165 * Sixteen9, width, 6);
    }

    [Fact]
    public void Un_ancho_negativo_no_rompe()
    {
        var (width, height) = AspectFit.Compute(-10, 165, Sixteen9);

        Assert.Equal(0, width);
        Assert.Equal(0, height);
    }
}
