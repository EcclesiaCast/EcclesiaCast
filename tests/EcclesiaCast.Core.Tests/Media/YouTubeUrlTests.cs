using EcclesiaCast.Core.Media;

namespace EcclesiaCast.Core.Tests.Media;

public class YouTubeUrlTests
{
    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtube.com/watch?v=dQw4w9WgXcQ&t=42s")]
    [InlineData("https://www.youtube.com/watch?list=PL123&v=dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ?t=30")]
    [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/shorts/dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/live/dQw4w9WgXcQ")]
    [InlineData("  https://www.youtube.com/watch?v=dQw4w9WgXcQ  ")]
    public void Extracts_the_video_id_from_every_link_shape(string url)
    {
        Assert.Equal("dQw4w9WgXcQ", YouTubeUrl.TryParseVideoId(url));
    }

    [Fact]
    public void Accepts_a_bare_video_id()
    {
        Assert.Equal("dQw4w9WgXcQ", YouTubeUrl.TryParseVideoId("dQw4w9WgXcQ"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("https://vimeo.com/12345678")]
    [InlineData("no es un link")]
    [InlineData("https://www.youtube.com/results?search_query=algo")]
    public void Returns_null_for_anything_that_is_not_a_video(string? text)
    {
        Assert.Null(YouTubeUrl.TryParseVideoId(text));
    }

    [Fact]
    public void Builds_thumbnail_and_watch_urls()
    {
        Assert.Equal("https://img.youtube.com/vi/abc12345678/hqdefault.jpg", YouTubeUrl.ThumbnailUrl("abc12345678"));
        Assert.Equal("https://www.youtube.com/watch?v=abc12345678", YouTubeUrl.WatchUrl("abc12345678"));
    }
}
