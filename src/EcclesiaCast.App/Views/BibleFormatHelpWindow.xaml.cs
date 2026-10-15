using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using EcclesiaCast.Core.Bible;
using Serilog;

namespace EcclesiaCast.App.Views;

/// <summary>
/// For whoever brings their own Bible: the two file formats the program reads,
/// a real example of each to save and open in Notepad, and instructions to hand
/// to an AI chat. Everything it shows comes from <see cref="BibleImportHelp"/>.
/// </summary>
public partial class BibleFormatHelpWindow : Window
{
    public BibleFormatHelpWindow()
    {
        InitializeComponent();
        JsonPreview.Text = BibleImportHelp.JsonPreview();
        XmlPreview.Text = BibleImportHelp.ExampleZefaniaXml();
    }

    /// <summary>The operator pressed «Importar una Biblia…»: the caller opens the import.</summary>
    public bool ImportRequested { get; private set; }

    private void SaveJson_Click(object sender, RoutedEventArgs e) =>
        Save("Biblia-ejemplo.json", "JSON (*.json)|*.json", BibleImportHelp.ExampleJson());

    private void SaveXml_Click(object sender, RoutedEventArgs e) =>
        Save("Biblia-ejemplo.xml", "Zefania XML (*.xml)|*.xml", BibleImportHelp.ExampleZefaniaXml());

    private void Save(string suggestedName, string filter, string content)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Guardar el ejemplo",
            FileName = suggestedName,
            Filter = filter,
            DefaultExt = Path.GetExtension(suggestedName),
        };
        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            // Without a byte-order mark: every importer and every editor reads it the same.
            File.WriteAllText(dialog.FileName, content, new UTF8Encoding(false));
            CopiedText.Text = $"✓ Guardado: {Path.GetFileName(dialog.FileName)}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warning(ex, "No se pudo guardar el ejemplo de Biblia {Path}", dialog.FileName);
            MessageBox.Show(this, $"No se pudo guardar el archivo:\n\n{ex.Message}", "EcclesiaCast",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void CopyInstructions_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(BibleImportHelp.InstructionsForAi());
            CopiedText.Text = "✓ Copiado. Pegalo en el chat de la IA (Ctrl+V).";
        }
        catch (COMException)
        {
            // Another program has the clipboard for a moment.
            CopiedText.Text = "No se pudo copiar ahora: probá de nuevo.";
        }
    }

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        ImportRequested = true;
        Close();
    }
}
