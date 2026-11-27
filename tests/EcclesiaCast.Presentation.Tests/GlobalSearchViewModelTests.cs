using EcclesiaCast.Presentation.Services;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Presentation.Tests;

public sealed class GlobalSearchViewModelTests
{
    private readonly ManualTimers _timers = new();
    private readonly List<string> _searched = [];

    private GlobalSearchViewModel Create(string initial = "", int found = 3) => new(
        query =>
        {
            _searched.Add(query);
            return Enumerable.Range(1, found)
                .Select(i => new SearchHit(SearchHitKind.Song, i, "♪", $"Canción {i}", "detalle"))
                .ToList();
        },
        initial,
        _timers);

    private ManualTimer Timer => Assert.Single(_timers.Created);

    [Fact]
    public void It_waits_for_a_pause_in_the_typing()
    {
        var search = Create();
        Assert.Equal(TimeSpan.FromMilliseconds(180), Timer.Interval);
        Assert.False(Timer.IsEnabled);
        Assert.Equal(string.Empty, search.Summary);

        search.Query = "sa";
        search.Query = "san";
        Assert.True(Timer.IsEnabled);
        Assert.Empty(_searched);

        Timer.RaiseTick();

        Assert.Equal(["san"], _searched);
        Assert.False(Timer.IsEnabled);
    }

    [Fact]
    public void What_it_opens_with_is_searched_after_the_same_pause()
    {
        var search = Create("  santo ");
        Assert.True(Timer.IsEnabled);

        Timer.RaiseTick();

        Assert.Equal(["santo"], _searched);
        Assert.Equal(0, search.SelectedIndex);
    }

    [Theory]
    [InlineData("", 3, "Escribí para buscar.")]
    [InlineData("   ", 3, "Escribí para buscar.")]
    [InlineData("x", 0, "Nada con eso.")]
    [InlineData("x", 1, "1 resultado.")]
    [InlineData("x", 4, "4 resultados.")]
    public void It_says_how_many_it_found(string query, int found, string expected)
    {
        var search = Create(found: found);
        search.Query = query;

        search.SearchNow();

        Assert.Equal(expected, search.Summary);
        Assert.Equal(query.Trim().Length == 0 || found == 0 ? -1 : 0, search.SelectedIndex);
    }

    [Fact]
    public void Searching_again_chooses_the_first_one_again()
    {
        var search = Create();
        search.Query = "a";
        search.SearchNow();
        search.Move(1);
        var chosen = new List<int>();
        search.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(GlobalSearchViewModel.SelectedIndex))
                chosen.Add(search.SelectedIndex);
        };

        search.Query = "ab";
        search.SearchNow();

        Assert.Equal([-1, 0], chosen);
    }

    [Fact]
    public void The_arrows_do_not_go_past_either_end()
    {
        var search = Create();
        search.Move(1);
        Assert.Equal(-1, search.SelectedIndex);

        search.Query = "a";
        search.SearchNow();
        search.Move(-1);
        Assert.Equal(0, search.SelectedIndex);
        search.Move(1);
        search.Move(1);
        search.Move(1);
        Assert.Equal(2, search.SelectedIndex);
    }

    [Fact]
    public void Accepting_gives_back_the_chosen_one()
    {
        var search = Create();
        Assert.False(search.Accept());
        Assert.Null(search.Result);

        search.Query = "a";
        search.SearchNow();
        search.Move(1);

        Assert.True(search.Accept());
        Assert.Equal("Canción 2", search.Result!.Title);
    }

    [Fact]
    public void Closing_leaves_no_search_waiting()
    {
        var search = Create("a");

        search.Closed();

        Assert.False(Timer.IsEnabled);
    }
}
