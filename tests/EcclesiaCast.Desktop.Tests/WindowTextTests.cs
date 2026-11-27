using System.Xml.Linq;

namespace EcclesiaCast.Desktop.Tests;

/// <summary>
/// The windows of batch 16 say what the WPF ones say: every text written in a
/// WPF window is also written in its Avalonia pair. A text changed on one
/// side only fails here.
/// </summary>
public sealed class WindowTextTests
{
    private static readonly string[] Says = ["Text", "Content", "ToolTip", "Tip", "ToolTip.Tip", "Title"];

    [Theory]
    // No spelling checker on this side yet (phase 5 of the plan).
    [InlineData("SongEditorWindow", "Las palabras dudosas se subrayan en rojo")]
    [InlineData("BibleImportWindow")]
    [InlineData("BiblePackageWindow")]
    // It names the system's notepad: BibleFormatHelpViewModel.
    [InlineData("BibleFormatHelpWindow", "para abrirlo con el Bloc de notas")]
    [InlineData("GlobalSearchWindow")]
    [InlineData("TrashWindow")]
    // It names the system's file browser: SongImportViewModel.OpenFolderText.
    [InlineData("SongImportWindow", "Abrir la carpeta en el Explorador")]
    // The line under the search box depends on the system: WebSongSearchViewModel.Hint.
    [InlineData("WebSongSearchWindow", "o una frase de la letra y apretá Enter")]
    // The text of the sound names the system: SettingsViewModel.SoundText.
    [InlineData("SettingsWindow", "Si no suena nada después de cambiar")]
    public void The_avalonia_window_says_what_the_wpf_one_says(string window, string? movedToTheViewModel = null)
    {
        var wpf = Texts(Path.Combine("src", "EcclesiaCast.App", "Views", window + ".xaml"));
        var avalonia = Texts(Path.Combine("src", "EcclesiaCast.Desktop", "Views", window + ".axaml"))
            .ToHashSet();

        Assert.NotEmpty(wpf);
        var missing = wpf
            .Where(text => !avalonia.Contains(text))
            .Where(text => movedToTheViewModel is null || !text.Contains(movedToTheViewModel))
            .ToList();

        Assert.True(missing.Count == 0, "Faltan en Avalonia:\n" + string.Join("\n", missing));
    }

    /// <summary>The texts written in a window, as attributes or inside an element.</summary>
    private static List<string> Texts(string relativePath)
    {
        var document = XDocument.Load(Path.Combine(Root, relativePath));
        var attributes = document.Descendants()
            // A Setter gives a property its value; what it says is not a text of the window.
            .Where(e => e.Name.LocalName != "Setter")
            .SelectMany(e => e.Attributes())
            .Where(a => Says.Contains(a.Name.LocalName))
            .Select(a => a.Value);
        var inside = document.Descendants()
            .Where(e => !e.HasElements && e.Name.LocalName is "TextBlock" or "Run" or "TextBox")
            .Select(e => e.Value);

        return attributes.Concat(inside)
            .Select(text => string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)))
            .Where(text => !text.StartsWith('{') && text.Any(char.IsLetter))
            .Distinct()
            .ToList();
    }

    private static string Root
    {
        get
        {
            var folder = new DirectoryInfo(AppContext.BaseDirectory);
            while (folder is not null && !File.Exists(Path.Combine(folder.FullName, "EcclesiaCast.slnx")))
                folder = folder.Parent;
            return folder!.FullName;
        }
    }
}
