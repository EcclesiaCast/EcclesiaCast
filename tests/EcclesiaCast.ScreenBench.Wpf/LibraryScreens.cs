using System.Windows;
using EcclesiaCast.App.Views;
using EcclesiaCast.Core.Songs.Web;
using EcclesiaCast.Presentation.Services;
using EcclesiaCast.Presentation.ViewModels;
using EcclesiaCast.ScreenSamples;

namespace EcclesiaCast.ScreenBench.Wpf;

/// <summary>
/// The screens of the Bible and the songs (batch 16), with the sample data
/// both benches share (LibrarySamples).
/// </summary>
internal static class LibraryScreens
{
    /// <summary>What the system's title bar takes from a window of fixed height.</summary>
    private const double TitleBar = 39;

    public static IEnumerable<(string Name, Func<FrameworkElement> Make, double Width)> All()
    {
        yield return ("editor-cancion", () => Take(
            new SongEditorWindow(LibrarySamples.Song(), LibrarySamples.Themes()), 680 - TitleBar), 680);
        yield return ("editor-cancion-nueva", () => Take(
            new SongEditorWindow(null, LibrarySamples.Themes()), 680 - TitleBar), 680);

        yield return ("importar-biblia", () => Take(new BibleImportWindow("Mi Biblia")), 440);

        yield return ("biblias-exportar", () => Take(ExportPackage()), 520);
        yield return ("biblias-importar", () => Take(ImportPackage()), 520);
        yield return ("biblias-bajar", () => Take(Download()), 520);

        yield return ("ayuda-formato-biblia", () => Take(new BibleFormatHelpWindow()), 760);

        yield return ("buscador", () =>
        {
            var window = new GlobalSearchWindow(LibrarySamples.SearchHits, LibrarySamples.SearchQuery);
            // The search waits for a pause in the typing. It is run by hand:
            // waiting for the clock would mean running the dispatcher (see Program.Main).
            var viewModel = (EcclesiaCast.Presentation.ViewModels.GlobalSearchViewModel)typeof(GlobalSearchWindow)
                .GetField("_viewModel", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .GetValue(window)!;
            viewModel.SearchNow();
            return Take(window, 520 - TitleBar);
        }, 720);

        yield return ("papelera", () => Take(Trash(empty: false), 460 - TitleBar), 640);
        yield return ("papelera-vacia", () => Take(Trash(empty: true), 460 - TitleBar), 640);

        yield return ("importar-canciones", () => Take(SongImport(holyrics: false), 600 - TitleBar), 640);
        yield return ("importar-canciones-holyrics", () => Take(SongImport(holyrics: true), 600 - TitleBar), 640);

        yield return ("buscar-letra-vacia", () => Take(WebSearch(search: false), 680 - TitleBar), 980);
        yield return ("buscar-letra", () => Take(WebSearch(search: true), 680 - TitleBar), 980);
    }

    private static BiblePackageWindow ExportPackage() =>
        new(BiblePackageDialog.ForExport(LibrarySamples.InstalledVersions()));

    private static BiblePackageWindow ImportPackage() =>
        new(BiblePackageDialog.ForImport("mis-biblias.ecbiblias", LibrarySamples.PackageChoices()));

    private static BiblePackageWindow Download()
    {
        var list = BiblePackageDialog.ForDownload(LibrarySamples.Catalog(), LibrarySamples.CatalogInstalled()).List;
        // One ticked, as when the operator is about to download it.
        list.VisibleRows[0].IsChecked = true;
        return new BiblePackageWindow(list);
    }

    private static TrashWindow Trash(bool empty)
    {
        var (songs, themes) = LibrarySamples.Trash(empty);
        return new TrashWindow(new TrashViewModel(songs, themes));
    }

    private static SongImportWindow SongImport(bool holyrics)
    {
        var viewModel = new SongImportViewModel(
            new SongSaver(LibrarySamples.NoSongs(), LibrarySamples.Sample.Of<ITextPrompt>()),
            LibrarySamples.ImportPaths());
        var window = new SongImportWindow(viewModel);
        // Never shown, so it is told it loaded: it looks in the made-up folders.
        window.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
        if (holyrics)
            window.ChooseHolyrics();
        return window;
    }

    private static WebSongSearchWindow WebSearch(bool search)
    {
        var viewModel = new WebSongSearchViewModel(
            new SongSaver(LibrarySamples.NoSongs(), LibrarySamples.Sample.Of<ITextPrompt>()),
            LibrarySamples.NoSongs(), new LibrarySamples.WebSongs());
        var window = new WebSongSearchWindow(viewModel);
        if (search)
            viewModel.SearchAsync("renuévame").GetAwaiter().GetResult();
        return window;
    }

    /// <summary>
    /// <paramref name="height"/> is the whole inside of the window: the margin
    /// of what it shows comes out of it.
    /// </summary>
    internal static FrameworkElement Take(Window window, double height = double.NaN)
    {
        var margin = ((FrameworkElement)window.Content).Margin;
        return DialogScreens.Take(window, height - margin.Top - margin.Bottom);
    }
}
