using System.ComponentModel;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using EcclesiaCast.Core.Bible;
using EcclesiaCast.Core.Media;
using EcclesiaCast.Core.Songs;
using EcclesiaCast.Desktop.Controls;
using EcclesiaCast.Desktop.Services;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Desktop.Views;

/// <summary>
/// PROVISIONAL. The operator's window of the test program for the second
/// visit to the Mac; batch 19 replaces it with the real one. It only lets the
/// real <see cref="MainViewModel"/> be driven: every button is one of its
/// commands and every list one of its collections, with no logic of its own.
/// Its look does not matter.
/// </summary>
public sealed class TestOperatorWindow : Window
{
    private readonly MainViewModel _vm;
    private readonly ListBox _songs;
    private readonly ListBox _slides;
    private readonly ListBox _books;
    private readonly ListBox _videos;
    private readonly ComboBox _versions;
    private readonly ComboBox _displays;
    private readonly ComboBox _mediaTabs;
    private readonly WrapPanel _chapters = new();
    private readonly WrapPanel _verses = new();
    private readonly TextBlock _status;
    private readonly TextBlock _onAir;
    private readonly ProjectedView _live;

    public TestOperatorWindow(MainViewModel vm)
    {
        _vm = vm;
        DataContext = vm;
        Title = "EcclesiaCast para Mac — programa de prueba";
        Width = 1280;
        Height = 800;
        MinWidth = 900;
        MinHeight = 600;

        var banner = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#7A2E0E")),
            Padding = new Thickness(12, 6),
            Child = new TextBlock
            {
                Text = "Ventana de prueba, provisoria: sirve para probar la salida en la Mac. La reemplaza la ventana de verdad (tanda 19).",
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap,
            },
        };

        _status = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(12, 6) };
        _onAir = new TextBlock { FontWeight = FontWeight.Bold, VerticalAlignment = VerticalAlignment.Center };

        // ── Canciones ────────────────────────────────────────────
        var search = new TextBox { PlaceholderText ="Buscar canción…", Text = vm.SearchText };
        search.TextChanged += (_, _) => vm.SearchText = search.Text ?? string.Empty;
        _songs = new ListBox
        {
            ItemsSource = vm.Songs,
            ItemTemplate = new FuncDataTemplate<Song>((song, _) => new TextBlock { Text = song?.Title }),
        };
        _songs.SelectionChanged += (_, _) =>
        {
            if (_songs.SelectedItem is Song song && !ReferenceEquals(song, vm.SelectedSong))
            {
                vm.SelectedSong = song;
                vm.ShowSongsTabCommand.Execute(null);
            }
        };
        var songsPanel = Titled("Canciones", search, _songs);

        // ── Biblia ───────────────────────────────────────────────
        _versions = new ComboBox
        {
            ItemsSource = vm.BibleVersionOptions,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemTemplate = new FuncDataTemplate<BibleVersionOption>((option, _) =>
                new TextBlock { Text = option is null ? "" : $"{option.Info.Abbreviation} — {option.Info.Name}" }),
        };
        _versions.SelectionChanged += (_, _) =>
        {
            if (_versions.SelectedItem is BibleVersionOption option)
                Run(vm.ChooseVersionCommand, option);
        };
        _books = new ListBox
        {
            Height = 220,
            ItemsSource = vm.BibleBooksAvailable,
            ItemTemplate = new FuncDataTemplate<BibleBookInfo>((book, _) => new TextBlock { Text = book?.Name }),
        };
        _books.SelectionChanged += (_, _) =>
        {
            if (_books.SelectedItem is BibleBookInfo book && book != vm.SelectedBibleBook)
            {
                vm.ShowBibleTabCommand.Execute(null);
                vm.SelectedBibleBook = book;
            }
        };
        var biblePanel = Titled("Biblia", _versions, _books,
            Label("Capítulo"), Scroll(_chapters, 110), Label("Versículo"), Scroll(_verses, 150));
        vm.BibleChapters.CollectionChanged += (_, _) => FillChapters();
        vm.BibleVerses.CollectionChanged += (_, _) => FillVerses();

        // ── Diapositivas y botones de la salida ──────────────────
        _slides = new ListBox
        {
            ItemsSource = vm.Slides,
            ItemTemplate = new FuncDataTemplate<SlideItemViewModel>((slide, _) => new TextBlock
            {
                Text = slide is null ? "" : $"{slide.Label}  {slide.PreviewText}",
                TextWrapping = TextWrapping.Wrap,
                MaxHeight = 60,
            }),
        };
        _slides.DoubleTapped += (_, _) => ProjectSelectedSlide();
        _slides.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
                ProjectSelectedSlide();
        };

        var controls = new WrapPanel();
        controls.Children.Add(Command("◀ Anterior", vm.PreviousSlideCommand));
        controls.Children.Add(Command("Siguiente ▶", vm.NextSlideCommand));
        controls.Children.Add(Command("Proyectar la elegida", null, ProjectSelectedSlide));
        controls.Children.Add(Command("Negro", vm.ToggleBlackCommand));
        controls.Children.Add(Command("Logo", vm.ToggleLogoCommand));
        controls.Children.Add(Command("Prender / apagar la salida", vm.ToggleOutputCommand));
        var slidesPanel = Titled("Diapositivas (doble clic o Enter para proyectar)", controls, _slides);

        // ── Vista en vivo, pantalla, fondo y celular ─────────────
        _live = new ProjectedView { Height = 220 };
        CopyLive();
        vm.Projection.PropertyChanged += (_, _) => CopyLive();
        // The small copy of the projector's video is written in place: it is
        // drawn again on every frame the output window says it wrote.
        LiveFrames.VideoWritten += (_, _) => _live.RedrawLiveVideo();
        LiveFrames.LogoWritten += (_, _) => _live.RedrawLogoVideo();

        _displays = new ComboBox
        {
            ItemsSource = vm.Displays,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemTemplate = new FuncDataTemplate<DisplayOption>((option, _) => new TextBlock { Text = option?.Label }),
        };
        _displays.SelectionChanged += (_, _) =>
        {
            if (_displays.SelectedItem is DisplayOption option && option != vm.SelectedDisplay)
                vm.SelectedDisplay = option;
        };

        // The library's tabs: the sample backgrounds have one of their own.
        _mediaTabs = new ComboBox { ItemsSource = vm.MediaTabs, HorizontalAlignment = HorizontalAlignment.Stretch };
        _mediaTabs.SelectionChanged += (_, _) =>
        {
            if (_mediaTabs.SelectedItem is string tab && tab != vm.SelectedMediaTab)
                vm.SelectedMediaTab = tab;
        };
        _videos = new ListBox
        {
            ItemTemplate = new FuncDataTemplate<MediaItem>((item, _) => new TextBlock { Text = item?.Name }),
        };
        FillVideos();
        vm.MediaItems.CollectionChanged += (_, _) => FillVideos();

        var backgroundButtons = new WrapPanel();
        backgroundButtons.Children.Add(Command("Usar de fondo", null, () => Run(vm.ApplyBackgroundCommand, _videos.SelectedItem)));
        backgroundButtons.Children.Add(Command("Sacar el fondo", vm.ClearBackgroundCommand));

        var remote = new WrapPanel();
        remote.Children.Add(Command("📱 Control desde el celular…", vm.OpenRemoteCommand));

        var right = new StackPanel { Spacing = 6, Margin = new Thickness(6) };
        right.Children.Add(Label("En vivo"));
        right.Children.Add(new Border { Background = Brushes.Black, Child = _live });
        right.Children.Add(_onAir);
        right.Children.Add(Label("Pantalla de salida"));
        right.Children.Add(_displays);
        right.Children.Add(Command("Volver a buscar las pantallas", vm.RefreshDisplaysCommand));
        right.Children.Add(Label("Video de fondo (de la biblioteca)"));
        right.Children.Add(_mediaTabs);
        right.Children.Add(new Border { Height = 180, Child = _videos });
        right.Children.Add(backgroundButtons);
        right.Children.Add(Label("Celular"));
        right.Children.Add(remote);

        var columns = new Grid { ColumnDefinitions = new ColumnDefinitions("240,260,*,360") };
        AddColumn(columns, songsPanel, 0);
        AddColumn(columns, biblePanel, 1);
        AddColumn(columns, slidesPanel, 2);
        AddColumn(columns, new ScrollViewer { Content = right }, 3);

        var root = new DockPanel();
        DockPanel.SetDock(banner, Dock.Top);
        DockPanel.SetDock(_status, Dock.Bottom);
        root.Children.Add(banner);
        root.Children.Add(_status);
        root.Children.Add(columns);
        Content = root;

        vm.PropertyChanged += OnViewModelChanged;
        SyncFromViewModel();

        // Esc switches the output off, as in Windows: with a single screen it
        // covers this window, which still has the keyboard. The arrows move
        // the slides when the keyboard is not in a text box.
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                Run(vm.EscapeOutputCommand, null);
                e.Handled = true;
                return;
            }

            if (e.Source is TextBox)
                return;
            if (e.Key is Key.Right or Key.PageDown or Key.Space)
            {
                Run(vm.NextSlideCommand, null);
                e.Handled = true;
            }
            else if (e.Key is Key.Left or Key.PageUp)
            {
                Run(vm.PreviousSlideCommand, null);
                e.Handled = true;
            }
        };
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e) => SyncFromViewModel();

    /// <summary>What the view model says, shown back: the status line, the choices made, what is live.</summary>
    private void SyncFromViewModel()
    {
        _status.Text = _vm.StatusText;
        _onAir.Text = _vm.IsProjecting ? "● Salida prendida" : "○ Salida apagada";
        _onAir.Foreground = _vm.IsProjecting ? Brushes.Red : Brushes.Gray;

        if (!ReferenceEquals(_songs.SelectedItem, _vm.SelectedSong))
            _songs.SelectedItem = _vm.SelectedSong;
        if (!ReferenceEquals(_displays.SelectedItem, _vm.SelectedDisplay))
            _displays.SelectedItem = _vm.SelectedDisplay;
        if (!Equals(_books.SelectedItem, _vm.SelectedBibleBook))
            _books.SelectedItem = _vm.SelectedBibleBook;
        if (!Equals(_mediaTabs.SelectedItem, _vm.SelectedMediaTab))
            _mediaTabs.SelectedItem = _vm.SelectedMediaTab;
        if (!ReferenceEquals(_versions.SelectedItem, _vm.SelectedBibleVersionOption))
            _versions.SelectedItem = _vm.SelectedBibleVersionOption;
        if (_vm.LiveSlideIndex >= 0 && _vm.LiveSlideIndex < _vm.Slides.Count)
            _slides.SelectedIndex = _vm.LiveSlideIndex;
    }

    private void CopyLive()
    {
        var p = _vm.Projection;
        _live.Slide = p.Slide;
        _live.State = p.State;
        _live.Overlay = p.Overlay;
        _live.Countdown = p.Countdown;
        _live.Highlight = p.Highlight;
        _live.BackgroundMedia = p.Background;
        _live.FillMedia = p.BackgroundFill;
        _live.BackgroundBlur = p.BackgroundBlur;
        _live.LiveVideoFrame = p.LiveFrame as IImage;
        _live.LogoVideoFrame = p.LiveLogoFrame as IImage;
        _live.Logo = p.ActiveLogo;
    }

    private void ProjectSelectedSlide()
    {
        if (_slides.SelectedItem is SlideItemViewModel slide)
            Run(_vm.ProjectSlideCommand, slide);
    }

    private void FillChapters()
    {
        _chapters.Children.Clear();
        foreach (var chapter in _vm.BibleChapters)
            _chapters.Children.Add(Small(chapter.Number.ToString(), () => Run(_vm.SelectChapterCommand, chapter)));
    }

    private void FillVerses()
    {
        _verses.Children.Clear();
        foreach (var verse in _vm.BibleVerses)
            _verses.Children.Add(Small(verse.Number.ToString(), () => Run(_vm.SelectVerseCommand, verse)));
    }

    private void FillVideos()
    {
        var keep = _videos.SelectedItem;
        _videos.ItemsSource = _vm.MediaItems.Where(m => m.Type == MediaType.Video).ToList();
        _videos.SelectedItem = keep;
    }

    // ── Piezas ───────────────────────────────────────────────────

    private static void Run(ICommand command, object? parameter)
    {
        if (command.CanExecute(parameter))
            command.Execute(parameter);
    }

    private static Button Command(string text, ICommand? command, Action? action = null)
    {
        var button = new Button { Content = text, Margin = new Thickness(0, 0, 6, 6) };
        button.Click += (_, _) =>
        {
            if (command is not null)
                Run(command, null);
            action?.Invoke();
        };
        return button;
    }

    private static Button Small(string text, Action action)
    {
        var button = new Button { Content = text, MinWidth = 36, Margin = new Thickness(0, 0, 4, 4), Padding = new Thickness(6, 2) };
        button.Click += (_, _) => action();
        return button;
    }

    private static TextBlock Label(string text) =>
        new() { Text = text, FontWeight = FontWeight.SemiBold, Margin = new Thickness(0, 6, 0, 2) };

    private static ScrollViewer Scroll(Control content, double height) => new() { Content = content, Height = height };

    private static DockPanel Titled(string title, params Control[] parts)
    {
        var panel = new DockPanel { Margin = new Thickness(6) };
        var heading = Label(title);
        DockPanel.SetDock(heading, Dock.Top);
        panel.Children.Add(heading);
        for (var i = 0; i < parts.Length; i++)
        {
            // The last part takes the room left.
            if (i < parts.Length - 1)
                DockPanel.SetDock(parts[i], Dock.Top);
            panel.Children.Add(parts[i]);
        }

        return panel;
    }

    private static void AddColumn(Grid grid, Control control, int column)
    {
        Grid.SetColumn(control, column);
        grid.Children.Add(control);
    }
}
