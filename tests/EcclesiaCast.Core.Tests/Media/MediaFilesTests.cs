using EcclesiaCast.Core.Media;

namespace EcclesiaCast.Core.Tests.Media;

public class MediaFilesTests
{
    [Theory]
    [InlineData(@"C:\a\foto.jpg", MediaFileKind.Image)]
    [InlineData(@"C:\a\FOTO.PNG", MediaFileKind.Image)]
    [InlineData(@"C:\a\loop.mp4", MediaFileKind.Video)]
    [InlineData(@"C:\a\loop.MKV", MediaFileKind.Video)]
    [InlineData(@"C:\a\avisos.pdf", MediaFileKind.Document)]
    [InlineData(@"C:\a\culto.pptx", MediaFileKind.Document)]
    [InlineData(@"C:\a\culto.ppt", MediaFileKind.Document)]
    public void Each_accepted_file_is_told_apart_by_its_extension(string path, MediaFileKind expected)
    {
        Assert.Equal(expected, MediaFiles.Classify(path));
    }

    [Theory]
    [InlineData(@"C:\a\notas.txt")]
    [InlineData(@"C:\a\programa.exe")]
    [InlineData(@"C:\a\sin-extension")]
    [InlineData(@"C:\a\carpeta")]
    public void Anything_else_is_not_accepted(string path)
    {
        Assert.Equal(MediaFileKind.Unsupported, MediaFiles.Classify(path));
    }
}
