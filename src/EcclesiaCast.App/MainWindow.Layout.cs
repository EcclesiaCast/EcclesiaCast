using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;

namespace EcclesiaCast.App;

/// <summary>
/// Where each part of the operator window sits, and which parts are folded
/// away. Every church sets up its desk differently, so the big blocks can
/// trade places (Ver menu) and the library and the playlist fold down to
/// their title (the media bar and the text tools too, and the library and the
/// playlist can shrink together to a rail of icons); all of it is remembered
/// for the next service. The blocks move
/// between fixed slots instead of floating free: floating windows get lost
/// behind the projector mid-service, and the usual docking library (MS-PL)
/// can't be combined with the GPL.
/// </summary>
public partial class MainWindow
{
    private const string MirroredKey = "layout.mirrored";
    private const string PlaylistTopKey = "layout.playlist.top";
    private const string MediaTopKey = "layout.media.top";
    private const string LiveBottomKey = "layout.live.bottom";
    private const string OutputSplitKey = "layout.output.split";
    private const string LibraryCollapsedKey = "layout.library.collapsed";
    private const string PlaylistCollapsedKey = "layout.playlist.collapsed";
    private const string MediaCollapsedKey = "layout.media.collapsed";
    private const string ToolsCollapsedKey = "layout.tools.collapsed";
    private const string LeftRailKey = "layout.left.rail";
    private const string AudioListCollapsedKey = "layout.audiolist.collapsed";

    private const double LibraryMinWidth = 200;
    private const double RightMinWidth = 260;
    private const double PlaylistMinHeight = 120;
    private const double MediaMinHeight = 86;
    private const double ToolsMinHeight = 130;

    /// <summary>Wide enough for the 36 px buttons of the rail plus its border.</summary>
    private const double RailWidth = 44;

    /// <summary>Library and playlist on the right, live and preview on the left.</summary>
    private bool _mirrored;

    /// <summary>Playlist above the song and Bible library instead of below it.</summary>
    private bool _playlistOnTop;

    /// <summary>Media bar above the slide grid instead of below it.</summary>
    private bool _mediaOnTop;

    /// <summary>Preview above Live instead of below it.</summary>
    private bool _liveAtBottom;

    /// <summary>
    /// LIVE and PREVIEW as two boxes. Off (the default) they, and the stage
    /// view, are the tabs of a single box: the right column is narrow, and one
    /// box leaves the rest of it to the text panels under it.
    /// </summary>
    private bool _outputSplit;

    // The sections that fold down: see Folds.
    private bool _libraryCollapsed;
    private bool _playlistCollapsed;

    /// <summary>Only the header (title and buttons) of the media bar is left.</summary>
    private bool _mediaCollapsed;

    /// <summary>The text tools (quick text, footer notice...) as a strip of icons.</summary>
    private bool _toolsCollapsed;

    /// <summary>Library and playlist shrunk to a narrow column of icons.</summary>
    private bool _leftRail;

    /// <summary>The Music tab without its list of audios: only the players are left.</summary>
    private bool _audioListCollapsed;

    // Sizes of the blocks themselves, whichever slot they sit in.
    private double _libraryWidth = 270;
    private double _rightWidth = 320;
    private double _playlistHeight = 240;
    private double _mediaHeight = 170;

    /// <summary>
    /// One section that can fold away: the setting that remembers it, how to
    /// read and write its flag, and the Ver menu entry that mirrors it.
    /// Restoring, saving, ticking the menu and resetting all walk this list, so
    /// a new foldable section is one line here and its look in ApplyLayout.
    /// </summary>
    private sealed record Fold(string Key, Func<bool> Get, Action<bool> Set, Func<MenuItem> Item);

    private IReadOnlyList<Fold>? _folds;

    private IReadOnlyList<Fold> Folds => _folds ??=
    [
        new(LibraryCollapsedKey, () => _libraryCollapsed, v => _libraryCollapsed = v, () => LibraryCollapsedItem),
        new(PlaylistCollapsedKey, () => _playlistCollapsed, v => _playlistCollapsed = v, () => PlaylistCollapsedItem),
        new(MediaCollapsedKey, () => _mediaCollapsed, v => _mediaCollapsed = v, () => MediaCollapsedItem),
        new(ToolsCollapsedKey, () => _toolsCollapsed, v => _toolsCollapsed = v, () => ToolsCollapsedItem),
        new(LeftRailKey, () => _leftRail, v => _leftRail = v, () => LeftRailItem),
        new(AudioListCollapsedKey, () => _audioListCollapsed, v => _audioListCollapsed = v, () => AudioListCollapsedItem),
    ];

    /// <summary>The tabs of the text tools and the icon each one shrinks to.</summary>
    private IEnumerable<(RadioButton Tab, string Icon)> ToolTabIcons =>
    [
        (TabQuick, "✍"),
        (TabOverlay, "📢"),
        (TabHighlight, "🖍"),
        (TabNotes, "📝"),
        (TabMusic, "🎵"),
        (TabTimes, "⏱"),
    ];

    private Fold FoldOf(string key) => Folds.First(f => f.Key == key);

    private void ToggleFold(string key)
    {
        var fold = FoldOf(key);
        ChangeLayout(() => fold.Set(!fold.Get()));
    }

    private void RestoreLayoutOptions()
    {
        _mirrored = _settings?.Get(MirroredKey) == "1";
        _playlistOnTop = _settings?.Get(PlaylistTopKey) == "1";
        _mediaOnTop = _settings?.Get(MediaTopKey) == "1";
        _liveAtBottom = _settings?.Get(LiveBottomKey) == "1";
        _outputSplit = _settings?.Get(OutputSplitKey) == "1";
        foreach (var fold in Folds)
            fold.Set(_settings?.Get(fold.Key) == "1");
        ApplyLayout();
    }

    private void SaveLayoutOptions()
    {
        if (_settings is null)
            return;

        _settings.Set(MirroredKey, _mirrored ? "1" : "0");
        _settings.Set(PlaylistTopKey, _playlistOnTop ? "1" : "0");
        _settings.Set(MediaTopKey, _mediaOnTop ? "1" : "0");
        _settings.Set(LiveBottomKey, _liveAtBottom ? "1" : "0");
        _settings.Set(OutputSplitKey, _outputSplit ? "1" : "0");
        foreach (var fold in Folds)
            _settings.Set(fold.Key, fold.Get() ? "1" : "0");
    }

    /// <summary>
    /// Reads the sizes the operator dragged the splitters to back into the
    /// blocks, before the blocks move to other slots.
    /// </summary>
    private void CaptureSizes()
    {
        var columns = MainArea.ColumnDefinitions;

        // A folded block is as big as its header (or its rail): that is not
        // the size the operator chose, so it must not overwrite it.
        if (!_leftRail)
            _libraryWidth = columns[_mirrored ? 4 : 0].ActualWidth is > 0 and var lw ? lw : _libraryWidth;
        _rightWidth = columns[_mirrored ? 0 : 4].ActualWidth is > 0 and var rw ? rw : _rightWidth;

        var mediaRow = MainArea.RowDefinitions[_mediaOnTop ? 0 : 2];
        if (!_mediaCollapsed && mediaRow.ActualHeight > 0)
            _mediaHeight = mediaRow.ActualHeight;

        // The playlist only has a size of its own while it is open and
        // sharing the column with an open library.
        if (!_leftRail && !_playlistCollapsed && !_libraryCollapsed)
        {
            var playlistRow = LeftPanel.RowDefinitions[_playlistOnTop ? 0 : 2];
            if (playlistRow.ActualHeight > 0)
                _playlistHeight = playlistRow.ActualHeight;
        }
    }

    private void ApplyLayout()
    {
        // ── Columns: library block | slides | live block ──
        var leftColumn = _mirrored ? 4 : 0;
        var rightColumn = _mirrored ? 0 : 4;
        var columns = MainArea.ColumnDefinitions;
        columns[leftColumn].Width = new GridLength(_leftRail ? RailWidth : _libraryWidth);
        columns[leftColumn].MinWidth = _leftRail ? RailWidth : LibraryMinWidth;
        columns[rightColumn].Width = new GridLength(_rightWidth);
        columns[rightColumn].MinWidth = RightMinWidth;

        Grid.SetColumn(LeftPanel, leftColumn);
        Grid.SetColumn(LeftRail, leftColumn);
        Grid.SetColumn(RightPanel, rightColumn);

        // ── Rows: slides and media; the live block runs the full height ──
        var contentRow = _mediaOnTop ? 2 : 0;
        var mediaRow = _mediaOnTop ? 0 : 2;
        var rows = MainArea.RowDefinitions;
        rows[contentRow].Height = new GridLength(1, GridUnitType.Star);
        rows[contentRow].MinHeight = 0;
        rows[mediaRow].Height = _mediaCollapsed ? GridLength.Auto : new GridLength(_mediaHeight);
        rows[mediaRow].MinHeight = _mediaCollapsed ? 0 : MediaMinHeight;

        Grid.SetRow(LeftPanel, contentRow);
        Grid.SetRow(LeftRail, contentRow);
        Grid.SetRow(SlidesPanel, contentRow);
        Grid.SetRow(MediaPanel, mediaRow);

        // Folded, the media bar is its header: tabs, cards, the blur bar and
        // the playback checkboxes go; the buttons stay.
        var mediaVisibility = _mediaCollapsed ? Visibility.Collapsed : Visibility.Visible;
        MediaBody.Visibility = mediaVisibility;
        MediaNewTabButton.Visibility = mediaVisibility;
        MediaContinuousCheck.Visibility = mediaVisibility;
        MediaRandomCheck.Visibility = mediaVisibility;
        MediaSplitter.Visibility = mediaVisibility;
        MediaTitle.Text = _mediaCollapsed ? "▸  MEDIOS" : "▾  MEDIOS";

        // ── The text tools: a strip of icons, or the tabs with their bodies ──
        var toolsRow = RightPanel.RowDefinitions[4];
        toolsRow.Height = _toolsCollapsed ? GridLength.Auto : new GridLength(1, GridUnitType.Star);
        toolsRow.MinHeight = _toolsCollapsed ? 0 : ToolsMinHeight;
        ToolsBody.Visibility = _toolsCollapsed ? Visibility.Collapsed : Visibility.Visible;
        ToolsCollapseButton.Content = _toolsCollapsed ? "▸" : "▾";
        foreach (var (tab, icon) in ToolTabIcons)
        {
            // The name stays reachable (screen readers and the tooltip).
            tab.Content = _toolsCollapsed ? icon : AutomationProperties.GetName(tab);
            if (_toolsCollapsed)
                tab.FontSize = 15;
            else
                tab.ClearValue(Control.FontSizeProperty);
        }

        // ── Music tab: the list of audios folds to its title ──
        AudioListBox.Visibility = _audioListCollapsed ? Visibility.Collapsed : Visibility.Visible;
        AudioButtons.Visibility = _audioListCollapsed ? Visibility.Collapsed : Visibility.Visible;
        AudioTitle.Text = _audioListCollapsed ? "▸  AUDIOS" : "▾  AUDIOS";

        // The media bar sits under (or over) the library and the slides.
        var mediaStart = _mirrored ? 2 : 0;
        Grid.SetColumn(MediaPanel, mediaStart);
        Grid.SetColumn(MediaSplitter, mediaStart);

        // The splitter beside the live block runs its full height; the other
        // one only spans the row the slides are in.
        var fullSplitter = _mirrored ? LeftSplitter : RightSplitter;
        var shortSplitter = _mirrored ? RightSplitter : LeftSplitter;
        Grid.SetRow(fullSplitter, 0);
        Grid.SetRowSpan(fullSplitter, 3);
        Grid.SetRow(shortSplitter, contentRow);
        Grid.SetRowSpan(shortSplitter, 1);

        // ── The rail of icons stands in for the library and the playlist ──
        LeftPanel.Visibility = _leftRail ? Visibility.Collapsed : Visibility.Visible;
        LeftRail.Visibility = _leftRail ? Visibility.Visible : Visibility.Collapsed;

        // The splitter beside the library has nothing to drag while it is a
        // fixed rail; which one that is depends on the side it sits on.
        var libraryNeighbor = _mirrored ? RightSplitter : LeftSplitter;
        var liveNeighbor = _mirrored ? LeftSplitter : RightSplitter;
        libraryNeighbor.IsEnabled = !_leftRail;
        liveNeighbor.IsEnabled = true;

        // ── Library and playlist, stacked ──
        var libraryIndex = _playlistOnTop ? 2 : 0;
        var playlistIndex = _playlistOnTop ? 0 : 2;
        Grid.SetRow(LibraryPanel, libraryIndex);
        Grid.SetRow(PlaylistPanel, playlistIndex);

        var libraryRow = LeftPanel.RowDefinitions[libraryIndex];
        var playlistRow = LeftPanel.RowDefinitions[playlistIndex];

        libraryRow.Height = _libraryCollapsed ? GridLength.Auto : new GridLength(1, GridUnitType.Star);
        libraryRow.MinHeight = 0;
        playlistRow.Height = _playlistCollapsed
            ? GridLength.Auto
            : _libraryCollapsed
                ? new GridLength(1, GridUnitType.Star)
                : new GridLength(_playlistHeight);
        playlistRow.MinHeight = _playlistCollapsed || _libraryCollapsed ? 0 : PlaylistMinHeight;

        LibraryBody.Visibility = _libraryCollapsed ? Visibility.Collapsed : Visibility.Visible;
        LibraryCollapseButton.Content = _libraryCollapsed ? "▸" : "▾";

        PlaylistPicker.Visibility = _playlistCollapsed ? Visibility.Collapsed : Visibility.Visible;
        PlaylistBody.Visibility = _playlistCollapsed ? Visibility.Collapsed : Visibility.Visible;
        PlaylistTitle.Text = _playlistCollapsed ? "▸  PLAYLIST" : "▾  PLAYLIST";

        // Nothing to drag between them while either one is folded.
        PlaylistSplitter.Visibility = _libraryCollapsed || _playlistCollapsed
            ? Visibility.Collapsed
            : Visibility.Visible;

        // ── Live and preview ──
        // Merged, both sit in the first row (only one is visible at a time, the
        // view model decides which) and the gap that would separate them goes.
        var rightRows = RightPanel.RowDefinitions;
        rightRows[3].Height = new GridLength(_outputSplit ? 10 : 0);
        Grid.SetRow(LivePanel, _outputSplit && _liveAtBottom ? 2 : 0);
        Grid.SetRow(PreviewPanel, !_outputSplit ? 0 : _liveAtBottom ? 0 : 2);
        if (DataContext is ViewModels.MainViewModel vm)
            vm.IsOutputMerged = !_outputSplit;
    }

    private void ChangeLayout(Action change)
    {
        CaptureSizes();
        change();
        ApplyLayout();
        SaveLayoutOptions();
    }

    // ── Achicar secciones ────────────────────────────────────────

    private void LibraryCollapse_Click(object sender, RoutedEventArgs e) => ToggleFold(LibraryCollapsedKey);

    private void LibraryTitle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) => ToggleFold(LibraryCollapsedKey);

    private void PlaylistTitle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) => ToggleFold(PlaylistCollapsedKey);

    private void MediaTitle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) => ToggleFold(MediaCollapsedKey);

    private void AudioTitle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) => ToggleFold(AudioListCollapsedKey);

    private void AudioListCollapsedItem_Click(object sender, RoutedEventArgs e) => ToggleFold(AudioListCollapsedKey);

    private void ToolsCollapse_Click(object sender, RoutedEventArgs e) => ToggleFold(ToolsCollapsedKey);

    /// <summary>
    /// A click on any tab of the folded tools opens them again, on that tab:
    /// the radio button has already taken the check by the time Click arrives.
    /// </summary>
    private void ToolsTab_Click(object sender, RoutedEventArgs e)
    {
        if (_toolsCollapsed && e.OriginalSource is RadioButton)
            ToggleFold(ToolsCollapsedKey);
    }

    // ── El riel de íconos de canciones, Biblia y playlist ────────

    private void LeftRail_Enter_Click(object sender, RoutedEventArgs e) => ToggleFold(LeftRailKey);

    private void LeftRail_Exit_Click(object sender, RoutedEventArgs e) => ToggleFold(LeftRailKey);

    private void LeftRail_Songs_Click(object sender, RoutedEventArgs e) =>
        LeaveRail(() =>
        {
            _libraryCollapsed = false;
            (DataContext as ViewModels.MainViewModel)?.ShowSongsTabCommand.Execute(null);
        });

    private void LeftRail_Bible_Click(object sender, RoutedEventArgs e) =>
        LeaveRail(() =>
        {
            _libraryCollapsed = false;
            (DataContext as ViewModels.MainViewModel)?.ShowBibleTabCommand.Execute(null);
        });

    private void LeftRail_Playlist_Click(object sender, RoutedEventArgs e) =>
        LeaveRail(() => _playlistCollapsed = false);

    /// <summary>
    /// Takes the panel off the rail and opens just the part whose icon was
    /// pressed; the other part stays as it was before the rail.
    /// </summary>
    private void LeaveRail(Action open) =>
        ChangeLayout(() =>
        {
            _leftRail = false;
            open();
        });

    // ── Menú Ver ─────────────────────────────────────────────────

    /// <summary>Ticks the Ver menu's entries to match the layout as it is now.</summary>
    private void ViewMenu_SubmenuOpened(object sender, RoutedEventArgs e)
    {
        LibraryRightItem.IsChecked = _mirrored;
        PlaylistTopItem.IsChecked = _playlistOnTop;
        MediaTopItem.IsChecked = _mediaOnTop;
        SplitOutputItem.IsChecked = _outputSplit;
        LiveBottomItem.IsChecked = _liveAtBottom;

        // Which of the two goes on top only means something with two boxes.
        LiveBottomItem.IsEnabled = _outputSplit;
        foreach (var fold in Folds)
            fold.Item().IsChecked = fold.Get();
    }

    private void LibraryRight_Click(object sender, RoutedEventArgs e) =>
        ChangeLayout(() => _mirrored = !_mirrored);

    private void PlaylistTop_Click(object sender, RoutedEventArgs e) =>
        ChangeLayout(() => _playlistOnTop = !_playlistOnTop);

    private void MediaTop_Click(object sender, RoutedEventArgs e) =>
        ChangeLayout(() => _mediaOnTop = !_mediaOnTop);

    private void SplitOutput_Click(object sender, RoutedEventArgs e) =>
        ChangeLayout(() => _outputSplit = !_outputSplit);

    private void LiveBottom_Click(object sender, RoutedEventArgs e) =>
        ChangeLayout(() => _liveAtBottom = !_liveAtBottom);

    private void LibraryCollapsedItem_Click(object sender, RoutedEventArgs e) => ToggleFold(LibraryCollapsedKey);

    private void PlaylistCollapsedItem_Click(object sender, RoutedEventArgs e) => ToggleFold(PlaylistCollapsedKey);

    private void MediaCollapsedItem_Click(object sender, RoutedEventArgs e) => ToggleFold(MediaCollapsedKey);

    private void ToolsCollapsedItem_Click(object sender, RoutedEventArgs e) => ToggleFold(ToolsCollapsedKey);

    private void LeftRailItem_Click(object sender, RoutedEventArgs e) => ToggleFold(LeftRailKey);

    // ── Archivo y Ayuda ──────────────────────────────────────────

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();

    private const string GuideUrl = "https://github.com/EcclesiaCast/EcclesiaCast/blob/main/docs/guia-de-uso.md";

    private void Guide_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(GuideUrl) { UseShellExecute = true });
        }
        catch
        {
            MessageBox.Show($"No se pudo abrir el navegador. La guía está en:\n\n{GuideUrl}",
                "EcclesiaCast", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void About_Click(object sender, RoutedEventArgs e) =>
        new Views.AboutWindow(Services.AppInfo.Version) { Owner = this }.ShowDialog();

    /// <summary>Everything back where it comes out of the box.</summary>
    private void ResetLayout_Click(object sender, RoutedEventArgs e)
    {
        _mirrored = _playlistOnTop = _mediaOnTop = _liveAtBottom = _outputSplit = false;
        foreach (var fold in Folds)
            fold.Set(false);
        _libraryWidth = 270;
        _rightWidth = 320;
        _playlistHeight = 240;
        _mediaHeight = 170;
        ApplyLayout();
        SaveLayoutOptions();

        // Vuelve también al diseño de aprendizaje: es el que trae el programa.
        SetUiMode(advanced: false);
    }
}
