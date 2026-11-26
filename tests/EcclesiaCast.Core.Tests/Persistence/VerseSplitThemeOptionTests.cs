using EcclesiaCast.Core.Themes;
using EcclesiaCast.Data.Persistence;

namespace EcclesiaCast.Core.Tests.Persistence;

public class VerseSplitThemeOptionTests
{
    [Fact]
    public void A_new_theme_does_not_split_long_verses()
    {
        Assert.Equal(0, new SlideTheme().VerseSplitMaxChars);
    }

    [Fact]
    public void The_limit_is_saved_with_the_theme()
    {
        using var temp = new TempDatabase();
        var themes = new ThemeRepository(temp.Path);
        var saved = themes.Save(new SlideTheme { Name = "Biblia", Kind = ThemeKind.Bible, VerseSplitMaxChars = 150 });

        Assert.Equal(150, themes.Get(saved.Id)!.VerseSplitMaxChars);

        saved.VerseSplitMaxChars = 0;
        themes.Save(saved);
        Assert.Equal(0, themes.Get(saved.Id)!.VerseSplitMaxChars);
    }
}
