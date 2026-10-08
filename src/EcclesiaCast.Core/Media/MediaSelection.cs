namespace EcclesiaCast.Core.Media;

/// <summary>
/// Which media of the bar are picked, with the rules of the Windows Explorer:
/// Ctrl adds or removes one, Shift takes the stretch from the last one picked,
/// Ctrl+A takes all. Keeps ids and not objects, because the bar builds its
/// items again after almost every change and the picking has to survive that.
/// </summary>
public sealed class MediaSelection
{
    private readonly HashSet<int> _ids = [];

    /// <summary>The item the next Shift-click counts from: the last one clicked.</summary>
    public int? Anchor { get; private set; }

    public int Count => _ids.Count;

    public bool Contains(int id) => _ids.Contains(id);

    /// <summary>A plain click: only this one stays picked.</summary>
    public void Only(int id)
    {
        _ids.Clear();
        _ids.Add(id);
        Anchor = id;
    }

    /// <summary>Ctrl+click: adds the item, or takes it out if it was picked.</summary>
    public void Toggle(int id)
    {
        if (!_ids.Remove(id))
            _ids.Add(id);
        Anchor = id;
    }

    /// <summary>
    /// Shift+click: the stretch of <paramref name="order"/> between the anchor
    /// and <paramref name="id"/>. It replaces what was picked, unless
    /// <paramref name="additive"/> (Ctrl+Shift) keeps it. The anchor stays, so
    /// shift-clicking again reshapes the same stretch. With no anchor it
    /// behaves as a plain click.
    /// </summary>
    public void Range(IReadOnlyList<int> order, int id, bool additive)
    {
        var to = IndexOf(order, id);
        var from = Anchor is int anchor ? IndexOf(order, anchor) : -1;
        if (to < 0 || from < 0)
        {
            Only(id);
            return;
        }

        if (!additive)
            _ids.Clear();

        for (var i = Math.Min(from, to); i <= Math.Max(from, to); i++)
            _ids.Add(order[i]);
    }

    /// <summary>Ctrl+A, or the rectangle dragged over the empty space.</summary>
    public void Set(IEnumerable<int> ids, bool additive)
    {
        if (!additive)
            _ids.Clear();
        foreach (var id in ids)
            _ids.Add(id);
        Anchor ??= _ids.Count > 0 ? _ids.First() : null;
    }

    public void Clear()
    {
        _ids.Clear();
        Anchor = null;
    }

    /// <summary>Forgets whatever is no longer shown (another tab, a removed item).</summary>
    public void Retain(IEnumerable<int> visible)
    {
        _ids.IntersectWith(visible);
        if (Anchor is int anchor && !_ids.Contains(anchor) && _ids.Count == 0)
            Anchor = null;
    }

    /// <summary>The picked ids in the order <paramref name="order"/> shows them.</summary>
    public IReadOnlyList<int> InOrder(IEnumerable<int> order) => order.Where(_ids.Contains).ToList();

    private static int IndexOf(IReadOnlyList<int> order, int id)
    {
        for (var i = 0; i < order.Count; i++)
            if (order[i] == id)
                return i;
        return -1;
    }
}
