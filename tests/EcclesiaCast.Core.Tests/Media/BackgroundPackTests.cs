using System.IO.Compression;
using EcclesiaCast.Core.Media;

namespace EcclesiaCast.Core.Tests.Media;

public sealed class BackgroundPackTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "ecc-pack-" + Guid.NewGuid().ToString("N"));

    public BackgroundPackTests() => Directory.CreateDirectory(_root);

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private string MakeZip(params (string Name, string Text)[] entries)
    {
        var path = Path.Combine(_root, "pack.zip");
        using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
        foreach (var (name, text) in entries)
        {
            using var w = new StreamWriter(zip.CreateEntry(name).Open());
            w.Write(text);
        }

        return path;
    }

    [Fact]
    public void Extract_KeepsVideosAndPictures_AndDropsTheRest()
    {
        var zip = MakeZip(("Aurora.mp4", "v"), ("sub/Luz.webm", "v"), ("foto.JPG", "p"), ("LEEME.txt", "t"), ("run.exe", "x"));

        var files = BackgroundPack.Extract(zip, Path.Combine(_root, "out"));

        Assert.Equal(["Aurora.mp4", "Luz.webm", "foto.JPG"], files.Select(Path.GetFileName));
        Assert.All(files, f => Assert.True(File.Exists(f)));
    }

    [Fact]
    public void Extract_CannotWriteOutsideTheFolder()
    {
        var zip = MakeZip(("../../escapa.mp4", "v"));
        var dest = Path.Combine(_root, "out");

        var files = BackgroundPack.Extract(zip, dest);

        Assert.Single(files);
        Assert.Equal(dest, Path.GetDirectoryName(files[0]));
    }

    [Fact]
    public void Extract_RenamesAName_ThatIsAlreadyTaken()
    {
        var dest = Path.Combine(_root, "out");
        var zip = MakeZip(("Aurora.mp4", "v"));

        BackgroundPack.Extract(zip, dest);
        var second = BackgroundPack.Extract(zip, dest);

        Assert.Equal("Aurora (2).mp4", Path.GetFileName(second[0]));
    }

    [Fact]
    public void Extract_ThrowsOnAFileThatIsNotAZip()
    {
        var path = Path.Combine(_root, "falso.zip");
        File.WriteAllText(path, "no soy un zip");

        Assert.Throws<InvalidDataException>(() => BackgroundPack.Extract(path, Path.Combine(_root, "out")));
    }

    [Theory]
    [InlineData("a.mp4", true)]
    [InlineData("a.PNG", true)]
    [InlineData("a.exe", false)]
    [InlineData("a", false)]
    public void IsBackground_ByExtension(string name, bool expected) =>
        Assert.Equal(expected, BackgroundPack.IsBackground(name));
}
