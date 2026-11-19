using EcclesiaCast.Presentation.Rendering;

namespace EcclesiaCast.Desktop.Tests;

/// <summary>The render test set (tests/casos-de-render) and the saved measures of the WPF bench.</summary>
internal static class RenderCases
{
    public static string Folder =>
        RenderCase.FindFolder(AppContext.BaseDirectory)
        ?? throw new DirectoryNotFoundException("tests/casos-de-render was not found.");

    public static RenderCase Load(string id) => RenderCase.Load(Path.Combine(Folder, id + ".json"));

    public static RenderResult Wpf(string id, string theme) =>
        RenderResult.Load(Path.Combine(Folder, "referencia-wpf", RenderResult.FileKey(id, theme) + ".json"));

    /// <summary>artifacts/render/&lt;name&gt; at the root of the repository, created.</summary>
    public static string OutputFolder(string name)
    {
        var root = Directory.GetParent(Directory.GetParent(Folder)!.FullName)!.FullName;
        var folder = Path.Combine(root, "artifacts", "render", name);
        Directory.CreateDirectory(folder);
        return folder;
    }
}
