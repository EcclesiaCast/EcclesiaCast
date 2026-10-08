using EcclesiaCast.Core.Themes;
using EcclesiaCast.Data.Persistence;

namespace EcclesiaCast.Core.Tests.Persistence;

public class SelahThemeOptionTests
{
    [Fact]
    public void A_new_theme_hides_Selah_and_the_Psalm_titles()
    {
        Assert.False(new SlideTheme().ShowSelahAndTitles);
    }

    [Fact]
    public void The_choice_is_saved_with_the_theme()
    {
        using var temp = new TempDatabase();
        var themes = new ThemeRepository(temp.Path);
        var saved = themes.Save(new SlideTheme { Name = "Biblia", Kind = ThemeKind.Bible, ShowSelahAndTitles = true });

        Assert.True(themes.Get(saved.Id)!.ShowSelahAndTitles);

        saved.ShowSelahAndTitles = false;
        themes.Save(saved);
        Assert.False(themes.Get(saved.Id)!.ShowSelahAndTitles);
    }
}
