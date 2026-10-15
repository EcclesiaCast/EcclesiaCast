using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Core.Remote;

namespace EcclesiaCast.Core.Tests.Remote;

public class OutputChannelTests
{
    [Theory]
    [InlineData("/salida", OutputChannel.All)]
    [InlineData("/output", OutputChannel.All)]
    [InlineData("/salida/biblia", OutputChannel.Bible)]
    [InlineData("/salida/canciones", OutputChannel.Songs)]
    [InlineData("/salida/anuncios", OutputChannel.Announcements)]
    [InlineData("/SALIDA/Biblia/", OutputChannel.Bible)]
    [InlineData("/salida/biblia?pin=1234", OutputChannel.Bible)]
    public void Every_page_of_the_stream_belongs_to_its_channel(string path, OutputChannel expected)
    {
        Assert.Equal(expected, OutputChannels.FromPath(path));
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/salida/otra")]
    [InlineData("/api/output")]
    [InlineData("")]
    [InlineData(null)]
    public void Any_other_address_is_not_a_page_of_the_stream(string? path)
    {
        Assert.Null(OutputChannels.FromPath(path));
    }

    [Fact]
    public void The_address_of_a_channel_reads_back_as_the_same_channel()
    {
        foreach (var channel in Enum.GetValues<OutputChannel>())
            Assert.Equal(channel, OutputChannels.FromPath(OutputChannels.Path(channel)));
    }

    [Fact]
    public void The_name_of_a_channel_in_the_query_reads_back_as_the_same_channel()
    {
        foreach (var channel in new[] { OutputChannel.Bible, OutputChannel.Songs, OutputChannel.Announcements })
            Assert.Equal(channel, OutputChannels.FromName(OutputChannels.Name(channel)));

        Assert.Equal(OutputChannel.All, OutputChannels.FromName(null));
        Assert.Equal(OutputChannel.All, OutputChannels.FromName("cualquier cosa"));
    }

    [Theory]
    [InlineData(OutputChannel.Bible, OnAirSource.Bible, true)]
    [InlineData(OutputChannel.Bible, OnAirSource.Song, false)]
    [InlineData(OutputChannel.Bible, OnAirSource.Media, false)]
    [InlineData(OutputChannel.Songs, OnAirSource.Song, true)]
    [InlineData(OutputChannel.Songs, OnAirSource.Bible, false)]
    [InlineData(OutputChannel.Songs, OnAirSource.QuickText, false)]
    [InlineData(OutputChannel.Announcements, OnAirSource.QuickText, true)]
    [InlineData(OutputChannel.Announcements, OnAirSource.Media, true)]
    [InlineData(OutputChannel.Announcements, OnAirSource.Presentation, true)]
    [InlineData(OutputChannel.Announcements, OnAirSource.Web, true)]
    [InlineData(OutputChannel.Announcements, OnAirSource.Song, false)]
    [InlineData(OutputChannel.Announcements, OnAirSource.Bible, false)]
    public void Each_channel_shows_only_its_own_kind_of_thing(OutputChannel channel, OnAirSource source, bool expected)
    {
        Assert.Equal(expected, OutputChannels.Shows(channel, source));
    }

    [Theory]
    [InlineData(OnAirSource.Song)]
    [InlineData(OnAirSource.Bible)]
    [InlineData(OnAirSource.QuickText)]
    [InlineData(OnAirSource.Media)]
    [InlineData(OnAirSource.Web)]
    [InlineData(OnAirSource.Presentation)]
    public void The_page_with_everything_shows_whatever_is_on_air(OnAirSource source)
    {
        Assert.True(OutputChannels.Shows(OutputChannel.All, source));
    }

    [Fact]
    public void Nothing_on_air_shows_nowhere()
    {
        foreach (var channel in Enum.GetValues<OutputChannel>())
            Assert.False(OutputChannels.Shows(channel, OnAirSource.None));
    }

    [Fact]
    public void The_lower_third_and_the_countdown_ride_only_on_announcements_and_on_everything()
    {
        Assert.True(OutputChannels.ShowsOverlays(OutputChannel.All));
        Assert.True(OutputChannels.ShowsOverlays(OutputChannel.Announcements));
        Assert.False(OutputChannels.ShowsOverlays(OutputChannel.Bible));
        Assert.False(OutputChannels.ShowsOverlays(OutputChannel.Songs));
    }
}
