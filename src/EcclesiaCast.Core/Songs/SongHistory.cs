namespace EcclesiaCast.Core.Songs;

/// <summary>
/// What a song said at one moment: its title and its slides, in order, with
/// their text and their design. The theme and the background are left out:
/// undoing a moved slide must not take away a theme chosen meanwhile.
/// </summary>
public sealed record SongSnapshot(
    string Title,
    string Artist,
    string? Copyright,
    IReadOnlyList<SongSnapshot.Slide> Slides)
{
    public sealed record Slide(string Label, string Text, string? StyleJson);

    public static SongSnapshot Of(Song song) => new(
        song.Title,
        song.Artist,
        song.Copyright,
        song.Sections.OrderBy(s => s.Order).Select(s => new Slide(s.Label, s.Text, s.StyleJson)).ToList());

    /// <summary>Puts this state back into the song (new slides, numbered in order).</summary>
    public void ApplyTo(Song song)
    {
        song.Title = Title;
        song.Artist = Artist;
        song.Copyright = Copyright;
        song.Sections = Slides
            .Select((s, i) => new SongSection { Order = i, Label = s.Label, Text = s.Text, StyleJson = s.StyleJson })
            .ToList();
    }

    public bool SameAs(SongSnapshot other) =>
        Title == other.Title && Artist == other.Artist && Copyright == other.Copyright
        && Slides.SequenceEqual(other.Slides);
}

/// <summary>
/// Ctrl+Z / Ctrl+Y for the changes made to songs while the program is open:
/// moving, adding, deleting or editing slides, or the whole lyrics. Each song
/// keeps its own list, so undoing in one song never touches another. Nothing
/// is kept after closing the program.
/// </summary>
public sealed class SongHistory
{
    /// <summary>How many steps back each song keeps.</summary>
    public const int MaxSteps = 50;

    private sealed record Step(SongSnapshot Before, string What);

    private readonly Dictionary<int, (List<Step> Undo, List<Step> Redo)> _songs = [];

    private (List<Step> Undo, List<Step> Redo) Of(int songId)
    {
        if (!_songs.TryGetValue(songId, out var lists))
            _songs[songId] = lists = ([], []);
        return lists;
    }

    /// <summary>
    /// A change was saved: <paramref name="before"/> is how the song was just
    /// before it, <paramref name="what"/> says it in a few words («mover
    /// "Coro"»). A new change forgets what had been undone.
    /// </summary>
    public void Record(int songId, SongSnapshot before, string what)
    {
        if (songId <= 0)
            return;

        var (undo, redo) = Of(songId);
        undo.Add(new Step(before, what));
        if (undo.Count > MaxSteps)
            undo.RemoveAt(0);
        redo.Clear();
    }

    public bool CanUndo(int songId) => _songs.TryGetValue(songId, out var l) && l.Undo.Count > 0;

    public bool CanRedo(int songId) => _songs.TryGetValue(songId, out var l) && l.Redo.Count > 0;

    /// <summary>The words of the change Ctrl+Z would take back, or null.</summary>
    public string? NextUndo(int songId) =>
        _songs.TryGetValue(songId, out var l) && l.Undo.Count > 0 ? l.Undo[^1].What : null;

    /// <summary>The words of the change Ctrl+Y would bring back, or null.</summary>
    public string? NextRedo(int songId) =>
        _songs.TryGetValue(songId, out var l) && l.Redo.Count > 0 ? l.Redo[^1].What : null;

    /// <summary>
    /// Takes the last change back: returns the state to put into the song and
    /// what it was, and keeps <paramref name="current"/> to redo it. Null when
    /// there is nothing to undo.
    /// </summary>
    public (SongSnapshot State, string What)? Undo(int songId, SongSnapshot current)
    {
        if (!CanUndo(songId))
            return null;

        var (undo, redo) = Of(songId);
        var step = undo[^1];
        undo.RemoveAt(undo.Count - 1);
        redo.Add(new Step(current, step.What));
        return (step.Before, step.What);
    }

    /// <summary>Brings back the last change undone; null when there is none.</summary>
    public (SongSnapshot State, string What)? Redo(int songId, SongSnapshot current)
    {
        if (!CanRedo(songId))
            return null;

        var (undo, redo) = Of(songId);
        var step = redo[^1];
        redo.RemoveAt(redo.Count - 1);
        undo.Add(new Step(current, step.What));
        return (step.Before, step.What);
    }

    /// <summary>The song was deleted or replaced: its steps go too.</summary>
    public void Forget(int songId) => _songs.Remove(songId);
}
