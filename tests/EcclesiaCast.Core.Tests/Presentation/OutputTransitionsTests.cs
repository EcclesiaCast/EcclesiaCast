using System.Globalization;
using EcclesiaCast.Core.Presentation;

namespace EcclesiaCast.Core.Tests.Presentation;

public class OutputTransitionsTests
{
    [Fact]
    public void Los_valores_de_fabrica_son_los_de_siempre_y_la_salida_corta()
    {
        var d = OutputTransitions.Default;

        Assert.Equal(0.5, d.LogoSeconds);
        Assert.Equal(0.4, d.MediaSeconds);
        Assert.Equal(0.25, d.BlackSeconds);
        Assert.Equal(0, d.OutputSeconds);
    }

    [Fact]
    public void Lo_guardado_se_lee_igual_que_se_escribio()
    {
        var original = new OutputTransitions(1.5, 2, 0.3, 4.5);

        var back = OutputTransitions.Parse(original.ToSetting());

        Assert.Equal(original, back);
    }

    [Fact]
    public void El_texto_guardado_tiene_la_forma_documentada()
    {
        Assert.Equal("logo=0.5;media=0.4;black=0.25;output=0", OutputTransitions.Default.ToSetting());
    }

    [Fact]
    public void Se_escribe_con_punto_aunque_Windows_este_en_castellano()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("es-AR");

            var text = new OutputTransitions(1.5, 0.4, 0.25, 0).ToSetting();
            var back = OutputTransitions.Parse(text);

            Assert.Contains("logo=1.5", text);
            Assert.Equal(1.5, back.LogoSeconds);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("basura")]
    [InlineData("logo=;media=x")]
    public void Un_texto_que_no_se_puede_leer_deja_los_valores_de_fabrica(string? setting)
    {
        Assert.Equal(OutputTransitions.Default, OutputTransitions.Parse(setting));
    }

    [Fact]
    public void Un_valor_roto_no_se_lleva_puestos_a_los_demas()
    {
        var parsed = OutputTransitions.Parse("logo=2;media=roto;black=1;output=3");

        Assert.Equal(2, parsed.LogoSeconds);
        Assert.Equal(OutputTransitions.DefaultMediaSeconds, parsed.MediaSeconds);
        Assert.Equal(1, parsed.BlackSeconds);
        Assert.Equal(3, parsed.OutputSeconds);
    }

    [Fact]
    public void Falta_un_valor_y_queda_el_de_fabrica()
    {
        var parsed = OutputTransitions.Parse("black=2");

        Assert.Equal(2, parsed.BlackSeconds);
        Assert.Equal(OutputTransitions.DefaultLogoSeconds, parsed.LogoSeconds);
    }

    [Fact]
    public void Los_segundos_se_recortan_entre_cero_y_diez()
    {
        var clamped = new OutputTransitions(-3, 99, double.NaN, double.PositiveInfinity).Clamp();

        Assert.Equal(0, clamped.LogoSeconds);
        Assert.Equal(10, clamped.MediaSeconds);
        Assert.Equal(OutputTransitions.DefaultBlackSeconds, clamped.BlackSeconds);
        Assert.Equal(OutputTransitions.DefaultOutputSeconds, clamped.OutputSeconds);
    }

    [Fact]
    public void Un_texto_guardado_fuera_de_rango_se_recorta_al_leerlo()
    {
        var parsed = OutputTransitions.Parse("logo=500;media=-1");

        Assert.Equal(10, parsed.LogoSeconds);
        Assert.Equal(0, parsed.MediaSeconds);
    }

    [Fact]
    public void Las_duraciones_salen_en_TimeSpan()
    {
        var t = new OutputTransitions(1.5, 0, 2, 0.25);

        Assert.Equal(TimeSpan.FromSeconds(1.5), t.Logo);
        Assert.Equal(TimeSpan.Zero, t.Media);
        Assert.Equal(TimeSpan.FromSeconds(2), t.Black);
        Assert.Equal(TimeSpan.FromSeconds(0.25), t.Output);
    }
}
