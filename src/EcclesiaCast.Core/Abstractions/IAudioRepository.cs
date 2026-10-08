using EcclesiaCast.Core.Audio;

namespace EcclesiaCast.Core.Abstractions;

public interface IAudioRepository
{
    /// <summary>Every track, in the order of the list.</summary>
    IReadOnlyList<AudioTrack> GetAll();

    /// <summary>Adds the track at the end of the list.</summary>
    AudioTrack Add(AudioTrack track);

    void Delete(int id);

    /// <summary>Saves the order of the list: the first id goes first, and so on.</summary>
    void SaveOrder(IReadOnlyList<int> idsInOrder);
}
