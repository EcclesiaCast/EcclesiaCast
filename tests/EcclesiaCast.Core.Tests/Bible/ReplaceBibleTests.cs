using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Bible;
using EcclesiaCast.Core.Playlists;
using EcclesiaCast.Core.Tests.Persistence;
using EcclesiaCast.Data.Persistence;

namespace EcclesiaCast.Core.Tests.Bible;

/// <summary>
/// Downloading or importing a Bible the library already has: the program asks
/// once for all of them, and the new copy takes the place of the old one.
/// </summary>
public class ReplaceBibleTests
{
    private static BiblePackageEntry Entry(string name, string abbreviation) => new("x.json", name, abbreviation, "es");

    private static BibleVersionInfo Installed(int id, string name, string abbreviation) => new(id, name, abbreviation, "es", 100);

    private static ParsedBible Sample(string text) => new(
    [
        new ParsedBook(43, "Juan", [new ParsedVerse(3, 16, text)]),
    ]);

    [Fact]
    public void Every_installed_copy_of_the_same_bible_is_replaced()
    {
        // The library had NTV twice: a new copy takes the place of both.
        var installed = new[] { Installed(1, "NTV", "NTV"), Installed(2, "Nueva Traducción Viviente", "NTV"), Installed(3, "NVI", "NVI") };

        var matching = BibleInstallPlanner.Matching(Entry("Nueva Traducción Viviente", "NTV"), installed);

        Assert.Equal([1, 2], matching.Select(v => v.Id));
    }

    [Fact]
    public void A_bible_the_library_does_not_have_replaces_nothing()
    {
        var matching = BibleInstallPlanner.Matching(Entry("King James Version", "KJV"), [Installed(1, "Reina-Valera 1909", "RV1909")]);

        Assert.Empty(matching);
    }

    [Fact]
    public void The_same_bible_is_found_by_its_name_under_another_abbreviation()
    {
        var matching = BibleInstallPlanner.Matching(Entry("Reina-Valera 1909", "RV1909"), [Installed(7, "reina valera 1909", "RV09")]);

        Assert.Equal([7], matching.Select(v => v.Id));
    }

    [Fact]
    public void One_repeated_bible_is_asked_about_by_its_name()
    {
        var question = BibleInstallPlanner.ReplaceQuestion(["NTV"], "bajar", othersToo: false);

        Assert.Contains("Ya tenés «NTV» en tu biblioteca.", question);
        Assert.Contains("¿La querés bajar igual?", question);
        Assert.Contains("Se borra la que tenés", question);
        Assert.DoesNotContain("No:", question);
    }

    [Fact]
    public void Several_repeated_bibles_are_asked_about_all_together()
    {
        var question = BibleInstallPlanner.ReplaceQuestion(["NTV", "RV1909"], "importar", othersToo: true);

        Assert.Contains("Ya tenés estas Biblias en tu biblioteca: NTV, RV1909.", question);
        Assert.Contains("¿Las querés importar igual?", question);
        // With new ones among the chosen, «No» still brings those.
        Assert.Contains("No: se importan sólo las que no tenés.", question);
    }

    [Fact]
    public void Replacing_moves_the_playlist_passages_to_the_new_copy_and_deletes_the_old_one()
    {
        using var temp = new TempDatabase();
        var bibles = new BibleRepository(temp.Path);
        var playlists = new PlaylistRepository(temp.Path);
        var old = bibles.Import("Nueva Traducción Viviente", "NTV", "es", Sample("viejo"));
        var other = bibles.Import("Reina-Valera 1909", "RV1909", "es", Sample("otra"));
        var saved = playlists.Save(new Playlist
        {
            Name = "Domingo",
            Items =
            [
                new PlaylistItem { Order = 0, Type = PlaylistItemType.BiblePassage, Caption = "Juan 3:16 (NTV)", BibleVersionId = old.Id, BookNumber = 43, Chapter = 3, VerseStart = 16 },
                new PlaylistItem { Order = 1, Type = PlaylistItemType.BiblePassage, Caption = "Juan 3:16 (RV1909)", BibleVersionId = other.Id, BookNumber = 43, Chapter = 3, VerseStart = 16 },
            ],
        });

        var fresh = bibles.Import("Nueva Traducción Viviente", "NTV", "es", Sample("nuevo"));
        bibles.ReplaceVersion(old.Id, fresh.Id);

        Assert.Equal([other.Id, fresh.Id], bibles.GetVersions().Select(v => v.Id).Order());
        var items = playlists.Get(saved.Id)!.Items.OrderBy(i => i.Order).ToList();
        Assert.Equal(fresh.Id, items[0].BibleVersionId);
        Assert.Equal("Juan 3:16 (NTV)", items[0].Caption);
        Assert.Equal(other.Id, items[1].BibleVersionId);
        Assert.Equal("nuevo", bibles.GetPassage(fresh.Id, new BibleReference(43, 3, 16, 16))[0].Text);
    }

    [Fact]
    public void A_new_copy_with_another_abbreviation_fixes_the_playlist_captions()
    {
        using var temp = new TempDatabase();
        var bibles = new BibleRepository(temp.Path);
        var playlists = new PlaylistRepository(temp.Path);
        var old = bibles.Import("Reina-Valera 1909", "RV09", "es", Sample("viejo"));
        var saved = playlists.Save(new Playlist
        {
            Name = "Domingo",
            Items = [new PlaylistItem { Type = PlaylistItemType.BiblePassage, Caption = "Juan 3:16 (RV09)", BibleVersionId = old.Id, BookNumber = 43, Chapter = 3, VerseStart = 16 }],
        });

        var fresh = bibles.Import("Reina-Valera 1909", "RV1909", "es", Sample("nuevo"));
        bibles.ReplaceVersion(old.Id, fresh.Id);

        var item = playlists.Get(saved.Id)!.Items.Single();
        Assert.Equal(fresh.Id, item.BibleVersionId);
        Assert.Equal("Juan 3:16 (RV1909)", item.Caption);
    }
}
