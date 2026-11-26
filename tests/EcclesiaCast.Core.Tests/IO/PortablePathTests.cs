using EcclesiaCast.Core.IO;

namespace EcclesiaCast.Core.Tests.IO;

public class PortablePathTests
{
    private static readonly char Sep = Path.DirectorySeparatorChar;

    [Fact]
    public void Normalize_turns_both_bars_into_the_separator_of_the_system()
    {
        Assert.Equal($"C:{Sep}Videos{Sep}a.mp4", PortablePath.Normalize(@"C:\Videos\a.mp4"));
        Assert.Equal($"{Sep}Users{Sep}x{Sep}a.mp4", PortablePath.Normalize("/Users/x/a.mp4"));
        Assert.Equal($"a{Sep}b{Sep}c", PortablePath.Normalize(@"a\b/c"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("sin-barras.mp4")]
    public void Normalize_leaves_alone_what_has_no_bars(string path)
    {
        Assert.Equal(path, PortablePath.Normalize(path));
    }

    [Theory]
    [InlineData(@"C:\Videos\a.mp4", "a.mp4")]
    [InlineData("/Users/x/a.mp4", "a.mp4")]
    [InlineData(@"\\servidor\carpeta\Culto domingo.mp4", "Culto domingo.mp4")]
    [InlineData(@"C:\Mis videos/mezcla\b.mov", "b.mov")]
    [InlineData("a.mp4", "a.mp4")]
    [InlineData(@"C:\Videos\", "")]
    [InlineData("/Users/x/", "")]
    [InlineData("", "")]
    public void FileName_is_what_follows_the_last_bar_of_either_kind(string path, string expected)
    {
        Assert.Equal(expected, PortablePath.FileName(path));
    }

    [Fact]
    public void FileName_keeps_the_extension_and_the_dots()
    {
        Assert.Equal("Alabanza 1.2.final.mp4", PortablePath.FileName(@"C:\Cultos\Alabanza 1.2.final.mp4"));
    }

    [Theory]
    [InlineData(@"C:\Videos\a.mp4", @"c:\videos\A.MP4")]
    [InlineData(@"C:\Videos\a.mp4", "C:/Videos/a.mp4")]
    [InlineData("/Users/x/a.mp4", @"\users\X\a.mp4")]
    [InlineData(@"C:\Videos\", @"C:\Videos")]
    [InlineData("/Users/x/", "/users/x")]
    [InlineData("/", "\\")]
    public void SamePath_ignores_case_bars_and_a_bar_at_the_end(string first, string second)
    {
        Assert.True(PortablePath.SamePath(first, second));
        Assert.True(PortablePath.SamePath(second, first));
    }

    [Theory]
    [InlineData(@"C:\Videos\a.mp4", @"C:\Videos\b.mp4")]
    [InlineData(@"C:\Videos\a.mp4", @"D:\Videos\a.mp4")]
    [InlineData(@"C:\Videos\a.mp4", @"C:\Videos\sub\a.mp4")]
    [InlineData("/Users/x", "/Users/xy")]
    public void SamePath_tells_different_paths_apart(string first, string second)
    {
        Assert.False(PortablePath.SamePath(first, second));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData(null, "a")]
    [InlineData("a", "")]
    public void SamePath_never_matches_when_a_path_is_missing(string? first, string? second)
    {
        Assert.False(PortablePath.SamePath(first, second));
    }

    [Fact]
    public void IsInside_finds_a_file_in_the_folder_or_below_on_this_system()
    {
        // Built with this system's separator: on a Mac the folder used to be
        // compared with a «\» at the end and nothing was ever inside it.
        var folder = Path.Combine(Path.GetTempPath(), "Posters");

        Assert.True(PortablePath.IsInside(folder, Path.Combine(folder, "a.jpg")));
        Assert.True(PortablePath.IsInside(folder + Path.DirectorySeparatorChar, Path.Combine(folder, "sub", "a.jpg")));
        Assert.True(PortablePath.IsInside(folder.ToUpperInvariant(), Path.Combine(folder, "a.jpg")));
    }

    [Fact]
    public void IsInside_does_not_take_a_neighbour_or_a_way_out()
    {
        var folder = Path.Combine(Path.GetTempPath(), "Posters");

        Assert.False(PortablePath.IsInside(folder, Path.Combine(Path.GetTempPath(), "Posters2", "a.jpg")));
        Assert.False(PortablePath.IsInside(folder, Path.Combine(folder, "..", "a.jpg")));
        Assert.False(PortablePath.IsInside(folder, folder));
    }
}
