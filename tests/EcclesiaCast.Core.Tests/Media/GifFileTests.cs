using EcclesiaCast.Core.Media;

namespace EcclesiaCast.Core.Tests.Media;

public class GifFileTests
{
    /// <summary>A GIF made of the bare structure: header, screen, optional extensions, and <paramref name="frames"/> 1×1 images.</summary>
    private static MemoryStream Gif(int frames, bool globalTable = true, bool extensions = false, bool localTable = false, bool trailer = true)
    {
        var bytes = new List<byte>("GIF89a"u8.ToArray());
        bytes.AddRange([1, 0, 1, 0, (byte)(globalTable ? 0x80 : 0x00), 0, 0]);
        if (globalTable)
            bytes.AddRange([0, 0, 0, 255, 255, 255]);

        if (extensions)
        {
            // The loop extension, then the timing of the first frame.
            bytes.AddRange([0x21, 0xFF, 11]);
            bytes.AddRange("NETSCAPE2.0"u8.ToArray());
            bytes.AddRange([3, 1, 0, 0, 0]);
            bytes.AddRange([0x21, 0xF9, 4, 0, 10, 0, 0, 0]);
        }

        for (var i = 0; i < frames; i++)
        {
            bytes.Add(0x2C);
            bytes.AddRange([0, 0, 0, 0, 1, 0, 1, 0, (byte)(localTable ? 0x80 : 0x00)]);
            if (localTable)
                bytes.AddRange([9, 9, 9, 8, 8, 8]);
            bytes.AddRange([2, 2, 0x4C, 0x01, 0]);
        }

        if (trailer)
            bytes.Add(0x3B);
        return new MemoryStream([.. bytes]);
    }

    [Fact]
    public void A_gif_with_one_frame_is_a_picture()
    {
        Assert.False(GifFile.IsAnimated(Gif(1)));
    }

    [Fact]
    public void A_gif_with_two_frames_is_animated()
    {
        Assert.True(GifFile.IsAnimated(Gif(2)));
    }

    [Fact]
    public void Many_frames_with_the_usual_extensions_are_animated()
    {
        Assert.True(GifFile.IsAnimated(Gif(5, extensions: true)));
    }

    [Fact]
    public void A_single_frame_with_extensions_is_still_a_picture()
    {
        Assert.False(GifFile.IsAnimated(Gif(1, extensions: true)));
    }

    [Fact]
    public void Frames_with_their_own_colour_tables_are_counted_too()
    {
        Assert.True(GifFile.IsAnimated(Gif(2, globalTable: false, localTable: true)));
        Assert.False(GifFile.IsAnimated(Gif(1, globalTable: false, localTable: true)));
    }

    [Fact]
    public void A_gif_cut_short_is_not_animated()
    {
        var whole = Gif(3).ToArray();

        Assert.False(GifFile.IsAnimated(new MemoryStream(whole[..20])));
        Assert.False(GifFile.IsAnimated(new MemoryStream(whole[..5])));
    }

    [Fact]
    public void A_file_that_is_not_a_gif_is_not_animated()
    {
        Assert.False(GifFile.IsAnimated(new MemoryStream("PNG not a gif at all"u8.ToArray())));
        Assert.False(GifFile.IsAnimated(new MemoryStream([])));
    }

    [Fact]
    public void A_missing_file_is_not_animated()
    {
        Assert.False(GifFile.IsAnimated(Path.Combine(Path.GetTempPath(), "no-existe-" + Guid.NewGuid().ToString("N") + ".gif")));
    }

    [Fact]
    public void The_file_is_read_from_the_disk_by_path()
    {
        var path = Path.Combine(Path.GetTempPath(), "ecclesiacast-test-" + Guid.NewGuid().ToString("N") + ".gif");
        try
        {
            File.WriteAllBytes(path, Gif(3).ToArray());
            Assert.True(GifFile.IsAnimated(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void The_conversion_arguments_make_an_even_sided_yuv420p_mp4_without_sound()
    {
        var arguments = GifFile.ToVideoArguments(@"C:\medios\saludo.gif", @"C:\datos\saludo.mp4");

        Assert.Equal(@"C:\medios\saludo.gif", arguments[arguments.ToList().IndexOf("-i") + 1]);
        Assert.Equal(@"C:\datos\saludo.mp4", arguments[^1]);
        Assert.Contains("+faststart", arguments);
        Assert.Contains("yuv420p", arguments);
        Assert.Contains("scale=trunc(iw/2)*2:trunc(ih/2)*2", arguments);
        Assert.Contains("-an", arguments);
    }
}
