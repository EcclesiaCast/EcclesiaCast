using System.Reflection;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Songs;

namespace EcclesiaCast.Core.Themes;

/// <summary>How a song's theme relates to the main song theme.</summary>
public enum SongThemeState
{
    /// <summary>The song follows the main song theme: editing it changes the song.</summary>
    FollowsMain,

    /// <summary>The song uses another shared theme, chosen for it.</summary>
    Chosen,

    /// <summary>
    /// The song's look was edited from the song itself: it has a theme of its
    /// own that nothing else changes.
    /// </summary>
    Own,
}

/// <summary>
/// How many songs a theme does not reach right now: the ones «Aplicar a todas
/// las canciones» would change.
/// </summary>
public sealed record SongThemeSurvey(int Own, int Chosen, int Designed)
{
    /// <summary>Songs with their own theme or another one chosen.</summary>
    public int Apart => Own + Chosen;
}

/// <summary>
/// The rules between a song and its theme. A song follows the main theme
/// until its look is edited from the song: then that look becomes the song's
/// own theme, and editing the main theme no longer touches it. It can go
/// back to the main theme or take another shared one, and «Aplicar a todas
/// las canciones» puts every song back on the main theme.
/// </summary>
public sealed class SongThemes(IThemeRepository themes, ISongRepository songs)
{
    /// <summary>Where the song stands. A theme that no longer exists counts as following the main one.</summary>
    public SongThemeState StateOf(Song song)
    {
        if (song.ThemeId is not int id || themes.Get(id) is not { } theme)
            return SongThemeState.FollowsMain;

        return IsOwnThemeOf(theme, song) ? SongThemeState.Own : SongThemeState.Chosen;
    }

    /// <summary>The song's own theme, or null when it uses a shared one.</summary>
    public SlideTheme? OwnThemeOf(Song song) =>
        song.ThemeId is int id && themes.Get(id) is { } theme && IsOwnThemeOf(theme, song)
            ? theme
            : null;

    public static bool IsOwnThemeOf(SlideTheme theme, Song song) =>
        theme.OwnerSongId is int owner && owner == song.Id && song.Id != 0;

    /// <summary>
    /// Saves what the operator did with the song's theme in the editor.
    /// <paramref name="startedFrom"/> is the theme the editor showed first
    /// (null: the main one); <paramref name="editedLook"/> is the look with
    /// the operator's changes, or null when nothing was changed.
    ///
    /// Any change makes the look the song's own. Without changes the song
    /// simply takes the theme it started from: the main one, a shared one, or
    /// the own theme it already had.
    /// </summary>
    public void Save(Song song, int? startedFrom, SlideTheme? editedLook)
    {
        if (editedLook is not null)
        {
            SaveOwn(song, editedLook);
            return;
        }

        if (startedFrom is null)
            FollowMain(song);
        else if (themes.Get(startedFrom.Value) is { } theme && !IsOwnThemeOf(theme, song))
            Use(song, theme.Id);
    }

    /// <summary>
    /// Makes <paramref name="look"/> the song's own theme: the one it already
    /// had is updated, or a new one is made for it.
    /// </summary>
    public SlideTheme SaveOwn(Song song, SlideTheme look)
    {
        var own = look.Clone();
        own.Kind = ThemeKind.Song;
        own.OwnerSongId = song.Id;
        own.Name = $"Tema propio de «{song.Title}»";
        own.Id = OwnThemeOf(song)?.Id ?? 0;

        var saved = themes.Save(own);
        if (song.ThemeId != saved.Id)
        {
            songs.SetTheme(song.Id, saved.Id);
            song.ThemeId = saved.Id;
        }

        return saved;
    }

    /// <summary>The song goes back to following the main theme; its own theme, if any, is deleted.</summary>
    public void FollowMain(Song song)
    {
        songs.SetTheme(song.Id, null);
        song.ThemeId = null;
    }

    /// <summary>The song takes a shared theme; its own theme, if any, is deleted.</summary>
    public void Use(Song song, int themeId)
    {
        songs.SetTheme(song.Id, themeId);
        song.ThemeId = themeId;
    }

    /// <summary>
    /// Gives a copied song a copy of the original's own theme, so the two
    /// can be changed apart. A shared theme is simply shared.
    /// </summary>
    public void CopyTheme(Song from, Song to)
    {
        if (OwnThemeOf(from) is { } own)
            SaveOwn(to, own);
        else if (from.ThemeId != to.ThemeId)
            songs.SetTheme(to.Id, to.ThemeId = from.ThemeId);
    }

    /// <summary>The songs the main theme does not reach today.</summary>
    public SongThemeSurvey Survey()
    {
        var shared = themes.GetAll().Select(t => t.Id).ToHashSet();
        int own = 0, chosen = 0, designed = 0;
        foreach (var song in songs.Search(string.Empty))
        {
            if (song.ThemeId is int id)
            {
                if (shared.Contains(id))
                    chosen++;
                else
                    own++;
            }

            if (HasDesign(song))
                designed++;
        }

        return new SongThemeSurvey(own, chosen, designed);
    }

    /// <summary>
    /// «Aplicar a todas las canciones»: every song goes back to the main
    /// theme, losing its own theme or the one chosen for it. With
    /// <paramref name="clearDesigns"/> the slides designed by hand go back to
    /// the theme too. Returns how many songs changed.
    /// </summary>
    public int ApplyMainToAll(bool clearDesigns)
    {
        var changed = 0;
        foreach (var song in songs.Search(string.Empty))
        {
            var designed = clearDesigns && HasDesign(song);
            if (song.ThemeId is null && !designed)
                continue;

            if (designed)
            {
                foreach (var section in song.Sections)
                    section.StyleJson = null;
                song.ThemeId = null;
                songs.Save(song);
            }
            else
            {
                songs.SetTheme(song.Id, null);
            }

            changed++;
        }

        return changed;
    }

    private static bool HasDesign(Song song) =>
        song.Sections.Any(s => !string.IsNullOrEmpty(s.StyleJson));

    private static readonly PropertyInfo[] LookProperties = typeof(SlideTheme)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.CanRead && p.CanWrite)
        .Where(p => p.Name is not (nameof(SlideTheme.Id) or nameof(SlideTheme.Name)
            or nameof(SlideTheme.Kind) or nameof(SlideTheme.OwnerSongId)))
        .ToArray();

    /// <summary>
    /// True when two themes put the same thing on the screen: everything but
    /// their id, name, kind and owner is equal. Sizes and percentages that
    /// went through the editor may differ in the last decimal, so numbers
    /// count as equal when they are that close.
    /// </summary>
    public static bool SameLook(SlideTheme a, SlideTheme b)
    {
        foreach (var property in LookProperties)
        {
            var x = property.GetValue(a);
            var y = property.GetValue(b);
            if (x is double dx && y is double dy)
            {
                if (Math.Abs(dx - dy) > 1e-6)
                    return false;
            }
            else if (!Equals(x, y))
            {
                return false;
            }
        }

        return true;
    }
}
