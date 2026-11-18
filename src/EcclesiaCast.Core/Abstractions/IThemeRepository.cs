using EcclesiaCast.Core.Themes;

namespace EcclesiaCast.Core.Abstractions;

public interface IThemeRepository
{
    /// <summary>
    /// The shared themes, the ones the theme manager lists. A song's own
    /// theme is left out: it is reached only through its song, with <see cref="Get"/>.
    /// </summary>
    IReadOnlyList<SlideTheme> GetAll();

    SlideTheme? Get(int id);

    /// <summary>Inserts (Id 0) or updates a theme.</summary>
    SlideTheme Save(SlideTheme theme);

    /// <summary>Sends the theme to the trash (it is not erased: see <see cref="Purge"/>).</summary>
    void Delete(int id);

    /// <summary>The themes in the trash, the most recently deleted first.</summary>
    IReadOnlyList<SlideTheme> GetDeleted();

    /// <summary>Takes a theme back out of the trash.</summary>
    void Restore(int id);

    /// <summary>Erases a trashed theme for good.</summary>
    void Purge(int id);

    /// <summary>Erases for good what was trashed before <paramref name="limit"/> (UTC).</summary>
    void PurgeOlderThan(DateTime limit);
}
