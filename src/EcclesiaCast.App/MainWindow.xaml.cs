using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using EcclesiaCast.App.Controls;
using EcclesiaCast.App.ViewModels;
using EcclesiaCast.Core.Abstractions;
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

            // Keep the relevant slide card visible: the live one while
            // navigating with the arrows, the previewed one after a search.
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(MainViewModel.LiveSlideIndex))
                    ScrollSlideIntoView(vm.LiveSlideIndex);
                else if (args.PropertyName == nameof(MainViewModel.PreviewSlideIndex))
                    ScrollSlideIntoView(vm.PreviewSlideIndex);
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

    /// <summary>Screen picker and switches for the stage display, under its ▾ button.</summary>
    private void StageOptions_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm || sender is not FrameworkElement anchor)
            return;

        var menu = new ContextMenu
        {
            PlacementTarget = anchor,
            Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom,
        };

        menu.Items.Add(new MenuItem { Header = "Pantalla del escenario", IsEnabled = false });
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
        menu.Items.Add(Toggle("Mostrar la hora", vm.StageShowClock, v => vm.StageShowClock = v));
        menu.Items.Add(Toggle("Mostrar el cronómetro", vm.StageShowTimer, v => vm.StageShowTimer = v));
        menu.Items.Add(Toggle("Mostrar la diapositiva siguiente", vm.StageShowNext, v => vm.StageShowNext = v));
        menu.Items.Add(Toggle("Mostrar las notas", vm.StageShowNotes, v => vm.StageShowNotes = v));

        menu.Items.Add(new Separator());
        foreach (var size in new[] { 72d, 96d, 120d, 150d })
        {
            var item = new MenuItem
            {
                Header = $"Letra {size:0}",
                IsCheckable = true,
                IsChecked = Math.Abs(vm.StageTextScale - size) < 1,
            };
            item.Click += (_, _) => vm.StageTextScale = size;
            menu.Items.Add(item);
        }

        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem
        {
            Header = "⟲  Poner el cronómetro en cero",
            Command = vm.ResetStageTimerCommand,
        });

        menu.IsOpen = true;
        return;

        static MenuItem Toggle(string header, bool isChecked, Action<bool> set)
        {
            var item = new MenuItem { Header = header, IsCheckable = true, IsChecked = isChecked };
            item.Click += (s, _) => set(((MenuItem)s).IsChecked);
            return item;
        }
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

    private void PlaylistBody_DragOver(object sender, DragEventArgs e)
    {
        e.Handled = true;
        if (!e.Data.GetDataPresent(typeof(Song)))
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
        if (DataContext is not MainViewModel vm || e.Data.GetData(typeof(Song)) is not Song song)
            return;

        var (index, _) = PlaylistDropPoint(e);
        vm.InsertSongInPlaylist(song, index);
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
    /// screen: a click takes the word, a drag the phrase. Pointing at what is
    /// already highlighted takes the highlight off again.
    /// </summary>
    private void LiveView_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        LiveView.ReleaseMouseCapture();
        LiveView.SlideView.ClearSelection();
        if (_liveHighlightStart is not Point start || DataContext is not MainViewModel vm)
            return;
        _liveHighlightStart = null;

        var phrase = LiveView.SlideView.PhraseBetween(start, e.GetPosition(LiveView.SlideView));
        if (phrase is null)
            return;

        vm.HighlightText = string.Equals(phrase, vm.HighlightText, StringComparison.CurrentCultureIgnoreCase)
            ? string.Empty
            : phrase;
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

        // Leave the arrows alone while the operator is typing.
        if (e.OriginalSource is TextBox)
            return;

        if (DataContext is not MainViewModel vm)
            return;

        if (e.Key == Key.Right)
            vm.NextSlideCommand.Execute(null);
        else
            vm.PreviousSlideCommand.Execute(null);

        e.Handled = true;
    }
}
