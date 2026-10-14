using System.Globalization;
using EcclesiaCast.Core.Presentation;

namespace EcclesiaCast.Core.Tests.Presentation;

public class OverlayStyleTests
{
    [Theory]
    [InlineData(54, 54)]
    [InlineData(5, 20)]
    [InlineData(999, 160)]
    [InlineData(20, 20)]
    [InlineData(160, 160)]
    public void El_tamano_se_recorta_entre_20_y_160(double asked, double expected)
    {
        Assert.Equal(expected, OverlayStyle.ClampSize(asked));
    }

    [Fact]
    public void Un_tamano_que_no_es_numero_vuelve_al_de_fabrica()
    {
        Assert.Equal(54, OverlayStyle.ClampSize(double.NaN));
        Assert.Equal(54, OverlayStyle.ClampSize(double.PositiveInfinity));
    }

    [Theory]
    [InlineData(null, 54)]
    [InlineData("", 54)]
    [InlineData("basura", 54)]
    [InlineData("72", 72)]
    [InlineData("72.5", 72.5)]
    [InlineData("3", 20)]
    [InlineData("500", 160)]
    public void Lo_guardado_se_lee_o_queda_el_de_fabrica(string? saved, double expected)
    {
        Assert.Equal(expected, OverlayStyle.ParseSize(saved));
    }

    [Fact]
    public void El_tamano_se_guarda_con_punto_aunque_Windows_este_en_castellano()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("es-AR");

            var text = OverlayStyle.FormatSize(72.5);

            Assert.Equal("72.5", text);
            Assert.Equal(72.5, OverlayStyle.ParseSize(text));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Theory]
    [InlineData(null, "Segoe UI")]
    [InlineData("", "Segoe UI")]
    [InlineData("   ", "Segoe UI")]
    [InlineData("Georgia", "Georgia")]
    [InlineData("  Georgia ", "Georgia")]
    public void Sin_tipografia_elegida_se_usa_Segoe_UI(string? font, string expected)
    {
        Assert.Equal(expected, OverlayStyle.FontOrDefault(font));
    }
}
