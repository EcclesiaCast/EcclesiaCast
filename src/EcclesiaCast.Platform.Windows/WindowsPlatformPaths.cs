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

    public string SoftouchFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), "Softouch");

    /// <summary>
    /// Documents and the OneDrive mirror of it, in both the English and Spanish
    /// spellings — with OneDrive on, the real folder usually lives there and not
    /// in the local Documents.
    /// </summary>
    public IReadOnlyList<string> DocumentFolders
    {
        get
        {
            var folders = new List<string>
            {
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documentos"),
            };

            foreach (var oneDrive in new[]
                     {
                         Environment.GetEnvironmentVariable("OneDrive"),
                         Environment.GetEnvironmentVariable("OneDriveConsumer"),
                         Environment.GetEnvironmentVariable("OneDriveCommercial"),
                     })
            {
                if (string.IsNullOrWhiteSpace(oneDrive))
                    continue;
                folders.Add(Path.Combine(oneDrive, "Documents"));
                folders.Add(Path.Combine(oneDrive, "Documentos"));
            }

            return folders;
        }
    }
}
