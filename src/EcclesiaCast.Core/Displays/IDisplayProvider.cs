namespace EcclesiaCast.Core.Displays;

/// <summary>Enumerates the displays currently attached to the system.</summary>
public interface IDisplayProvider
{
    IReadOnlyList<DisplayInfo> GetDisplays();
}
