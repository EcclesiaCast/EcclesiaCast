using EcclesiaCast.Core.Media;

namespace EcclesiaCast.Core.Tests.Media;

public class YouTubeAudioTests
{
    [Fact]
    public void A_search_line_gives_id_title_channel_and_length()
    {
        var result = YouTubeAudio.ParseSearchLine("uuS-jyi5Puw\tWorship Pad [Key of D] (54 min)\tJoshua J Gartner\t3257.0");

        Assert.NotNull(result);
        Assert.Equal("uuS-jyi5Puw", result.VideoId);
        Assert.Equal("Worship Pad [Key of D] (54 min)", result.Title);
        Assert.Equal("Joshua J Gartner", result.Channel);
        Assert.Equal(TimeSpan.FromSeconds(3257), result.Duration);
        Assert.Equal("54:17", result.DurationText);
    }

    [Fact]
    public void A_long_video_shows_its_hours()
    {
        var result = YouTubeAudio.ParseSearchLine("abcdefghijk\tPad\tCanal\t7200");
        Assert.Equal("2:00:00", result!.DurationText);
    }

    [Fact]
    public void A_live_broadcast_has_no_length()
    {
        var result = YouTubeAudio.ParseSearchLine("abcdefghijk\tRadio 24/7\tNA\tNA");

        Assert.NotNull(result);
        Assert.Null(result.Duration);
        Assert.Equal("En vivo", result.DurationText);
        Assert.Equal(string.Empty, result.Channel);
    }

    [Theory]
    [InlineData("")]
    [InlineData("WARNING: [youtube] algo")]
    [InlineData("no es un id\tTítulo\tCanal\t60")]
    [InlineData("abcdefghijk\tsólo dos")]
    public void Anything_else_yt_dlp_prints_is_skipped(string line)
    {
        Assert.Null(YouTubeAudio.ParseSearchLine(line));
    }

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=uuS-jyi5Puw", "uuS-jyi5Puw")]
    [InlineData(@"C:\Users\x\Music\EcclesiaCast\Pad D [uuS-jyi5Puw].m4a", null)]
    [InlineData(@"C:\musica\abcdefghijk", null)]
    [InlineData("abcdefghijk", null)]
    public void Only_a_youtube_link_is_a_youtube_track(string path, string? expected)
    {
        Assert.Equal(expected, YouTubeAudio.TrackVideoId(path));
    }

    [Fact]
    public void A_saved_sound_is_still_known_as_its_video()
    {
        Assert.True(YouTubeAudio.IsTrackOf(@"C:\Música\EcclesiaCast\Pad D [uuS-jyi5Puw].m4a", "uuS-jyi5Puw"));
        Assert.True(YouTubeAudio.IsTrackOf("https://www.youtube.com/watch?v=uuS-jyi5Puw", "uuS-jyi5Puw"));
        Assert.False(YouTubeAudio.IsTrackOf(@"C:\Música\otro.mp3", "uuS-jyi5Puw"));
    }

    [Theory]
    [InlineData("Re", "worship pad D")]
    [InlineData("Do#", "worship pad C#")]
    [InlineData("Mib", "worship pad Eb")]
    [InlineData("Sib", "worship pad Bb")]
    [InlineData("Si", "worship pad B")]
    public void A_pad_button_searches_the_tone_by_its_letter(string key, string expected)
    {
        Assert.Equal(expected, YouTubeAudio.PadQuery(key));
    }

    [Fact]
    public void Every_pad_button_has_a_letter()
    {
        Assert.Equal(12, YouTubeAudio.PadKeys.Count);
        Assert.All(YouTubeAudio.PadKeys, key =>
            Assert.Matches("^worship pad [A-G][#b]?$", YouTubeAudio.PadQuery(key)));
    }
}
