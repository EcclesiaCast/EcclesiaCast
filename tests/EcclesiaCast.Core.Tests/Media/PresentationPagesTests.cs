using EcclesiaCast.Core.Media;
using EcclesiaCast.Core.Playlists;

namespace EcclesiaCast.Core.Tests.Media;

public class PresentationPagesTests
{
    private static MediaItem Item(int id, string tab, MediaType type = MediaType.Image, string? thumbnail = "t.png") =>
        new() { Id = id, Name = $"Página {id}", Path = $@"C:\medios\{id}.png", Category = tab, Type = type, ThumbnailPath = thumbnail };

    [Fact]
    public void The_pages_are_the_images_and_videos_of_the_tab_in_library_order()
    {
        var library = new[]
        {
            Item(1, "Fondos"), Item(2, "Charla"), Item(3, "Charla", MediaType.Video), Item(4, "Charla"),
        };

        var pages = PresentationPages.For("Charla", library);

        Assert.Equal([2, 3, 4], pages.Select(p => p.Id));
    }

    [Fact]
    public void YouTube_videos_and_saved_web_pages_are_not_pages()
    {
        var library = new[]
        {
            Item(1, "Charla"), Item(2, "Charla", MediaType.YouTube), Item(3, "Charla", MediaType.Web),
        };

        Assert.Equal([1], PresentationPages.For("Charla", library).Select(p => p.Id));
    }

    [Fact]
    public void The_tab_is_found_without_looking_at_capitals()
    {
        var library = new[] { Item(1, "Charla") };

        Assert.Single(PresentationPages.For("CHARLA", library));
    }

    [Fact]
    public void A_tab_without_a_name_has_no_pages()
    {
        Assert.Empty(PresentationPages.For(null, [Item(1, "Charla")]));
        Assert.Empty(PresentationPages.For("  ", [Item(1, "Charla")]));
    }

    [Fact]
    public void The_signature_is_the_same_while_nothing_shown_in_the_grid_changes()
    {
        var before = PresentationPages.Signature([Item(1, "Charla"), Item(2, "Charla")]);
        var after = PresentationPages.Signature([Item(1, "Charla"), Item(2, "Charla")]);

        Assert.Equal(before, after);
    }

    [Fact]
    public void The_signature_changes_with_the_order_the_poster_or_a_missing_page()
    {
        var baseline = PresentationPages.Signature([Item(1, "Charla"), Item(2, "Charla")]);

        Assert.NotEqual(baseline, PresentationPages.Signature([Item(2, "Charla"), Item(1, "Charla")]));
        Assert.NotEqual(baseline, PresentationPages.Signature([Item(1, "Charla"), Item(2, "Charla", thumbnail: "otra.png")]));
        Assert.NotEqual(baseline, PresentationPages.Signature([Item(1, "Charla")]));
    }

    [Fact]
    public void A_playlist_entry_for_a_presentation_is_stored_as_the_number_three()
    {
        // The column is an INTEGER: adding the type at the end of the enum is
        // what spares the database a migration.
        Assert.Equal(3, (int)PlaylistItemType.Presentation);
        Assert.Equal(2, (int)PlaylistItemType.Media);
    }
}

public class MediaTabUniqueNameTests
{
    [Fact]
    public void A_free_name_is_kept_as_it_is()
    {
        Assert.Equal("Charla", MediaTabNames.Unique(["Fondos", "Anuncios"], "Charla"));
    }

    [Fact]
    public void A_taken_name_gets_a_number()
    {
        Assert.Equal("Charla (2)", MediaTabNames.Unique(["Fondos", "Charla"], "Charla"));
    }

    [Fact]
    public void The_number_goes_up_until_the_name_is_free()
    {
        Assert.Equal("Charla (3)", MediaTabNames.Unique(["Charla", "Charla (2)"], "Charla"));
    }

    [Fact]
    public void Capitals_do_not_make_a_name_free()
    {
        Assert.Equal("CHARLA (2)", MediaTabNames.Unique(["charla"], "CHARLA"));
    }

    [Fact]
    public void A_blank_name_falls_back_to_the_first_starting_tab()
    {
        Assert.Equal("Fondos (2)", MediaTabNames.Unique(["Fondos"], "   "));
    }
}
