using EcclesiaCast.Core.Ui;

namespace EcclesiaCast.Core.Tests.Ui;

public class ColorToolsTests
{
    [Theory]
    [InlineData(0, 1, 1, 255, 0, 0)]
    [InlineData(120, 1, 1, 0, 255, 0)]
    [InlineData(240, 1, 1, 0, 0, 255)]
    [InlineData(0, 0, 1, 255, 255, 255)]
    [InlineData(0, 0, 0, 0, 0, 0)]
    public void De_HSV_a_RGB_en_los_colores_conocidos(double h, double s, double v, int r, int g, int b)
        => Assert.Equal(((byte)r, (byte)g, (byte)b), ColorMath.HsvToRgb(h, s, v));

    [Theory]
    [InlineData(200, 40, 90)]
    [InlineData(10, 200, 30)]
    [InlineData(255, 128, 0)]
    [InlineData(90, 90, 90)]
    public void Ir_y_volver_deja_el_mismo_color(int r, int g, int b)
    {
        var (h, s, v) = ColorMath.RgbToHsv((byte)r, (byte)g, (byte)b);
        Assert.Equal(((byte)r, (byte)g, (byte)b), ColorMath.HsvToRgb(h, s, v));
    }

    [Fact]
    public void Un_gris_no_tiene_tono_ni_intensidad()
    {
        var (h, s, _) = ColorMath.RgbToHsv(128, 128, 128);
        Assert.Equal(0, h);
        Assert.Equal(0, s);
    }

    [Fact]
    public void El_tono_se_da_la_vuelta_pasados_360()
        => Assert.Equal(ColorMath.HsvToRgb(10, 1, 1), ColorMath.HsvToRgb(370, 1, 1));

    [Fact]
    public void El_ultimo_color_va_primero_y_sin_repetidos()
    {
        IReadOnlyList<string> list = [];
        list = RecentColors.Add(list, "#112233");
        list = RecentColors.Add(list, "#445566");
        list = RecentColors.Add(list, "#112233");
        Assert.Equal(["#112233", "#445566"], list);
    }

    [Fact]
    public void Repetir_un_color_en_minusculas_no_lo_duplica()
    {
        var list = RecentColors.Add(RecentColors.Add([], "#aabbcc"), "#AABBCC");
        Assert.Equal(["#AABBCC"], list);
    }

    [Fact]
    public void La_lista_tiene_tope()
    {
        IReadOnlyList<string> list = [];
        for (var i = 0; i < 20; i++)
            list = RecentColors.Add(list, $"#0000{i:X2}");
        Assert.Equal(RecentColors.Max, list.Count);
        Assert.Equal("#000013", list[0]);
    }

    [Fact]
    public void Un_codigo_invalido_se_ignora()
    {
        IReadOnlyList<string> list = ["#112233"];
        Assert.Equal(list, RecentColors.Add(list, "TRANSPARENT"));
        Assert.Equal(list, RecentColors.Add(list, "#12"));
        Assert.Equal(list, RecentColors.Add(list, null));
    }

    [Fact]
    public void Se_guarda_y_se_lee_de_vuelta()
    {
        IReadOnlyList<string> list = ["#112233", "#445566"];
        Assert.Equal(list, RecentColors.Parse(RecentColors.Serialize(list)));
    }

    [Fact]
    public void Leer_un_texto_roto_no_falla()
    {
        Assert.Empty(RecentColors.Parse(null));
        Assert.Equal(["#112233"], RecentColors.Parse("basura,#112233,,#112233,#zzzzzz"));
    }
}
