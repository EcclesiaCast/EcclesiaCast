using Avalonia.Headless.XUnit;
using EcclesiaCast.Desktop.Views;
using EcclesiaCast.Presentation.Services;
using EcclesiaCast.ScreenSamples;

namespace EcclesiaCast.Desktop.Tests;

/// <summary>
/// The windows of songs, Bibles and the library (batch 16), with the same
/// names and the same made-up data as in the WPF bench (LibraryScreens.cs).
/// </summary>
public sealed partial class ScreenBenchTests
{
    [AvaloniaFact]
    public void Importing_a_bible_is_drawn() => DrawWindow(new BibleImportWindow("Mi Biblia"), "importar-biblia");

    [AvaloniaFact]
    public void The_list_to_export_is_drawn() => DrawWindow(
        new BiblePackageWindow(BiblePackageDialog.ForExport(LibrarySamples.InstalledVersions())), "biblias-exportar");

    [AvaloniaFact]
    public void The_list_to_import_is_drawn() => DrawWindow(
        new BiblePackageWindow(BiblePackageDialog.ForImport("mis-biblias.ecbiblias", LibrarySamples.PackageChoices())),
        "biblias-importar");

    [AvaloniaFact]
    public void The_list_to_download_is_drawn()
    {
        var list = BiblePackageDialog.ForDownload(LibrarySamples.Catalog(), LibrarySamples.CatalogInstalled()).List;
        // One ticked, as when the operator is about to download it.
        list.VisibleRows[0].IsChecked = true;
        DrawWindow(new BiblePackageWindow(list), "biblias-bajar");
    }
}
