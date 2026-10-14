using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using EcclesiaCast.App.Controls;
using EcclesiaCast.App.Services;
using EcclesiaCast.App.ViewModels;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Audio;
using EcclesiaCast.Core.Bible;
using EcclesiaCast.Core.Media;
using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Core.Songs;

namespace EcclesiaCast.App;

public partial class MainWindow : Window
{
    private const string LayoutLibraryKey = "layout.library.width";
    private const string LayoutPreviewKey = "layout.preview.width";
    private const string LayoutPlaylistKey = "layout.playlist.height";
    private const string LayoutMediaKey = "layout.media.height";
    private const string LayoutWindowKey = "layout.window";

    private ISettingsStore? _settings;

    public MainWindow()
    {
        InitializeComponent();

        DataContextChanged += (_, _) =>
        {
            if (DataContext is not MainViewModel vm)
                return;

            vm.IsOutputMerged = !_outputSplit;
            vm.MediaSelectionChanged += (_, _) => RefreshMediaSelectionVisuals();

            // The web panel: the view model decides when it is open or live;
            // this window shows or hides it and keeps the keyboard honest.
            vm.WebHost = WebPanelView;
            WebPanelView.KeyboardFocusChanged += (_, _) => SyncShortcutsWithWebFocus();
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName is nameof(MainViewModel.IsWebPanelOpen) or nameof(MainViewModel.IsWebLive))
                    SyncWebPanel(vm);
            };

            // Keep the relevant slide card visible: the live one while
            // navigating with the arrows, the previewed one after a search.
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(MainViewModel.LiveSlideIndex))
                    ScrollSlideIntoView(vm.LiveSlideIndex);
                else if (args.PropertyName == nameof(MainViewModel.PreviewSlideIndex))
                    ScrollSlideIntoView(vm.PreviewSlideIndex);
                else if (args.PropertyName == nameof(MainViewModel.LiveMediaId))
                    RefreshMediaLiveVisuals();
            };

            // A fresh chapter or passage always starts at the top.
            vm.Slides.CollectionChanged += (_, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Reset)
                    SlidesScroll.ScrollToTop();
            };
            vm.BibleChapters.CollectionChanged += (_, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Reset)
                    ChaptersScroll.ScrollToTop();
            };
        };
    }

    // ── Panel web ────────────────────────────────────────────────

    /// <summary>
    /// Open: shown. Closed but projected live: still there, invisible, so the
    /// page goes on moving on the screen. Closed and not live: gone, so a
    /// browser nobody sees does not keep drawing.
    /// </summary>
    private void SyncWebPanel(MainViewModel vm)
    {
        WebPanelView.SetState(vm.IsWebPanelOpen, keepAlive: vm.IsWebLive);
        SyncShortcutsWithWebFocus();
    }

    private List<InputBinding>? _shortcuts;
    private bool _shortcutsOff;

    /// <summary>
    /// While the keyboard is typing into the web page (or its address box) the
    /// window's shortcuts are taken away: Esc and Ctrl+K/F are keys of the
    /// page, and an arrow must not move the slides behind it. F1–F4 stay:
    /// they are the emergency keys (black, clear, logo) and pages hardly use
    /// them. The rest come back when the focus leaves.
    /// </summary>
    private void SyncShortcutsWithWebFocus()
    {
        var typing = WebPanelView.Visibility == Visibility.Visible
                     && WebPanelView.IsHitTestVisible
                     && WebPanelView.HasKeyboardInside;
        if (typing == _shortcutsOff)
            return;

        _shortcuts ??= InputBindings.Cast<InputBinding>().ToList();
        _shortcutsOff = typing;
        InputBindings.Clear();
        foreach (var binding in _shortcuts)
        {
            if (!typing || IsEmergencyKey(binding))
                InputBindings.Add(binding);
        }
    }

    private static bool IsEmergencyKey(InputBinding binding) =>
        binding is KeyBinding { Modifiers: ModifierKeys.None, Key: Key.F1 or Key.F2 or Key.F3 or Key.F4 };

    /// <summary>True while the keyboard belongs to the web page: nothing of this window reacts to it.</summary>
    private bool WebHasKeyboard =>
        WebPanelView.Visibility == Visibility.Visible && WebPanelView.IsHitTestVisible && WebPanelView.HasKeyboardInside;

    /// <summary>Restores the panel sizes saved from the last session.</summary>
    public void AttachLayoutPersistence(ISettingsStore settings)
    {
        _settings = settings;

        // The sizes belong to the blocks, not to the slots: a library moved to
        // the right keeps the width it had on the left.
        if (ReadDouble(LayoutLibraryKey) is double library)
            _libraryWidth = library;
        if (ReadDouble(LayoutPreviewKey) is double preview)
            _rightWidth = preview;
        if (ReadDouble(LayoutPlaylistKey) is double playlist)
            _playlistHeight = playlist;
        if (ReadDouble(LayoutMediaKey) is double media)
            _mediaHeight = media;
        RestoreLayoutOptions();
        RestoreUiMode();

        if (settings.Get(LayoutWindowKey)?.Split(';') is [var w, var h, var state]
            && double.TryParse(w, out var width) && double.TryParse(h, out var height))
        {
            Width = Math.Max(MinWidth, width);
            Height = Math.Max(MinHeight, height);
            if (state == "max")
                WindowState = WindowState.Maximized;
        }

        Closing += (_, _) => SaveLayout();
    }

    private double? ReadDouble(string key) =>
        double.TryParse(_settings?.Get(key), out var value) && value > 0 ? value : null;

    private void SaveLayout()
    {
        if (_settings is null)
            return;

        try
        {
            CaptureSizes();
            _settings.Set(LayoutLibraryKey, _libraryWidth.ToString("0"));
            _settings.Set(LayoutPreviewKey, _rightWidth.ToString("0"));
            _settings.Set(LayoutPlaylistKey, _playlistHeight.ToString("0"));
            _settings.Set(LayoutMediaKey, _mediaHeight.ToString("0"));

            var size = WindowState == WindowState.Maximized
                ? $"{RestoreBounds.Width:0};{RestoreBounds.Height:0};max"
                : $"{Width:0};{Height:0};normal";
            _settings.Set(LayoutWindowKey, size);
        }
        catch
        {
            // El layout es cosmético: nunca debe impedir cerrar la app.
        }
    }

    /// <summary>
    /// Drops the logo list under the ▾ button: one entry per logo (the active
    /// one ticked), then the way into the manager. Built here rather than in
    /// XAML so the fixed entries and the list can live in the same menu.
    /// </summary>
    private void LogoPicker_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm || sender is not FrameworkElement anchor)
            return;

        var menu = new ContextMenu
        {
            PlacementTarget = anchor,
            Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom,
        };

        foreach (var logo in vm.Logos)
        {
            menu.Items.Add(new MenuItem
            {
                Header = logo.Name,
                IsCheckable = true,
                IsChecked = logo.Id == vm.SelectedLogo?.Id,
                Command = vm.SelectLogoCommand,
                CommandParameter = logo,
            });
        }

        if (vm.Logos.Count == 0)
            menu.Items.Add(new MenuItem { Header = "(todavía no hay logos)", IsEnabled = false });

        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem
        {
            Header = "Administrar logos…",
            Command = vm.OpenLogosCommand,
        });

        menu.IsOpen = true;
    }

    // ── Música ───────────────────────────────────────────────────

    /// <summary>Sends the media key named in the button's Tag, as the keyboard would.</summary>
    private void MusicKey_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string name } && Enum.TryParse<MediaKey>(name, out var key))
            MediaKeys.Send(key);
    }

    // ── Lista de audios de la pestaña Música ─────────────────────

    /// <summary>
    /// A double click plays the track under the pointer. It is read on the
    /// press, not with a MouseBinding: a list row keeps the click for itself
    /// and the binding never fires.
    /// </summary>
    private void AudioList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount != 2 || DataContext is not MainViewModel vm || e.OriginalSource is not DependencyObject source)
            return;

        if (ItemsControl.ContainerFromElement(AudioList, source) is ListBoxItem { DataContext: AudioTrackItem track })
        {
            vm.PlayAudioCommand.Execute(track);
            e.Handled = true;
        }
    }

    private void AudioList_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
            return;

        if (e.Key == Key.Enter && vm.SelectedAudio is { } track)
        {
            vm.PlayAudioCommand.Execute(track);
            e.Handled = true;
        }
        else if (e.Key == Key.Delete)
        {
            vm.RemoveAudioCommand.Execute(null);
            e.Handled = true;
        }
    }

    /// <summary>Audio files dragged in from Explorer; anything else is refused with the "no" cursor.</summary>
    private void AudioList_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = DroppedFiles(e)?.Any(AudioFiles.IsAudio) == true ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void AudioList_Drop(object sender, DragEventArgs e)
    {
        if (DroppedFiles(e) is { } files && DataContext is MainViewModel vm)
            vm.AddAudioFiles(files);

        e.Handled = true;
    }

    // ── Soltar archivos en la barra de medios ────────────────────

    /// <summary>
    /// Files dragged in from Explorer. Only real files count: the slides and
    /// songs the program drags around use their own formats and must pass
    /// through untouched.
    /// </summary>
    private static string[]? DroppedFiles(DragEventArgs e) =>
        e.Data.GetDataPresent(DataFormats.FileDrop) ? e.Data.GetData(DataFormats.FileDrop) as string[] : null;

    private void MediaPanel_DragOver(object sender, DragEventArgs e)
    {
        if (DroppedFiles(e) is null)
            return;

        e.Effects = DragDropEffects.Copy;
        e.Handled = true;

        // A coloured border says "this is where they go" before letting go.
        MediaPanel.BorderBrush = (Brush)FindResource("AccentBrush");
        MediaPanel.BorderThickness = new Thickness(2);
    }

    private void MediaPanel_DragLeave(object sender, DragEventArgs e) => ClearMediaDropHighlight();

    private void MediaPanel_Drop(object sender, DragEventArgs e)
    {
        ClearMediaDropHighlight();
        if (DroppedFiles(e) is not { } files || DataContext is not MainViewModel vm)
            return;

        vm.ImportMediaFiles(files);
        e.Handled = true;
    }

    private void ClearMediaDropHighlight()
    {
        MediaPanel.ClearValue(Border.BorderBrushProperty);
        MediaPanel.ClearValue(Border.BorderThicknessProperty);
    }

    /// <summary>
    /// Every way of getting a song into the library behind the one ➕: writing
    /// it, finding it online, bringing a whole ProPresenter library or loose
    /// files. They used to be two buttons, and the operator had to guess which
    /// of them held the one they wanted.
    /// </summary>
    private void AddSongMenu_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm || sender is not FrameworkElement anchor)
            return;

        var menu = new ContextMenu
        {
            PlacementTarget = anchor,
            Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom,
        };

        menu.Items.Add(new MenuItem
        {
            Header = "Crear desde cero",
            Command = vm.NewSongCommand,
            ToolTip = "Una canción nueva en blanco, para escribir la letra",
        });
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem
        {
            Header = "Buscar en internet…",
            Command = vm.SearchSongsOnlineCommand,
            ToolTip = "Por nombre, artista o una frase de la letra, en musica.com y lrclib.net",
        });
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem
        {
            Header = "Traer de ProPresenter…",
            Command = vm.ImportFromProPresenterCommand,
            ToolTip = "Busca solo dónde está instalado ProPresenter y trae sus bibliotecas enteras",
        });
        menu.Items.Add(new MenuItem
        {
            Header = "Importar archivo (.txt, .pro)…",
            Command = vm.ImportSongsCommand,
            ToolTip = "Elegir archivos sueltos de texto o de ProPresenter",
        });

        menu.IsOpen = true;
    }

    /// <summary>A dimmed, bold row that names the group of entries under it.</summary>
    private static MenuItem SectionTitle(string text) =>
        new() { Header = text, IsEnabled = false, FontWeight = FontWeights.SemiBold, FontSize = 11 };

    /// <summary>Stage layouts, their editor and the screen picker, under the ▾ of the stage button.</summary>
    private void StageOptions_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm || sender is not FrameworkElement anchor)
            return;

        var menu = new ContextMenu
        {
            PlacementTarget = anchor,
            Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom,
        };

        // Quick switch between the saved arrangements; the one in use is ticked.
        menu.Items.Add(SectionTitle("Escenario"));
        foreach (var layout in vm.StageLayouts)
        {
            var item = new MenuItem
            {
                Header = layout.Name,
                IsCheckable = true,
                IsChecked = ReferenceEquals(layout, vm.ActiveStageLayout),
            };
            item.Click += (_, _) => vm.SelectStageLayoutCommand.Execute(layout);
            menu.Items.Add(item);
        }

        menu.Items.Add(new MenuItem { Header = "✏️  Editar los escenarios…", Command = vm.EditStageCommand });

        menu.Items.Add(new Separator());
        menu.Items.Add(SectionTitle("Pantalla del escenario"));
        foreach (var display in vm.StageDisplays)
        {
            var item = new MenuItem
            {
                Header = display.Label,
                IsCheckable = true,
                IsChecked = display.Info.DeviceName == vm.SelectedStageDisplay?.Info.DeviceName,
            };
            item.Click += (_, _) => vm.SelectedStageDisplay = display;
            menu.Items.Add(item);
        }

        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem
        {
            Header = "⟲  Poner el cronómetro en cero",
            Command = vm.ResetStageTimerCommand,
        });

        menu.IsOpen = true;
    }

    /// <summary>
    /// One click on a playlist item loads it without projecting it, so the
    /// operator can look ahead (a media item is only shown in the centre);
    /// the second press of a double click projects it. Both are taken here,
    /// before the row handles the press: left to a binding on the list, the
    /// double click never arrived, because the row marks the press as handled.
    /// </summary>
    private void PlaylistItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not MainViewModel vm
            || sender is not FrameworkElement { DataContext: Core.Playlists.PlaylistItem item })
            return;

        if (e.ClickCount == 1)
        {
            vm.OpenPlaylistItemCommand.Execute(item);
        }
        else if (e.ClickCount == 2)
        {
            vm.ProjectPlaylistItemCommand.Execute(item);
            e.Handled = true;
        }
    }

    /// <summary>Double click on a found verse projects it (see the playlist for why it is taken here).</summary>
    private void BibleResultsList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount != 2
            || DataContext is not MainViewModel vm
            || e.OriginalSource is not DependencyObject source
            || ItemsControl.ContainerFromElement(BibleResultsList, source) is not ListBoxItem { DataContext: BibleVerseResult result })
            return;

        vm.ProjectBibleResultCommand.Execute(result);
        e.Handled = true;
    }

    // ── Arrastrar canciones a la playlist ────────────────────────

    private Point? _songDragStart;
    private Song? _songDragItem;

    private void SongsList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _songDragItem = e.OriginalSource is DependencyObject source
            && ItemsControl.ContainerFromElement(SongsList, source) is ListBoxItem row
                ? row.DataContext as Song
                : null;
        _songDragStart = _songDragItem is null ? null : e.GetPosition(this);

        // The double click is taken here for the same reason as in the
        // playlist: the row handles the press before a binding on the list
        // gets to see it.
        if (e.ClickCount == 2 && _songDragItem is not null && DataContext is MainViewModel vm)
        {
            _songDragStart = null;
            vm.ProjectFirstSlideCommand.Execute(null);
            e.Handled = true;
        }
    }

    /// <summary>
    /// A press that travels a few pixels becomes a drag, so a plain click
    /// still selects the song and a double click still projects it.
    /// </summary>
    private void SongsList_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed
            || _songDragStart is not Point start
            || _songDragItem is not { } song)
            return;

        var moved = e.GetPosition(this) - start;
        if (Math.Abs(moved.X) < SystemParameters.MinimumHorizontalDragDistance * 2
            && Math.Abs(moved.Y) < SystemParameters.MinimumVerticalDragDistance * 2)
            return;

        _songDragStart = null;
        DragDrop.DoDragDrop(SongsList, new DataObject(typeof(Song), song), DragDropEffects.Copy);
        _songDragItem = null;
        PlaylistDropLine.Visibility = Visibility.Collapsed;
    }

    // ── Selección y arrastre de medios ───────────────────────────

    private Point? _mediaDragStart;
    private MediaItem? _mediaDragItem;

    private static MediaItem? CardMedia(object sender) => (sender as FrameworkElement)?.DataContext as MediaItem;

    /// <summary>
    /// A card that appears (the bar is rebuilt after almost every change)
    /// takes its picked frame from the selection, and the fill picture of the
    /// few media that have one.
    /// </summary>
    private void MediaCard_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not Button card || card.DataContext is not MediaItem media || DataContext is not MainViewModel vm)
            return;

        MediaCardState.SetIsSelected(card, vm.MediaSelection.Contains(media.Id));
        MediaCardState.SetIsLive(card, vm.LiveMediaId == media.Id);
        if (media.FillMediaId is int fillId
            && card.Template.FindName("preview", card) is FramedMediaPreview preview)
            preview.FillMedia = vm.FindMedia(fillId);
    }

    /// <summary>
    /// A plain click projects, as always, and leaves only this card picked.
    /// With Ctrl or Shift it only picks (adds or takes out; the stretch from
    /// the last one), the way the Explorer does.
    /// </summary>
    private void MediaCard_Click(object sender, RoutedEventArgs e)
    {
        if (CardMedia(sender) is not { } media || DataContext is not MainViewModel vm)
            return;

        var modifiers = Keyboard.Modifiers;
        if (vm.ClickMedia(media,
                ctrl: modifiers.HasFlag(ModifierKeys.Control),
                shift: modifiers.HasFlag(ModifierKeys.Shift)))
            vm.ApplyBackgroundCommand.Execute(media);
    }

    /// <summary>Right click on a card that is not picked picks only it, so the menu acts on what was clicked.</summary>
    private void MediaCard_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (CardMedia(sender) is { } media && DataContext is MainViewModel vm)
            vm.SelectMediaForMenu(media);
    }

    /// <summary>Paints the picked frame on the cards that exist right now.</summary>
    private void RefreshMediaSelectionVisuals()
    {
        if (DataContext is not MainViewModel vm)
            return;

        for (var i = 0; i < MediaCards.Items.Count; i++)
        {
            if (MediaCards.ItemContainerGenerator.ContainerFromIndex(i) is ContentPresenter holder
                && VisualTreeHelper.GetChildrenCount(holder) > 0
                && VisualTreeHelper.GetChild(holder, 0) is Button card
                && card.DataContext is MediaItem media)
                MediaCardState.SetIsSelected(card, vm.MediaSelection.Contains(media.Id));
        }
    }

    /// <summary>Marks the card of the media that is on the screen now (red border) and unmarks the rest.</summary>
    private void RefreshMediaLiveVisuals()
    {
        if (DataContext is not MainViewModel vm)
            return;

        for (var i = 0; i < MediaCards.Items.Count; i++)
        {
            if (MediaCards.ItemContainerGenerator.ContainerFromIndex(i) is ContentPresenter holder
                && VisualTreeHelper.GetChildrenCount(holder) > 0
                && VisualTreeHelper.GetChild(holder, 0) is Button card
                && card.DataContext is MediaItem media)
                MediaCardState.SetIsLive(card, vm.LiveMediaId == media.Id);
        }
    }

    private void MediaCard_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _mediaDragItem = CardMedia(sender);
        _mediaDragStart = e.GetPosition(this);
    }

    /// <summary>
    /// A press that travels a few pixels becomes a drag, like the song list's:
    /// a plain click still applies the media. Dragging one of the picked cards
    /// takes all the picked ones; any other takes just itself. It can be
    /// dropped on the playlist (into the exact place) or on another tab.
    /// </summary>
    private void MediaCard_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed
            || _mediaDragStart is not Point start
            || _mediaDragItem is not { } media
            || sender is not FrameworkElement card
            || DataContext is not MainViewModel vm)
            return;

        var moved = e.GetPosition(this) - start;
        if (Math.Abs(moved.X) < SystemParameters.MinimumHorizontalDragDistance * 2
            && Math.Abs(moved.Y) < SystemParameters.MinimumVerticalDragDistance * 2)
            return;

        _mediaDragStart = null;
        var data = new DataObject(typeof(MediaItem), media);
        data.SetData(typeof(MediaItem[]), vm.MediaTargets(media).ToArray());
        try
        {
            DragDrop.DoDragDrop(card, data, DragDropEffects.Copy | DragDropEffects.Move);
        }
        finally
        {
            _mediaDragItem = null;
            PlaylistDropLine.Visibility = Visibility.Collapsed;
            ClearTabDropHighlights();
        }
    }

    /// <summary>Ctrl+A, Supr and Esc on the bar, once the focus is on one of its cards (or its empty space).</summary>
    private void MediaPanel_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not MainViewModel vm || e.OriginalSource is TextBox)
            return;

        if (e.Key == Key.A && Keyboard.Modifiers == ModifierKeys.Control)
        {
            vm.SelectAllMedia();
            e.Handled = true;
        }
        else if (e.Key == Key.Delete && vm.MediaSelection.Count > 0)
        {
            vm.DeleteSelectedMedia();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && vm.MediaSelection.Count > 1)
        {
            // Only with several picked: a lone card stays picked after a click
            // that projected it, and Esc there must still switch the output off.
            vm.ClearMediaSelection();
            e.Handled = true;
        }
    }

    // ── El menú de clic derecho de las tarjetas ──────────────────

    /// <summary>
    /// Fits the one shared menu to the card that opened it: the words say how
    /// many it acts on, and «Volver a importar» only shows on a page the
    /// program drew itself.
    /// </summary>
    /// <summary>
    /// «Reiniciar el sonido»: the escape hatch for when Windows' audio device
    /// changed and nothing is heard even though the automatic reopening ran
    /// (or did not notice).
    /// </summary>
    private void Transitions_Click(object sender, RoutedEventArgs e)
    {
        if (_settings is null)
            return;

        new Views.TransitionsWindow(_settings) { Owner = this }.ShowDialog();
    }

    private void RestartSound_Click(object sender, RoutedEventArgs e)
    {
        AudioEndpointWatcher.RequestRestart();
        if (DataContext is MainViewModel vm)
            vm.StatusText = "Sonido reiniciado: la música y los videos vuelven a abrir la salida de audio.";
    }

    private void MediaMenu_Opened(object sender, RoutedEventArgs e)
    {
        if (sender is not ContextMenu menu
            || (menu.PlacementTarget as FrameworkElement)?.DataContext is not MediaItem media
            || DataContext is not MainViewModel vm)
            return;

        var count = vm.MediaTargets(media).Count;
        var suffix = count > 1 ? $" ({count})" : string.Empty;

        // A saved web page (or window) has no file and no framing: only what
        // makes sense for it stays (rename, move, playlist, remove).
        var isWeb = media.IsWeb || media.IsWindow;
        foreach (var separator in menu.Items.OfType<Separator>().Where(s => s.Name == "InspectSeparator"))
            separator.Visibility = isWeb ? Visibility.Collapsed : Visibility.Visible;

        foreach (var item in menu.Items.OfType<MenuItem>())
        {
            switch (item.Name)
            {
                case "ReimportItem":
                    item.Visibility = !isWeb && DocumentImporter.IsRenderedPage(media.Path) ? Visibility.Visible : Visibility.Collapsed;
                    break;
                case "RenameMediaItem":
                    item.Visibility = isWeb && count == 1 ? Visibility.Visible : Visibility.Collapsed;
                    break;
                case "InspectItem":
                case "OpenLocationItem":
                    item.Visibility = isWeb ? Visibility.Collapsed : Visibility.Visible;
                    break;
                case "UseAsLogoItem":
                    // A page drawn from a PDF or presentation is deleted with
                    // its media, and the logo would be left pointing at nothing.
                    item.Visibility = count == 1
                        && media.Type is MediaType.Image or MediaType.Video
                        && !DocumentImporter.IsRenderedPage(media.Path)
                        ? Visibility.Visible : Visibility.Collapsed;
                    break;
                case "RemoveMediaItem":
                    item.Header = "Quitar de la biblioteca" + suffix;
                    break;
                case "MoveToTabItem":
                    item.Header = "Mover a la pestaña…" + suffix;
                    break;
            }
        }

        if (menu.Items.OfType<MenuItem>().FirstOrDefault(i => i.Command == vm.AddMediaToPlaylistCommand) is { } add)
            add.Header = "Agregar a la playlist" + suffix;
    }

    /// <summary>The tabs to move to, without the one the media is already in.</summary>
    private void MoveToTab_SubmenuOpened(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem submenu
            || submenu.Parent is not ContextMenu { PlacementTarget: FrameworkElement { DataContext: MediaItem media } }
            || DataContext is not MainViewModel vm)
            return;

        submenu.Items.Clear();
        foreach (var tab in vm.MediaTabs.Where(t => !string.Equals(t, media.Category, StringComparison.OrdinalIgnoreCase)))
        {
            var target = tab;
            var entry = new MenuItem { Header = tab.Replace("_", "__") };
            entry.Click += (_, _) => vm.MoveMediaToTab(vm.MediaTargets(media), target);
            submenu.Items.Add(entry);
        }

        if (submenu.Items.Count == 0)
            submenu.Items.Add(new MenuItem { Header = "(no hay otras pestañas)", IsEnabled = false });
    }

    // ── Rectángulo de selección en el espacio vacío ──────────────

    private Point? _bandStart;
    private bool _bandActive;
    private bool _bandAdditive;
    private List<MediaItem> _bandBase = [];
    private List<(MediaItem Item, Rect Bounds)> _bandCards = [];
    private HashSet<int> _bandLastHits = [];

    private bool IsInsideCardOrScrollBar(DependencyObject? source)
    {
        while (source is not null && !ReferenceEquals(source, MediaScroll))
        {
            if (source is System.Windows.Controls.Primitives.ButtonBase or System.Windows.Controls.Primitives.ScrollBar)
                return true;
            source = source is Visual or System.Windows.Media.Media3D.Visual3D
                ? VisualTreeHelper.GetParent(source)
                : LogicalTreeHelper.GetParent(source);
        }

        return false;
    }

    /// <summary>
    /// A press on the empty space starts a rectangle. Released without moving
    /// it clears the selection, like clicking the blank part of an Explorer
    /// window; Ctrl keeps what was picked and adds to it.
    /// </summary>
    private void MediaScroll_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject source || IsInsideCardOrScrollBar(source)
            || DataContext is not MainViewModel vm)
            return;

        MediaScroll.Focus();
        _bandStart = e.GetPosition(MediaCards);
        _bandActive = false;
        _bandAdditive = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
        _bandBase = _bandAdditive ? vm.SelectedMedia.ToList() : [];
        MediaScroll.CaptureMouse();
        e.Handled = true;
    }

    private void MediaScroll_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_bandStart is not Point start || !MediaScroll.IsMouseCaptured || DataContext is not MainViewModel vm)
            return;

        var now = e.GetPosition(MediaCards);
        if (!_bandActive)
        {
            if (Math.Abs(now.X - start.X) < SystemParameters.MinimumHorizontalDragDistance
                && Math.Abs(now.Y - start.Y) < SystemParameters.MinimumVerticalDragDistance)
                return;

            // Where every card is, taken once: the rectangle is compared with
            // these on each move instead of asking the layout again.
            _bandActive = true;
            _bandCards = [];
            for (var i = 0; i < MediaCards.Items.Count; i++)
            {
                if (MediaCards.Items[i] is MediaItem item
                    && MediaCards.ItemContainerGenerator.ContainerFromIndex(i) is FrameworkElement holder)
                {
                    var origin = holder.TranslatePoint(new Point(0, 0), MediaCards);
                    _bandCards.Add((item, new Rect(origin, holder.RenderSize)));
                }
            }

            _bandLastHits = [];
        }

        var rect = new Rect(start, now);
        var hits = _bandCards.Where(c => c.Bounds.IntersectsWith(rect)).Select(c => c.Item).ToList();
        var ids = hits.Select(m => m.Id).ToHashSet();
        if (!ids.SetEquals(_bandLastHits))
        {
            _bandLastHits = ids;
            vm.SelectMediaIn(hits.Concat(_bandBase), additive: false);
        }

        // Drawn in the scroll's own coordinates, so it follows the content
        // when the bar is scrolled and is cut at the edges of what is visible.
        var topLeft = MediaCards.TranslatePoint(rect.TopLeft, MediaScroll);
        var bottomRight = MediaCards.TranslatePoint(rect.BottomRight, MediaScroll);
        var left = Math.Max(0, topLeft.X);
        var top = Math.Max(0, topLeft.Y);
        var right = Math.Min(MediaScroll.ActualWidth, bottomRight.X);
        var bottom = Math.Min(MediaScroll.ActualHeight, bottomRight.Y);
        if (right > left && bottom > top)
        {
            MediaBand.Margin = new Thickness(left, top, 0, 0);
            MediaBand.Width = right - left;
            MediaBand.Height = bottom - top;
            MediaBand.Visibility = Visibility.Visible;
        }
        else
        {
            MediaBand.Visibility = Visibility.Collapsed;
        }
    }

    private void MediaScroll_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_bandStart is null)
            return;

        var wasClick = !_bandActive;
        _bandStart = null;
        MediaScroll.ReleaseMouseCapture();

        if (wasClick && !_bandAdditive && DataContext is MainViewModel vm)
            vm.ClearMediaSelection();
    }

    private void MediaScroll_LostMouseCapture(object sender, MouseEventArgs e)
    {
        _bandStart = null;
        _bandActive = false;
        MediaBand.Visibility = Visibility.Collapsed;
    }

    private static MediaItem? DraggedMedia(DragEventArgs e) =>
        e.Data.GetDataPresent(typeof(MediaItem)) ? e.Data.GetData(typeof(MediaItem)) as MediaItem : null;

    /// <summary>Everything the drag carries: the picked ones when several were dragged, in the bar's order.</summary>
    private static IReadOnlyList<MediaItem> DraggedMediaList(DragEventArgs e) =>
        e.Data.GetDataPresent(typeof(MediaItem[])) && e.Data.GetData(typeof(MediaItem[])) is MediaItem[] { Length: > 0 } many
            ? many
            : DraggedMedia(e) is { } one ? [one] : [];

    /// <summary>The tab a dragged media is over is lit up; the one it came from can't take it.</summary>
    private void MediaTab_DragOver(object sender, DragEventArgs e)
    {
        e.Handled = true;
        if (sender is not ListBoxItem { DataContext: string tab } row
            || DraggedMediaList(e).All(m => string.Equals(m.Category, tab, StringComparison.OrdinalIgnoreCase)))
        {
            e.Effects = DragDropEffects.None;
            if (sender is ListBoxItem other)
                other.ClearValue(TagProperty);
            return;
        }

        e.Effects = DragDropEffects.Move;
        row.Tag = "drop";
    }

    private void MediaTab_DragLeave(object sender, DragEventArgs e)
    {
        if (sender is ListBoxItem row)
            row.ClearValue(TagProperty);
    }

    private void MediaTab_Drop(object sender, DragEventArgs e)
    {
        e.Handled = true;
        if (sender is not ListBoxItem { DataContext: string tab } row)
            return;

        row.ClearValue(TagProperty);
        if (DataContext is MainViewModel vm && DraggedMediaList(e) is { Count: > 0 } media)
            vm.MoveMediaToTab(media, tab);
    }

    // ── Clic derecho sobre una pestaña de medios ─────────────────

    /// <summary>
    /// Builds the menu of a tab when its row appears. It is made
    /// here and not in the XAML because a Click inside a style setter breaks
    /// the loading of the window.
    /// </summary>
    private void MediaTab_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not ListBoxItem tabItem || tabItem.ContextMenu is not null)
            return;

        MenuItem Entry(string header, Action<MainViewModel, string> run)
        {
            var entry = new MenuItem { Header = header };
            entry.Click += (_, _) =>
            {
                if (DataContext is MainViewModel vm && tabItem.DataContext is string tab)
                    run(vm, tab);
            };
            return entry;
        }

        var muteEntry = Entry("🔇 Silenciar la pestaña", (vm, tab) => vm.ToggleMediaTabMuteCommand.Execute(tab));
        var openPresentation = Entry("📑 Abrir como presentación", (vm, tab) => vm.OpenPresentationTabCommand.Execute(tab));
        var presentationToPlaylist = Entry("Agregar a la playlist como presentación",
            (vm, tab) => vm.AddPresentationToPlaylistCommand.Execute(tab));

        var menu = new ContextMenu
        {
            Items =
            {
                openPresentation,
                presentationToPlaylist,
                new Separator(),
                Entry("Cambiar nombre…", (vm, tab) => vm.RenameMediaTabCommand.Execute(tab)),
                Entry("Configurar…", (vm, tab) => vm.ConfigureMediaTabCommand.Execute(tab)),
                muteEntry,
                new Separator(),
                Entry("Eliminar…", (vm, tab) => vm.DeleteMediaTabCommand.Execute(tab)),
            },
        };

        // The wording follows the state of the tab at the moment of opening:
        // it can change from «Configurar…» or from these very entries.
        // Marked presentation tabs just open; the others are marked by the
        // same click.
        menu.Opened += (_, _) =>
        {
            var vm = DataContext as MainViewModel;
            var tab = tabItem.DataContext as string;
            muteEntry.Header = vm is not null && vm.IsMediaTabMuted(tab)
                ? "🔊 Quitar el silencio"
                : "🔇 Silenciar la pestaña";
            var marked = vm is not null && tab is not null && vm.IsPresentationTab(tab);
            openPresentation.Header = marked ? "📑 Abrir como presentación" : "📑 Usar como presentación y abrirla";
            presentationToPlaylist.Visibility = marked ? Visibility.Visible : Visibility.Collapsed;
        };
        tabItem.ContextMenu = menu;
    }

    private void ClearTabDropHighlights()
    {
        for (var i = 0; i < MediaTabsList.Items.Count; i++)
            if (MediaTabsList.ItemContainerGenerator.ContainerFromIndex(i) is ListBoxItem row)
                row.ClearValue(TagProperty);
    }

    private void PlaylistBody_DragOver(object sender, DragEventArgs e)
    {
        e.Handled = true;
        if (!e.Data.GetDataPresent(typeof(Song)) && DraggedMedia(e) is null)
        {
            e.Effects = DragDropEffects.None;
            return;
        }

        e.Effects = DragDropEffects.Copy;
        var (_, lineY) = PlaylistDropPoint(e);
        PlaylistDropLine.Margin = new Thickness(10, Math.Max(0, lineY - 1.5), 10, 0);
        PlaylistDropLine.Visibility = Visibility.Visible;
    }

    private void PlaylistBody_DragLeave(object sender, DragEventArgs e)
    {
        // Leaving for a child (a row) also raises DragLeave: only hide the
        // line when the pointer really left the playlist.
        var position = e.GetPosition(PlaylistBody);
        if (position.X < 0 || position.Y < 0
            || position.X > PlaylistBody.ActualWidth || position.Y > PlaylistBody.ActualHeight)
            PlaylistDropLine.Visibility = Visibility.Collapsed;
    }

    private void PlaylistBody_Drop(object sender, DragEventArgs e)
    {
        PlaylistDropLine.Visibility = Visibility.Collapsed;
        e.Handled = true;
        if (DataContext is not MainViewModel vm)
            return;

        var (index, _) = PlaylistDropPoint(e);
        if (e.Data.GetData(typeof(Song)) is Song song)
            vm.InsertSongInPlaylist(song, index);
        else if (DraggedMediaList(e) is { Count: > 0 } media)
            vm.InsertMediaInPlaylist(media, index);
    }

    /// <summary>
    /// Where a drop lands: before the row whose upper half is under the
    /// pointer, or after the last one. Returns that position in the list and
    /// the height (within the playlist) where the line marking it goes.
    /// </summary>
    private (int Index, double LineY) PlaylistDropPoint(DragEventArgs e)
    {
        var pointer = e.GetPosition(PlaylistBody).Y;
        var count = PlaylistList.Items.Count;
        var lineY = 0d;

        for (var i = 0; i < count; i++)
        {
            if (PlaylistList.ItemContainerGenerator.ContainerFromIndex(i) is not ListBoxItem row
                || !row.IsVisible)
                continue;

            var top = row.TranslatePoint(new Point(0, 0), PlaylistBody).Y;
            if (pointer < top + row.ActualHeight / 2)
                return (i, top);
            lineY = top + row.ActualHeight;
        }

        return (count, lineY);
    }

    /// <summary>
    /// Supr takes the selected item off the playlist and selects the one that
    /// took its place, so a few presses clear several in a row.
    /// </summary>
    private void PlaylistList_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Delete
            || DataContext is not MainViewModel vm
            || PlaylistList.SelectedItem is not Core.Playlists.PlaylistItem item)
            return;

        var index = PlaylistList.SelectedIndex;
        vm.RemovePlaylistItemCommand.Execute(item);
        e.Handled = true;

        if (PlaylistList.Items.Count == 0)
            return;

        PlaylistList.SelectedIndex = Math.Min(index, PlaylistList.Items.Count - 1);

        // Saving rebuilds the list, so the keyboard focus has to be put back
        // on the new row once it exists.
        Dispatcher.InvokeAsync(() =>
        {
            if (PlaylistList.ItemContainerGenerator.ContainerFromIndex(PlaylistList.SelectedIndex) is ListBoxItem row)
                row.Focus();
        }, DispatcherPriority.Background);
    }

    /// <summary>
    /// Enter on a half-typed book writes its full name into the box; putting
    /// the caret after it lets the operator type the chapter straight on.
    /// </summary>
    private void BibleQueryBox_KeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            BibleQueryBox.CaretIndex = BibleQueryBox.Text.Length;
    }

    // ── Historial de versículos (🕘 junto a la caja de la Biblia) ──

    private DateTime _historyClosedAt = DateTime.MinValue;

    private void BibleHistoryButton_Click(object sender, RoutedEventArgs e)
    {
        // A popup that does not stay open closes on the mouse press that is
        // about to click this button; without the check the click opened it
        // again at once and the button could never close it.
        if (DateTime.UtcNow - _historyClosedAt < TimeSpan.FromMilliseconds(250))
            return;

        BibleHistoryPopup.IsOpen = true;
    }

    /// <summary>↓ in the empty box opens the history, as a list of what was projected comes to hand there.</summary>
    private void BibleQueryBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Down
            || BibleQueryBox.Text.Length > 0
            || Keyboard.Modifiers != ModifierKeys.None
            || DataContext is not MainViewModel { BibleHistory.Count: > 0 })
            return;

        BibleHistoryPopup.IsOpen = true;
        e.Handled = true;
    }

    /// <summary>Opens with the newest entry ready: arrows move, Enter projects.</summary>
    private void BibleHistoryPopup_Opened(object? sender, EventArgs e) =>
        Dispatcher.BeginInvoke(() =>
        {
            if (HistoryListBox.Items.Count == 0)
                return;

            if (HistoryListBox.SelectedIndex < 0)
                HistoryListBox.SelectedIndex = 0;

            HistoryListBox.UpdateLayout();
            if (HistoryListBox.ItemContainerGenerator.ContainerFromIndex(HistoryListBox.SelectedIndex) is ListBoxItem row)
                row.Focus();
        }, DispatcherPriority.Input);

    private void BibleHistoryPopup_Closed(object? sender, EventArgs e) => _historyClosedAt = DateTime.UtcNow;

    private void BibleHistoryPopup_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            BibleHistoryPopup.IsOpen = false;
            BibleQueryBox.Focus();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter
            && HistoryListBox.SelectedItem is BibleHistoryEntry entry
            && DataContext is MainViewModel vm)
        {
            BibleHistoryPopup.IsOpen = false;
            vm.ProjectHistoryEntryCommand.Execute(entry);
            e.Handled = true;
        }
    }

    /// <summary>
    /// One click loads the chapter and marks the verse; a double click
    /// projects it. The double click is taken here (not a MouseBinding) for
    /// the same reason as in the other lists.
    /// </summary>
    private void HistoryListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not MainViewModel vm
            || e.OriginalSource is not DependencyObject source
            || ItemsControl.ContainerFromElement(HistoryListBox, source) is not ListBoxItem { DataContext: BibleHistoryEntry entry })
            return;

        if (e.ClickCount == 2)
        {
            BibleHistoryPopup.IsOpen = false;
            vm.ProjectHistoryEntryCommand.Execute(entry);
            e.Handled = true;
        }
        else
        {
            vm.OpenHistoryEntryCommand.Execute(entry);
        }
    }

    /// <summary>A click puts the caret somewhere: the arrows are for the text again.</summary>
    private void BibleQueryBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.ArrowsDriveSlidesFromSearch = false;
    }

    // ── La cajita del ➕ Playlist de la Biblia ───────────────────

    /// <summary>The box opens ready to type: the proposal is selected, so typing replaces it.</summary>
    private void AddPassagePopup_Opened(object? sender, EventArgs e) =>
        Dispatcher.BeginInvoke(() =>
        {
            Keyboard.Focus(AddPassageBox);
            AddPassageBox.SelectAll();
        }, DispatcherPriority.Input);

    private void AddPassagePopup_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
            return;

        AddPassagePopup.IsOpen = false;
        e.Handled = true;
    }

    private void AddPassageCancel_Click(object sender, RoutedEventArgs e) => AddPassagePopup.IsOpen = false;

    // ── Resaltar señalando el Live ───────────────────────────────

    private Point? _liveHighlightStart;

    private void LiveView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _liveHighlightStart = e.GetPosition(LiveView.SlideView);
        LiveView.CaptureMouse();
        e.Handled = true;
    }

    /// <summary>
    /// While the button is down the swept words show as selected on the Live
    /// preview only, like in a text box; the screen waits for the release.
    /// </summary>
    private void LiveView_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_liveHighlightStart is Point start && e.LeftButton == MouseButtonState.Pressed)
            LiveView.SlideView.ShowSelection(start, e.GetPosition(LiveView.SlideView));
    }

    /// <summary>
    /// Pointing at the words on the Live preview highlights them on the
    /// screen: a click takes the word, a drag the phrase, and only that
    /// appearance is marked. With Shift the mark joins the earlier ones;
    /// pointing at what is already highlighted takes it off again.
    /// </summary>
    private void LiveView_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        LiveView.ReleaseMouseCapture();
        LiveView.SlideView.ClearSelection();
        if (_liveHighlightStart is not Point start || DataContext is not MainViewModel vm)
            return;
        _liveHighlightStart = null;

        var phrase = LiveView.SlideView.PhraseBetween(start, e.GetPosition(LiveView.SlideView));
        if (phrase is not { } mark)
            return;

        vm.MarkOnLive(mark, addToOthers: (Keyboard.Modifiers & ModifierKeys.Shift) != 0);
        e.Handled = true;
    }

    // ── Reordenar diapositivas arrastrando ───────────────────────

    private Point? _slideDragStart;
    private SlideItemViewModel? _slideDragItem;
    private SlideDragAdorner? _slideDragAdorner;

    private void SlideCard_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _slideDragItem = (sender as FrameworkElement)?.DataContext as SlideItemViewModel;
        _slideDragStart = e.GetPosition(this);
    }

    /// <summary>
    /// A press that travels a few pixels becomes a drag. Until then it is an
    /// ordinary click, which projects the slide on release; once the drag
    /// takes the mouse, the button never sees its release and doesn't fire.
    /// While it lasts, a copy of the card follows the mouse and the card
    /// itself fades in its place.
    /// </summary>
    private void SlideCard_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed
            || _slideDragStart is not Point start
            || _slideDragItem is not { } item
            || sender is not FrameworkElement card
            || DataContext is not MainViewModel vm
            || !vm.CanMoveSlide(item))
            return;

        var moved = e.GetPosition(this) - start;
        if (Math.Abs(moved.X) < SystemParameters.MinimumHorizontalDragDistance * 2
            && Math.Abs(moved.Y) < SystemParameters.MinimumVerticalDragDistance * 2)
            return;

        _slideDragStart = null;
        var layer = AdornerLayer.GetAdornerLayer(SlidesScroll);
        _slideDragAdorner = new SlideDragAdorner(SlidesScroll, card, (Brush)FindResource("AccentBrush"));
        layer?.Add(_slideDragAdorner);
        card.Opacity = 0.35;

        try
        {
            DragDrop.DoDragDrop(card, new DataObject(typeof(SlideItemViewModel), item), DragDropEffects.Move);
        }
        finally
        {
            card.Opacity = 1;
            layer?.Remove(_slideDragAdorner);
            _slideDragAdorner = null;
            _slideDragItem = null;
        }
    }

    private static SlideItemViewModel? DraggedSlide(DragEventArgs e) =>
        e.Data.GetData(typeof(SlideItemViewModel)) as SlideItemViewModel;

    private void SlidesScroll_DragOver(object sender, DragEventArgs e)
    {
        e.Handled = true;
        if (DraggedSlide(e) is null || DataContext is not MainViewModel)
        {
            e.Effects = DragDropEffects.None;
            return;
        }

        e.Effects = DragDropEffects.Move;
        var pointer = e.GetPosition(SlidesScroll);
        _slideDragAdorner?.Update(pointer, SlideDropGap(e)?.Bar);

        // Near the top or bottom edge the grid scrolls, so a slide can be
        // carried to a place that is not in sight.
        const double edge = 36;
        if (pointer.Y < edge)
            SlidesScroll.ScrollToVerticalOffset(SlidesScroll.VerticalOffset - 12);
        else if (pointer.Y > SlidesScroll.ActualHeight - edge)
            SlidesScroll.ScrollToVerticalOffset(SlidesScroll.VerticalOffset + 12);
    }

    private void SlidesScroll_DragLeave(object sender, DragEventArgs e) =>
        _slideDragAdorner?.Update(null, null);

    private void SlidesScroll_Drop(object sender, DragEventArgs e)
    {
        e.Handled = true;
        _slideDragAdorner?.Update(null, null);
        if (DataContext is MainViewModel vm && DraggedSlide(e) is { } dragged && SlideDropGap(e) is { } gap)
            vm.MoveSlideTo(dragged, gap.Index);
    }

    /// <summary>
    /// The gap a dragged card would fall into: before the card under the
    /// pointer when it is on that card's left half, after it on the right
    /// half, after the last card on empty space. Also where to draw the bar
    /// that marks it, in the grid's scroll viewer coordinates.
    /// </summary>
    private (int Index, Rect Bar)? SlideDropGap(DragEventArgs e)
    {
        var count = SlideGrid.Items.Count;
        if (count == 0)
            return null;

        var index = count - 1;
        var after = true;
        if (e.OriginalSource is DependencyObject source
            && ItemsControl.ContainerFromElement(SlideGrid, source) is FrameworkElement hovered)
        {
            index = SlideGrid.ItemContainerGenerator.IndexFromContainer(hovered);
            after = e.GetPosition(hovered).X > hovered.ActualWidth / 2;
        }

        if (SlideGrid.ItemContainerGenerator.ContainerFromIndex(index) is not FrameworkElement container)
            return null;

        // The cards carry a 6 px margin, so the edge of the container is the
        // middle of the gap between two cards.
        var topLeft = container.TranslatePoint(new Point(0, 0), SlidesScroll);
        var x = after ? topLeft.X + container.ActualWidth : topLeft.X;
        var bar = new Rect(x - 2, topLeft.Y + 8, 4, Math.Max(0, container.ActualHeight - 16));
        return (after ? index + 1 : index, bar);
    }

    // The card's own menu is about songs: a verse gets its own, and the
    // chapter jump cards none.
    private void SlideCard_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: SlideItemViewModel slide } card)
            return;

        // A page of a presentation has no text to edit: only its properties.
        if (slide.Page is { } page)
        {
            e.Handled = true;
            if (DataContext is MainViewModel pageVm)
            {
                var pageMenu = new ContextMenu { PlacementTarget = card };
                pageMenu.Items.Add(new MenuItem
                {
                    Header = "Propiedades…",
                    Command = pageVm.InspectMediaCommand,
                    CommandParameter = page,
                });
                pageMenu.IsOpen = true;
            }
            return;
        }

        if (slide.Verses is not null)
        {
            e.Handled = true;
            if (DataContext is MainViewModel vm)
                ShowVerseMenu(vm, card, slide);
            return;
        }

        if (slide.SectionId == 0)
            e.Handled = true;
    }

    /// <summary>
    /// Right click on a verse: add it to the playlist, or add everything from
    /// the verse on screen (or marked as next) up to this one («Juan 8:5-10»).
    /// </summary>
    private static void ShowVerseMenu(MainViewModel vm, FrameworkElement card, SlideItemViewModel slide)
    {
        var verses = slide.Verses!;
        var menu = new ContextMenu { PlacementTarget = card };

        menu.Items.Add(new MenuItem
        {
            Header = $"Agregar {verses.Chapter}:{verses.Numbers} a la playlist",
            Command = vm.AddVerseSlideToPlaylistCommand,
            CommandParameter = slide,
        });

        if (vm.RangeFromLive(slide) is var (first, last))
        {
            menu.Items.Add(new MenuItem
            {
                Header = $"Agregar {verses.Chapter}:{first}-{last} a la playlist",
                Command = vm.AddRangeToSlideToPlaylistCommand,
                CommandParameter = slide,
            });
        }

        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem
        {
            Header = "Corregir el texto…",
            Command = vm.CorrectVerseTextCommand,
            CommandParameter = slide,
        });

        menu.IsOpen = true;
    }

    private void ScrollSlideIntoView(int index)
    {
        if (index < 0)
            return;

        // Defer until the cards have been laid out (they may have just been added).
        Dispatcher.InvokeAsync(() =>
        {
            if (SlideGrid.ItemContainerGenerator.ContainerFromIndex(index) is FrameworkElement container)
                container.BringIntoView();
        }, DispatcherPriority.Background);
    }

    // Arrow keys must be intercepted before WPF's directional focus
    // navigation consumes them (it moves focus between slide cards and
    // the event never reaches the window's input bindings).
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        if (e.Handled || (e.Key != Key.Left && e.Key != Key.Right))
            return;

        if (DataContext is not MainViewModel vm)
            return;

        // Leave the arrows alone while the operator is typing — in a box, or in
        // the web page, whose arrows scroll it and move its cursor. The one
        // exception is the Bible search box right after Enter projected a
        // verse: there the arrows go on to the next verse until a key is typed.
        var searchDrivesSlides = ReferenceEquals(e.OriginalSource, BibleQueryBox)
            && vm.ArrowsDriveSlidesFromSearch
            && Keyboard.Modifiers == ModifierKeys.None;
        if ((e.OriginalSource is TextBox && !searchDrivesSlides) || WebHasKeyboard)
            return;

        // IsRepeat tells a held key from a new press: the view model stops the
        // held one at the edge of a playlist item.
        vm.Navigate(e.Key == Key.Right ? +1 : -1, e.IsRepeat);

        e.Handled = true;
    }
}
