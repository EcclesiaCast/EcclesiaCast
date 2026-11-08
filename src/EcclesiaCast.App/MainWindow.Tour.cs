using System.Windows;
using System.Windows.Input;
using EcclesiaCast.App.ViewModels;
using EcclesiaCast.Core.Ui;

namespace EcclesiaCast.App;

/// <summary>
/// The welcome tour (<see cref="Controls.WelcomeTourOverlay"/>): which part
/// of this window each step lights, and the Canciones / Biblia tab it needs.
/// It opens by itself once, the first time the program opens (see App), and
/// from Ayuda → «Recorrido del programa».
/// </summary>
public partial class MainWindow
{
    private bool? _tourWasOnBible;

    /// <summary>Starts the tour from its greeting; does nothing if it is already on.</summary>
    public void StartWelcomeTour()
    {
        if (Tour.IsRunning)
            return;

        _tourWasOnBible = (DataContext as MainViewModel)?.IsBibleTabActive;
        PreviewKeyDown += TourKeys;
        Tour.Finished += TourFinished;
        Tour.Start(WindowContent, FindTourPart, PrepareTourPart);
    }

    private FrameworkElement? FindTourPart(string part) => part switch
    {
        WelcomeTour.Songs or WelcomeTour.Bible => LibraryPanel,
        WelcomeTour.Playlist => PlaylistPanel,
        WelcomeTour.Slides => SlidesPanel,
        WelcomeTour.Media => MediaPanel,
        WelcomeTour.Tools => ToolsPanel,
        WelcomeTour.Output => RightPanel,
        WelcomeTour.ScreenButtons => ScreenButtons,
        WelcomeTour.GoLive => OutputControls,
        _ => null,
    };

    /// <summary>
    /// Canciones and Biblia share a panel: their steps open their tab. When
    /// the tour ends (null), the tab that was open before comes back.
    /// </summary>
    private void PrepareTourPart(string? part)
    {
        if (DataContext is not MainViewModel vm)
            return;

        var wantBible = part switch
        {
            WelcomeTour.Songs => false,
            WelcomeTour.Bible => true,
            null => _tourWasOnBible,
            _ => (bool?)null,
        };
        if (wantBible is not { } bible || vm.IsBibleTabActive == bible)
            return;

        var command = bible ? vm.ShowBibleTabCommand : vm.ShowSongsTabCommand;
        if (command.CanExecute(null))
            command.Execute(null);
    }

    /// <summary>
    /// While the tour is on, every key goes to it first, wherever the focus
    /// was: F1 or Esc must not reach the projection behind it.
    /// </summary>
    private void TourKeys(object sender, KeyEventArgs e) => Tour.HandleKey(e);

    private void TourFinished(object? sender, EventArgs e)
    {
        PreviewKeyDown -= TourKeys;
        Tour.Finished -= TourFinished;
    }

    private void WelcomeTour_Click(object sender, RoutedEventArgs e) => StartWelcomeTour();
}
