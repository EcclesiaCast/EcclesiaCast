using EcclesiaCast.Core.Songs;

namespace EcclesiaCast.Core.Playlists;

/// <summary>
/// Fills a smart playlist from the library.
///
/// A church that has been going for years ends up with hundreds of songs and
/// sings the same twenty, because those are the ones anyone remembers. These
/// lists answer the questions nobody can answer from a long alphabetical
/// list: what did we just add, and what have we stopped singing.
/// </summary>
public static class SmartPlaylist
{
    /// <summary>Days used when the rule's parameter is missing or nonsense.</summary>
    public const int DefaultDays = 30;

    public static IReadOnlyList<Song> Select(
        IReadOnlyList<Song> songs, PlaylistRule rule, string? value, DateTime now)
    {
        switch (rule)
        {
            case PlaylistRule.RecentlyAdded:
            {
                var since = now.AddDays(-Days(value));
                return songs
                    .Where(s => s.AddedAt >= since)
                    .OrderByDescending(s => s.AddedAt)
                    .ToList();
            }

            case PlaylistRule.NotSungLately:
            {
                var since = now.AddDays(-Days(value));

                // A song that was never projected is the strongest case of
                // all, so it comes first rather than being left out for
                // having no date to compare.
                return songs
                    .Where(s => s.LastProjectedAt is null || s.LastProjectedAt < since)
                    .OrderBy(s => s.LastProjectedAt ?? DateTime.MinValue)
                    .ToList();
            }

            case PlaylistRule.ByArtist:
            {
                var artist = (value ?? string.Empty).Trim();
                if (artist.Length == 0)
                    return [];

                return songs
                    .Where(s => s.Artist.Contains(artist, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(s => s.Title, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
            }

            case PlaylistRule.Containing:
            {
                var text = (value ?? string.Empty).Trim();
                if (text.Length == 0)
                    return [];

                return songs
                    .Where(s => s.Title.Contains(text, StringComparison.OrdinalIgnoreCase)
                             || s.Sections.Any(x => x.Text.Contains(text, StringComparison.OrdinalIgnoreCase)))
                    .OrderBy(s => s.Title, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
            }

            default:
                return [];
        }
    }

    /// <summary>What the list is, in the operator's words, for the status bar.</summary>
    public static string Describe(PlaylistRule rule, string? value) => rule switch
    {
        PlaylistRule.RecentlyAdded => $"canciones agregadas en los últimos {Days(value)} días",
        PlaylistRule.NotSungLately => $"canciones que no cantamos hace {Days(value)} días",
        PlaylistRule.ByArtist => $"canciones de «{(value ?? string.Empty).Trim()}»",
        PlaylistRule.Containing => $"canciones que dicen «{(value ?? string.Empty).Trim()}»",
        _ => "lista armada a mano",
    };

    private static int Days(string? value) =>
        int.TryParse(value, out var days) && days > 0 ? days : DefaultDays;
}
