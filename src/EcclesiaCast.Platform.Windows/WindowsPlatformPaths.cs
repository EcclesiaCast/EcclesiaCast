using EcclesiaCast.Presentation.Platform;

namespace EcclesiaCast.Platform.Windows;

/// <summary>
/// The folders of EcclesiaCast on Windows, as they always were. Only the
/// library and the light copies follow ECCLESIACAST_DATA_DIR; the helper
/// programs and the speech models are shared by every copy of the program.
/// </summary>
public sealed class WindowsPlatformPaths : IPlatformPaths
{
    private const string TestDataVariable = "ECCLESIACAST_DATA_DIR";

    /// <summary>Read once, at start-up.</summary>
    public string DataFolder { get; } =
        Environment.GetEnvironmentVariable(TestDataVariable) is { Length: > 0 } dataDir
            ? dataDir
            : Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EcclesiaCast");

    public string DownloadsFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "EcclesiaCast");

    public string ToolsFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "EcclesiaCast", "tools");

    public string ModelsFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "EcclesiaCast", "modelos");

    /// <summary>Local, not roaming: a cache.</summary>
    public string LightCopiesFolder =>
        Environment.GetEnvironmentVariable(TestDataVariable) is { Length: > 0 } dataDir
            ? Path.Combine(dataDir, "copias-livianas")
            : Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "EcclesiaCast", "copias-livianas");

    public string BundledFolder => AppContext.BaseDirectory;
}
