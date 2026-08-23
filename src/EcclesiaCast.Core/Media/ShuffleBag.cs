namespace EcclesiaCast.Core.Media;

/// <summary>
/// Hands out items in random order without repeating until every one has come
/// up, like drawing raffle tickets from a bag and only refilling it once it is
/// empty.
///
/// Plain random picking is the obvious approach and the wrong one: it happily
/// serves the same background two songs in a row, which reads as broken rather
/// than random. On top of the bag, the first draw of a fresh round is also kept
/// from repeating the last one of the previous round.
/// </summary>
public sealed class ShuffleBag
{
    private readonly List<int> _remaining = [];
    private int? _last;

    /// <summary>Ids still waiting to come up in this round.</summary>
    public IReadOnlyList<int> Remaining => _remaining;

    /// <summary>
    /// Draws the next id from <paramref name="pool"/>, or null when the pool is
    /// empty. Items added to or removed from the pool between calls are picked
    /// up on the next refill; ids that vanished are skipped.
    /// </summary>
    public int? Take(IReadOnlyList<int> pool, Random random)
    {
        if (pool.Count == 0)
        {
            _remaining.Clear();
            _last = null;
            return null;
        }

        if (pool.Count == 1)
        {
            _remaining.Clear();
            _last = pool[0];
            return pool[0];
        }

        while (true)
        {
            if (_remaining.Count == 0)
                Refill(pool, random);

            var id = _remaining[0];
            _remaining.RemoveAt(0);

            // The pool may have changed since the bag was filled.
            if (!pool.Contains(id))
            {
                if (_remaining.Count == 0)
                    Refill(pool, random);
                continue;
            }

            _last = id;
            return id;
        }
    }

    /// <summary>Empties the bag, so the next draw starts a fresh round.</summary>
    public void Reset()
    {
        _remaining.Clear();
        _last = null;
    }

    private void Refill(IReadOnlyList<int> pool, Random random)
    {
        _remaining.Clear();
        _remaining.AddRange(pool);

        // Fisher–Yates.
        for (var i = _remaining.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (_remaining[i], _remaining[j]) = (_remaining[j], _remaining[i]);
        }

        // Starting the new round with the id that just played would show the
        // same background twice in a row, which is what the bag exists to avoid.
        if (_remaining.Count > 1 && _last is int last && _remaining[0] == last)
            (_remaining[0], _remaining[1]) = (_remaining[1], _remaining[0]);
    }
}
