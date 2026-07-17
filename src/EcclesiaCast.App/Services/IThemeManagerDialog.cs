namespace EcclesiaCast.App.Services;

/// <summary>Opens the theme manager window.</summary>
public interface IThemeManagerDialog
{
    /// <summary>Returns true if any theme or default assignment changed.</summary>
    bool Show();
}
