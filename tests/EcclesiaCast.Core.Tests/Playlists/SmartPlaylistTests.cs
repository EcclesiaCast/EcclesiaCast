using EcclesiaCast.Core.Playlists;
using EcclesiaCast.Core.Songs;

namespace EcclesiaCast.Core.Tests.Playlists;

public class SmartPlaylistTests
{
    private static readonly DateTime Sunday = new(2026, 9, 13, 10, 0, 0);

    private static Song SongNamed(string title, string artist = "", int addedDaysAgo = 0,
        int? projectedDaysAgo = null, string lyrics = "")
    {
        return new Song
        {
            Title = title,
            Artist = artist,
            AddedAt = Sunday.AddDays(-addedDaysAgo),
            LastProjectedAt = projectedDaysAgo is { } d ? Sunday.AddDays(-d) : null,
            Sections = lyrics.Length == 0 ? [] : [new SongSection { Text = lyrics }],
        };
    }

    [Fact]
    public void Recently_added_takes_the_new_ones_newest_first()
    {
        List<Song> library =
        [
            SongNamed("Vieja", addedDaysAgo: 200),
            SongNamed("De ayer", addedDaysAgo: 1),
            SongNamed("Del mes pasado", addedDaysAgo: 25),
        ];

        var chosen = SmartPlaylist.Select(library, PlaylistRule.RecentlyAdded, "30", Sunday);

        Assert.Equal(["De ayer", "Del mes pasado"], chosen.Select(s => s.Title));
    }

    [Fact]
    public void Songs_never_projected_lead_the_forgotten_list()
    {
        List<Song> library =
        [
            SongNamed("La de todos los domingos", projectedDaysAgo: 3),
            SongNamed("Nunca la cantamos"),
            SongNamed("La del año pasado", projectedDaysAgo: 300),
        ];

        var chosen = SmartPlaylist.Select(library, PlaylistRule.NotSungLately, "90", Sunday);

        Assert.Equal(["Nunca la cantamos", "La del año pasado"], chosen.Select(s => s.Title));
    }

    [Fact]
    public void A_song_sung_last_week_is_not_forgotten()
    {
        List<Song> library = [SongNamed("Reciente", projectedDaysAgo: 7)];

        Assert.Empty(SmartPlaylist.Select(library, PlaylistRule.NotSungLately, "90", Sunday));
    }

    [Fact]
    public void By_artist_ignores_capitals_and_matches_part_of_the_name()
    {
        List<Song> library =
        [
            SongNamed("Mi Gozo", "Barak"),
            SongNamed("Bautizados en Fuego", "MonteSanto"),
            SongNamed("Otra de Barak", "barak ft. Marcos Brunet"),
        ];

        var chosen = SmartPlaylist.Select(library, PlaylistRule.ByArtist, "BARAK", Sunday);

        Assert.Equal(["Mi Gozo", "Otra de Barak"], chosen.Select(s => s.Title));
    }

    [Fact]
    public void Containing_looks_in_the_title_and_in_the_lyrics()
    {
        List<Song> library =
        [
            SongNamed("Santo espíritu", lyrics: "Ven a este lugar"),
            SongNamed("Otra", lyrics: "Eres santo, Señor"),
            SongNamed("Ninguna", lyrics: "Cantaré de tu amor"),
        ];

        var chosen = SmartPlaylist.Select(library, PlaylistRule.Containing, "santo", Sunday);

        Assert.Equal(["Otra", "Santo espíritu"], chosen.Select(s => s.Title).OrderBy(t => t));
    }

    [Fact]
    public void A_rule_without_its_parameter_returns_nothing_rather_than_everything()
    {
        List<Song> library = [SongNamed("Mi Gozo", "Barak")];

        Assert.Empty(SmartPlaylist.Select(library, PlaylistRule.ByArtist, "  ", Sunday));
        Assert.Empty(SmartPlaylist.Select(library, PlaylistRule.Containing, null, Sunday));
    }

    [Fact]
    public void A_nonsense_number_of_days_falls_back_instead_of_emptying_the_list()
    {
        List<Song> library = [SongNamed("De ayer", addedDaysAgo: 1)];

        Assert.Single(SmartPlaylist.Select(library, PlaylistRule.RecentlyAdded, "no es un número", Sunday));
        Assert.Contains("30 días", SmartPlaylist.Describe(PlaylistRule.RecentlyAdded, "-5"));
    }

    [Fact]
    public void A_hand_made_list_is_never_filled_in()
    {
        List<Song> library = [SongNamed("Mi Gozo")];

        Assert.Empty(SmartPlaylist.Select(library, PlaylistRule.None, "30", Sunday));
    }
}
