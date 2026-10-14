using EcclesiaCast.Core.Media;
using EcclesiaCast.Core.Presentation;

namespace EcclesiaCast.Core.Tests.Presentation;

public class OnAirResolverTests
{
    private static MediaItem Media(MediaBehavior behavior = MediaBehavior.Foreground, MediaType type = MediaType.Image) =>
        new() { Id = 1, Name = "Aviso", Type = type, Behavior = behavior };

    private static OnAirSource Resolve(
        bool projecting = true, OutputState state = OutputState.Content, SlideOrigin origin = SlideOrigin.None,
        MediaItem? background = null, bool page = false) =>
        OnAirResolver.Resolve(projecting, state, origin, background, page);

    [Fact]
    public void With_the_output_off_nothing_is_on_the_air()
    {
        Assert.Equal(OnAirSource.None, Resolve(projecting: false, origin: SlideOrigin.Song));
    }

    [Theory]
    [InlineData(OutputState.Black)]
    [InlineData(OutputState.Logo)]
    public void Black_and_the_logo_hide_everything_even_a_media(OutputState state)
    {
        Assert.Equal(OnAirSource.None, Resolve(state: state, origin: SlideOrigin.Bible, background: Media()));
    }

    [Theory]
    [InlineData(SlideOrigin.Song, OnAirSource.Song)]
    [InlineData(SlideOrigin.Bible, OnAirSource.Bible)]
    [InlineData(SlideOrigin.QuickText, OnAirSource.QuickText)]
    [InlineData(SlideOrigin.None, OnAirSource.None)]
    public void Words_on_the_screen_belong_to_where_they_came_from(SlideOrigin origin, OnAirSource expected)
    {
        Assert.Equal(expected, Resolve(origin: origin));
    }

    [Fact]
    public void Words_over_a_background_are_still_the_words()
    {
        var background = Media(MediaBehavior.Background, MediaType.Video);

        Assert.Equal(OnAirSource.Song, Resolve(origin: SlideOrigin.Song, background: background));
    }

    [Fact]
    public void A_page_of_a_presentation_takes_the_screen_from_the_verse_before_it()
    {
        Assert.Equal(OnAirSource.Presentation,
            Resolve(state: OutputState.Clear, origin: SlideOrigin.Bible, background: Media(), page: true));
    }

    [Fact]
    public void A_foreground_media_is_media_not_the_section_behind_it()
    {
        Assert.Equal(OnAirSource.Media, Resolve(state: OutputState.Clear, origin: SlideOrigin.Song, background: Media()));
    }

    [Fact]
    public void A_background_alone_after_Clear_is_media()
    {
        var background = Media(MediaBehavior.Background);

        Assert.Equal(OnAirSource.Media, Resolve(state: OutputState.Clear, origin: SlideOrigin.Song, background: background));
    }

    [Fact]
    public void A_web_page_is_web()
    {
        Assert.Equal(OnAirSource.Web, Resolve(state: OutputState.Clear, background: Media(type: MediaType.Web)));
    }

    [Fact]
    public void Clear_without_a_background_leaves_nothing_on_the_air()
    {
        Assert.Equal(OnAirSource.None, Resolve(state: OutputState.Clear, origin: SlideOrigin.Song));
    }
}
