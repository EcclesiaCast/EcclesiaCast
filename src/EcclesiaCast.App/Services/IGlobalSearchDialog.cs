namespace EcclesiaCast.App.Services;

/// <summary>What kind of thing a search result points at.</summary>
public enum SearchHitKind
{
    Song,
    Verse,
    Media,
}

/// <summary>
/// One line of the unified search. <see cref="Id"/> is the song, media or
/// Bible version the operator picked; verses carry the place to go to.
/// </summary>
public sealed record SearchHit(
    SearchHitKind Kind,
    int Id,
    string Icon,
    string Title,
    string Detail,
    int BookNumber = 0,
    int Chapter = 0,
    int Verse = 0);

/// <summary>The one box that searches songs, verses and media together.</summary>
public interface IGlobalSearchDialog
{
    /// <summary>Returns what the operator chose, or null if they closed it.</summary>
    SearchHit? Show(Func<string, IReadOnlyList<SearchHit>> search, string initialQuery);
}
