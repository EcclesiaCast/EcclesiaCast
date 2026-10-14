namespace EcclesiaCast.Core.Media;

/// <summary>
/// Animated GIFs. WPF and the video engine both show only the first frame of
/// one, so an animated GIF is turned into a looping video when it is brought
/// in; a still one stays a picture.
/// </summary>
public static class GifFile
{
    /// <summary>
    /// True when the stream holds a GIF with two or more frames. It walks the
    /// file's own block structure and stops at the second frame, so it never
    /// reads (or decodes) the pictures; anything that is not a well-formed GIF
    /// is simply «not animated».
    /// </summary>
    public static bool IsAnimated(Stream gif)
    {
        try
        {
            var header = new byte[6];
            if (gif.Read(header, 0, 6) != 6
                || header[0] != 'G' || header[1] != 'I' || header[2] != 'F'
                || header[3] != '8' || (header[4] != '7' && header[4] != '9') || header[5] != 'a')
                return false;

            // Width, height, flags, background, aspect; the flags say whether a colour table follows.
            var screen = new byte[7];
            if (gif.Read(screen, 0, 7) != 7)
                return false;
            if ((screen[4] & 0x80) != 0 && !Skip(gif, ColourTableBytes(screen[4])))
                return false;

            var frames = 0;
            while (true)
            {
                var block = gif.ReadByte();
                switch (block)
                {
                    case 0x21: // extension: a label, then sub-blocks
                        if (gif.ReadByte() < 0 || !SkipSubBlocks(gif))
                            return false;
                        break;

                    case 0x2C: // image: left, top, width, height, flags, [table], code size, sub-blocks
                        if (++frames >= 2)
                            return true;

                        var image = new byte[9];
                        if (gif.Read(image, 0, 9) != 9)
                            return false;
                        if ((image[8] & 0x80) != 0 && !Skip(gif, ColourTableBytes(image[8])))
                            return false;
                        if (gif.ReadByte() < 0 || !SkipSubBlocks(gif))
                            return false;
                        break;

                    default: // trailer, end of file or garbage: no second frame was found
                        return false;
                }
            }
        }
        catch (IOException)
        {
            return false;
        }
    }

    /// <summary>True when the file at <paramref name="path"/> is an animated GIF; false if it cannot be read.</summary>
    public static bool IsAnimated(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            return IsAnimated(stream);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>
    /// The ffmpeg arguments that turn a GIF into an MP4 any player takes:
    /// both sides of the picture even (H.264 needs it), the usual pixel format,
    /// the index first so it starts playing at once, and no sound track.
    /// </summary>
    public static IReadOnlyList<string> ToVideoArguments(string input, string output) =>
    [
        "-y", "-hide_banner", "-loglevel", "error",
        "-i", input,
        "-movflags", "+faststart",
        "-pix_fmt", "yuv420p",
        "-vf", "scale=trunc(iw/2)*2:trunc(ih/2)*2",
        "-an",
        output,
    ];

    private static int ColourTableBytes(byte flags) => 3 * (1 << ((flags & 0x07) + 1));

    private static bool Skip(Stream stream, int count)
    {
        var buffer = new byte[count];
        return stream.Read(buffer, 0, count) == count;
    }

    private static bool SkipSubBlocks(Stream stream)
    {
        while (true)
        {
            var length = stream.ReadByte();
            if (length < 0)
                return false;
            if (length == 0)
                return true;
            if (!Skip(stream, length))
                return false;
        }
    }
}
