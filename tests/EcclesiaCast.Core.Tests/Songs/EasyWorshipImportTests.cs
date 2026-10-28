using EcclesiaCast.Core.Songs;
using EcclesiaCast.Data;
using Microsoft.Data.Sqlite;

namespace EcclesiaCast.Core.Tests.Songs;

public class EasyWorshipImportTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "ecclesia-ew-" + Guid.NewGuid().ToString("N"));

    public EasyWorshipImportTests() => Directory.CreateDirectory(_folder);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(_folder, recursive: true);
    }

    private const string Rtf =
        @"{\rtf1\ansi\deff0{\fonttbl{\f0 Arial;}}\pard Verse 1\par Ardiendo en fuego mi alma est\'e1,\par ardiendo en fuego mi alma est\'e1.\par\par Coro\par \'a1Oh Se\'f1or!\par quiero que ardas en mi ser.\par}";

    private SqliteConnection Connect(string file)
    {
        var connection = new SqliteConnection($"Data Source={Path.Combine(_folder, file)};Pooling=False");
        connection.Open();
        return connection;
    }

    private void Exec(string file, params string[] statements)
    {
        using var connection = Connect(file);
        foreach (var sql in statements)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }
    }

    private void Build()
    {
        Exec("Songs.db",
            "CREATE TABLE song (title TEXT, author TEXT, copyright TEXT, description TEXT)",
            "INSERT INTO song (title, author, copyright) VALUES ('Ardiendo en fuego', 'Autor Uno', '2001 Alguien')",
            "INSERT INTO song (title, author, copyright) VALUES ('Sin letra', 'Nadie', NULL)");
        Exec("SongWords.db", "CREATE TABLE word (song_id INTEGER, words TEXT)");

        using var connection = Connect("SongWords.db");
        using var insert = connection.CreateCommand();
        insert.CommandText = "INSERT INTO word (song_id, words) VALUES (1, $rtf)";
        insert.Parameters.AddWithValue("$rtf", Rtf);
        insert.ExecuteNonQuery();
    }

    private List<Song> Read(out int unreadable) => EasyWorshipDatabase.ReadSongs(
        Path.Combine(_folder, "Songs.db"), Path.Combine(_folder, "SongWords.db"), out unreadable);

    [Fact]
    public void Reads_title_author_copyright_and_the_slides()
    {
        Build();

        var songs = Read(out var unreadable);

        var song = Assert.Single(songs);
        Assert.Equal(0, unreadable);
        Assert.Equal("Ardiendo en fuego", song.Title);
        Assert.Equal("Autor Uno", song.Artist);
        Assert.Equal("2001 Alguien", song.Copyright);
        Assert.Equal(2, song.Sections.Count);
        Assert.Equal("Verse 1", song.Sections[0].Label);
        Assert.Equal("Ardiendo en fuego mi alma está,\nardiendo en fuego mi alma está.", song.Sections[0].Text);
        Assert.Equal("Coro", song.Sections[1].Label);
        Assert.Equal("¡Oh Señor!\nquiero que ardas en mi ser.", song.Sections[1].Text);
    }

    [Fact]
    public void A_song_without_words_is_left_out()
    {
        Build();

        Assert.DoesNotContain(Read(out _), s => s.Title == "Sin letra");
    }

    [Fact]
    public void Finds_both_files_whatever_their_capitals()
    {
        File.WriteAllText(Path.Combine(_folder, "songs.DB"), "");
        File.WriteAllText(Path.Combine(_folder, "SONGWORDS.db"), "");

        Assert.NotNull(EasyWorshipDatabase.FindFiles(_folder));
    }

    [Fact]
    public void A_folder_without_the_files_gives_nothing()
    {
        Assert.Null(EasyWorshipDatabase.FindFiles(_folder));
        Assert.Null(EasyWorshipDatabase.FindFiles(Path.Combine(_folder, "no-existe")));
    }

    [Fact]
    public void Plain_text_words_are_accepted_too()
    {
        var song = EasyWorshipImporter.FromRtf("Canción", null, "", ["Uno\ndos\n\nTres\ncuatro"]);

        Assert.Equal(2, song.Sections.Count);
        Assert.Null(song.Copyright);
    }

    [Fact]
    public void A_database_that_is_not_EasyWorship_gives_a_clear_error()
    {
        Exec("Songs.db", "CREATE TABLE otra (x TEXT)");
        Exec("SongWords.db", "CREATE TABLE otra (x TEXT)");

        Assert.Throws<InvalidDataException>(() => Read(out _));
    }
}
