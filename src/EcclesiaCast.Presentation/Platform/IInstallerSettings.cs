namespace EcclesiaCast.Presentation.Platform;

/// <summary>What the installer asked the operator and left written down for the program.</summary>
public interface IInstallerSettings
{
    /// <summary>
    /// The answer to «PC de bajo rendimiento» the installer left, or null when
    /// there is none (or it cannot be read).
    /// </summary>
    string? ReadLowPerformanceChoice();
}
