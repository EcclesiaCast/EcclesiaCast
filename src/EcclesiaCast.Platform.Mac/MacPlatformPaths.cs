using EcclesiaCast.Presentation.Platform;

namespace EcclesiaCast.Platform.Mac;

/// <summary>
/// The folders of EcclesiaCast on a Mac (plan, section 11). The library goes
/// to <c>~/Library/Application Support/EcclesiaCast</c>: in .NET that is
/// <see cref="Environment.SpecialFolder.LocalApplicationData"/> (batch 1
/// checked it on a real Mac; <c>ApplicationData</c> is <c>~/.config</c>).
/// As on Windows, only the library and the light copies follow
/// ECCLESIACAST_DATA_DIR; the helper programs and the speech models are
/// shared by every copy of the program.
/// </summary>
public sealed class MacPlatformPaths : IPlatformPaths
{
    private const string TestDataVariable = "ECCLESIACAST_DATA_DIR";

    private static string Home => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    private static string Support => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EcclesiaCast");

    /// <summary>Read once, at start-up.</summary>
    public string DataFolder { get; } =
        Environment.GetEnvironmentVariable(TestDataVariable) is { Length: > 0 } dataDir ? dataDir : Support;

    /// <summary>Movies, where a Mac keeps videos.</summary>
    public string DownloadsFolder => Path.Combine(Home, "Movies", "EcclesiaCast");

    public string ToolsFolder => Path.Combine(Support, "tools");

    public string ModelsFolder => Path.Combine(Support, "modelos");

    /// <summary>A cache, so it goes to Caches (macOS may empty it when the disk is full).</summary>
    public string LightCopiesFolder =>
        Environment.GetEnvironmentVariable(TestDataVariable) is { Length: > 0 } dataDir
            ? Path.Combine(dataDir, "copias-livianas")
            : Path.Combine(Home, "Library", "Caches", "EcclesiaCast", "copias-livianas");

    /// <summary>
    /// Inside the bundle, what comes with the program (the Bibles, the sample
    /// backgrounds) is in <c>EcclesiaCast.app/Contents/Resources</c>, next to
    /// <c>Contents/MacOS</c> where the program runs. Outside a bundle (a
    /// build being tried out), next to the program.
    /// </summary>
    public string BundledFolder => BundledFolderFor(AppContext.BaseDirectory);

    internal static string BundledFolderFor(string programFolder)
    {
        var resources = Path.GetFullPath(Path.Combine(programFolder, "..", "Resources"));
        return Directory.Exists(resources) ? resources : programFolder;
    }

    /// <summary>EasyWorship does not exist on a Mac: a folder copied from the PC goes in Documents.</summary>
    public string SoftouchFolder => Path.Combine(Home, "Documents", "Softouch");

    /// <summary>Documents, where ProPresenter keeps its libraries on a Mac.</summary>
    public IReadOnlyList<string> DocumentFolders => [Path.Combine(Home, "Documents")];
}
