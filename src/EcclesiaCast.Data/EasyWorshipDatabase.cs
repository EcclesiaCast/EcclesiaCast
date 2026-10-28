using System.Text;
using EcclesiaCast.Core.Songs;
using Microsoft.Data.Sqlite;

namespace EcclesiaCast.Data;

/// <summary>
/// Reads the songs of an EasyWorship 6 or 7 library: <c>Songs.db</c> (title,
/// author, copyright) and <c>SongWords.db</c> (the words, in RTF), both SQLite.
/// The files are opened read-only and never changed.
///
/// The tables and columns are found by what they hold rather than assumed, so
/// a small difference between versions of EasyWorship does not stop the import.
/// </summary>
public static class EasyWorshipDatabase
{
    public const string SongsFile = "Songs.db";
    public const string WordsFile = "SongWords.db";

    /// <summary>The two files in a folder, in any capitals; null when either is missing.</summary>
    public static (string Songs, string Words)? FindFiles(string folder)
    {
        if (!Directory.Exists(folder))
            return null;

        string? Find(string name) => Directory.EnumerateFiles(folder)
            .FirstOrDefault(f => string.Equals(Path.GetFileName(f), name, StringComparison.OrdinalIgnoreCase));

        var songs = Find(SongsFile);
        var words = Find(WordsFile);
        return songs is null || words is null ? null : (songs, words);
    }

    /// <summary>Every song with words. Rows that cannot be read are counted in <paramref name="unreadable"/>.</summary>
    public static List<Song> ReadSongs(string songsPath, string wordsPath, out int unreadable)
    {
        unreadable = 0;
        var words = ReadWords(wordsPath);
        var result = new List<Song>();

        using var connection = Open(songsPath);
        var table = FindTable(connection, ["title"])
            ?? throw new InvalidDataException("No se encontró la tabla de canciones en " + Path.GetFileName(songsPath));
        var columns = Columns(connection, table);
        string? Pick(params string[] names) => names.FirstOrDefault(n => columns.Contains(n));

        var author = Pick("author", "authors");
        var copyright = Pick("copyright");

        using var command = connection.CreateCommand();
        command.CommandText =
            $"SELECT rowid, title, {(author is null ? "NULL" : Quote(author))}, {(copyright is null ? "NULL" : Quote(copyright))} FROM {Quote(table)} ORDER BY rowid";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            try
            {
                var id = reader.GetInt64(0);
                if (!words.TryGetValue(id, out var rtf))
                    continue;

                var song = EasyWorshipImporter.FromRtf(
                    Text(reader, 1), Text(reader, 2), Text(reader, 3), rtf);
                if (song.Sections.Count > 0)
                    result.Add(song);
            }
            catch (Exception ex) when (ex is InvalidCastException or FormatException or ArgumentException)
            {
                unreadable++;
            }
        }

        return result;
    }

    /// <summary>The RTF of each song, by the song's row number, in the order of the rows.</summary>
    private static Dictionary<long, List<string>> ReadWords(string wordsPath)
    {
        using var connection = Open(wordsPath);
        var table = FindTable(connection, ["song_id", "words"])
            ?? throw new InvalidDataException("No se encontró la tabla de letras en " + Path.GetFileName(wordsPath));

        var words = new Dictionary<long, List<string>>();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT song_id, words FROM {Quote(table)} ORDER BY rowid";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (reader.IsDBNull(0) || reader.IsDBNull(1))
                continue;

            var text = reader.GetValue(1) switch
            {
                byte[] bytes => Decode(bytes),
                var other => other.ToString() ?? string.Empty,
            };

            var id = Convert.ToInt64(reader.GetValue(0));
            if (!words.TryGetValue(id, out var list))
                words[id] = list = [];
            list.Add(text);
        }

        return words;
    }

    /// <summary>The words may be stored as bytes: UTF-8 when it is, Windows Latin-1 otherwise.</summary>
    private static string Decode(byte[] bytes)
    {
        try
        {
            return new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            return Encoding.Latin1.GetString(bytes);
        }
    }

    private static SqliteConnection Open(string path)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false,
        }.ToString());
        connection.Open();
        return connection;
    }

    /// <summary>The first table that has all these columns, preferring «song» or «word» by name.</summary>
    private static string? FindTable(SqliteConnection connection, string[] required)
    {
        var tables = new List<string>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%'";
            using var reader = command.ExecuteReader();
            while (reader.Read())
                tables.Add(reader.GetString(0));
        }

        return tables
            .Where(t => required.All(Columns(connection, t).Contains))
            .OrderByDescending(t => t.Equals("song", StringComparison.OrdinalIgnoreCase) || t.Equals("word", StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();
    }

    private static HashSet<string> Columns(SqliteConnection connection, string table)
    {
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({Quote(table)})";
        using var reader = command.ExecuteReader();
        while (reader.Read())
            columns.Add(reader.GetString(1));
        return columns;
    }

    private static string Quote(string name) => "\"" + name.Replace("\"", "\"\"") + "\"";

    private static string Text(SqliteDataReader reader, int index) =>
        reader.IsDBNull(index) ? string.Empty : reader.GetValue(index).ToString() ?? string.Empty;
}
