namespace EcclesiaCast.Presentation.Platform;

/// <summary>The fonts the operator can choose from.</summary>
public interface IFontCatalog
{
    /// <summary>The names of the fonts installed on the computer, in alphabetical order.</summary>
    IReadOnlyList<string> InstalledFamilies();

    /// <summary>
    /// True for a font that comes with the program instead of the computer:
    /// a theme made with it looks the same everywhere. None on Windows yet.
    /// </summary>
    bool IsBundled(string family) => false;
}
