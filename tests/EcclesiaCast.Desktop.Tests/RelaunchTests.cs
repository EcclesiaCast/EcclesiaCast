using EcclesiaCast.Desktop.Services;

namespace EcclesiaCast.Desktop.Tests;

/// <summary>How the program opens itself again after restoring a backup. Nothing is started.</summary>
public sealed class RelaunchTests
{
    [Fact]
    public void Inside_a_bundle_the_mac_opens_the_application()
    {
        var how = Relaunch.How(
            "/Applications/Ecclesia Cast.app/Contents/MacOS/EcclesiaCast.Desktop", isMac: true);

        Assert.Equal("/usr/bin/open", how.FileName);
        Assert.Equal(["-n", "/Applications/Ecclesia Cast.app"], how.ArgumentList);
    }

    [Theory]
    [InlineData("/Users/alguien/pruebas/EcclesiaCast.Desktop", true)]
    [InlineData(@"C:\Programas\EcclesiaCast.Desktop.exe", false)]
    public void Otherwise_the_program_itself_is_started(string program, bool isMac)
    {
        var how = Relaunch.How(program, isMac);

        Assert.Equal(program, how.FileName);
        Assert.Empty(how.ArgumentList);
    }
}
