using EcclesiaCast.Core.Bible;

namespace EcclesiaCast.Core.Tests.Bible;

/// <summary>
/// The Bibles in <c>installer/bibles</c> go to GitHub and into every installer,
/// so each one must read back whole and have its license written down.
/// </summary>
public class ShippedBiblesTests
{
    private static string? Folder()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "installer", "bibles")))
            directory = directory.Parent;
        return directory is null ? null : Path.Combine(directory.FullName, "installer", "bibles");
    }

    private static IEnumerable<string> Packages() =>
        Folder() is { } folder ? Directory.EnumerateFiles(folder, "*" + BiblePackage.Extension) : [];

    [Fact]
    public void Each_bible_of_the_installer_reads_back_with_the_66_books()
    {
        foreach (var file in Packages())
        {
            using var stream = File.OpenRead(file);
            var entry = Assert.Single(BiblePackage.ReadManifest(stream));
            stream.Position = 0;
            var bible = BiblePackage.ReadVersion(stream, entry);

            Assert.True(bible.MissingBookNumbers.Count == 0, $"{entry.Abbreviation}: faltan libros {string.Join(",", bible.MissingBookNumbers)}");
            Assert.True(bible.VerseCount > 30000, $"{entry.Abbreviation}: sólo {bible.VerseCount} versículos");
        }
    }

    [Fact]
    public void Each_bible_of_the_installer_has_its_license_written_down()
    {
        var folder = Folder();
        if (folder is null)
            return; // not run from the repository

        var licenses = File.ReadAllText(Path.Combine(folder, "LICENCIAS.md"));
        foreach (var file in Packages())
        {
            using var stream = File.OpenRead(file);
            var entry = Assert.Single(BiblePackage.ReadManifest(stream));

            Assert.Contains(Path.GetFileName(file), licenses);
            Assert.Contains(entry.Name, licenses);
        }
    }

    [Theory]
    [InlineData("RVR1960")]
    [InlineData("RV1960")]
    [InlineData("NTV")]
    [InlineData("NVI")]
    [InlineData("TLA")]
    [InlineData("PDT")]
    [InlineData("TCB")]
    [InlineData("LBLA")]
    [InlineData("NBLH")]
    [InlineData("DHH")]
    public void The_installer_never_carries_a_bible_with_rights(string abbreviation)
    {
        foreach (var file in Packages())
        {
            using var stream = File.OpenRead(file);
            foreach (var entry in BiblePackage.ReadManifest(stream))
                Assert.False(BibleInstallPlanner.SameKey(entry.Abbreviation, abbreviation), $"{file} trae {abbreviation}");
        }
    }
}
