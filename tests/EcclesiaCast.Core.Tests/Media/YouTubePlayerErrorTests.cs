using EcclesiaCast.Core.Media;

namespace EcclesiaCast.Core.Tests.Media;

public class YouTubePlayerErrorTests
{
    [Theory]
    [InlineData(101)]
    [InlineData(150)]
    public void A_video_its_owner_keeps_inside_YouTube_says_to_download_it(int code)
    {
        Assert.True(YouTubePlayerError.OnlyByDownloading(code));
        Assert.Contains("descargado", YouTubePlayerError.Title(code));
        Assert.Contains("Descargar para uso local", YouTubePlayerError.Explain(code));
    }

    [Fact]
    public void A_video_that_is_gone_says_so()
    {
        Assert.False(YouTubePlayerError.OnlyByDownloading(100));
        Assert.Contains("privado", YouTubePlayerError.Explain(100));
    }

    [Fact]
    public void Any_other_error_gives_its_number_and_the_way_out()
    {
        var text = YouTubePlayerError.Explain(5);

        Assert.Contains("error 5", text);
        Assert.Contains("Descargar para uso local", text);
    }
}
