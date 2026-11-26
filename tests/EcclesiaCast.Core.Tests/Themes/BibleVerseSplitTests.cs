using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Themes;

namespace EcclesiaCast.Core.Tests.Themes;

public class BibleVerseSplitTests
{
    [Fact]
    public void Turning_it_on_saves_the_number_in_that_theme_and_off_saves_zero()
    {
        var themes = new FakeThemes(new SlideTheme { Id = 7, Name = "Biblia" });

        Assert.True(BibleVerseSplit.Save(themes, 7, on: true, chars: 180));
        Assert.Equal(180, themes.Theme.VerseSplitMaxChars);

        Assert.True(BibleVerseSplit.Save(themes, 7, on: false, chars: 180));
        Assert.Equal(0, themes.Theme.VerseSplitMaxChars);
        Assert.Equal(2, themes.Saves);
    }

    [Fact]
    public void Nothing_is_written_when_the_theme_already_says_that()
    {
        var themes = new FakeThemes(new SlideTheme { Id = 7, VerseSplitMaxChars = 150 });

        Assert.False(BibleVerseSplit.Save(themes, 7, on: true, chars: 150));
        Assert.Equal(0, themes.Saves);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)] // el aspecto integrado, que no está en la biblioteca
    [InlineData(99)]
    public void With_no_theme_of_the_library_nothing_is_written(int? id)
    {
        var themes = new FakeThemes(new SlideTheme { Id = 7 });

        Assert.False(BibleVerseSplit.Save(themes, id, on: true, chars: 150));
        Assert.Equal(0, themes.Saves);
        Assert.Equal(0, SlideTheme.Fallback.VerseSplitMaxChars);
    }

    [Theory]
    [InlineData(1, 50)]
    [InlineData(-5, 50)]
    [InlineData(9000, 500)]
    [InlineData(220, 220)]
    public void The_number_stays_within_what_the_editor_allows(int typed, int kept) =>
        Assert.Equal(kept, BibleVerseSplit.MaxCharsFor(true, typed));

    [Fact]
    public void The_number_shown_is_the_themes_else_the_last_used_else_150()
    {
        Assert.Equal(220, BibleVerseSplit.ShownChars(new SlideTheme { VerseSplitMaxChars = 220 }, lastUsed: 90));
        Assert.Equal(90, BibleVerseSplit.ShownChars(new SlideTheme(), lastUsed: 90));
        Assert.Equal(150, BibleVerseSplit.ShownChars(new SlideTheme()));
    }

    private sealed class FakeThemes(SlideTheme theme) : IThemeRepository
    {
        public SlideTheme Theme { get; private set; } = theme;

        public int Saves { get; private set; }

        public SlideTheme? Get(int id) => id == Theme.Id ? Theme.Clone() : null;

        public SlideTheme Save(SlideTheme saved)
        {
            Saves++;
            return Theme = saved;
        }

        public IReadOnlyList<SlideTheme> GetAll() => [Theme];
        public void Delete(int id) => throw new NotSupportedException();
        public IReadOnlyList<SlideTheme> GetDeleted() => [];
        public void Restore(int id) => throw new NotSupportedException();
        public void Purge(int id) => throw new NotSupportedException();
        public void PurgeOlderThan(DateTime limit) => throw new NotSupportedException();
    }

    [Theory]
    [InlineData(0, 0, 200, 200)]     // sin tocar en el editor: queda lo del celular
    [InlineData(150, 150, 0, 0)]
    [InlineData(180, 0, 200, 180)]   // cambiado en el editor: gana el editor
    [InlineData(0, 150, 150, 0)]
    public void An_open_editor_only_overwrites_the_split_it_changed(int edited, int loaded, int stored, int expected) =>
        Assert.Equal(expected, BibleVerseSplit.ValueToSave(edited, loaded, stored));
}
