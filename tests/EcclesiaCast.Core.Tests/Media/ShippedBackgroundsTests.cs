using System.Text.RegularExpressions;
using EcclesiaCast.Core.Media;

namespace EcclesiaCast.Core.Tests.Media;

/// <summary>
/// The sample backgrounds in <c>installer/fondos</c> go into every installer:
/// each one must fill a 1920×1080 screen, stay light, and be listed in the
/// license; and the installer's box must say how many there are.
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

    private static IReadOnlyList<string> Pictures() =>
        Root() is { } root
            ? Directory.EnumerateFiles(Path.Combine(root, "installer", "fondos"), "*" + BundledBackgrounds.Extension).ToList()
            : [];

    [Fact]
    public void Each_background_is_a_1920_by_1080_jpeg_under_300_kb()
    {
        foreach (var file in Pictures())
        {
            Assert.Equal((1920, 1080), JpegSize(File.ReadAllBytes(file)));
            Assert.True(new FileInfo(file).Length < 300 * 1024, $"{Path.GetFileName(file)} pesa demasiado");
        }
    }

    [Fact]
    public void Each_background_is_listed_in_the_license()
    {
        if (Root() is not { } root)
            return; // not run from the repository

        var license = File.ReadAllText(Path.Combine(root, "installer", "fondos", "LICENCIA.md"));
        foreach (var file in Pictures())
            Assert.Contains(BundledBackgrounds.DisplayName(Path.GetFileName(file)), license);
    }

    [Fact]
    public void The_installer_box_says_how_many_backgrounds_there_are()
    {
        if (Root() is not { } root)
            return;

        var script = File.ReadAllText(Path.Combine(root, "installer", "EcclesiaCast.iss"));
        var said = Regex.Match(script, @"spanish\.BackgroundsTask=Agregar (\d+) fondos");

        Assert.True(said.Success);
        Assert.Equal(Pictures().Count, int.Parse(said.Groups[1].Value));
    }

    /// <summary>Width and height from the JPEG's frame header (SOF0 or SOF2).</summary>
    private static (int Width, int Height) JpegSize(byte[] data)
    {
        var i = 2;
        while (i + 9 < data.Length)
        {
            if (data[i] != 0xFF)
                throw new FormatException("No es un JPEG");
            var marker = data[i + 1];
            var length = (data[i + 2] << 8) | data[i + 3];
            if (marker is 0xC0 or 0xC2)
                return ((data[i + 7] << 8) | data[i + 8], (data[i + 5] << 8) | data[i + 6]);
            i += 2 + length;
        }
        throw new FormatException("El JPEG no dice su tamaño");
    }
}
