using EcclesiaCast.Platform.Windows;

namespace EcclesiaCast.Presentation.Tests;

/// <summary>
/// The folders of the Windows version, letter by letter as they were before
/// they moved behind IPlatformPaths: a library, a tool or a model looked for
/// in another folder would look lost to the operator. Only on Windows; on
/// another system each test returns at once.
/// </summary>
[Collection("Environment")]
public class WindowsPlatformPathsTests
{
    private const string Variable = "ECCLESIACAST_DATA_DIR";

    private static string AppData => Environment.GetEnvironmentVariable("APPDATA")!;

    private static string LocalAppData => Environment.GetEnvironmentVariable("LOCALAPPDATA")!;

    [Fact]
    public void Without_the_variable_everything_is_where_it_always_was()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var paths = WithVariable(null, () => new WindowsPlatformPaths());

        Assert.Equal(AppData + @"\EcclesiaCast", paths.DataFolder);
        Assert.Equal(AppData + @"\EcclesiaCast\tools", paths.ToolsFolder);
        Assert.Equal(LocalAppData + @"\EcclesiaCast\modelos", paths.ModelsFolder);
        Assert.Equal(
            Environment.GetFolderPath(Environment.SpecialFolder.MyVideos) + @"\EcclesiaCast",
            paths.DownloadsFolder);
        Assert.Equal(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments) + @"\Softouch",
            paths.SoftouchFolder);
        Assert.Equal(AppContext.BaseDirectory, paths.BundledFolder);
        WithVariable(null, () =>
            Assert.Equal(LocalAppData + @"\EcclesiaCast\copias-livianas", paths.LightCopiesFolder));
    }

    [Fact]
    public void The_variable_moves_the_library_and_the_light_copies_only()
    {
        if (!OperatingSystem.IsWindows())
            return;

        const string copy = @"C:\copia-de-prueba";
        WithVariable(copy, () =>
        {
            var paths = new WindowsPlatformPaths();

            Assert.Equal(copy, paths.DataFolder);
            Assert.Equal(copy + @"\copias-livianas", paths.LightCopiesFolder);

            // Shared by every copy of the program, as before.
            Assert.Equal(AppData + @"\EcclesiaCast\tools", paths.ToolsFolder);
            Assert.Equal(LocalAppData + @"\EcclesiaCast\modelos", paths.ModelsFolder);
        });
    }

    [Fact]
    public void Documents_are_looked_for_in_both_spellings()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var folders = new WindowsPlatformPaths().DocumentFolders;

        Assert.Equal(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), folders[0]);
        Assert.Contains(profile + @"\Documents", folders);
        Assert.Contains(profile + @"\Documentos", folders);
    }

    private static T WithVariable<T>(string? value, Func<T> action)
    {
        var before = Environment.GetEnvironmentVariable(Variable);
        Environment.SetEnvironmentVariable(Variable, value);
        try
        {
            return action();
        }
        finally
        {
            Environment.SetEnvironmentVariable(Variable, before);
        }
    }

    private static void WithVariable(string? value, Action action) =>
        WithVariable<object?>(value, () => { action(); return null; });
}
