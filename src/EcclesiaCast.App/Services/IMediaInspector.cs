using EcclesiaCast.Core.Media;

namespace EcclesiaCast.App.Services;

/// <summary>Opens the media properties (Inspector) dialog.</summary>
public interface IMediaInspector
{
    /// <summary>
    /// Edits the item in place; returns true if saved. The library is passed
    /// in so the operator can choose another item to fill the screen behind
    /// this one.
    /// </summary>
    bool Edit(MediaItem item, IReadOnlyList<string> categories, IReadOnlyList<MediaItem> library);
}
