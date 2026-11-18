using EcclesiaCast.Core.Media;

namespace EcclesiaCast.Core.Tests.Media;

public class PixabayBackgroundsTests
{
    private const string Answer = """
        {"resultados":[
          {"id":12345,"tipo":"video","ancho":1920,"alto":1080,"duracion":12.4,
           "miniatura":"https://cdn.pixabay.com/videos/12345/a.jpeg","autor":"Ana López",
           "autorUrl":"https://pixabay.com/users/ana-1","paginaUrl":"https://pixabay.com/videos/cruz-12345/",
           "archivo":{"url":"https://cdn.pixabay.com/video/12345/hd.mp4","ancho":1920,"alto":1080}},
          {"id":2,"tipo":"video","ancho":1920,"alto":1080,"duracion":5,
           "miniatura":"https://cdn.pixabay.com/2.jpeg","autor":"X",
           "archivo":{"url":"https://evil.example.com/2.mp4","ancho":1920,"alto":1080}},
          {"id":3,"tipo":"foto","ancho":4000,"alto":3000,"duracion":0,
           "miniatura":"https://cdn.pixabay.com/3.jpeg","autor":null,
           "archivo":{"url":"https://cdn.pixabay.com/photos/3/large.jpeg","ancho":1880,"alto":0}}
        ],"hayMas":true}
        """;

    [Fact]
    public void A_page_keeps_only_what_comes_from_Pixabay()
    {
        var page = PixabayBackgrounds.ParsePage(Answer);

        Assert.True(page.HasMore);
        Assert.Equal([12345L, 3L], page.Hits.Select(h => h.Id));
        Assert.Equal("Ana López", page.Hits[0].Author);
        Assert.Equal("https://cdn.pixabay.com/video/12345/hd.mp4", page.Hits[0].File!.Url);
    }

    [Fact]
    public void A_video_says_how_long_it_lasts_and_a_photo_does_not()
    {
        var page = PixabayBackgrounds.ParsePage(Answer);

        Assert.Equal("0:12", page.Hits[0].DurationText);
        Assert.Equal(string.Empty, page.Hits[1].DurationText);
        Assert.False(page.Hits[1].IsVideo);
    }

    [Theory]
    [InlineData("no es json")]
    [InlineData("{\"error\":\"Hubo muchas búsquedas\"}")]
    public void What_is_not_a_page_is_said_clearly(string json)
    {
        Assert.Throws<FormatException>(() => PixabayBackgrounds.ParsePage(json));
    }

    [Fact]
    public void The_service_s_explanation_is_read()
    {
        Assert.Equal("Probá en un rato.", PixabayBackgrounds.ErrorOf("{\"error\":\"Probá en un rato.\"}"));
        Assert.Null(PixabayBackgrounds.ErrorOf("{\"resultados\":[]}"));
        Assert.Null(PixabayBackgrounds.ErrorOf("<html>"));
    }

    [Theory]
    [InlineData("https://videos.pixabay.com/a.mp4", true)]
    [InlineData("https://www.pixabay.com/x", true)]
    [InlineData("https://pixabay.com/x", true)]
    [InlineData("http://videos.pixabay.com/a.mp4", false)]
    [InlineData("https://pixabay.com.evil.com/a.mp4", false)]
    [InlineData("https://notpixabay.com/a.mp4", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Only_https_addresses_on_Pixabay_are_downloaded(string? url, bool expected)
    {
        Assert.Equal(expected, PixabayBackgrounds.IsPixabayAddress(url));
    }

    [Fact]
    public void The_file_name_says_what_who_and_which()
    {
        var hit = PixabayBackgrounds.ParsePage(Answer).Hits[0];

        Assert.Equal("Cruz atardecer · Ana López · Pixabay 12345.mp4", PixabayBackgrounds.FileName(hit, "  cruz   atardecer "));
    }

    [Fact]
    public void A_photo_without_author_or_search_still_gets_a_good_name()
    {
        var photo = PixabayBackgrounds.ParsePage(Answer).Hits[1];

        Assert.Equal("Foto · Pixabay 3.jpg", PixabayBackgrounds.FileName(photo, ""));
        Assert.Equal("Cielo · Pixabay 3.jpg", PixabayBackgrounds.FileName(photo, "cielo/?*"));
    }
}
