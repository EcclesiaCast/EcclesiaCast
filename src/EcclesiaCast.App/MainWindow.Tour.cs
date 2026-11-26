using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using EcclesiaCast.App.Controls;
using EcclesiaCast.Presentation.ViewModels;
using EcclesiaCast.Core.Ui;

namespace EcclesiaCast.App;

/// <summary>
/// The welcome tour and the tutorials (<see cref="Controls.WelcomeTourOverlay"/>):
/// which part of this window each step lights, the Canciones / Biblia tab it
/// needs, and what it opens first (a menu, a window, a tab of the tools) so the
/// step shows the screen it talks about. The welcome tour opens by itself once,
/// the first time the program opens (see App), and from Ayuda → «Recorrido del
/// programa»; each tutorial starts from Ayuda → «Tutoriales».
/// </summary>
public partial class MainWindow
{
    private bool? _tourWasOnBible;

    // What the current step opened, so the next step can close it.
    private string? _tourAction;
    private MenuItem? _tourMenu;
    private ContextMenu? _tourContext;
    private Popup? _tourPopup;
    private FrameworkElement? _tourPopupAnchor;
    private Window? _tourWindow;
    private Func<MainViewModel, string, Window?>? _tutorialWindows;
    private TourPulse? _tourPulse;
    private int _tourPulseGeneration;

    // What the tour changed for good, put back when it ends.
    private RadioButton? _toolsTabBefore;
    private bool? _gridViewBefore;
    private TourViewBefore? _viewBefore;

    /// <summary>How the window was folded before a tutorial opened what its steps talk about.</summary>
    private sealed record TourViewBefore(
        bool Tools, bool Library, bool Playlist, bool PlaylistByGrid, bool Media, bool Rail, int MergedTab);

    private static readonly HashSet<string> SongsTabParts =
        ["SongsAddButton", "SongsList", "LyricsSearchBox"];

    private static readonly HashSet<string> BibleTabParts =
        ["AddPassageButton", "BibleDownloadButton", "BibleQueryBox", "VersionsRow", "BibleViewToggleButton",
         "BibleHistoryButton", "BibleQueryRow", "BibleResultsList"];

    /// <summary>Starts the tour from its greeting; does nothing if it is already on.</summary>
    public void StartWelcomeTour() => StartTour(null);

    /// <summary>
    /// Starts one of the Ayuda → «Tutoriales» (a short tour over the real
    /// window, from a text file); does nothing if a tour is already on.
    /// </summary>
    public void StartTutorial(Tutorial tutorial) => StartTour(tutorial.Steps);

    private void StartTour(IReadOnlyList<TourStep>? steps)
    {
        if (Tour.IsRunning)
            return;

        _tourWasOnBible = (DataContext as MainViewModel)?.IsBibleTabActive;
        _viewBefore = DataContext is MainViewModel before
            ? new(_toolsCollapsed, _libraryCollapsed, _playlistCollapsed, _playlistFoldedByGrid,
                _mediaCollapsed, _leftRail, before.MergedTab)
            : null;
        PreviewKeyDown += TourKeys;
        Tour.Finished += TourFinished;
        Tour.ExtraSpot = TourExtraSpot;
        Tour.KeepClear = TourKeepClear;
        Tour.Start(WindowContent, FindTourPart, PrepareTourStep, steps);
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
    /// What a step needs before its card shows: the tab of Canciones or Biblia, and
    /// whatever it opens (see <see cref="TourActions"/>). What the step before it
    /// opened is closed first. Null when the tour ends: everything goes back as it was.
    /// </summary>
    private void PrepareTourStep(TourStep? step)
    {
        if (DataContext is not MainViewModel vm)
            return;

        if (step is null)
        {
            CloseTourExtras();
            _tourAction = null;
            ShowLibraryTab(vm, _tourWasOnBible);
            if (_gridViewBefore is { } grid)
                vm.IsBibleGridView = grid;
            _gridViewBefore = null;
            if (_toolsTabBefore is { } tab)
                tab.IsChecked = true;
            _toolsTabBefore = null;
            RestoreViewAfterTour(vm);
            return;
        }

        var changed = step.Open != _tourAction;
        if (changed)
        {
            CloseTourExtras();
            _tourAction = step.Open;
        }

        ShowLibraryTab(vm, WantsBibleTab(step));
        RevealTourPart(vm, step);
        if (changed && step.Open is { } action)
            RunTourAction(vm, action);

        ClearTourPulse();
        if (step.Highlight is { } name && _tourWindow is { } window)
        {
            // The window lays itself out after it opens: frame the element once it has.
            var generation = ++_tourPulseGeneration;
            window.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, () =>
            {
                if (generation == _tourPulseGeneration && ReferenceEquals(_tourWindow, window)
                    && window.FindName(name) is FrameworkElement element)
                    _tourPulse = TourPulse.Show(element);
            });
        }
    }

    /// <summary>
    /// Opens whatever is folded away around what the step talks about: the
    /// tools shrunk to icons, the library or the playlist (or the rail that
    /// stands for both), the media bar, and the LIVE tab of the box it shares
    /// with PREVIEW. It all goes back when the tour ends.
    /// </summary>
    private void RevealTourPart(MainViewModel vm, TourStep step)
    {
        var parts = new List<DependencyObject>();
        if (step.Target is { } target && FindName(target) is DependencyObject lit)
            parts.Add(lit);
        if (step.Open is { } open && TourActions.TryGetElementName(open, out var name)
            && FindName(name) is DependencyObject opened)
            parts.Add(opened);
        if (step.Open is TourActions.AddMenu)
            parts.Add(SongsAddButton);
        if (step.Open is TourActions.BibleHistory)
            parts.Add(BibleHistoryButton);

        bool Needs(params DependencyObject[] sections) =>
            parts.Any(part => sections.Any(section => IsInside(part, section)));

        var tools = _toolsCollapsed && Needs(ToolsPanel);
        var rail = _leftRail && Needs(LibraryPanel, PlaylistPanel);
        var library = _libraryCollapsed && (Needs(LibraryBody) || parts.Contains(LibraryPanel));
        var playlist = _playlistCollapsed && Needs(PlaylistPanel);
        var media = _mediaCollapsed && Needs(MediaPanel);

        if (tools || rail || library || playlist || media)
        {
            CaptureSizes();
            if (tools)
                _toolsCollapsed = false;
            if (rail)
                _leftRail = false;
            if (library)
                _libraryCollapsed = false;
            if (playlist)
                _playlistCollapsed = _playlistFoldedByGrid = false;
            if (media)
                _mediaCollapsed = false;
            ApplyLayout();
        }

        if (Needs(LivePanel) && vm.IsOutputMerged)
            vm.MergedTab = 0;
    }

    private static bool IsInside(DependencyObject part, DependencyObject section)
    {
        for (DependencyObject? at = part; at is not null; at = LogicalTreeHelper.GetParent(at))
        {
            if (ReferenceEquals(at, section))
                return true;
        }

        return false;
    }

    private void RestoreViewAfterTour(MainViewModel vm)
    {
        if (_viewBefore is not { } before)
            return;
        _viewBefore = null;

        CaptureSizes();
        _toolsCollapsed = before.Tools;
        _libraryCollapsed = before.Library;
        _playlistCollapsed = before.Playlist;
        _playlistFoldedByGrid = before.PlaylistByGrid;
        _mediaCollapsed = before.Media;
        _leftRail = before.Rail;
        ApplyLayout();
        // The Bible grid, shown by a step, may have saved the layout opened up.
        SaveLayoutOptions();
        vm.MergedTab = before.MergedTab;
    }

    /// <summary>The tab a step needs: Biblia, Canciones, or null for "whichever is open".</summary>
    private static bool? WantsBibleTab(TourStep step)
    {
        if (step.Open is TourActions.BibleHistory or TourActions.BibleGrid or TourActions.BibleList or TourActions.BibleTab)
            return true;
        if (step.Open is TourActions.SongsTab)
            return false;

        var opensSongWindow = step.Open is { } open && TourActions.TryGetWindow(open, out var window)
            && window is TourActions.SongEditor or TourActions.SongSearch or TourActions.SongImport or TourActions.SongImportHolyrics;
        if (step.Open is TourActions.AddMenu || opensSongWindow)
            return false;

        return step.Target switch
        {
            WelcomeTour.Songs => false,
            WelcomeTour.Bible => true,
            { } t when SongsTabParts.Contains(t) => false,
            { } t when BibleTabParts.Contains(t) => true,
            _ => null,
        };
    }

    private static void ShowLibraryTab(MainViewModel vm, bool? bible)
    {
        if (bible is not { } wantBible || vm.IsBibleTabActive == wantBible)
            return;

        var command = wantBible ? vm.ShowBibleTabCommand : vm.ShowSongsTabCommand;
        if (command.CanExecute(null))
            command.Execute(null);
    }

    private void RunTourAction(MainViewModel vm, string action)
    {
        if (TourActions.TryGetWindow(action, out var windowId))
        {
            OpenTourWindow(vm, windowId);
            return;
        }

        if (TourActions.TryGetElementName(action, out var name))
        {
            if (action.StartsWith(TourActions.MenuPrefix, StringComparison.Ordinal))
            {
                if (FindName(name) is MenuItem menu)
                {
                    menu.IsSubmenuOpen = true;
                    _tourMenu = menu;
                }
            }
            else if (FindName(name) is RadioButton tab)
            {
                _toolsTabBefore ??= ToolsTabs.Children.OfType<RadioButton>().FirstOrDefault(r => r.IsChecked == true);
                tab.IsChecked = true;
            }

            return;
        }

        switch (action)
        {
            case TourActions.AddMenu:
                if (BuildAddSongMenu(SongsAddButton) is { } add)
                {
                    add.IsOpen = true;
                    _tourContext = add;
                }
                break;
            case TourActions.BibleHistory:
                BibleHistoryPopup.IsOpen = true;
                _tourPopup = BibleHistoryPopup;
                _tourPopupAnchor = BibleQueryRow;
                break;
            case TourActions.BibleGrid:
            case TourActions.BibleList:
                _gridViewBefore ??= vm.IsBibleGridView;
                vm.IsBibleGridView = action == TourActions.BibleGrid;
                break;
        }
    }

    /// <summary>What builds the windows a tutorial opens (see <see cref="Services.TutorialWindows"/>).</summary>
    public void AttachTutorialWindows(Func<MainViewModel, string, Window?> create) => _tutorialWindows = create;

    /// <summary>
    /// Opens one of the program's windows beside the card, not on top of it: it
    /// goes to the right edge of this window, and the card keeps to the left.
    /// It is not modal, so the card keeps answering to Next.
    /// </summary>
    private void OpenTourWindow(MainViewModel vm, string id)
    {
        var window = _tutorialWindows?.Invoke(vm, id);
        if (window is null)
            return;

        var main = MainBoundsInDips();
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Left = Math.Max(main.X, main.Right - window.Width - 24);
        window.Top = Math.Max(main.Y, main.Y + (main.Height - window.Height) / 2);
        window.ShowInTaskbar = false;
        window.Closed += (_, _) =>
        {
            if (ReferenceEquals(_tourWindow, window))
            {
                _tourWindow = null;
                ClearTourPulse();
            }
        };
        _tourWindow = window;
        window.Show();

        // The new window took the keyboard; give it back to the card.
        Dispatcher.BeginInvoke(() =>
        {
            Activate();
            Tour.FocusNext();
        }, DispatcherPriority.ApplicationIdle);
    }

    private Rect MainBoundsInDips()
    {
        var topLeft = PointToScreen(new Point(0, 0));
        var scale = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformFromDevice;
        var origin = scale?.Transform(topLeft) ?? topLeft;
        return new Rect(origin, new Size(ActualWidth, ActualHeight));
    }

    private void ClearTourPulse()
    {
        _tourPulseGeneration++;
        _tourPulse?.Remove();
        _tourPulse = null;
    }

    private void CloseTourExtras()
    {
        ClearTourPulse();

        if (_tourMenu is { } menu)
            menu.IsSubmenuOpen = false;
        _tourMenu = null;

        if (_tourContext is { } context)
            context.IsOpen = false;
        _tourContext = null;

        if (_tourPopup is { } popup)
            popup.IsOpen = false;
        _tourPopup = null;
        _tourPopupAnchor = null;

        if (_tourWindow is { } window)
        {
            _tourWindow = null;
            window.Close();
        }
    }

    /// <summary>The drop-down of an open menu is lit with its header, so the card never covers it.</summary>
    private Rect? TourExtraSpot(TourStep step)
    {
        FrameworkElement? anchor = null;
        FrameworkElement? child = null;
        if (_tourMenu is { } menu)
        {
            anchor = menu;
            child = (menu.Template.FindName("PART_Popup", menu) as Popup)?.Child as FrameworkElement;
        }
        else if (_tourContext is { } context)
        {
            anchor = context.PlacementTarget as FrameworkElement;
            child = context;
        }
        else if (_tourPopup is { } popup)
        {
            anchor = _tourPopupAnchor;
            child = popup.Child as FrameworkElement;
        }

        if (anchor is null || child is null || !anchor.IsVisible)
            return null;

        child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var size = child.DesiredSize;
        var bounds = anchor.TransformToVisual(Tour).TransformBounds(new Rect(anchor.RenderSize));
        return new Rect(bounds.X, bounds.Bottom, Math.Max(size.Width, 1), Math.Max(size.Height, 1));
    }

    /// <summary>Where the window the step opened sits over this one, so the card goes to the other side.</summary>
    private Rect? TourKeepClear(TourStep step)
    {
        if (_tourWindow is not { IsLoaded: true } window)
            return null;

        var from = Tour.PointFromScreen(window.PointToScreen(new Point(0, 0)));
        var to = Tour.PointFromScreen(window.PointToScreen(new Point(window.ActualWidth, window.ActualHeight)));
        return new Rect(from, to);
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
    /// Ayuda → «Tutoriales ▸»: one item per tutorial, the ones that ship with
    /// the program plus any <c>.txt</c> in «Tutoriales» inside the data folder.
    /// Filled when Ayuda opens, so a file dropped in the folder shows up at once.
    /// </summary>
    private void HelpMenu_SubmenuOpened(object sender, RoutedEventArgs e)
    {
        if (!ReferenceEquals(e.OriginalSource, sender))
            return;

        TutorialsMenu.Items.Clear();
        foreach (var tutorial in TutorialFile.LoadAll(System.IO.Path.Combine(App.DataDir, "Tutoriales")))
        {
            var item = new MenuItem
            {
                Header = $"{tutorial.Icon}  {tutorial.Title}",
                ToolTip = tutorial.Summary,
                Tag = tutorial,
            };
            item.Click += (_, _) =>
            {
                if (!Tour.IsRunning)
                    StartTutorial(tutorial);
            };
            TutorialsMenu.Items.Add(item);
        }
    }
}
