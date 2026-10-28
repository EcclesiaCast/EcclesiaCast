using EcclesiaCast.Core.Remote;
using Xunit;

namespace EcclesiaCast.Core.Tests.Remote;

public class BroadcastPresetTests
{
    [Fact]
    public void There_are_six_looks_with_different_names()
    {
        Assert.Equal(6, BroadcastPresets.All.Count);
        Assert.Equal(6, BroadcastPresets.All.Select(p => p.Name).Distinct().Count());
    }

    [Fact]
    public void A_look_keeps_what_belongs_to_the_layer()
    {
        var mine = BroadcastStyle.Default(OutputChannel.Songs) with
        {
            Position = BroadcastPosition.Top,
            ShowLabel = false,
            Background = BroadcastBackground.Color,
            BackgroundColor = "#112233",
            FollowProjector = true,
        };

        var styled = BroadcastPresets.All.Single(p => p.Name == "Noticiero").ApplyTo(mine);

        Assert.Equal(BroadcastPlate.Line, styled.Plate);
        Assert.Equal("#F5B301", styled.AccentColor);
        Assert.False(styled.FollowProjector);
        Assert.Equal(BroadcastPosition.Top, styled.Position);
        Assert.True(styled.Uppercase);
        Assert.False(styled.ShowLabel);
        Assert.Equal(BroadcastBackground.Color, styled.Background);
        Assert.Equal("#112233", styled.BackgroundColor);
    }

    [Fact]
    public void The_factory_style_wears_the_modern_look()
    {
        foreach (var channel in new[] { OutputChannel.Bible, OutputChannel.Songs, OutputChannel.Announcements })
        {
            var style = BroadcastStyle.Default(channel);
            Assert.Equal(["Moderna"], BroadcastPresets.All.Where(p => p.Matches(style)).Select(p => p.Name));
        }
    }

    [Fact]
    public void A_touched_up_look_is_no_longer_marked()
    {
        var preset = BroadcastPresets.All.Single(p => p.Name == "Vidrio");
        var styled = preset.ApplyTo(BroadcastStyle.Default(OutputChannel.Bible));

        Assert.True(preset.Matches(styled));
        Assert.False(preset.Matches(styled with { FontSize = styled.FontSize + 4 }));
        Assert.False(preset.Matches(styled with { FollowProjector = true }));
    }

    [Fact]
    public void Every_look_survives_being_saved()
    {
        foreach (var preset in BroadcastPresets.All)
        {
            var styled = preset.ApplyTo(BroadcastStyle.Default(OutputChannel.Bible));
            Assert.True(preset.Matches(BroadcastStyle.Parse(styled.ToSetting(), OutputChannel.Bible)));
        }
    }
}
