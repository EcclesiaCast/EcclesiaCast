using EcclesiaCast.Core.Media;
using LibVLCSharp.Shared;
using MediaType = EcclesiaCast.Core.Media.MediaType;

namespace EcclesiaCast.Presentation.Platform;

/// <summary>
/// Makes the small poster picture shown for a media item in the library, and
/// keeps the generated PNGs in the program's data folder.
/// </summary>
public interface IThumbnailer
{
    /// <summary>Where the generated posters live.</summary>
    string Directory { get; }

    /// <summary>
    /// The path of the poster for a media file: an image gets one of its own
    /// shape, a video gets the system's picture or, failing that, a frame read
    /// with <paramref name="videoEngine"/>. Images fall back to the file
    /// itself. Null when nothing could be produced.
    /// </summary>
    string? Create(string sourcePath, MediaType type, LibVLC? videoEngine);

    /// <summary>Deletes a generated poster; ignores the media's own file.</summary>
    void Delete(string? thumbnailPath);

    /// <summary>Removes the generated posters no media item refers to any more.</summary>
    void DeleteOrphans(IEnumerable<string?> referenced);
}
