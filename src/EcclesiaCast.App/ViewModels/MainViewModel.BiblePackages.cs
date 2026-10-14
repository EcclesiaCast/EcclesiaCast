using System.IO;
using EcclesiaCast.Core.Bible;
using Serilog;

namespace EcclesiaCast.App.ViewModels;

/// <summary>
/// The Bibles the installer leaves next to the program and the packages
/// (<c>.ecbiblias</c>) the operator moves between computers.
/// </summary>
public sealed partial class MainViewModel
{
    /// <summary>Where the installer copies the Bibles that were ticked.</summary>
    private static string BundledBiblesFolder => Path.Combine(AppContext.BaseDirectory, "bibles");

    /// <summary>
    /// At start-up, brings into the library the Bibles the installer left that
    /// it does not have yet. It reads and saves tens of thousands of verses, so
    /// it runs apart from the window; the status bar says what came in.
    /// </summary>
    private async Task ImportBundledBiblesAsync()
    {
        try
        {
            var folder = BundledBiblesFolder;
            if (!Directory.Exists(folder))
                return;

            var hadVersions = BibleVersionOptions.Count > 0;
            var result = await Task.Run(() => BundledBibleImporter.Run(folder, _bibles, _settings));

            foreach (var failed in result.Failed)
                Log.Warning("No se pudo leer la Biblia del instalador: {File}", failed);
            if (result.Imported.Count == 0)
                return;

            LoadBibleVersions();
            // A library that had no Bible shows its first one right away.
            if (!hadVersions)
                RefreshAfterVersionChange();

            StatusText = result.Imported.Count == 1
                ? $"Se instaló la Biblia «{result.Imported[0]}», que venía con el instalador."
                : $"Se instalaron {result.Imported.Count} Biblias que venían con el instalador: {string.Join(", ", result.Imported)}.";
            Log.Information("Biblias del instalador traídas a la biblioteca: {Names}", string.Join(", ", result.Imported));
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "No se pudieron traer las Biblias del instalador");
        }
    }
}
