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
}
