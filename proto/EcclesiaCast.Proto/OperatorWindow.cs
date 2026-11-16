using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Bible;
using EcclesiaCast.Core.Songs;
using EcclesiaCast.Core.Themes;
using LibVLCSharp.Shared;
using QRCoder;
using Serilog;

namespace EcclesiaCast.Proto;

/// <summary>
/// The operator's window. Ugly on purpose: it only has to drive the six
/// checks of the prototype (plan-mac.md, section 4).
/// </summary>
public sealed class OperatorWindow : Window
{
    private readonly ProtoData _data;
    private readonly LibVLC? _engine;
    private readonly VideoBackground? _video;
    private readonly RemoteServer _remote;
    private readonly KeepAwake _keepAwake = new();

    private readonly ListBox _songList = new() { Height = 160 };
    private readonly ComboBox _bookBox = new() { MinWidth = 160 };
    private readonly ComboBox _chapterBox = new() { MinWidth = 70 };
    private readonly ComboBox _verseBox = new() { MinWidth = 70 };
    private readonly ListBox _screenList = new() { Height = 110 };
    private readonly SlideView _preview = new();
    private readonly Image _previewVideo = new() { Stretch = Stretch.UniformToFill, IsHitTestVisible = false };
    private readonly TextBlock _slideInfo = new() { TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock _notice = new() { Foreground = Brushes.DarkRed, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock _videoInfo = new() { TextWrapping = TextWrapping.Wrap, Text = "Sin video de fondo." };
    private readonly CheckBox _videoSound = new() { Content = "Video con sonido (para probar el HDMI)" };
    private readonly Image _qr = new() { Width = 160, Height = 160 };
    private readonly TextBlock _address = new() { FontSize = 18, FontWeight = FontWeight.SemiBold };

    private BibleVersionInfo? _bible;
    private IReadOnlyList<BibleVerseResult> _chapterVerses = [];
    private readonly List<(string Text, string? Caption)> _slides = [];
    private int _index;
    private SlideTheme _theme;
    private bool _bibleSlides;

    private OutputWindow? _output;
    private bool _outputLost;
    private IReadOnlyList<Screen> _screens = [];

    public OperatorWindow(ProtoData data)
    {
        _data = data;
        _theme = data.SongTheme();
        Title = "EcclesiaCast Proto — operador";
        Width = 1100;
        Height = 760;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        _engine = VideoEngine.Start();
        if (_engine is not null)
        {
            _video = new VideoBackground(_engine);
            _video.Attach(_previewVideo, TimeSpan.FromMilliseconds(100));
        }

        _remote = new RemoteServer(command => Dispatcher.UIThread.InvokeAsync(() =>
        {
            Move(command == "anterior" ? -1 : 1);
            return CurrentText();
        }).GetTask());

        Content = BuildLayout();
        LoadSongs();
        LoadBible();

        Opened += (_, _) => OnOpened();
        KeyDown += OnKeyDown;
    }

    // ── Armado de la ventana ─────────────────────────────────────

    private Control BuildLayout()
    {
        var left = new StackPanel { Spacing = 8, Width = 360 };
        left.Children.Add(Title2("Canciones"));
        left.Children.Add(_songList);
        _songList.SelectionChanged += (_, _) => OnSongChosen();

        left.Children.Add(Title2("Biblia"));
        var bibleRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        bibleRow.Children.Add(_bookBox);
        bibleRow.Children.Add(_chapterBox);
        bibleRow.Children.Add(_verseBox);
        left.Children.Add(bibleRow);
        left.Children.Add(MakeButton("Mostrar versículo", ShowVerse));
        _bookBox.SelectionChanged += (_, _) => OnBookChosen();
        _chapterBox.SelectionChanged += (_, _) => OnChapterChosen();

        left.Children.Add(Title2("Pantallas"));
        left.Children.Add(_screenList);

        left.Children.Add(Title2("Para probar que no roba el teclado"));
        left.Children.Add(new TextBox { PlaceholderText = "Escribí acá mientras proyecta", AcceptsReturn = false });

        var buttons = new WrapPanel { Orientation = Orientation.Horizontal };
        foreach (var button in new[]
                 {
                     MakeButton("Proyectar", Project),
                     MakeButton("Apagar", TurnOff),
                     MakeButton("Anterior", () => Move(-1)),
                     MakeButton("Siguiente", () => Move(1)),
                     MakeButton("Video de fondo", () => _ = ChooseVideoAsync()),
                     MakeButton("Sacar video", StopVideo),
                     MakeButton("Abrir carpeta de registros", OpenLogFolder),
                 })
        {
            button.Margin = new Thickness(0, 0, 6, 6);
            buttons.Children.Add(button);
        }

        var previewLayers = new Grid();
        previewLayers.Children.Add(_previewVideo);
        previewLayers.Children.Add(_preview);
        var previewBox = new Border
        {
            Width = 576,
            Height = 324,
            Background = Brushes.Black,
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(1),
            HorizontalAlignment = HorizontalAlignment.Left,
            Child = previewLayers,
        };

        var remoteRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        remoteRow.Children.Add(_qr);
        var remoteText = new StackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        remoteText.Children.Add(new TextBlock { Text = "Control desde el celular (misma wifi):" });
        remoteText.Children.Add(_address);
        remoteRow.Children.Add(remoteText);

        var right = new StackPanel { Spacing = 8 };
        right.Children.Add(buttons);
        right.Children.Add(_notice);
        right.Children.Add(Title2("Vista previa"));
        right.Children.Add(previewBox);
        right.Children.Add(_slideInfo);
        right.Children.Add(_videoSound);
        right.Children.Add(_videoInfo);
        right.Children.Add(remoteRow);

        var columns = new DockPanel { Margin = new Thickness(12) };
        DockPanel.SetDock(left, Dock.Left);
        left.Margin = new Thickness(0, 0, 16, 0);
        columns.Children.Add(left);
        columns.Children.Add(right);

        return new ScrollViewer { Content = columns };
    }

    private static TextBlock Title2(string text) =>
        new() { Text = text, FontSize = 16, FontWeight = FontWeight.SemiBold, Margin = new Thickness(0, 6, 0, 0) };

    private static Button MakeButton(string text, Action action)
    {
        var button = new Button { Content = text };
        button.Click += (_, _) =>
        {
            Log.Information("Botón «{Button}»", text);
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Falló el botón «{Button}»", text);
            }
        };
        return button;
    }

    // ── Arranque ─────────────────────────────────────────────────

    private void OnOpened()
    {
        Log.Information("Ventana del operador abierta a los {Ms} ms del arranque", Program.SinceStart.ElapsedMilliseconds);

        if (Screens is { } screens)
        {
            screens.Changed += (_, _) => Dispatcher.UIThread.Post(OnScreensChanged);
            RefreshScreens("al abrir");
        }

        RemoteServer.LogInterfaces();
        if (_remote.Start())
        {
            _address.Text = _remote.Address;
            _qr.Source = MakeQr(_remote.Address);
        }
        else
        {
            _address.Text = $"No se pudo abrir el puerto {RemoteServer.DefaultPort}.";
        }
    }

    private static Bitmap MakeQr(string text)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.M);
        var png = new PngByteQRCode(data).GetGraphic(8);
        using var stream = new MemoryStream(png);
        return new Bitmap(stream);
    }

    // ── Canciones y Biblia ───────────────────────────────────────

    private void LoadSongs()
    {
        var titles = _data.Songs.GetTitles();
        _songList.ItemsSource = titles.Select(t => new Choice<int>(t.Id, t.Title)).ToList();
        Log.Information("Canciones en la biblioteca: {Count}", titles.Count);
    }

    private void OnSongChosen()
    {
        if (_songList.SelectedItem is not Choice<int> choice || _data.Songs.Get(choice.Value) is not Song song)
            return;

        _slides.Clear();
        _slides.AddRange(song.Sections.OrderBy(s => s.Order).Select(s => (s.Text, (string?)null)));
        _theme = _data.SongTheme();
        _bibleSlides = false;
        _index = 0;
        Log.Information("Canción elegida: {Title} ({Count} diapositivas)", song.Title, _slides.Count);
        ShowCurrent();
    }

    private void LoadBible()
    {
        _bible = _data.Bibles.GetVersions().FirstOrDefault();
        if (_bible is null)
        {
            Log.Error("No hay ninguna Biblia en la biblioteca");
            _notice.Text = "No se cargó ninguna Biblia. Mandá la carpeta de registros.";
            return;
        }

        Log.Information("Biblia: {Name} ({Abbreviation}), {Count} versículos", _bible.Name, _bible.Abbreviation, _bible.VerseCount);
        var books = _data.Bibles.GetAvailableBookNumbers(_bible.Id)
            .Select(n => new Choice<int>(n, BibleBookCatalog.FindByNumber(n)?.Name ?? $"Libro {n}"))
            .ToList();
        _bookBox.ItemsSource = books;

        // Starts on John 3:16, so the first start already shows a verse.
        _bookBox.SelectedItem = books.FirstOrDefault(b => b.Value == 43) ?? books.FirstOrDefault();
        _chapterBox.SelectedItem = (_chapterBox.ItemsSource as IEnumerable<int>)?.FirstOrDefault(c => c == 3);
        _verseBox.SelectedIndex = Math.Min(15, Math.Max(0, _chapterVerses.Count - 1));
        ShowVerse();
    }

    private void OnBookChosen()
    {
        if (_bible is null || _bookBox.SelectedItem is not Choice<int> book)
            return;

        var chapters = _data.Bibles.GetChapterNumbers(_bible.Id, book.Value);
        _chapterBox.ItemsSource = chapters;
        _chapterBox.SelectedIndex = chapters.Count > 0 ? 0 : -1;
    }

    private void OnChapterChosen()
    {
        if (_bible is null || _bookBox.SelectedItem is not Choice<int> book || _chapterBox.SelectedItem is not int chapter)
            return;

        _chapterVerses = _data.Bibles.GetPassage(_bible.Id, new BibleReference(book.Value, chapter, null, null));
        _verseBox.ItemsSource = _chapterVerses.Select(v => v.Verse).ToList();
        _verseBox.SelectedIndex = _chapterVerses.Count > 0 ? 0 : -1;
    }

    private void ShowVerse()
    {
        if (_bible is null || _chapterVerses.Count == 0)
            return;

        _slides.Clear();
        _slides.AddRange(_chapterVerses.Select(v => (v.Text, (string?)$"{v.Reference} ({_bible.Abbreviation})")));
        _theme = _data.BibleTheme();
        _bibleSlides = true;
        _index = Math.Clamp(_verseBox.SelectedIndex, 0, _slides.Count - 1);
        ShowCurrent();
    }

    // ── Diapositivas ─────────────────────────────────────────────

    private void Move(int step)
    {
        if (_slides.Count == 0)
            return;

        _index = Math.Clamp(_index + step, 0, _slides.Count - 1);
        ShowCurrent();
    }

    private string CurrentText() => _slides.Count == 0 ? string.Empty : _slides[_index].Text;

    private void ShowCurrent()
    {
        if (_slides.Count == 0)
            return;

        var (text, caption) = _slides[_index];
        var overVideo = _video?.IsPlaying == true;
        _preview.Show(text, caption, _theme, overVideo);
        _output?.Slide.Show(text, caption, _theme, overVideo);
        _slideInfo.Text = $"{(_bibleSlides ? "Versículo" : "Diapositiva")} {_index + 1} de {_slides.Count}"
            + (caption is null ? string.Empty : $" — {caption}");
        Log.Information("Diapositiva {Index}/{Count}: {Start}", _index + 1, _slides.Count,
            text.Length > 40 ? text[..40] + "…" : text);
    }

    // ── Salida ───────────────────────────────────────────────────

    private void Project()
    {
        if (_output is not null)
            return;

        var screen = (_screenList.SelectedItem as Choice<Screen>)?.Value
            ?? _screens.FirstOrDefault(s => !s.IsPrimary)
            ?? _screens.FirstOrDefault();
        if (screen is null)
        {
            _notice.Text = "No hay pantallas para proyectar.";
            return;
        }

        var single = _screens.Count <= 1;
        var output = new OutputWindow(screen, single);
        output.CloseRequested += (_, _) => TurnOff();
        output.Closed += (_, _) => OnOutputClosed(output);
        _video?.Attach(output.VideoImage, TimeSpan.Zero);
        _output = output;
        _outputLost = false;
        ShowCurrent();
        output.Show();
        _keepAwake.Start();

        _notice.Text = single
            ? "Proyectando en la única pantalla. Para volver: Esc o doble clic sobre la salida."
            : $"Proyectando en {Describe(screen)}.";
        Log.Information("Proyectar en {Screen} (una sola pantalla: {Single})", Describe(screen), single);
    }

    private void TurnOff()
    {
        var output = _output;
        if (output is null)
            return;

        output.Close();
        _notice.Text = string.Empty;
    }

    private void OnOutputClosed(OutputWindow output)
    {
        _video?.Detach(output.VideoImage);
        if (_output == output)
            _output = null;
        _keepAwake.Stop();
        Log.Information("Salida cerrada");
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        // The output never takes the keyboard, so with one screen Esc lands here.
        if (e.Key == Key.Escape && _output is not null && _screens.Count <= 1)
        {
            Log.Information("Esc con una sola pantalla: se apaga la salida");
            TurnOff();
            e.Handled = true;
        }
    }

    // ── Pantallas en caliente ────────────────────────────────────

    private void OnScreensChanged()
    {
        try
        {
            RefreshScreens("cambio");

            if (_output is { } output && !_screens.Any(s => s.Bounds == output.ScreenBounds))
            {
                Log.Warning("Se fue la pantalla de la salida ({Bounds}): se cierra la salida", output.ScreenBounds);
                _outputLost = true;
                output.Close();
                _notice.Text = "Se desconectó la pantalla de salida. Cuando vuelva, elegila en «Pantallas» y tocá «Proyectar».";
            }
            else if (_outputLost && _output is null && _screens.Count > 1)
            {
                _outputLost = false;
                _notice.Text = "Volvió una pantalla. Ya podés tocar «Proyectar».";
                Log.Information("Volvió una pantalla: se puede proyectar de nuevo");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Falló al atender el cambio de pantallas");
        }
    }

    private void RefreshScreens(string why)
    {
        _screens = Screens?.All ?? [];
        Log.Information("Pantallas ({Why}): {Count}", why, _screens.Count);
        foreach (var screen in _screens)
            Log.Information("  {Screen}", Describe(screen));

        var previous = (_screenList.SelectedItem as Choice<Screen>)?.Value.Bounds;
        var items = _screens.Select(s => new Choice<Screen>(s, Describe(s))).ToList();
        _screenList.ItemsSource = items;
        _screenList.SelectedItem = items.FirstOrDefault(i => i.Value.Bounds == previous)
            ?? items.FirstOrDefault(i => !i.Value.IsPrimary)
            ?? items.FirstOrDefault();
    }

    private static string Describe(Screen screen) =>
        $"{screen.DisplayName ?? "Pantalla"}{(screen.IsPrimary ? " (principal)" : string.Empty)}: "
        + $"{screen.Bounds.Width}×{screen.Bounds.Height} en {screen.Bounds.X},{screen.Bounds.Y}, escala {screen.Scaling:0.##}";

    // ── Video ────────────────────────────────────────────────────

    private async Task ChooseVideoAsync()
    {
        if (_video is null)
        {
            _videoInfo.Text = "No se pudo iniciar VLC: sin video. Mandá la carpeta de registros.";
            return;
        }

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Elegí un video de fondo",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Videos") { Patterns = ["*.mp4", "*.mov", "*.m4v", "*.mkv", "*.webm", "*.avi"] },
                FilePickerFileTypes.All,
            ],
        });

        var path = files.FirstOrDefault()?.TryGetLocalPath();
        if (path is null)
            return;

        _video.Play(path, _videoSound.IsChecked == true);
        _videoInfo.Text = $"Video: {Path.GetFileName(path)} (en bucle; el registro anota los cuadros cada 10 segundos)";
        ShowCurrent();
    }

    private void StopVideo()
    {
        _video?.Stop();
        _videoInfo.Text = "Sin video de fondo.";
        ShowCurrent();
    }

    // ── Registros ────────────────────────────────────────────────

    private void OpenLogFolder()
    {
        var folder = _data.LogFolder;
        Log.Information("Abrir carpeta de registros: {Folder}", folder);
        var info = OperatingSystem.IsMacOS()
            ? new ProcessStartInfo("open", $"\"{folder}\"")
            : new ProcessStartInfo("explorer.exe", $"\"{folder}\"");
        info.UseShellExecute = false;
        Process.Start(info)?.Dispose();
    }

    /// <summary>Closes everything on the way out: output, video, remote, caffeinate.</summary>
    public void ShutDown()
    {
        _output?.Close();
        _keepAwake.Dispose();
        _remote.Dispose();
        _video?.Dispose();
        _engine?.Dispose();
    }

    private sealed record Choice<T>(T Value, string Text)
    {
        public override string ToString() => Text;
    }
}
