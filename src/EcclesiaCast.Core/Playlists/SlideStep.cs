namespace EcclesiaCast.Core.Playlists;

/// <summary>
/// Where an arrow press lands in the slide grid. With a slide on screen it is
/// the neighbour; with none, it starts from the verse the operator marked
/// (BibleShow style) instead of from the first card, which in a Bible chapter
/// is the «ANTERIOR» jump card and sent the arrow back a whole chapter.
/// </summary>
public static class SlideStep
{
    /// <param name="live">Index of the slide on screen, or -1.</param>
    /// <param name="preview">Index of the slide marked as «next up», or -1.</param>
    /// <param name="direction">+1 for →, -1 for ←.</param>
    /// <param name="isJumpCard">One flag per card of the grid: true for the chapter-jump cards, which never project.</param>
    /// <returns>
    /// The index to go live on. It can fall outside the grid (-1, or the card
    /// count): the caller treats that as the edge of the item.
    /// </returns>
    public static int Target(int live, int preview, int direction, IReadOnlyList<bool> isJumpCard)
    {
        if (live >= 0)
            return live + direction;

        // Something marked and nothing live: → projects the mark itself, ←
        // goes to the card before it.
        if (preview >= 0)
            return direction > 0 ? preview : preview - 1;

        if (direction < 0)
            return -1;

        for (var i = 0; i < isJumpCard.Count; i++)
        {
            if (!isJumpCard[i])
                return i;
        }

        // Nothing projectable: one past the last card, which is out of range.
        return isJumpCard.Count;
    }
}
