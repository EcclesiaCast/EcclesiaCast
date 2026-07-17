using EcclesiaCast.Core.Songs;
using EcclesiaCast.Core.Themes;

namespace EcclesiaCast.App.Services;

/// <summary>Opens the full-song ProPresenter-style design editor.</summary>
public interface ISongDesigner
{
    /// <summary>
    /// Edits the song's per-slide design in place (mutates each section's
    /// override). Returns true if the operator saved.
    /// </summary>
    bool Edit(Song song, SlideTheme resolvedTheme, int selectIndex);
}
