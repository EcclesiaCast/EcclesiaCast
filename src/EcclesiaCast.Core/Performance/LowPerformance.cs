namespace EcclesiaCast.Core.Performance;

/// <summary>
/// The setting behind «Modo PC de bajo rendimiento»: what it is called in the
/// database, and how the choice made in the installer reaches it.
/// </summary>
public static class LowPerformance
{
    /// <summary>Setting that holds the mode: "1" on, anything else off.</summary>
    public const string SettingKey = "performance.low";

    /// <summary>Registry key (under HKCU) where the installer leaves its answer.</summary>
    public const string InstallerRegistryKey = @"Software\EcclesiaCast";

    /// <summary>Value in that key: 1 when the box «bajo rendimiento» was ticked.</summary>
    public const string InstallerValueName = "LowPerformance";

    public static bool IsOn(string? savedSetting) => savedSetting == "1";

    /// <summary>
    /// What to store at start-up, or null to store nothing. The installer's
    /// answer only counts while the operator has not decided anything in the
    /// program yet: once the setting exists, the menu is the one in charge, so
    /// an update that runs the installer again never undoes a choice.
    /// </summary>
    public static string? InitialSetting(string? savedSetting, string? installerValue) =>
        savedSetting is null && installerValue?.Trim() == "1" ? "1" : null;
}
