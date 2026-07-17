using EcclesiaCast.Core.Media;

namespace EcclesiaCast.Core.Abstractions;

public interface IMediaRepository
{
    IReadOnlyList<MediaItem> GetAll();

    MediaItem Add(MediaItem item);

    void Delete(int id);
}
