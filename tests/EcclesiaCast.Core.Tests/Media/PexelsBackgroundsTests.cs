using EcclesiaCast.Core.Media;

namespace EcclesiaCast.Core.Tests.Media;

public class PexelsBackgroundsTests
{
    private const string Answer = """
        {"resultados":[
          {"id":12345,"tipo":"video","ancho":1920,"alto":1080,"duracion":12.4,
           "miniatura":"https://images.pexels.com/videos/12345/a.jpeg","autor":"Ana López",
           "autorUrl":"https://www.pexels.com/@ana","paginaUrl":"https://www.pexels.com/video/12345/",
           "archivo":{"url":"https://videos.pexels.com/video-files/12345/hd.mp4","ancho":1920,"alto":1080}},
          {"id":2,"tipo":"video","ancho":1920,"alto":1080,"duracion":5,
           "miniatura":"https://images.pexels.com/2.jpeg","autor":"X",
           "archivo":{"url":"https://evil.example.com/2.mp4","ancho":1920,"alto":1080}},
          {"id":3,"tipo":"foto","ancho":4000,"alto":3000,"duracion":0,
           "miniatura":"https://images.pexels.com/3.jpeg","autor":null,
           "archivo":{"url":"https://images.pexels.com/photos/3/large.jpeg","ancho":1880,"alto":0}}
        ],"hayMas":true}
        """;

    [Fact]
    public void A_page_keeps_only_what_comes_from_Pexels()
    {
        var page = PexelsBackgrounds.ParsePage(Answer);

        Assert.True(page.HasMore);
        Assert.Equal([12345L, 3L], page.Hits.Select(h => h.Id));
        Assert.Equal("Ana López", page.Hits[0].Author);
        Assert.Equal("https://videos.pexels.com/video-files/12345/hd.mp4", page.Hits[0].File!.Url);
    }

    [Fact]
    public void A_video_says_how_long_it_lasts_and_a_photo_does_not()
    {
        var page = PexelsBackgrounds.ParsePage(Answer);

        Assert.Equal("0:12", page.Hits[0].DurationText);
        Assert.Equal(string.Empty, page.Hits[1].DurationText);
        Assert.False(page.Hits[1].IsVideo);
    }

    [Theory]
    [InlineData("no es json")]
    [InlineData("{\"error\":\"Hubo muchas búsquedas\"}")]
    public void What_is_not_a_page_is_said_clearly(string json)
    {
        Assert.Throws<FormatException>(() => PexelsBackgrounds.ParsePage(json));
    }

    [Fact]
    public void The_service_s_explanation_is_read()
    {
        Assert.Equal("Probá en un rato.", PexelsBackgrounds.ErrorOf("{\"error\":\"Probá en un rato.\"}"));
        Assert.Null(PexelsBackgrounds.ErrorOf("{\"resultados\":[]}"));
        Assert.Null(PexelsBackgrounds.ErrorOf("<html>"));
    }

    [Theory]
    [InlineData("https://videos.pexels.com/a.mp4", true)]
    [InlineData("https://www.pexels.com/x", true)]
    [InlineData("https://pexels.com/x", true)]
    [InlineData("http://videos.pexels.com/a.mp4", false)]
    [InlineData("https://pexels.com.evil.com/a.mp4", false)]
    [InlineData("https://notpexels.com/a.mp4", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Only_https_addresses_on_Pexels_are_downloaded(string? url, bool expected)
    {
        Assert.Equal(expected, PexelsBackgrounds.IsPexelsAddress(url));
    }

    [Fact]
    public void The_file_name_says_what_who_and_which()
    {
        var hit = PexelsBackgrounds.ParsePage(Answer).Hits[0];

        Assert.Equal("Cruz atardecer · Ana López · Pexels 12345.mp4", PexelsBackgrounds.FileName(hit, "  cruz   atardecer "));
    }

    [Fact]
    public void A_photo_without_author_or_search_still_gets_a_good_name()
    {
        var photo = PexelsBackgrounds.ParsePage(Answer).Hits[1];

        Assert.Equal("Foto · Pexels 3.jpg", PexelsBackgrounds.FileName(photo, ""));
        Assert.Equal("Cielo · Pexels 3.jpg", PexelsBackgrounds.FileName(photo, "cielo/?*"));
    }
}
