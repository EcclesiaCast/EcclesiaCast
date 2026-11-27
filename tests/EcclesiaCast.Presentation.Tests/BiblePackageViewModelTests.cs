using System.Globalization;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Bible;
using EcclesiaCast.Presentation.Services;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Presentation.Tests;

// Notice goes through PlatformServices, which is one for the whole program.
[Collection("ViewModels")]
public sealed class BiblePackageViewModelTests
{
    private static BibleCatalogItem Item(string name, string language, long size, string license = "") =>
        new(name, name[..1], language, new Uri("https://example.com/" + name), size, new string('a', 64), license);

    private static readonly IReadOnlyList<BibleCatalogItem> Catalog =
    [
        Item("Web", "en", 1_572_864, "Dominio público"),
        Item("Reina", "es", 2048),
        Item("Oso", "es", 10),
    ];

    [Fact]
    public void To_export_everything_comes_ticked_and_the_ids_come_back()
    {
        IReadOnlyList<BibleVersionInfo> versions =
            [new(4, "Reina", "RV", "es", 31102), new(9, "Oso", "O", "es", 10)];
        BiblePackageViewModel? seen = null;
        var dialog = new BiblePackageDialog(list =>
        {
            seen = list;
            list.VisibleRows[0].IsChecked = false;
            return true;
        });

        Assert.Equal([9], dialog.ChooseToExport(versions));
        Assert.Equal("Exportar paquete de Biblias", seen!.Title);
        Assert.True(seen.HasWarning);
        Assert.False(seen.HasGroups);
        Assert.Equal("Reina  (RV)", seen.VisibleRows[0].Title);
        Assert.Equal($"{31102.ToString("N0", CultureInfo.CurrentCulture)} versículos", seen.VisibleRows[0].Detail);
        // Without a picker the button does not count.
        Assert.Equal("Guardar…", seen.AcceptButtonText);
    }

    [Fact]
    public void To_import_only_the_missing_ones_come_ticked()
    {
        IReadOnlyList<BiblePackageChoice> choices =
        [
            new(new BiblePackageEntry("a", "Reina", "RV", "es"), true),
            new(new BiblePackageEntry("b", "Oso", "O", "es"), false),
        ];
        BiblePackageViewModel? seen = null;
        var dialog = new BiblePackageDialog(list => { seen = list; return true; });

        Assert.Equal([choices[1].Entry], dialog.ChooseToImport("mias.ecbiblias", choices));
        Assert.Contains("«mias.ecbiblias»", seen!.Intro);
        Assert.False(seen.HasWarning);
        Assert.Equal(["ya está en la biblioteca", "se va a agregar"], seen.VisibleRows.Select(r => r.Detail));
    }

    [Fact]
    public void Closing_without_accepting_gives_nothing_back()
    {
        var dialog = new BiblePackageDialog(_ => false);

        Assert.Null(dialog.ChooseToExport([new(1, "a", "a", "es", 1)]));
        Assert.Null(dialog.ChooseToImport("x", []));
        Assert.Null(dialog.ChooseToDownload(Catalog, [false, false, false]));
    }

    [Fact]
    public void To_download_spanish_goes_first_with_its_sizes_and_only_it_is_shown()
    {
        var (list, ordered) = BiblePackageDialog.ForDownload(Catalog, [true, false, false]);

        Assert.Equal("es", ordered[0].Language);
        Assert.Equal("en", ordered[^1].Language);
        Assert.Equal("Español", list.SelectedGroup);
        Assert.Equal(BiblePackageViewModel.AllGroups, list.Groups[0]);
        Assert.Equal(2, list.VisibleRows.Count);
        Assert.All(list.VisibleRows, r => Assert.False(r.IsChecked));

        list.SelectedGroup = BiblePackageViewModel.AllGroups;
        var web = list.VisibleRows[^1];
        Assert.Equal($"Inglés · {1.5.ToString("0.0", CultureInfo.CurrentCulture)} MB · Dominio público · ya está en la biblioteca", web.Detail);
        Assert.Contains(list.VisibleRows, r => r.Detail == "Español · 2 KB");
        // Never zero.
        Assert.Contains(list.VisibleRows, r => r.Detail == "Español · 1 KB");
    }

    [Fact]
    public void All_and_none_act_on_what_is_shown_and_the_button_counts_everything()
    {
        BiblePackageViewModel? seen = null;
        var dialog = new BiblePackageDialog(list =>
        {
            seen = list;
            list.CheckAll();
            Assert.Equal("Descargar (2)", list.AcceptButtonText);
            list.SelectedGroup = "Inglés";
            list.CheckAll();
            Assert.Equal("Descargar (3)", list.AcceptButtonText);
            list.CheckNone();
            Assert.Equal("Descargar (2)", list.AcceptButtonText);
            return true;
        });

        var chosen = dialog.ChooseToDownload(Catalog, [false, false, false]);

        Assert.Equal(["es", "es"], chosen!.Select(c => c.Language));
        seen!.SelectedGroup = "Español";
        seen.CheckNone();
        Assert.Equal("Descargar", seen.AcceptButtonText);
    }

    [Fact]
    public void With_nothing_ticked_it_says_so_and_stays()
    {
        using var notices = new ScriptedNotices();
        var list = BiblePackageDialog.ForImport("x", [new(new BiblePackageEntry("a", "A", "a", "es"), true)]);

        Assert.False(list.TryAccept());
        Assert.Equal("Marcá al menos una versión.", Assert.Single(notices.Asked).Message);

        list.CheckAll();
        Assert.True(list.TryAccept());
    }
}
