using EcclesiaCast.Core.Themes;

namespace EcclesiaCast.Core.Tests.Themes;

public class ThemePresetTests
{
    private static SlideTheme Sample() => new()
    {
        Id = 7, Name = "Mío", Kind = ThemeKind.Bible, ShowVerseNumbers = true,
        Transition = SlideTransition.Zoom, BackgroundImagePath = "C:\fondo.jpg",
    };

    [Fact]
    public void Every_look_keeps_what_belongs_to_the_theme()
    {
        foreach (var preset in ThemePresets.All)
        {
            var themed = preset.ApplyTo(Sample());

            Assert.Equal(7, themed.Id);
            Assert.Equal("Mío", themed.Name);
            Assert.Equal(ThemeKind.Bible, themed.Kind);
            Assert.True(themed.ShowVerseNumbers);
            Assert.Equal(SlideTransition.Zoom, themed.Transition);
        }
    }

    [Fact]
    public void A_look_changes_the_copy_and_never_the_theme_it_was_given()
    {
        var original = Sample();
        var themed = ThemePresets.All.Single(p => p.Name == "Alto contraste").ApplyTo(original);

        Assert.Equal("#FFE45C", themed.TextColor);
        Assert.Equal("#FFFFFF", original.TextColor);
        Assert.Equal("C:\fondo.jpg", original.BackgroundImagePath);
        Assert.Null(themed.BackgroundImagePath);
    }

    [Fact]
    public void The_looks_have_different_names_and_sensible_sizes()
    {
        Assert.Equal(ThemePresets.All.Count, ThemePresets.All.Select(p => p.Name).Distinct().Count());
        foreach (var preset in ThemePresets.All)
        {
            var themed = preset.ApplyTo(new SlideTheme());
            Assert.InRange(themed.MaxFontSize, themed.MinFontSize, 400);
        }
    }

    [Fact]
    public void The_lower_third_leaves_the_rest_of_the_picture_free()
    {
        var themed = ThemePresets.All.Single(p => p.Name == "Tercio inferior").ApplyTo(new SlideTheme());

        Assert.True(themed.TransparentBackground);
        Assert.Equal(VAlign.Bottom, themed.AlignV);
        Assert.True(themed.BoxY >= 700);
    }
}
