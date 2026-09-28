using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EcclesiaCast.App;

/// <summary>
/// Where each part of the operator window sits, and which parts are folded
/// away. Every church sets up its desk differently, so the big blocks can
/// trade places (Ver menu) and the library and the playlist fold down to
/// their title; all of it is remembered for the next service. The blocks move
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
    private const string LibraryCollapsedKey = "layout.library.collapsed";
    private const string PlaylistCollapsedKey = "layout.playlist.collapsed";

    private const double LibraryMinWidth = 200;
    private const double RightMinWidth = 260;
    private const double PlaylistMinHeight = 120;
    private const double MediaMinHeight = 86;

    /// <summary>Library and playlist on the right, live and preview on the left.</summary>
    private bool _mirrored;

    /// <summary>Playlist above the song and Bible library instead of below it.</summary>
    private bool _playlistOnTop;

    /// <summary>Media bar above the slide grid instead of below it.</summary>
    private bool _mediaOnTop;

    /// <summary>Preview above Live instead of below it.</summary>
    private bool _liveAtBottom;

    private bool _libraryCollapsed;
    private bool _playlistCollapsed;

    // Sizes of the blocks themselves, whichever slot they sit in.
    private double _libraryWidth = 270;
    private double _rightWidth = 320;
    private double _playlistHeight = 240;
    private double _mediaHeight = 150;

    private void RestoreLayoutOptions()
    {
        _mirrored = _settings?.Get(MirroredKey) == "1";
        _playlistOnTop = _settings?.Get(PlaylistTopKey) == "1";
        _mediaOnTop = _settings?.Get(MediaTopKey) == "1";
        _liveAtBottom = _settings?.Get(LiveBottomKey) == "1";
        _libraryCollapsed = _settings?.Get(LibraryCollapsedKey) == "1";
        _playlistCollapsed = _settings?.Get(PlaylistCollapsedKey) == "1";
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
        _settings.Set(LibraryCollapsedKey, _libraryCollapsed ? "1" : "0");
        _settings.Set(PlaylistCollapsedKey, _playlistCollapsed ? "1" : "0");
    }

    /// <summary>
    /// Reads the sizes the operator dragged the splitters to back into the
    /// blocks, before the blocks move to other slots.
    /// </summary>
    private void CaptureSizes()
    {
        var columns = MainArea.ColumnDefinitions;
        _libraryWidth = columns[_mirrored ? 4 : 0].ActualWidth is > 0 and var lw ? lw : _libraryWidth;
        _rightWidth = columns[_mirrored ? 0 : 4].ActualWidth is > 0 and var rw ? rw : _rightWidth;

        var mediaRow = MainArea.RowDefinitions[_mediaOnTop ? 0 : 2];
        if (mediaRow.ActualHeight > 0)
            _mediaHeight = mediaRow.ActualHeight;

        // The playlist only has a size of its own while it is open and
        // sharing the column with an open library.
        if (!_playlistCollapsed && !_libraryCollapsed)
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
        columns[leftColumn].Width = new GridLength(_libraryWidth);
        columns[leftColumn].MinWidth = LibraryMinWidth;
        columns[rightColumn].Width = new GridLength(_rightWidth);
        columns[rightColumn].MinWidth = RightMinWidth;

        Grid.SetColumn(LeftPanel, leftColumn);
        Grid.SetColumn(RightPanel, rightColumn);

        // ── Rows: slides and media; the live block runs the full height ──
        var contentRow = _mediaOnTop ? 2 : 0;
        var mediaRow = _mediaOnTop ? 0 : 2;
        var rows = MainArea.RowDefinitions;
        rows[contentRow].Height = new GridLength(1, GridUnitType.Star);
        rows[contentRow].MinHeight = 0;
        rows[mediaRow].Height = new GridLength(_mediaHeight);
        rows[mediaRow].MinHeight = MediaMinHeight;

        Grid.SetRow(LeftPanel, contentRow);
        Grid.SetRow(SlidesPanel, contentRow);
        Grid.SetRow(MediaPanel, mediaRow);

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
        Grid.SetRow(LivePanel, _liveAtBottom ? 2 : 0);
        Grid.SetRow(PreviewPanel, _liveAtBottom ? 0 : 2);
    }

    private void ChangeLayout(Action change)
    {
        CaptureSizes();
        change();
        ApplyLayout();
        SaveLayoutOptions();
    }

    // ── Achicar la biblioteca y la playlist ──────────────────────

    private void LibraryCollapse_Click(object sender, RoutedEventArgs e) =>
        ChangeLayout(() => _libraryCollapsed = !_libraryCollapsed);

    private void LibraryTitle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) =>
        ChangeLayout(() => _libraryCollapsed = !_libraryCollapsed);

    private void PlaylistTitle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) =>
        ChangeLayout(() => _playlistCollapsed = !_playlistCollapsed);

    // ── Menú Ver ─────────────────────────────────────────────────

    /// <summary>Ticks the Ver menu's entries to match the layout as it is now.</summary>
    private void ViewMenu_SubmenuOpened(object sender, RoutedEventArgs e)
    {
        LibraryRightItem.IsChecked = _mirrored;
        PlaylistTopItem.IsChecked = _playlistOnTop;
        MediaTopItem.IsChecked = _mediaOnTop;
        LiveBottomItem.IsChecked = _liveAtBottom;
        LibraryCollapsedItem.IsChecked = _libraryCollapsed;
        PlaylistCollapsedItem.IsChecked = _playlistCollapsed;
    }

    private void LibraryRight_Click(object sender, RoutedEventArgs e) =>
        ChangeLayout(() => _mirrored = !_mirrored);

    private void PlaylistTop_Click(object sender, RoutedEventArgs e) =>
        ChangeLayout(() => _playlistOnTop = !_playlistOnTop);

    private void MediaTop_Click(object sender, RoutedEventArgs e) =>
        ChangeLayout(() => _mediaOnTop = !_mediaOnTop);

    private void LiveBottom_Click(object sender, RoutedEventArgs e) =>
        ChangeLayout(() => _liveAtBottom = !_liveAtBottom);

    private void LibraryCollapsedItem_Click(object sender, RoutedEventArgs e) =>
        ChangeLayout(() => _libraryCollapsed = !_libraryCollapsed);

    private void PlaylistCollapsedItem_Click(object sender, RoutedEventArgs e) =>
        ChangeLayout(() => _playlistCollapsed = !_playlistCollapsed);

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

    private void About_Click(object sender, RoutedEventArgs e)
    {
        var version = typeof(MainWindow).Assembly
            .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
            .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
            .FirstOrDefault()?.InformationalVersion.Split('+')[0] ?? "?";

        MessageBox.Show(
            $"EcclesiaCast {version}\n\n"
            + "Software libre de proyección para iglesias.\n"
            + "Licencia GPL-3.0 · github.com/EcclesiaCast",
            "Acerca de EcclesiaCast", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    /// <summary>Everything back where it comes out of the box.</summary>
    private void ResetLayout_Click(object sender, RoutedEventArgs e)
    {
        _mirrored = _playlistOnTop = _mediaOnTop = _liveAtBottom = false;
        _libraryCollapsed = _playlistCollapsed = false;
        _libraryWidth = 270;
        _rightWidth = 320;
        _playlistHeight = 240;
        _mediaHeight = 150;
        ApplyLayout();
        SaveLayoutOptions();
    }
}
