using System.Windows;
using EcclesiaCast.Core.Ui;

namespace EcclesiaCast.App.ViewModels;

public sealed partial class MainViewModel
{
    /// <summary>
    /// A real window of the program, built but not shown, for a tutorial step to
    /// open beside its card (<see cref="TourActions"/>): the same ones the menus
    /// open, so what the card talks about is what the person will see. Null if
    /// it cannot be opened (the phone and OBS windows need the local server).
    /// </summary>
    public Window? CreateTutorialWindow(string id)
    {
        var owner = Application.Current.MainWindow;
        switch (id)
        {
            case TourActions.SongEditor:
                return new Views.SongEditorWindow(null, _themes.GetAll()) { Owner = owner };
            case TourActions.SongSearch:
                return new Views.WebSongSearchWindow(_songSaver, _songs) { Owner = owner };
            case TourActions.SongImport:
                return new Views.SongImportWindow(_songSaver, _settings) { Owner = owner };
            case TourActions.SongImportHolyrics:
                var import = new Views.SongImportWindow(_songSaver, _settings) { Owner = owner };
                import.Loaded += (_, _) => import.ChooseHolyrics();
                return import;
            case TourActions.Broadcast when StartRemote():
                return new Views.BroadcastWindow(_remote!, _settings, newPin => _settings.Set(RemoteObsPinKey, newPin)) { Owner = owner };
            case TourActions.Remote when StartRemote():
                return new Views.RemoteControlWindow(_remote!, newPin => _settings.Set(RemotePinKey, newPin)) { Owner = owner };
            default:
                return null;
        }
    }
}
