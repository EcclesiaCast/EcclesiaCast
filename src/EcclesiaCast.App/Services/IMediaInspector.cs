using EcclesiaCast.Core.Media;

namespace EcclesiaCast.App.Services;

/// <summary>Opens the media properties (Inspector) dialog.</summary>
public interface IMediaInspector
{
    /// <summary>Edits the item in place; returns true if saved.</summary>
    bool Edit(MediaItem item, IReadOnlyList<string> categories);
}
