using EcclesiaCast.Core.Media;

namespace EcclesiaCast.Core.Tests.Media;

public class ShuffleBagTests
{
    /// <summary>A fixed seed keeps the shuffling deterministic across runs.</summary>
    private static Random Seeded() => new(20260823);

    [Fact]
    public void Every_item_comes_up_once_before_any_repeats()
    {
        var bag = new ShuffleBag();
        var random = Seeded();
        int[] pool = [1, 2, 3, 4, 5];

        var round = Enumerable.Range(0, 5).Select(_ => bag.Take(pool, random)).ToList();

        Assert.Equal(pool.OrderBy(i => i), round.Select(i => i!.Value).OrderBy(i => i));
    }

    [Fact]
    public void The_bag_refills_and_keeps_serving()
    {
        var bag = new ShuffleBag();
        var random = Seeded();
        int[] pool = [1, 2, 3];

        var draws = Enumerable.Range(0, 9).Select(_ => bag.Take(pool, random)!.Value).ToList();

        Assert.All(draws, d => Assert.Contains(d, pool));
        // Three full rounds: each id exactly three times.
        Assert.All(pool, id => Assert.Equal(3, draws.Count(d => d == id)));
    }

    [Fact]
    public void The_same_item_never_comes_up_twice_in_a_row()
    {
        // Across rounds is where naive shuffling repeats: the last of one
        // round and the first of the next can be the same id.
        var bag = new ShuffleBag();
        var random = Seeded();
        int[] pool = [1, 2, 3, 4];

        var draws = Enumerable.Range(0, 200).Select(_ => bag.Take(pool, random)!.Value).ToList();

        for (var i = 1; i < draws.Count; i++)
            Assert.NotEqual(draws[i - 1], draws[i]);
    }

    [Fact]
    public void An_empty_pool_gives_nothing()
    {
        Assert.Null(new ShuffleBag().Take([], Seeded()));
    }

    [Fact]
    public void A_single_item_pool_always_gives_that_item()
    {
        var bag = new ShuffleBag();
        var random = Seeded();

        Assert.Equal(7, bag.Take([7], random));
        Assert.Equal(7, bag.Take([7], random));
    }

    [Fact]
    public void Items_removed_from_the_pool_are_skipped()
    {
        var bag = new ShuffleBag();
        var random = Seeded();
        int[] full = [1, 2, 3, 4, 5];
        bag.Take(full, random);

        // The media tab lost some items while the bag still held their ids.
        int[] shrunk = [1, 2];
        var draws = Enumerable.Range(0, 10).Select(_ => bag.Take(shrunk, random)!.Value).ToList();

        Assert.All(draws, d => Assert.Contains(d, shrunk));
    }

    [Fact]
    public void Items_added_to_the_pool_appear_in_the_next_round()
    {
        var bag = new ShuffleBag();
        var random = Seeded();
        int[] small = [1, 2];
        bag.Take(small, random);
        bag.Take(small, random);

        int[] grown = [1, 2, 3];
        var draws = Enumerable.Range(0, 6).Select(_ => bag.Take(grown, random)!.Value).ToList();

        Assert.Contains(3, draws);
    }

    [Fact]
    public void Reset_starts_a_fresh_round()
    {
        var bag = new ShuffleBag();
        var random = Seeded();
        int[] pool = [1, 2, 3, 4];

        bag.Take(pool, random);
        bag.Take(pool, random);
        bag.Reset();

        Assert.Empty(bag.Remaining);

        var round = Enumerable.Range(0, 4).Select(_ => bag.Take(pool, random)!.Value).ToList();
        Assert.Equal(pool.OrderBy(i => i), round.OrderBy(i => i));
    }
}
