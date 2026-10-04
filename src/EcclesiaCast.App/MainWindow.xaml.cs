using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using EcclesiaCast.App.ViewModels;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Presentation;

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
    /// Backups behind one button: saving is done now and then, restoring
    /// hopefully never, so neither earns a permanent spot on the bar.
    /// </summary>
    private void Backup_Click(object sender, RoutedEventArgs e)
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
            Header = "Guardar una copia de la biblioteca…",
            Command = vm.SaveBackupCommand,
        });
        menu.Items.Add(new MenuItem
        {
            Header = "Restaurar desde una copia…",
            Command = vm.RestoreBackupCommand,
        });

        menu.IsOpen = true;
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
    /// Ways to bring songs in, behind one button. Importing happens once when a
    /// church moves in and then hardly ever, so it doesn't deserve permanent
    /// buttons in a panel used every Sunday.
    /// </summary>
    private void ImportMenu_Click(object sender, RoutedEventArgs e)
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
            Header = "Buscar en internet…",
            Command = vm.SearchSongsOnlineCommand,
            ToolTip = "Por nombre, artista o una frase de la letra, en musica.com y lrclib.net",
        });
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem
        {
            Header = "Traer todo de ProPresenter…",
            Command = vm.ImportFromProPresenterCommand,
            ToolTip = "Busca solo dónde está instalado ProPresenter y trae sus bibliotecas enteras",
        });
        menu.Items.Add(new MenuItem
        {
            Header = "Importar archivos (.txt, .pro)…",
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
    /// operator can look ahead; the double click that projects it arrives as
    /// a second press and is left to the list's own binding. A media item is
    /// only selected: loading it would already put it on the screen.
    /// </summary>
    private void PlaylistItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount != 1
            || DataContext is not MainViewModel vm
            || sender is not FrameworkElement { DataContext: Core.Playlists.PlaylistItem item }
            || item.Type == Core.Playlists.PlaylistItemType.Media)
            return;

        vm.OpenPlaylistItemCommand.Execute(item);
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
    /// Pointing at the words on the Live preview highlights them on the
    /// screen: a click takes the word, a drag the phrase. Pointing at what is
    /// already highlighted takes the highlight off again.
    /// </summary>
    private void LiveView_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        LiveView.ReleaseMouseCapture();
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

    private void SlideCard_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _slideDragItem = (sender as FrameworkElement)?.DataContext as SlideItemViewModel;
        _slideDragStart = e.GetPosition(this);
    }

    /// <summary>
    /// A press that travels a few pixels becomes a drag. Until then it is an
    /// ordinary click, which projects the slide on release; once the drag
    /// takes the mouse, the button never sees its release and doesn't fire.
    /// </summary>
    private void SlideCard_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed
            || _slideDragStart is not Point start
            || _slideDragItem is not { } item
            || DataContext is not MainViewModel vm
            || !vm.CanMoveSlide(item))
            return;

        var moved = e.GetPosition(this) - start;
        if (Math.Abs(moved.X) < SystemParameters.MinimumHorizontalDragDistance * 2
            && Math.Abs(moved.Y) < SystemParameters.MinimumVerticalDragDistance * 2)
            return;

        _slideDragStart = null;
        DragDrop.DoDragDrop((DependencyObject)sender, new DataObject(typeof(SlideItemViewModel), item), DragDropEffects.Move);
        _slideDragItem = null;
    }

    private static SlideItemViewModel? DraggedSlide(DragEventArgs e) =>
        e.Data.GetData(typeof(SlideItemViewModel)) as SlideItemViewModel;

    private void SlideCard_DragEnter(object sender, DragEventArgs e) => MarkDropTarget(sender, e, true);

    private void SlideCard_DragOver(object sender, DragEventArgs e)
    {
        var target = (sender as FrameworkElement)?.DataContext as SlideItemViewModel;
        var dragged = DraggedSlide(e);
        e.Effects = dragged is not null && target is not null && !ReferenceEquals(dragged, target)
            && DataContext is MainViewModel vm && vm.CanMoveSlide(target)
            ? DragDropEffects.Move
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void SlideCard_DragLeave(object sender, DragEventArgs e) => MarkDropTarget(sender, e, false);

    private void SlideCard_Drop(object sender, DragEventArgs e)
    {
        MarkDropTarget(sender, e, false);
        if (DataContext is MainViewModel vm
            && DraggedSlide(e) is { } dragged
            && (sender as FrameworkElement)?.DataContext is SlideItemViewModel target)
            vm.MoveSlide(dragged, target);
        e.Handled = true;
    }

    /// <summary>Outlines the card the dragged slide would land on.</summary>
    private void MarkDropTarget(object sender, DragEventArgs e, bool on)
    {
        if (sender is not Button button || button.Template.FindName("card", button) is not Border card)
            return;

        if (on && DraggedSlide(e) is { } dragged && !ReferenceEquals(dragged, button.DataContext))
        {
            card.BorderBrush = (Brush)FindResource("AccentBrush");
            card.BorderThickness = new Thickness(4, 1, 1, 1);
        }
        else
        {
            card.ClearValue(Border.BorderBrushProperty);
            card.ClearValue(Border.BorderThicknessProperty);
        }
    }

    // Bible slides have no per-slide actions, so suppress their context menu.
    private void SlideCard_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: SlideItemViewModel { SectionId: 0 } })
            e.Handled = true;
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
