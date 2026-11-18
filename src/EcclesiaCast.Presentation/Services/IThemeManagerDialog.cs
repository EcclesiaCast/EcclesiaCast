using EcclesiaCast.Core.Songs;

namespace EcclesiaCast.Presentation.Services;

/// <summary>Opens the theme manager window.</summary>
public interface IThemeManagerDialog
{
    /// <summary>Returns true if any theme or default assignment changed.</summary>
    bool Show();

    /// <summary>
    /// Opens the editor on one song's theme: changing it there gives the song
    /// its own theme. Returns true if the song's theme changed.
    /// </summary>
    bool ShowForSong(Song song);
}
