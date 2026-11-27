using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Bible;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Presentation.Services;

/// <summary>
/// The three lists of Bible versions (to export, to import from a package and
/// to download): what each row says, which come ticked, in what order and what
/// is given back. Each interface only says how its window is shown.
/// </summary>
/// <param name="show">Shows the window of the list and waits; true when the operator accepted.</param>
public sealed class BiblePackageDialog(Func<BiblePackageViewModel, bool> show) : IBiblePackageDialog
{
    /// <summary>The language the program speaks: the Bibles to download open showing that one.</summary>
    private const string AppLanguage = "es";

    public IReadOnlyList<int>? ChooseToExport(IReadOnlyList<BibleVersionInfo> versions)
    {
        var list = ForExport(versions);
        return show(list) ? list.CheckedIndexes.Select(i => versions[i].Id).ToList() : null;
    }

    public IReadOnlyList<BiblePackageEntry>? ChooseToImport(string packageFileName, IReadOnlyList<BiblePackageChoice> choices)
    {
        var list = ForImport(packageFileName, choices);
        return show(list) ? list.CheckedIndexes.Select(i => choices[i].Entry).ToList() : null;
    }

    public IReadOnlyList<BibleCatalogItem>? ChooseToDownload(IReadOnlyList<BibleCatalogItem> items, IReadOnlyList<bool> installed)
    {
        var (list, ordered) = ForDownload(items, installed);
        return show(list) ? list.CheckedIndexes.Select(i => ordered[i]).ToList() : null;
    }

    public static BiblePackageViewModel ForExport(IReadOnlyList<BibleVersionInfo> versions) => new(
        "Exportar paquete de Biblias",
        "Marcá las versiones que querés guardar en un solo archivo (.ecbiblias) para llevarlas a otra computadora.",
        "Usalo sólo en tus propias computadoras: las versiones con derechos de autor (RVR1960, NVI, NTV…) no se pueden repartir a otras personas.",
        "Guardar…",
        versions
            .Select(v => new BiblePackageRow($"{v.Name}  ({v.Abbreviation})", $"{v.VerseCount:N0} versículos", isChecked: true))
            .ToList());

    public static BiblePackageViewModel ForImport(string packageFileName, IReadOnlyList<BiblePackageChoice> choices) => new(
        "Importar paquete de Biblias",
        $"El paquete «{packageFileName}» trae estas versiones. Marcá las que querés traer.",
        null,
        "Importar",
        choices
            .Select(c => new BiblePackageRow(
                $"{c.Entry.Name}  ({c.Entry.Abbreviation})",
                c.AlreadyInstalled ? "ya está en la biblioteca" : "se va a agregar",
                isChecked: !c.AlreadyInstalled))
            .ToList());

    /// <summary>The list to download, with its items in the order of its rows.</summary>
    public static (BiblePackageViewModel List, IReadOnlyList<BibleCatalogItem> Ordered) ForDownload(
        IReadOnlyList<BibleCatalogItem> items, IReadOnlyList<bool> installed)
    {
        // By language, Spanish first: the order of the rows is the order of the list.
        var byLanguage = BibleCatalog.ByLanguage(items);
        var ordered = byLanguage
            .SelectMany(g => g.Items.Select(i => (Item: i, g.Language)))
            .ToList();
        var rows = ordered
            .Select(o =>
            {
                var have = installed[IndexOf(items, o.Item)];
                var parts = new List<string> { o.Language, Size(o.Item.Size) };
                if (!string.IsNullOrWhiteSpace(o.Item.License))
                    parts.Add(o.Item.License);
                if (have)
                    parts.Add("ya está en la biblioteca");
                return new BiblePackageRow($"{o.Item.Name}  ({o.Item.Abbreviation})", string.Join(" · ", parts),
                    isChecked: false, group: o.Language);
            })
            .ToList();
        var list = new BiblePackageViewModel(
            "Bajar Biblias",
            "Marcá las que querés bajar a esta computadora. Se baja sólo lo que marques.",
            null,
            "Descargar",
            rows,
            groups: byLanguage.Select(g => g.Language).ToList(),
            group: BibleCatalog.LanguageName(AppLanguage));
        return (list, ordered.Select(o => o.Item).ToList());
    }

    private static int IndexOf(IReadOnlyList<BibleCatalogItem> list, BibleCatalogItem item)
    {
        for (var i = 0; i < list.Count; i++)
        {
            if (ReferenceEquals(list[i], item))
                return i;
        }

        return -1;
    }

    /// <summary>The size of a download, in KB under a megabyte.</summary>
    internal static string Size(long bytes) => bytes >= 1024 * 1024
        ? $"{bytes / (1024.0 * 1024):0.0} MB"
        : $"{Math.Max(1, bytes / 1024)} KB";
}
