using EcclesiaCast.Core.Bible;

namespace EcclesiaCast.Core.Tests.Bible;

public class BibleCatalogTests
{
    private static readonly Uri List = new("https://pub-123.r2.dev/catalogo.json");
    private static readonly string Sha = new('a', 64);

    private static string Line(string abbreviation, string file = "X.ecbiblias", string language = "es", long size = 100, string? sha = null) =>
        $$"""{ "name": "Biblia {{abbreviation}}", "abbreviation": "{{abbreviation}}", "language": "{{language}}", "file": "{{file}}", "size": {{size}}, "sha256": "{{sha ?? Sha}}", "license": "Dominio público" }""";

    private static string Catalog(params string[] lines) => $$"""{ "format": 1, "versions": [ {{string.Join(",", lines)}} ] }""";

    [Fact]
    public void Files_are_found_next_to_the_list()
    {
        var item = Assert.Single(BibleCatalog.Parse(Catalog(Line("RV1909", "RV1909.ecbiblias")), List));

        Assert.Equal("https://pub-123.r2.dev/RV1909.ecbiblias", item.Url.ToString());
        Assert.Equal("Biblia RV1909", item.Name);
        Assert.Equal(100, item.Size);
        Assert.Equal("Dominio público", item.License);
    }

    [Fact]
    public void Lines_that_are_incomplete_or_not_https_are_left_out()
    {
        var items = BibleCatalog.Parse(Catalog(
            Line("OK"),
            Line("SINTAM", size: 0),
            Line("MALSHA", sha: "abc"),
            Line("HTTP", file: "http://otro.sitio/x.ecbiblias"),
            Line("FTP", file: "ftp://otro.sitio/x.ecbiblias")), List);

        Assert.Equal(["OK"], items.Select(i => i.Abbreviation));
    }

    [Fact]
    public void Http_is_allowed_only_on_this_computer()
    {
        Assert.True(BibleCatalog.IsAllowed(new Uri("http://localhost:8765/catalogo.json")));
        Assert.True(BibleCatalog.IsAllowed(new Uri("https://pub-123.r2.dev/catalogo.json")));
        Assert.False(BibleCatalog.IsAllowed(new Uri("http://pub-123.r2.dev/catalogo.json")));
    }

    [Fact]
    public void Another_format_asks_for_a_newer_program()
    {
        Assert.Throws<FormatException>(() => BibleCatalog.Parse("""{ "format": 2, "versions": [] }""", List));
        Assert.Throws<FormatException>(() => BibleCatalog.Parse("no es json", List));
    }

    [Fact]
    public void Spanish_comes_first_then_the_other_languages()
    {
        var items = BibleCatalog.Parse(Catalog(
            Line("KJV", language: "en"), Line("RV1909"), Line("ARC", language: "pt"), Line("BLM")), List);

        var groups = BibleCatalog.ByLanguage(items);

        Assert.Equal(["Español", "Inglés", "Portugués"], groups.Select(g => g.Language));
        Assert.Equal(2, groups[0].Items.Count);
    }

    [Fact]
    public void A_written_list_reads_back()
    {
        var item = new BibleCatalogItem("Reina-Valera 1909", "RV1909", "es", new Uri("https://x/RV1909.ecbiblias"), 1234, Sha, "Dominio público");

        var back = Assert.Single(BibleCatalog.Parse(BibleCatalog.Write([(item, "RV1909.ecbiblias")]), List));

        Assert.Equal(item with { Url = new Uri("https://pub-123.r2.dev/RV1909.ecbiblias") }, back);
    }
}
