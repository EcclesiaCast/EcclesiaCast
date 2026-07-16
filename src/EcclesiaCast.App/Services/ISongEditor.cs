using EcclesiaCast.Core.Songs;

namespace EcclesiaCast.App.Services;

/// <summary>Opens the song editor dialog.</summary>
public interface ISongEditor
{
    /// <summary>Returns the edited song, or null if the operator cancelled.</summary>
    Song? Edit(Song? existing);
}
