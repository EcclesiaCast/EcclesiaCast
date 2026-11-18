using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Media;
using LibVLCSharp.Shared;
using MediaType = EcclesiaCast.Core.Media.MediaType;

namespace EcclesiaCast.App.Platform;

/// <summary>
/// The posters of the media library, made as the program always did
/// (<see cref="MediaThumbnails"/>), behind <see cref="IThumbnailer"/>.
/// </summary>
public sealed class WpfThumbnailer : IThumbnailer
{
    public string Directory => MediaThumbnails.Directory;

    public string? Create(string sourcePath, MediaType type, LibVLC? videoEngine) =>
        MediaThumbnails.Create(sourcePath, type, videoEngine);

    public void Delete(string? thumbnailPath) => MediaThumbnails.Delete(thumbnailPath);

    public void DeleteOrphans(IEnumerable<string?> referenced) => MediaThumbnails.DeleteOrphans(referenced);
}
