using System.Runtime.InteropServices;
using EcclesiaCast.Core.Bible;
using EcclesiaCast.Presentation.Platform;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Presentation.Tests;

[Collection("ViewModels")]
public sealed class BibleFormatHelpViewModelTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "ecclesiacast-ayuda-" + Guid.NewGuid().ToString("N"));

    public BibleFormatHelpViewModelTests() => Directory.CreateDirectory(_root);

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private sealed class SaveTo(string? path) : IDialogs
    {
        public SaveFileRequest? Asked { get; private set; }

        public string? SaveFile(SaveFileRequest request)
        {
            Asked = request;
            return path;
        }

        public string? OpenFile(OpenFileRequest request) => null;

        public IReadOnlyList<string>? OpenFiles(OpenFileRequest request) => null;

        public string? PickFolder(string title) => null;
    }

    private sealed class Clipboard(Exception? failure = null) : IClipboardText
    {
        public string? Text { get; private set; }

        public void SetText(string text)
        {
            if (failure is not null)
                throw failure;
            Text = text;
        }
    }

    [Theory]
    [InlineData(0, "Biblia-ejemplo.json", "JSON (*.json)", ".json")]
    [InlineData(1, "Biblia-ejemplo.xml", "Zefania XML (*.xml)", ".xml")]
    [InlineData(2, "Biblia-ejemplo-opensong.xml", "XML de OpenSong (*.xml)", ".xml")]
    public void Each_example_is_saved_without_a_byte_order_mark(int which, string name, string filter, string extension)
    {
        var path = Path.Combine(_root, name);
        var dialogs = new SaveTo(path);
        var help = new BibleFormatHelpViewModel(dialogs, new Clipboard(), PlatformSystem.Windows);
        Action[] buttons = [help.SaveJson, help.SaveZefania, help.SaveOpenSong];
        string[] contents = [BibleImportHelp.ExampleJson(), BibleImportHelp.ExampleZefaniaXml(), BibleImportHelp.ExampleOpenSongXml()];

        buttons[which]();

        Assert.Equal("Guardar el ejemplo", dialogs.Asked!.Title);
        Assert.Equal(name, dialogs.Asked.FileName);
        Assert.Equal(extension, dialogs.Asked.DefaultExtension);
        Assert.Equal(filter, Assert.Single(dialogs.Asked.Filters).Name);
        var bytes = File.ReadAllBytes(path);
        Assert.NotEqual(0xEF, bytes[0]);
        Assert.Equal(contents[which], File.ReadAllText(path));
        Assert.Equal("✓ Guardado: " + name, help.Status);
    }

    [Fact]
    public void Cancelling_the_save_says_nothing()
    {
        var help = new BibleFormatHelpViewModel(new SaveTo(null), new Clipboard(), PlatformSystem.Windows);

        help.SaveJson();

        Assert.Equal(string.Empty, help.Status);
    }

    [Fact]
    public void A_file_that_cannot_be_written_is_said()
    {
        using var notices = new ScriptedNotices();
        var path = Path.Combine(_root, "no", "hay", "carpeta.json");
        var help = new BibleFormatHelpViewModel(new SaveTo(path), new Clipboard(), PlatformSystem.Windows);

        help.SaveJson();

        Assert.Equal("No se pudo guardar el archivo", Assert.Single(notices.Asked).Title);
        Assert.Equal(string.Empty, help.Status);
    }

    [Theory]
    [InlineData(PlatformSystem.Windows, "(Ctrl+V)", "correrlo en Windows", "el Bloc de notas")]
    [InlineData(PlatformSystem.Mac, "(⌘V)", "correrlo en una Mac", "TextEdit")]
    public void The_instructions_and_the_texts_name_this_system(PlatformSystem system, string keys, string run, string editor)
    {
        var clipboard = new Clipboard();
        var help = new BibleFormatHelpViewModel(new SaveTo(null), clipboard, system);

        help.CopyInstructions();

        Assert.Contains(run, clipboard.Text);
        Assert.Equal($"✓ Copiado. Pegalo en el chat de la IA {keys}.", help.Status);
        Assert.EndsWith(editor, help.ExampleTip);
    }

    [Fact]
    public void A_busy_clipboard_is_said()
    {
        var help = new BibleFormatHelpViewModel(
            new SaveTo(null), new Clipboard(new COMException()), PlatformSystem.Windows);

        help.CopyInstructions();

        Assert.Equal("No se pudo copiar ahora: probá de nuevo.", help.Status);
    }

    [Fact]
    public void It_remembers_that_the_import_was_asked_for_and_shows_the_three_examples()
    {
        var help = new BibleFormatHelpViewModel(new SaveTo(null), new Clipboard(), PlatformSystem.Windows);
        Assert.False(help.ImportRequested);
        Assert.Equal(BibleImportHelp.JsonPreview(), help.JsonPreview);
        Assert.Equal(BibleImportHelp.ExampleZefaniaXml(), help.XmlPreview);
        Assert.Equal(BibleImportHelp.ExampleOpenSongXml(), help.OpenSongPreview);

        help.RequestImport();

        Assert.True(help.ImportRequested);
    }
}
