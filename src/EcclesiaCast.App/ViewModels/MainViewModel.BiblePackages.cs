using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Bible;
using Serilog;

namespace EcclesiaCast.App.ViewModels;

/// <summary>
/// The Bibles the installer leaves next to the program and the packages
/// (<c>.ecbiblias</c>) the operator moves between computers.
/// </summary>
public sealed partial class MainViewModel
{
    private readonly IBiblePackageDialog _biblePackageDialog = new BiblePackageDialogService();

    /// <summary>Where the installer copies the Bibles that were ticked.</summary>
    private static string BundledBiblesFolder => Path.Combine(AppContext.BaseDirectory, "bibles");

    /// <summary>
    /// Where the installer copies the package the operator chose on its own
    /// page. It is a folder apart so the start-up import (which brings in the
    /// free Bibles without asking) never takes it.
    /// </summary>
    private static string InstalledPackageFolder => Path.Combine(BundledBiblesFolder, "paquete");

    /// <summary>
    /// At start-up, brings into the library the Bibles the installer left that
    /// it does not have yet. It saves some 31,000 verses per version in one
    /// transaction (about a second and a half each on a modest notebook), and
    /// while that lasts every write the window makes to the library waits for
    /// it: measured, a settings write stalled for the whole version. So it
    /// runs here, in the constructor, behind the loading screen and before
    /// anything reads the Bibles, where nobody can be waiting. Once the
    /// Bibles are in, finding nothing pending costs a few milliseconds.
    /// </summary>
    /// <returns>What the status bar should say, or null when nothing came in.</returns>
    private string? ImportBundledBibles()
    {
        try
        {
            var folder = BundledBiblesFolder;
            if (!Directory.Exists(folder))
                return null;

            var result = BundledBibleImporter.Run(folder, _bibles, _settings);

            foreach (var failed in result.Failed)
                Log.Warning("No se pudo leer la Biblia del instalador: {File}", failed);
            if (result.Imported.Count == 0)
                return null;

            Log.Information("Biblias del instalador traídas a la biblioteca: {Names}", string.Join(", ", result.Imported));
            return result.Imported.Count == 1
                ? $"Se instaló la Biblia «{result.Imported[0]}», que venía con el instalador."
                : $"Se instalaron {result.Imported.Count} Biblias que venían con el instalador: {string.Join(", ", result.Imported)}.";
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "No se pudieron traer las Biblias del instalador");
            return null;
        }
    }

    /// <summary>
    /// Saves the versions the operator ticks in one file, to open it on
    /// another computer. The window warns that versions with rights are for
    /// the operator's own computers only.
    /// </summary>
    [RelayCommand]
    private async Task ExportBiblePackageAsync()
    {
        var versions = _bibles.GetVersions();
        if (versions.Count == 0)
        {
            StatusText = "No hay Biblias instaladas para guardar en un paquete.";
            return;
        }

        var chosen = _biblePackageDialog.ChooseToExport(versions);
        if (chosen is null)
            return;

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Exportar paquete de Biblias",
            Filter = "Paquete de Biblias (*.ecbiblias)|*.ecbiblias",
            FileName = "Biblias" + BiblePackage.Extension,
            AddExtension = true,
            DefaultExt = BiblePackage.Extension,
        };
        if (dialog.ShowDialog() != true)
            return;

        var path = dialog.FileName;
        StatusText = "Guardando el paquete de Biblias…";
        try
        {
            await Task.Run(() =>
            {
                try
                {
                    using var file = File.Create(path);
                    // One version is read at a time, as the zip is written.
                    BiblePackage.Write(file, chosen.Select(id =>
                    {
                        var info = versions.First(v => v.Id == id);
                        return (info.Name, info.Abbreviation, info.Language, _bibles.ReadVersion(id));
                    }));
                }
                catch
                {
                    // Not a half-written file that looks like a package.
                    try { File.Delete(path); } catch (IOException) { }
                    throw;
                }
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Falló la exportación del paquete de Biblias a {Path}", path);
            StatusText = "No se pudo guardar el paquete de Biblias.";
            MessageBox.Show(
                $"No se pudo guardar el archivo:\n\n{ex.Message}",
                "EcclesiaCast", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        StatusText = chosen.Count == 1
            ? "Paquete guardado con 1 Biblia. Se abre con «Importar paquete de Biblias» en el menú Archivo."
            : $"Paquete guardado con {chosen.Count} Biblias. Se abre con «Importar paquete de Biblias» en el menú Archivo.";
        Log.Information("Paquete de Biblias exportado: {Count} versiones a {Path}", chosen.Count, path);
    }

    /// <summary>Opens a package and brings in the versions the operator ticks.</summary>
    [RelayCommand]
    private async Task ImportBiblePackageAsync()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Importar paquete de Biblias",
            Filter = "Paquete de Biblias (*.ecbiblias)|*.ecbiblias",
        };
        if (dialog.ShowDialog() != true)
            return;

        await ImportBiblePackageFileAsync(dialog.FileName);
    }

    private async Task ImportBiblePackageFileAsync(string path)
    {
        IReadOnlyList<BiblePackageEntry> entries;
        try
        {
            using var stream = File.OpenRead(path);
            entries = BiblePackage.ReadManifest(stream);
        }
        catch (Exception ex) when (ex is IOException or FormatException or UnauthorizedAccessException)
        {
            Log.Warning(ex, "No se pudo leer el paquete de Biblias {Path}", path);
            MessageBox.Show(
                $"No se pudo leer el paquete:\n\n{ex.Message}",
                "EcclesiaCast", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (entries.Count == 0)
        {
            MessageBox.Show(
                "El paquete no trae ninguna Biblia.",
                "EcclesiaCast", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var choices = BibleInstallPlanner.Offer(entries, _bibles.GetVersions());
        var chosen = _biblePackageDialog.ChooseToImport(Path.GetFileName(path), choices);
        if (chosen is null || chosen.Count == 0)
            return;

        var hadVersions = BibleVersionOptions.Count > 0;
        StatusText = "Importando Biblias del paquete…";
        var imported = new List<string>();
        var failed = new List<string>();
        await Task.Run(() =>
        {
            foreach (var entry in chosen)
            {
                try
                {
                    ParsedBible bible;
                    using (var stream = File.OpenRead(path))
                        bible = BiblePackage.ReadVersion(stream, entry);

                    if (bible.VerseCount == 0)
                    {
                        failed.Add(entry.Name);
                        continue;
                    }

                    imported.Add(_bibles.Import(entry.Name, entry.Abbreviation, entry.Language, bible).Name);
                }
                catch (Exception ex) when (ex is IOException or FormatException or UnauthorizedAccessException)
                {
                    Log.Warning(ex, "No se pudo importar {Name} del paquete {Path}", entry.Name, path);
                    failed.Add(entry.Name);
                }
            }
        });

        LoadBibleVersions();
        if (!hadVersions && imported.Count > 0)
            RefreshAfterVersionChange();

        StatusText = imported.Count switch
        {
            0 => "No se importó ninguna Biblia del paquete.",
            1 => $"Se importó la Biblia «{imported[0]}».",
            _ => $"Se importaron {imported.Count} Biblias: {string.Join(", ", imported)}.",
        };
        if (failed.Count > 0)
        {
            MessageBox.Show(
                $"No se pudo leer: {string.Join(", ", failed)}.",
                "EcclesiaCast", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        Log.Information("Paquete de Biblias importado: {Imported} (fallaron {Failed})",
            string.Join(", ", imported), string.Join(", ", failed));
    }

    /// <summary>
    /// If the installer left a package the operator chose on its setup page,
    /// opens the import window for it. Once per package: it is written down
    /// before the window opens, so closing the program without answering does
    /// not make it come back; a different package (from another install) does.
    /// </summary>
    public async Task OfferInstalledBiblePackageAsync()
    {
        try
        {
            // The free Bibles are already in (they come in at start-up), so the
            // window marks them as already there.
            var folder = InstalledPackageFolder;
            if (!Directory.Exists(folder))
                return;

            var file = new DirectoryInfo(folder)
                .GetFiles("*" + BiblePackage.Extension)
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .FirstOrDefault();
            if (file is null)
                return;

            var fingerprint = BibleInstallPlanner.Fingerprint(file.Name, file.Length, file.LastWriteTimeUtc);
            if (!BibleInstallPlanner.IsNewPackage(_settings.Get(BibleInstallPlanner.PackageSeenSettingKey), fingerprint))
                return;

            _settings.Set(BibleInstallPlanner.PackageSeenSettingKey, fingerprint);
            await ImportBiblePackageFileAsync(file.FullName);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "No se pudo ofrecer el paquete de Biblias del instalador");
        }
    }
}
