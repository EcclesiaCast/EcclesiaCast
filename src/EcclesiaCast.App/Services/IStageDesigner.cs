using EcclesiaCast.Core.Stage;

namespace EcclesiaCast.App.Services;

/// <summary>Opens the editor where the stage screen's layouts are arranged.</summary>
public interface IStageDesigner
{
    /// <summary>
    /// Edits copies of <paramref name="layouts"/>. Returns the layouts as the
    /// operator left them and the one to use, or null if they cancelled.
    /// </summary>
    StageLayoutState? Edit(IReadOnlyList<StageLayout> layouts, string activeName);
}
