using EcclesiaCast.Presentation.Rendering;
using EcclesiaCast.RenderCompare;

namespace EcclesiaCast.Desktop.Tests;

/// <summary>The comparator itself, with made-up measures: it has to notice what it is meant to notice.</summary>
public sealed class ComparisonTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "ecclesiacast-compare-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, true);
    }

    private static RenderedText Text(double size, double x, params string[] lines) => new()
    {
        Name = "main",
        FontSize = size,
        Lines = [.. lines],
        Rect = new RenderRect { X = x, Y = 100, Width = 1000, Height = 400 },
    };

    private string Save(string side, RenderedText text)
    {
        var folder = Path.Combine(_root, side);
        Directory.CreateDirectory(folder);
        var result = new RenderResult { Case = "uno", Theme = "cancion", Width = 1920, Height = 1080, Texts = [text] };
        File.WriteAllText(Path.Combine(folder, "uno.cancion.json"), result.ToJson());
        return folder;
    }

    private Comparison Compare(RenderedText left, RenderedText right, Criteria criteria) =>
        Comparison.Run(new Comparison
        {
            Title = "t",
            LeftName = "A",
            RightName = "B",
            LeftFolder = Save("a", left),
            RightFolder = Save("b", right),
            Criteria = criteria,
        });

    [Fact]
    public void The_same_measures_pass()
    {
        var c = Compare(Text(90, 100, "uno dos", "tres"), Text(90, 100, "uno dos", "tres"), Criteria.Exact);
        Assert.True(c.Results.Single().Passes);
    }

    [Fact]
    public void A_line_cut_elsewhere_is_told_in_words()
    {
        var c = Compare(Text(90, 100, "amó al mundo", "Dios"), Text(90, 100, "amó", "al mundo Dios"), Criteria.Loose);
        var difference = Assert.Single(c.Results.Single().Differences);
        Assert.Equal(Reason.CutsElsewhere, difference.Reason);
        Assert.Contains("«mundo»", difference.Words);
        Assert.Contains("«amó»", difference.Words);
    }

    [Fact]
    public void A_different_number_of_lines_is_its_own_reason()
    {
        var c = Compare(Text(90, 100, "uno dos tres"), Text(90, 100, "uno dos", "tres"), Criteria.Loose);
        Assert.Equal(Reason.LineCount, c.Results.Single().Differences.Single().Reason);
    }

    [Fact]
    public void Size_and_rectangle_have_their_margins()
    {
        // 2 % of size and 19 px are allowed...
        var near = Compare(Text(90, 100, "a"), Text(88.5, 118, "a"), Criteria.Loose);
        Assert.True(near.Results.Single().Passes);

        // ...and a little more than that is not, nor is anything when it has to be exact.
        var far = Compare(Text(90, 100, "a"), Text(80, 125, "a"), Criteria.Loose);
        Assert.Equal([Reason.FontSize, Reason.Rectangle], far.Results.Single().Differences.Select(d => d.Reason).ToArray());
        var exact = Compare(Text(90, 100, "a"), Text(90, 101, "a"), Criteria.Exact);
        Assert.Equal(Reason.Rectangle, exact.Results.Single().Differences.Single().Reason);
    }

    [Fact]
    public void With_one_typeface_only_its_texts_are_compared()
    {
        // The countdown is Segoe UI on Windows and Selawik on a Mac: not compared, but told.
        var clock = Text(260, 100, "10:00");
        clock.FontUsed = "Segoe UI Semibold";
        var macClock = Text(250, 120, "10:00");
        var c = Comparison.Run(new Comparison
        {
            Title = "t", LeftName = "A", RightName = "B",
            LeftFolder = Save("a", clock), RightFolder = Save("b", macClock),
            Criteria = Criteria.Exact,
            OnlyFont = "Selawik",
        });

        Assert.True(c.Results.Single().Passes);
        Assert.Equal(["uno.cancion: main"], c.Skipped);
        Assert.Contains("No se comparan", Report.Summary([c]));

        var main = Text(90, 100, "a");
        main.FontUsed = "Selawik Semibold";
        var moved = Text(90, 101, "a");
        var d = Comparison.Run(new Comparison
        {
            Title = "t", LeftName = "A", RightName = "B",
            LeftFolder = Save("c", main), RightFolder = Save("d", moved),
            Criteria = Criteria.Exact,
            OnlyFont = "Selawik",
        });
        Assert.False(d.Results.Single().Passes);
    }

    [Fact]
    public void A_missing_folder_says_so_instead_of_failing()
    {
        var c = Comparison.Run(new Comparison
        {
            Title = "t", LeftName = "A", RightName = "B",
            LeftFolder = Path.Combine(_root, "no"), RightFolder = Path.Combine(_root, "tampoco"),
            Criteria = Criteria.Loose,
        });
        Assert.NotNull(c.NotRun);
        Assert.Contains("No se comparó", Report.Summary([c]));
    }
}
