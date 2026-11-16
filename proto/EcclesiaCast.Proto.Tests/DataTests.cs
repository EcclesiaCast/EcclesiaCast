using EcclesiaCast.Core.Bible;
using Microsoft.Data.Sqlite;

namespace EcclesiaCast.Proto.Tests;

/// <summary>
/// The prototype's library in a temporary folder: migrations, the bundled
/// RV1909 and the two test songs, exactly as on the first start.
/// </summary>
public sealed class DataTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "ecclesiacast-proto-test-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void First_start_creates_the_library_and_imports_the_Bible()
    {
        var data = ProtoData.Open(_folder, ProtoData.BiblesFolder());

        Assert.True(File.Exists(data.DbPath));
        var bible = Assert.Single(data.Bibles.GetVersions());
        Assert.Equal("RV1909", bible.Abbreviation);
        Assert.True(bible.VerseCount > 30000, $"Faltan versículos: {bible.VerseCount}");

        var john316 = Assert.Single(data.Bibles.GetPassage(bible.Id, new BibleReference(43, 3, 16, 16)));
        Assert.Contains("Dios", john316.Text);

        Assert.Equal(2, data.Songs.GetTitles().Count);
        Assert.Equal(6, data.SongTheme().OutlineWidth);
    }

    [Fact]
    public void Second_start_adds_nothing_again()
    {
        ProtoData.Open(_folder, ProtoData.BiblesFolder());
        var again = ProtoData.Open(_folder, ProtoData.BiblesFolder());

        Assert.Single(again.Bibles.GetVersions());
        Assert.Equal(2, again.Songs.GetTitles().Count);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try
        {
            Directory.Delete(_folder, recursive: true);
        }
        catch (IOException)
        {
            // A leftover temporary folder does no harm.
        }
    }
}
