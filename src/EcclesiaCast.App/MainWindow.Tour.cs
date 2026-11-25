using System.IO;
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
    public void StartWelcomeTour() => StartTour(null);

    /// <summary>
    /// Starts one of the Ayuda â†’ Â«Tutorialesâ€¦Â» (a short tour over the real
    /// window, from a text file); does nothing if a tour is already on.
    /// </summary>
    public void StartTutorial(Tutorial tutorial) => StartTour(tutorial.Steps);

    private void StartTour(IReadOnlyList<TourStep>? steps)
    {
        if (Tour.IsRunning)
            return;

        _tourWasOnBible = (DataContext as MainViewModel)?.IsBibleTabActive;
        PreviewKeyDown += TourKeys;
        Tour.Finished += TourFinished;
        Tour.Start(WindowContent, FindTourPart, PrepareTourPart, steps);
    }

    // A tutorial names the element it lights by its x:Name in this window.
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
        _ => FindName(part) as FrameworkElement,
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
            WelcomeTour.Songs or "SongsAddButton" or "SongsList" or "LyricsSearchBox" => false,
            WelcomeTour.Bible or "AddPassageButton" or "BibleDownloadButton" or "BibleQueryBox" or "VersionsRow" => true,
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

    /// <summary>
    /// The list of tutorials: the ones that ship with the program plus any
    /// <c>.txt</c> in Â«TutorialesÂ» inside the data folder. Choosing one closes
    /// the list and starts it over this window.
    /// </summary>
    private void Tutorials_Click(object sender, RoutedEventArgs e)
    {
        if (Tour.IsRunning)
            return;

        var list = TutorialFile.LoadAll(Path.Combine(App.DataDir, "Tutoriales"));
        var window = new Views.TutorialsWindow(list) { Owner = this };
        if (window.ShowDialog() == true && window.Chosen is { } chosen)
            StartTutorial(chosen);
    }
}
