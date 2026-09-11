using EcclesiaCast.App.Views;
using EcclesiaCast.Core.Playlists;

namespace EcclesiaCast.App.Services;

public sealed class SmartPlaylistDialogService : ISmartPlaylistDialog
{
    public SmartPlaylistChoice? Show(string name, PlaylistRule rule, string? value)
    {
        var window = new SmartPlaylistWindow(name, rule, value)
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };

        return window.ShowDialog() == true
            ? new SmartPlaylistChoice(window.ResultName, window.ResultRule, window.ResultValue)
            : null;
    }
}
