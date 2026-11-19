using EcclesiaCast.Core.Presentation;

namespace EcclesiaCast.Core.Tests.Presentation;

public class NamePlateStyleTests
{
    [Fact]
    public void With_nothing_saved_the_ready_styles_are_there()
    {
        var styles = NamePlateStyleList.Parse(null);

        Assert.Equal(NamePlateStyle.Presets, styles);
        Assert.Equal(["Clásica", "Moderna", "Noticias", "Vidrio", "Mínima", "Centrada"], styles.Select(s => s.Name));
        Assert.Equal(styles.Count, styles.Select(s => s.Name).Distinct().Count());
    }

    [Fact]
    public void Saved_styles_come_back_as_they_were()
    {
        var mine = NamePlateStyle.Classic with
        {
            Name = "Prédica", AccentColor = "#E8B931", Enter = NamePlateAnimation.Wipe, Exit = NamePlateExit.Slide,
            Target = NamePlateTarget.StreamOnly, Width = 900, Align = NamePlateAlign.Right,
        };

        var back = NamePlateStyleList.Parse(NamePlateStyleList.ToSetting([mine, NamePlateStyle.Classic]));

        Assert.Equal([mine, NamePlateStyle.Classic], back);
        Assert.False(back[0].OnProjector);
        Assert.True(back[0].OnStream);
    }

    [Fact]
    public void Values_out_of_range_and_bad_colours_are_fixed()
    {
        var odd = NamePlateStyle.Classic with
        {
            Name = "  ", BackgroundOpacity = 500, NameSize = 5, Width = 50, AccentColor = "rojo",
            NameColor = "ffcc00", Enter = (NamePlateAnimation)99,
        };

        var fixedUp = odd.Clamped();

        Assert.Equal("Sin nombre", fixedUp.Name);
        Assert.Equal(100, fixedUp.BackgroundOpacity);
        Assert.Equal(20, fixedUp.NameSize);
        Assert.Equal(300, fixedUp.Width);
        Assert.Equal("#2F63C9", fixedUp.AccentColor);
        Assert.Equal("#FFCC00", fixedUp.NameColor);
        Assert.Equal(NamePlateAnimation.Slide, fixedUp.Enter);
        Assert.Equal(0, (NamePlateStyle.Classic with { Width = 0 }).Clamped().Width); // 0 = fits the words
    }

    [Fact]
    public void A_broken_setting_gives_the_ready_styles()
    {
        Assert.Equal(NamePlateStyle.Presets, NamePlateStyleList.Parse("no es json"));
        Assert.Equal(NamePlateStyle.Presets, NamePlateStyleList.Parse("[]"));
    }

    [Fact]
    public void A_plate_whose_style_is_gone_uses_the_first_one()
    {
        var styles = NamePlateStyle.Presets;

        Assert.Equal("Moderna", NamePlateStyleList.Find(styles, "moderna").Name);
        Assert.Equal("Clásica", NamePlateStyleList.Find(styles, "Ya no existe").Name);
        Assert.Equal("Clásica", NamePlateStyleList.Find(styles, null).Name);
    }

    [Fact]
    public void A_new_style_gets_a_free_name()
    {
        Assert.Equal("Prédica", NamePlateStyleList.FreeName(NamePlateStyle.Presets, "Prédica"));
        Assert.Equal("Moderna 2", NamePlateStyleList.FreeName(NamePlateStyle.Presets, "Moderna"));
    }
}
