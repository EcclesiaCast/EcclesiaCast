using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using EcclesiaCast.Desktop.Views;
using EcclesiaCast.Presentation.Platform;
using EcclesiaCast.Presentation.Services;
using EcclesiaCast.Presentation.ViewModels;
using EcclesiaCast.ScreenSamples;

namespace EcclesiaCast.Desktop.Tests;

/// <summary>
/// The windows of songs, Bibles and the library (batch 16), with the same
/// names and the same made-up data as in the WPF bench (LibraryScreens.cs).
/// </summary>
public sealed partial class ScreenBenchTests
{
    [AvaloniaFact]
    public void The_song_editor_is_drawn() => DrawWindow(
        new SongEditorWindow(LibrarySamples.Song(), LibrarySamples.Themes()), "editor-cancion", 680 - 39);

    [AvaloniaFact]
    public void The_editor_of_a_new_song_is_drawn() => DrawWindow(
        new SongEditorWindow(null, LibrarySamples.Themes()), "editor-cancion-nueva", 680 - 39);

    /// <summary>Looks only in the made-up folders: never in those of this computer.</summary>
    internal static SongImportWindow SongImportWindow() => new(
        new SongImportViewModel(
            new SongSaver(LibrarySamples.NoSongs(), LibrarySamples.Sample.Of<ITextPrompt>()),
            LibrarySamples.ImportPaths()),
        LibrarySamples.Sample.Of<IDialogs>(), new RecordingShell());

    [AvaloniaFact]
    public void The_song_import_is_drawn() => DrawWindow(SongImportWindow(), "importar-canciones", 600 - 39);

    [AvaloniaFact]
    public void The_song_import_from_holyrics_is_drawn()
    {
        var window = SongImportWindow();
        window.Opened += (_, _) => Dispatcher.UIThread.Post(window.ChooseHolyrics);
        DrawWindow(window, "importar-canciones-holyrics", 600 - 39);
    }

    internal static GlobalSearchWindow SearchWindow() =>
        new(LibrarySamples.SearchHits, LibrarySamples.SearchQuery, new Platform.AvaloniaUiTimerFactory());

    [AvaloniaFact]
    public void The_global_search_is_drawn()
    {
        var window = SearchWindow();
        // The search waits for a pause in the typing: it is run by hand.
        window.SearchNow();
        DrawWindow(window, "buscador", 520 - 39);
    }

    [AvaloniaFact]
    public void Importing_a_bible_is_drawn() => DrawWindow(new BibleImportWindow("Mi Biblia"), "importar-biblia");

    [AvaloniaFact]
    public void The_help_with_the_bible_formats_is_drawn()
    {
        var window = HelpWindow();
        // The WPF picture has the whole content: here the screen does not limit it.
        window.MaxHeight = 2000;
        DrawWindow(window, "ayuda-formato-biblia");
    }

    [Theory]
    [InlineData(1080, 1.0, 900)]
    [InlineData(768, 1.0, 712)]
    [InlineData(1440, 2.0, 664)]
    [InlineData(0, 1.0, 900)]
    public void The_help_is_never_taller_than_the_screen(int workingPixels, double scaling, double expected) =>
        Assert.Equal(expected, BibleFormatHelpWindow.HeightLimit(900, workingPixels, scaling));

    internal static BibleFormatHelpWindow HelpWindow(IClipboardText? clipboard = null) => new(
        new BibleFormatHelpViewModel(
            LibrarySamples.Sample.Of<IDialogs>(), clipboard ?? LibrarySamples.Sample.Of<IClipboardText>(),
            PlatformSystem.Windows));

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
