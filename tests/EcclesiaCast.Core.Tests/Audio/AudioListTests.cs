using EcclesiaCast.Core.Audio;

namespace EcclesiaCast.Core.Tests.Audio;

public class AudioListTests
{
    [Theory]
    [InlineData(@"C:\musica\Ofrenda.mp3", true)]
    [InlineData(@"C:\musica\Ofrenda.MP3", true)]
    [InlineData(@"C:\musica\sonido.wav", true)]
    [InlineData(@"C:\musica\pista.m4a", true)]
    [InlineData(@"C:\musica\pista.ogg", true)]
    [InlineData(@"C:\musica\pista.flac", true)]
    [InlineData(@"C:\videos\clip.mp4", false)]
    [InlineData(@"C:\fotos\logo.png", false)]
    [InlineData(@"C:\musica\sin-extension", false)]
    public void Only_the_five_sound_formats_are_taken(string path, bool expected)
    {
        Assert.Equal(expected, AudioFiles.IsAudio(path));
    }

    [Fact]
    public void A_track_is_named_after_its_file_without_the_extension()
    {
        Assert.Equal("Sublime gracia", AudioFiles.DisplayName(@"C:\musica\Sublime gracia.mp3"));
    }

    [Fact]
    public void A_file_with_only_an_extension_keeps_its_whole_name()
    {
        Assert.Equal(".mp3", AudioFiles.DisplayName(@"C:\musica\.mp3"));
    }

    [Theory]
    [InlineData(3, -1, 0)]
    [InlineData(3, 0, 1)]
    [InlineData(3, 1, 2)]
    public void Next_goes_down_the_list(int count, int current, int expected)
    {
        Assert.Equal(expected, AudioNavigator.Next(count, current));
    }

    [Fact]
    public void Next_stops_at_the_end_instead_of_starting_over()
    {
        Assert.Null(AudioNavigator.Next(3, 2));
    }

    [Fact]
    public void Next_has_nowhere_to_go_with_an_empty_list()
    {
        Assert.Null(AudioNavigator.Next(0, -1));
    }

    [Fact]
    public void Previous_a_few_seconds_in_starts_the_same_track_over()
    {
        Assert.Equal(2, AudioNavigator.Previous(5, 2, positionSeconds: 12));
    }

    [Fact]
    public void Previous_at_the_start_goes_back_one_track()
    {
        Assert.Equal(1, AudioNavigator.Previous(5, 2, positionSeconds: 1.5));
    }

    [Fact]
    public void Previous_on_the_first_track_starts_it_over()
    {
        Assert.Equal(0, AudioNavigator.Previous(5, 0, positionSeconds: 0));
    }

    [Fact]
    public void Previous_with_nothing_playing_picks_the_first_track()
    {
        Assert.Equal(0, AudioNavigator.Previous(5, -1, positionSeconds: 0));
    }

    [Fact]
    public void Previous_has_nowhere_to_go_with_an_empty_list()
    {
        Assert.Null(AudioNavigator.Previous(0, -1, 0));
    }

    [Fact]
    public void A_track_moves_down_one_place()
    {
        var list = new List<string> { "a", "b", "c" };

        Assert.Equal(1, ListOrder.Move(list, 0, +1));
        Assert.Equal(["b", "a", "c"], list);
    }

    [Fact]
    public void A_track_moves_up_one_place()
    {
        var list = new List<string> { "a", "b", "c" };

        Assert.Equal(1, ListOrder.Move(list, 2, -1));
        Assert.Equal(["a", "c", "b"], list);
    }

    [Theory]
    [InlineData(0, -1)]
    [InlineData(2, +1)]
    [InlineData(-1, +1)]
    [InlineData(5, -1)]
    public void A_track_cannot_move_past_either_end(int index, int delta)
    {
        var list = new List<string> { "a", "b", "c" };

        Assert.Equal(-1, ListOrder.Move(list, index, delta));
        Assert.Equal(["a", "b", "c"], list);
    }

    [Fact]
    public void Numbers_inside_names_are_compared_by_value()
    {
        var names = new List<string> { "Canción 10", "Canción 2", "Canción 1" };

        names.Sort(ListOrder.CompareNatural);

        Assert.Equal(["Canción 1", "Canción 2", "Canción 10"], names);
    }

    [Fact]
    public void Names_are_sorted_ignoring_case_and_accents()
    {
        var names = new List<string> { "zamba", "Árbol", "ayuda", "Bendición" };

        names.Sort(ListOrder.CompareNatural);

        Assert.Equal(["Árbol", "ayuda", "Bendición", "zamba"], names);
    }

    [Fact]
    public void Leading_zeros_do_not_change_the_value_of_a_number()
    {
        Assert.True(ListOrder.CompareNatural("pista 02", "pista 10") < 0);
        Assert.Equal(0, ListOrder.CompareNatural("pista 02", "pista 2"));
    }

    [Fact]
    public void A_name_that_is_the_start_of_another_goes_first()
    {
        Assert.True(ListOrder.CompareNatural("Gloria", "Gloria a Dios") < 0);
    }
}
