using EcclesiaCast.Core.Songs;

namespace EcclesiaCast.App.Services;

/// <summary>Opens the "find a song on the internet" window.</summary>
public interface IWebSongSearchDialog
{
    /// <summary>Shows it modally; returns the songs imported, empty if none.</summary>
    IReadOnlyList<Song> Show();
}
