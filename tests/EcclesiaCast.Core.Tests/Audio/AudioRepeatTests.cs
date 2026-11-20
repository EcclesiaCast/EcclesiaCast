using EcclesiaCast.Core.Audio;

namespace EcclesiaCast.Core.Tests.Audio;

public class AudioRepeatTests
{
    [Fact]
    public void Without_repeat_the_music_stops_after_the_last()
    {
        Assert.Equal(2, AudioNavigator.AfterEnd(3, 1, AudioRepeatMode.Off));
        Assert.Null(AudioNavigator.AfterEnd(3, 2, AudioRepeatMode.Off));
    }

    [Fact]
    public void Repeating_the_list_goes_back_to_the_first()
    {
        Assert.Equal(2, AudioNavigator.AfterEnd(3, 1, AudioRepeatMode.All));
        Assert.Equal(0, AudioNavigator.AfterEnd(3, 2, AudioRepeatMode.All));
    }

    [Fact]
    public void Repeating_one_plays_the_same_again()
    {
        Assert.Equal(1, AudioNavigator.AfterEnd(3, 1, AudioRepeatMode.One));
        Assert.Equal(0, AudioNavigator.AfterEnd(1, 0, AudioRepeatMode.All));
    }

    [Fact]
    public void Repeating_one_with_nothing_loaded_starts_the_list()
    {
        Assert.Equal(0, AudioNavigator.AfterEnd(3, -1, AudioRepeatMode.One));
    }

    [Fact]
    public void An_empty_list_never_plays()
    {
        Assert.Null(AudioNavigator.AfterEnd(0, -1, AudioRepeatMode.All));
        Assert.Null(AudioNavigator.NextPressed(0, -1, AudioRepeatMode.All));
    }

    [Fact]
    public void The_next_button_wraps_only_with_the_list_on_repeat()
    {
        Assert.Null(AudioNavigator.NextPressed(3, 2, AudioRepeatMode.Off));
        Assert.Equal(0, AudioNavigator.NextPressed(3, 2, AudioRepeatMode.All));
        Assert.Null(AudioNavigator.NextPressed(3, 2, AudioRepeatMode.One));
        Assert.Equal(1, AudioNavigator.NextPressed(3, 0, AudioRepeatMode.One));
    }

    [Fact]
    public void The_repeat_button_goes_off_list_one_and_back()
    {
        Assert.Equal(AudioRepeatMode.All, AudioNavigator.Cycle(AudioRepeatMode.Off));
        Assert.Equal(AudioRepeatMode.One, AudioNavigator.Cycle(AudioRepeatMode.All));
        Assert.Equal(AudioRepeatMode.Off, AudioNavigator.Cycle(AudioRepeatMode.One));
    }
}
