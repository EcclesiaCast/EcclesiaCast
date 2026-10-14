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

    /// <summary>
    /// Asks what the tab decides for all its media (right click on a tab →
    /// Configurar). Null when the operator cancelled. <paramref name="presentation"/>
    /// says whether the tab opens as a presentation in the central grid, and
    /// comes back with the operator's answer.
    /// </summary>
    MediaTabRule? EditTabRule(string tab, MediaTabRule current, ref bool presentation);
}
