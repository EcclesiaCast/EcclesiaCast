using System.Windows;
using EcclesiaCast.App.Views;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Bible;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.App.Services;

/// <summary>The lists of Bible versions, in WPF's window; the rest is <see cref="BiblePackageDialog"/>.</summary>
public sealed class BiblePackageDialogService : IBiblePackageDialog
{
    private readonly BiblePackageDialog _dialog = new(Show);

    public IReadOnlyList<int>? ChooseToExport(IReadOnlyList<BibleVersionInfo> versions) =>
        _dialog.ChooseToExport(versions);

    public IReadOnlyList<BiblePackageEntry>? ChooseToImport(string packageFileName, IReadOnlyList<BiblePackageChoice> choices) =>
        _dialog.ChooseToImport(packageFileName, choices);

    public IReadOnlyList<BibleCatalogItem>? ChooseToDownload(IReadOnlyList<BibleCatalogItem> items, IReadOnlyList<bool> installed) =>
        _dialog.ChooseToDownload(items, installed);

    private static bool Show(BiblePackageViewModel list) =>
        new BiblePackageWindow(list) { Owner = Application.Current.MainWindow }.ShowDialog() == true;
}
