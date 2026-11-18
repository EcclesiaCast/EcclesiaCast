using EcclesiaCast.Core.Listening;

namespace EcclesiaCast.Core.Tests.Listening;

public class HeardTextTests
{
    [Theory]
    [InlineData("[Música]")]
    [InlineData("Música")]
    [InlineData("♪ ♪ ♪")]
    [InlineData("¡Suscríbete al canal!")]
    [InlineData("(aplausos)")]
    [InlineData("")]
    public void What_the_recogniser_writes_over_music_is_not_singing(string raw)
    {
        Assert.Equal(string.Empty, HeardText.Clean(raw, 8));
    }

    [Fact]
    public void A_sung_line_comes_back_without_notes_or_asides()
    {
        Assert.Equal("La alegría del Señor es nuestra fuerza",
            HeardText.Clean("♪ La alegría del Señor ♪ [Música] es nuestra fuerza", 8));
    }

    [Fact]
    public void More_words_than_fit_in_the_time_were_made_up()
    {
        var invented = string.Join(' ', Enumerable.Repeat("Dios es por nosotros vence las tinieblas su reino", 5));

        Assert.Equal(string.Empty, HeardText.Clean(invented, 8));
    }

    [Fact]
    public void One_line_in_a_loop_is_thrown_away()
    {
        Assert.Equal(string.Empty, HeardText.Clean("No podemos haber No podemos haber No podemos haber", 8));
        Assert.Equal(string.Empty, HeardText.Clean("y y y y", 8));
    }

    [Fact]
    public void A_chorus_that_repeats_a_word_inside_a_line_stays()
    {
        Assert.NotEqual(string.Empty, HeardText.Clean("Santo, santo, santo, Señor omnipotente", 8));
        Assert.NotEqual(string.Empty, HeardText.Clean("la música del cielo llena mi corazón de alegría", 8));
    }
}
