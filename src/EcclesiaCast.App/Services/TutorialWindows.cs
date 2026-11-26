using System.Windows;
using EcclesiaCast.App.Views;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Songs.Web;
using EcclesiaCast.Core.Ui;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.App.Services;

/// <summary>
/// The real windows of the program a tutorial step opens beside its card
/// (<see cref="TourActions"/>): the same ones the menus open, so what the card
/// talks about is what the person will see.
/// </summary>
public sealed class TutorialWindows(
    IThemeRepository themes,
    SongSaver songSaver,
    ISongRepository songs,
    IPlatformPaths paths,
    ISettingsStore settings,
    IColorPicker colorPicker)
{
    /// <summary>
    /// The window, built but not shown. Null if it cannot be opened (the phone
    /// and OBS windows need the local server).
    /// </summary>
    public Window? Create(MainViewModel main, string id)
    {
        var owner = Application.Current.MainWindow;
        switch (id)
        {
            case TourActions.SongEditor:
                return new SongEditorWindow(null, themes.GetAll()) { Owner = owner };
            case TourActions.SongSearch:
                return new WebSongSearchWindow(new WebSongSearchViewModel(songSaver, songs, new WebSongClient()))
                {
                    Owner = owner,
                };
            case TourActions.SongImport:
                return SongImport(owner);
            case TourActions.SongImportHolyrics:
                var import = SongImport(owner);
                import.Loaded += (_, _) => import.ChooseHolyrics();
                return import;
            case TourActions.Broadcast when main.RemoteForTutorial() is { } server:
                return new BroadcastWindow(server,
                    over => new BroadcastStyleWindow(new BroadcastStyleViewModel(settings, colorPicker))
                    {
                        Owner = over,
                    }.ShowDialog(),
                    main.KeepObsPin)
                {
                    Owner = owner,
                };
            case TourActions.Remote when main.RemoteForTutorial() is { } server:
                return new RemoteControlWindow(server, main.KeepRemotePin) { Owner = owner };
            default:
                return null;
        }
    }

    private SongImportWindow SongImport(Window? owner) =>
        new(new SongImportViewModel(songSaver, paths)) { Owner = owner };
}
