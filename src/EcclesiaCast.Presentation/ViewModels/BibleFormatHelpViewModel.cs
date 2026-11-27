using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using EcclesiaCast.Core.Bible;
using Serilog;

namespace EcclesiaCast.Presentation.ViewModels;

/// <summary>
/// For whoever brings their own Bible: the three file formats the program
/// reads, a real example of each to save and open in a text editor, and
/// instructions to hand to an AI chat. The texts of the examples and the
/// instructions come from <see cref="BibleImportHelp"/>.
/// </summary>
public sealed partial class BibleFormatHelpViewModel(IDialogs dialogs, IClipboardText clipboard, PlatformSystem system)
    : ObservableObject
{
    private bool IsMac => system == PlatformSystem.Mac;

    public string JsonPreview { get; } = BibleImportHelp.JsonPreview();

    public string XmlPreview { get; } = BibleImportHelp.ExampleZefaniaXml();

    public string OpenSongPreview { get; } = BibleImportHelp.ExampleOpenSongXml();

    /// <summary>The help of «Guardar este ejemplo…» under the first format, naming the editor of this system.</summary>
    public string ExampleTip =>
        "Guarda un archivo de ejemplo (Salmo 23 y Juan 3:16-17, Reina-Valera 1909) para abrirlo con "
        + (IsMac ? "TextEdit" : "el Bloc de notas");

    /// <summary>What the last button did: «✓ Guardado: …», «✓ Copiado…».</summary>
    [ObservableProperty]
    private string _status = string.Empty;

    /// <summary>The operator pressed «Importar una Biblia…»: the caller opens the import.</summary>
    public bool ImportRequested { get; private set; }

    public void RequestImport() => ImportRequested = true;

    public void SaveJson() =>
        Save("Biblia-ejemplo.json", new FileFilter("JSON (*.json)", ["*.json"]), BibleImportHelp.ExampleJson());

    public void SaveZefania() =>
        Save("Biblia-ejemplo.xml", new FileFilter("Zefania XML (*.xml)", ["*.xml"]), BibleImportHelp.ExampleZefaniaXml());

    public void SaveOpenSong() => Save("Biblia-ejemplo-opensong.xml",
        new FileFilter("XML de OpenSong (*.xml)", ["*.xml"]), BibleImportHelp.ExampleOpenSongXml());

    private void Save(string suggestedName, FileFilter filter, string content)
    {
        var path = dialogs.SaveFile(new SaveFileRequest("Guardar el ejemplo", [filter])
        {
            FileName = suggestedName,
            DefaultExtension = Path.GetExtension(suggestedName),
        });
        if (path is null)
            return;

        try
        {
            // Without a byte-order mark: every importer and every editor reads it the same.
            File.WriteAllText(path, content, new UTF8Encoding(false));
            Status = $"✓ Guardado: {Path.GetFileName(path)}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warning(ex, "No se pudo guardar el ejemplo de Biblia {Path}", path);
            Notice.Show(ex.Message, "No se pudo guardar el archivo", NoticeKind.Warning);
        }
    }

    public void CopyInstructions()
    {
        try
        {
            clipboard.SetText(BibleImportHelp.InstructionsForAi(IsMac ? "en una Mac" : "en Windows"));
            Status = $"✓ Copiado. Pegalo en el chat de la IA ({(IsMac ? "⌘V" : "Ctrl+V")}).";
        }
        catch (Exception ex) when (ex is COMException or InvalidOperationException or TimeoutException)
        {
            // Another program has the clipboard for a moment.
            Status = "No se pudo copiar ahora: probá de nuevo.";
        }
    }
}
