using EcclesiaCast.Core.Capture;

namespace EcclesiaCast.Core.Tests.Capture;

public class WindowCandidatesTests
{
    private const int Own = 4242;

    private static RawWindow Ok(long handle, string title, int processId = 100) =>
        new(handle, processId, "algo.exe", title);

    [Fact]
    public void Keeps_an_ordinary_window()
    {
        var kept = WindowCandidates.Filter([Ok(1, "Descargas")], Own);

        Assert.Single(kept);
    }

    [Fact]
    public void Drops_the_windows_of_this_very_program()
    {
        var kept = WindowCandidates.Filter([Ok(1, "EcclesiaCast", Own), Ok(2, "Otra")], Own);

        Assert.Equal([2L], kept.Select(w => w.Handle));
    }

    [Fact]
    public void Drops_what_nobody_would_call_a_window()
    {
        var windows = new[]
        {
            Ok(1, "visible"),
            Ok(2, "invisible") with { IsVisible = false },
            Ok(3, "oculta por el escritorio") with { IsCloaked = true },
            Ok(4, "paleta") with { IsToolWindow = true },
            Ok(5, "diálogo con dueño") with { HasOwner = true },
            Ok(6, "") ,
            Ok(7, "   "),
            Ok(8, "sin ancho") with { Width = 0 },
            Ok(9, "sin alto") with { Height = 0 },
        };

        var kept = WindowCandidates.Filter(windows, Own);

        Assert.Equal([1L], kept.Select(w => w.Handle));
    }

    [Fact]
    public void Sorts_by_title_without_mind_to_capitals()
    {
        var kept = WindowCandidates.Filter([Ok(1, "zeta"), Ok(2, "Alfa"), Ok(3, "beta")], Own);

        Assert.Equal(["Alfa", "beta", "zeta"], kept.Select(w => w.Title));
    }

    [Fact]
    public void Nothing_open_gives_an_empty_list()
    {
        Assert.Empty(WindowCandidates.Filter([], Own));
    }
}
