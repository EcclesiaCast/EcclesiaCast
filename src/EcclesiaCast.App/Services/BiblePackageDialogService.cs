using System.Windows;
using EcclesiaCast.App.Views;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Bible;

namespace EcclesiaCast.App.Services;

public sealed class BiblePackageDialogService : IBiblePackageDialog
{
    public IReadOnlyList<int>? ChooseToExport(IReadOnlyList<BibleVersionInfo> versions)
    {
        var rows = versions
            .Select(v => new BiblePackageWindow.Row($"{v.Name}  ({v.Abbreviation})", $"{v.VerseCount:N0} versículos", isChecked: true))
            .ToList();
        var window = new BiblePackageWindow(
            "Exportar paquete de Biblias",
            "Marcá las versiones que querés guardar en un solo archivo (.ecbiblias) para llevarlas a otra computadora.",
            "Usalo sólo en tus propias computadoras: las versiones con derechos de autor (RVR1960, NVI, NTV…) no se pueden repartir a otras personas.",
            "Guardar…",
            rows)
        {
            Owner = Application.Current.MainWindow,
        };

        return window.ShowDialog() == true
            ? window.CheckedIndexes.Select(i => versions[i].Id).ToList()
            : null;
    }

    public IReadOnlyList<BiblePackageEntry>? ChooseToImport(string packageFileName, IReadOnlyList<BiblePackageChoice> choices)
    {
        var rows = choices
            .Select(c => new BiblePackageWindow.Row(
                $"{c.Entry.Name}  ({c.Entry.Abbreviation})",
                c.AlreadyInstalled ? "ya está en la biblioteca" : "se va a agregar",
                isChecked: !c.AlreadyInstalled))
            .ToList();
        var window = new BiblePackageWindow(
            "Importar paquete de Biblias",
            $"El paquete «{packageFileName}» trae estas versiones. Marcá las que querés traer.",
            null,
            "Importar",
            rows)
        {
            Owner = Application.Current.MainWindow,
        };

        return window.ShowDialog() == true
            ? window.CheckedIndexes.Select(i => choices[i].Entry).ToList()
            : null;
    }

    public IReadOnlyList<BibleCatalogItem>? ChooseToDownload(IReadOnlyList<BibleCatalogItem> items, IReadOnlyList<bool> installed)
    {
        // By language, Spanish first: the order of the rows is the order of the list.
        var ordered = BibleCatalog.ByLanguage(items)
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
                return new BiblePackageWindow.Row($"{o.Item.Name}  ({o.Item.Abbreviation})", string.Join(" · ", parts), isChecked: false);
            })
            .ToList();
        var window = new BiblePackageWindow(
            "Bajar Biblias",
            "Marcá las que querés bajar a esta computadora. Se baja sólo lo que marques.",
            null,
            "Descargar",
            rows)
        {
            Owner = Application.Current.MainWindow,
        };

        return window.ShowDialog() == true
            ? window.CheckedIndexes.Select(i => ordered[i].Item).ToList()
            : null;

        static int IndexOf(IReadOnlyList<BibleCatalogItem> list, BibleCatalogItem item)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (ReferenceEquals(list[i], item))
                    return i;
            }

            return -1;
        }

        static string Size(long bytes) => bytes >= 1024 * 1024
            ? $"{bytes / (1024.0 * 1024):0.0} MB"
            : $"{Math.Max(1, bytes / 1024)} KB";
    }
}
