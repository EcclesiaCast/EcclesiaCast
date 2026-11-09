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

    void Delete(int id);
}
