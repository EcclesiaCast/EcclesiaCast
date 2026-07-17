using EcclesiaCast.Core.Themes;

namespace EcclesiaCast.Core.Abstractions;

public interface IThemeRepository
{
    IReadOnlyList<SlideTheme> GetAll();

    SlideTheme? Get(int id);

    /// <summary>Inserts (Id 0) or updates a theme.</summary>
    SlideTheme Save(SlideTheme theme);

    void Delete(int id);
}
