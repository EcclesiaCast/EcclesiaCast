using EcclesiaCast.Core.Displays;

namespace EcclesiaCast.Core.Tests.Displays;

public class DisplayInfoTests
{
    [Fact]
    public void Displays_with_same_values_are_equal()
    {
        var a = new DisplayInfo(@"\\.\DISPLAY2", 1920, 0, 1280, 720, IsPrimary: false);
        var b = new DisplayInfo(@"\\.\DISPLAY2", 1920, 0, 1280, 720, IsPrimary: false);

        Assert.Equal(a, b);
    }

    [Fact]
    public void Displays_with_different_device_names_are_not_equal()
    {
        var a = new DisplayInfo(@"\\.\DISPLAY1", 0, 0, 1920, 1080, IsPrimary: true);
        var b = a with { DeviceName = @"\\.\DISPLAY2" };

        Assert.NotEqual(a, b);
    }
}
