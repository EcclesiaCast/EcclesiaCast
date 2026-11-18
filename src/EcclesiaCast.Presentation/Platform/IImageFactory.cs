namespace EcclesiaCast.Presentation.Platform;

/// <summary>
/// Makes the picture each interface knows how to show (an
/// <c>ImageSource</c> in WPF, a <c>Bitmap</c> in Avalonia). View models keep
/// it as <see cref="object"/> and the screens bind an image to it, so the
/// view models never name either one.
/// </summary>
public interface IImageFactory
{
    /// <summary>
    /// The picture in the file, read whole now (the file is not left open),
    /// or null when it cannot be read. <paramref name="decodeWidth"/> reads it
    /// at that width, to keep the memory of a small card small.
    /// </summary>
    object? FromFile(string path, int? decodeWidth = null);

    /// <summary>The picture in <paramref name="bytes"/>, or null when it cannot be read.</summary>
    object? FromBytes(byte[] bytes);

    /// <summary>Width and height of a picture, or null when the system cannot read it (a format it has no codec for).</summary>
    (int Width, int Height)? ReadSize(byte[] bytes);
}
