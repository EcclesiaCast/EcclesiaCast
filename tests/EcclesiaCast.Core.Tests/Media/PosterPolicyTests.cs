using EcclesiaCast.Core.Media;

namespace EcclesiaCast.Core.Tests.Media;

public class PosterPolicyTests
{
    private static readonly HashSet<string> Files = [@"C:\fotos\aviso.jpg", @"C:\poster\aviso.png"];

    private static bool Exists(string path) => Files.Contains(path);

    private static MediaItem Image(string? poster) => new()
    {
        Type = MediaType.Image,
        Path = @"C:\fotos\aviso.jpg",
        ThumbnailPath = poster,
    };

    [Fact]
    public void An_image_whose_poster_file_vanished_gets_a_new_one()
    {
        Assert.True(PosterPolicy.NeedsPoster(Image(@"C:\poster\borrado.png"), Exists));
    }

    [Fact]
    public void An_image_with_no_poster_gets_one()
    {
        Assert.True(PosterPolicy.NeedsPoster(Image(null), Exists));
        Assert.True(PosterPolicy.NeedsPoster(Image("  "), Exists));
    }

    [Fact]
    public void A_poster_that_is_still_there_is_left_alone()
    {
        Assert.False(PosterPolicy.NeedsPoster(Image(@"C:\poster\aviso.png"), Exists));
    }

    [Fact]
    public void Nothing_can_be_drawn_when_the_media_file_is_gone_too()
    {
        var item = Image(null);
        item.Path = @"C:\fotos\perdida.jpg";

        Assert.False(PosterPolicy.NeedsPoster(item, Exists));
    }

    [Fact]
    public void YouTube_items_keep_their_web_thumbnail()
    {
        var item = new MediaItem
        {
            Type = MediaType.YouTube,
            Path = "https://youtu.be/abc",
            ThumbnailPath = "https://img.youtube.com/vi/abc/mqdefault.jpg",
        };

        Assert.False(PosterPolicy.NeedsPoster(item, Exists));
    }

    [Fact]
    public void A_window_never_gets_a_poster_even_if_something_answers_to_its_path()
    {
        var item = new MediaItem { Type = MediaType.Window, Path = "explorer.exe|Descargas" };

        Assert.False(PosterPolicy.NeedsPoster(item, _ => true));
    }

    [Fact]
    public void A_saved_web_page_is_never_asked_a_poster_from_its_address()
    {
        var item = new MediaItem
        {
            Type = MediaType.Web,
            Path = "https://www.canva.com/design/abc",
            ThumbnailPath = @"C:\poster\borrado.png",
        };

        Assert.False(PosterPolicy.NeedsPoster(item, Exists));
    }
}
