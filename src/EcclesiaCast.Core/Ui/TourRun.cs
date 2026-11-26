namespace EcclesiaCast.Core.Ui;

/// <summary>
/// Which steps a tour goes through, decided once when it starts, so «3 de 9»
/// never changes its total and no step vanishes on the way.
/// <para>
/// A tutorial goes through every step of its file: a step opens what it talks
/// about («abrir:») when its turn comes, so asking beforehand whether its part
/// is on show would drop every step behind a closed tab. (That was the bug of
/// the prueba.12: from step 1 it jumped to the one on the tab that happened to
/// be open.) The welcome tour opens nothing, so it does leave out the parts
/// that are folded away when it starts.
/// </para>
/// </summary>
public sealed class TourRun
{
    private readonly int[] _parts;
    private readonly bool _isWelcome;

    private TourRun(int[] parts, bool isWelcome)
    {
        _parts = parts;
        _isWelcome = isWelcome;
    }

    /// <summary>Every step of the tutorial, whatever is open right now.</summary>
    public static TourRun ForTutorial(IReadOnlyList<TourStep> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);
        return new TourRun([.. Enumerable.Range(0, steps.Count)], isWelcome: false);
    }

    /// <summary>The welcome tour: the greeting, the goodbye and the parts on show at the start.</summary>
    public static TourRun ForWelcome(IReadOnlyList<TourStep> steps, Func<string, bool> isOnShow)
    {
        ArgumentNullException.ThrowIfNull(steps);
        ArgumentNullException.ThrowIfNull(isOnShow);
        return new TourRun(
            [.. Enumerable.Range(0, steps.Count).Where(i => steps[i].Target is not { } t || isOnShow(t))],
            isWelcome: true);
    }

    /// <summary>The index (in the list of steps) where the tour starts; −1 with no steps.</summary>
    public int First => _parts.Length > 0 ? _parts[0] : -1;

    /// <summary>
    /// The step after (<paramref name="direction"/> 1) or before (−1)
    /// <paramref name="index"/>; −1 when there is none.
    /// </summary>
    public int Next(int index, int direction)
    {
        if (direction == 0)
            throw new ArgumentOutOfRangeException(nameof(direction));

        var at = Array.IndexOf(_parts, index);
        var position = at + Math.Sign(direction);
        return at >= 0 && position >= 0 && position < _parts.Length ? _parts[position] : -1;
    }

    public bool IsFirst(int index) => index == First;

    public bool IsLast(int index) => _parts.Length > 0 && index == _parts[^1];

    /// <summary>
    /// «3 de 9» for the card, or null when the step has no number: the welcome
    /// tour's greeting and goodbye are not parts of the window.
    /// </summary>
    public (int Number, int Total)? Count(IReadOnlyList<TourStep> steps, int index)
    {
        ArgumentNullException.ThrowIfNull(steps);
        var numbered = _parts.Where(i => !_isWelcome || steps[i].Target is not null).ToList();
        var position = numbered.IndexOf(index);
        return position < 0 ? null : (position + 1, numbered.Count);
    }
}
