using System.Text;
using System.Text.RegularExpressions;
using EcclesiaCast.Core.Media;

namespace EcclesiaCast.Core.Tests.Media;

/// <summary>
/// The sample backgrounds in <c>installer/fondos</c> go into every installer:
/// each one must be an MP4, stay light, and be listed in the license; and the
/// installer's box must say how many there are.
/// </summary>
public class ShippedBackgroundsTests
{
    private static string? Root()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "installer", "fondos")))
            directory = directory.Parent;
        return directory?.FullName;
    }

    private static IReadOnlyList<string> Videos() =>
        Root() is { } root
            ? Directory.EnumerateFiles(Path.Combine(root, "installer", "fondos"), "*" + BundledBackgrounds.Extension).ToList()
            : [];

    [Fact]
    public void Each_background_is_an_mp4_under_6_mb()
    {
        foreach (var file in Videos())
        {
            var head = new byte[12];
            using (var stream = File.OpenRead(file))
                stream.ReadExactly(head);

            Assert.Equal("ftyp", Encoding.ASCII.GetString(head, 4, 4));
            Assert.True(new FileInfo(file).Length < 6 * 1024 * 1024, $"{Path.GetFileName(file)} pesa demasiado");
        }
    }

    [Fact]
    public void Each_background_is_listed_in_the_license()
    {
        if (Root() is not { } root)
            return; // not run from the repository

        var license = File.ReadAllText(Path.Combine(root, "installer", "fondos", "LICENCIA.md"));
        foreach (var file in Videos())
            Assert.Contains($"`{Path.GetFileName(file)}`", license);
    }

    [Fact]
    public void The_installer_box_says_how_many_backgrounds_there_are()
    {
        if (Root() is not { } root)
            return;

        var script = File.ReadAllText(Path.Combine(root, "installer", "EcclesiaCast.iss"));
        var said = Regex.Match(script, @"spanish\.BackgroundsTask=Agregar (\d+) fondos");

        Assert.True(said.Success);
        Assert.Equal(Videos().Count, int.Parse(said.Groups[1].Value));
    }
}
