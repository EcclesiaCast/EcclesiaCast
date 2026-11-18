namespace EcclesiaCast.Presentation.Platform;

/// <summary>The fonts the operator can choose from.</summary>
public interface IFontCatalog
{
    /// <summary>The names of the fonts installed on the computer, in alphabetical order.</summary>
    IReadOnlyList<string> InstalledFamilies();
}
