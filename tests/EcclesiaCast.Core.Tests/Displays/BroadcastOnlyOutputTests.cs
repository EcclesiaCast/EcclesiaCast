using EcclesiaCast.Core.Displays;

namespace EcclesiaCast.Core.Tests.Displays;

public class BroadcastOnlyOutputTests
{
    private static readonly DisplayInfo Main = new(@"\\.\DISPLAY1", 0, 0, 1366, 768, IsPrimary: true);
    private static readonly DisplayInfo Projector = new(@"\\.\DISPLAY2", 1366, 0, 1920, 1080, IsPrimary: false);

    [Fact]
    public void The_entry_borrows_the_size_of_the_main_screen()
    {
        var entry = BroadcastOnlyOutput.For([Projector, Main]);

        Assert.Equal(1366, entry.Width);
        Assert.Equal(768, entry.Height);
    }

    [Fact]
    public void The_entry_is_recognised_and_no_real_screen_is()
    {
        var entry = BroadcastOnlyOutput.For([Main, Projector]);

        Assert.True(BroadcastOnlyOutput.IsBroadcastOnly(entry));
        Assert.False(BroadcastOnlyOutput.IsBroadcastOnly(Main));
        Assert.False(BroadcastOnlyOutput.IsBroadcastOnly(Projector));
        Assert.False(BroadcastOnlyOutput.IsBroadcastOnly(null));
    }

    [Fact]
    public void Without_screens_the_entry_is_a_widescreen_picture()
    {
        var entry = BroadcastOnlyOutput.For([]);

        Assert.Equal(1920, entry.Width);
        Assert.Equal(1080, entry.Height);
        Assert.True(BroadcastOnlyOutput.IsBroadcastOnly(entry));
    }
}
