using EcclesiaCast.Core.Remote;

namespace EcclesiaCast.Core.Tests.Remote;

public class BroadcastLabelTests
{
    [Fact]
    public void The_label_keeps_its_usual_size_and_place_by_default()
    {
        var style = BroadcastStyle.Default(OutputChannel.Bible);

        Assert.Equal(100, style.LabelSize);
        Assert.Equal(BroadcastLabelAlign.Center, style.LabelAlign);
        Assert.False(style.LabelBelow);
    }

    [Fact]
    public void The_size_and_place_survive_saving_and_loading()
    {
        var style = BroadcastStyle.Default(OutputChannel.Bible)
            with { LabelSize = 180, LabelAlign = BroadcastLabelAlign.Right, LabelBelow = true };

        var back = BroadcastStyle.Parse(style.ToSetting(), OutputChannel.Bible);

        Assert.Equal(180, back.LabelSize);
        Assert.Equal(BroadcastLabelAlign.Right, back.LabelAlign);
        Assert.True(back.LabelBelow);
    }

    [Fact]
    public void The_size_stays_inside_its_limits()
    {
        var tiny = (BroadcastStyle.Default(OutputChannel.Bible) with { LabelSize = 1 }).Clamp();
        var huge = (BroadcastStyle.Default(OutputChannel.Bible) with { LabelSize = 5000 }).Clamp();

        Assert.Equal(BroadcastStyle.MinLabelSize, tiny.LabelSize);
        Assert.Equal(BroadcastStyle.MaxLabelSize, huge.LabelSize);
    }

    [Fact]
    public void A_style_saved_before_these_fields_loads_with_the_usual_label()
    {
        var back = BroadcastStyle.Parse("{\"plate\":\"glass\",\"showLabel\":true}", OutputChannel.Bible);

        Assert.Equal(100, back.LabelSize);
        Assert.Equal(BroadcastLabelAlign.Center, back.LabelAlign);
        Assert.False(back.LabelBelow);
    }

    [Fact]
    public void A_broken_place_falls_back_to_the_centre_without_losing_the_rest()
    {
        var back = BroadcastStyle.Parse("{\"labelAlign\":\"diagonal\",\"labelSize\":150}", OutputChannel.Bible);

        Assert.Equal(BroadcastLabelAlign.Center, back.LabelAlign);
        Assert.Equal(150, back.LabelSize);
    }
}
