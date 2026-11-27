using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using EcclesiaCast.Core.Songs;
using EcclesiaCast.Desktop.Views;
using EcclesiaCast.Presentation.Services;
using EcclesiaCast.ScreenSamples;

namespace EcclesiaCast.Desktop.Tests;

/// <summary>The keys of the windows of songs, Bibles and the library (batch 16).</summary>
public sealed partial class DialogKeyTests
{
    private static T Named<T>(Window window, string name) where T : Control => window.FindControl<T>(name)!;

    private static void Press(Window window, PhysicalKey key, RawInputModifiers modifiers)
    {
        window.KeyPressQwerty(key, modifiers);
        window.KeyReleaseQwerty(key, modifiers);
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>Ctrl, or ⌘ on a Mac: the key of the system's «Deshacer».</summary>
    private static RawInputModifiers CommandKey =>
        OperatingSystem.IsMacOS() ? RawInputModifiers.Meta : RawInputModifiers.Control;

    [AvaloniaFact]
    public void The_song_editor_enter_writes_a_line_and_never_saves()
    {
        var window = new SongEditorWindow(null, LibrarySamples.Themes());
        var closed = Open(window);
        var title = Named<TextBox>(window, "TitleBox");
        var lyrics = Named<TextBox>(window, "LyricsBox");
        title.Text = "Santo";
        title.Focus();
        Hit(window, PhysicalKey.Enter);
        Assert.False(closed());

        lyrics.Focus();
        lyrics.Text = "uno";
        lyrics.CaretIndex = 3;
        Hit(window, PhysicalKey.Enter);

        Assert.False(closed());
        Assert.Equal("uno" + Environment.NewLine, lyrics.Text!.Replace("\r\n", "\n").Replace("\n", Environment.NewLine));
        Assert.Null(window.Result);

        Hit(window, PhysicalKey.Escape);
        Assert.True(closed());
        Assert.Null(window.Result);
    }

    [AvaloniaTheory]
    [InlineData("Coro")]
    [InlineData("Estrofa")]
    [InlineData("Puente")]
    [InlineData("Final")]
    public void The_tag_buttons_of_the_song_editor_are_undone_with_the_key_of_undo(string kind)
    {
        var window = new SongEditorWindow(null, LibrarySamples.Themes());
        Open(window);
        var lyrics = Named<TextBox>(window, "LyricsBox");
        const string written = "primera línea\n\nsegunda línea";
        lyrics.Text = written;
        lyrics.Focus();
        lyrics.CaretIndex = written.Length;
        Dispatcher.UIThread.RunJobs();
        var button = window.GetVisualDescendants().OfType<Button>().Single(b => Equals(b.Content, kind));
        Assert.False(button.Focusable);

        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(LyricsTags.Insert(written, written.Length, kind) is var change
            ? written.Remove(change.Start, change.Length).Insert(change.Start, change.Text)
            : null, lyrics.Text);
        Assert.Equal(change.Caret, lyrics.CaretIndex);

        Press(window, PhysicalKey.Z, CommandKey);

        Assert.Equal(written, lyrics.Text);
        window.Close();
    }

    [AvaloniaFact]
    public void The_song_import_enter_does_nothing_and_escape_cancels()
    {
        var window = ScreenBenchTests.SongImportWindow();
        var closed = Open(window);
        Assert.True(Named<Button>(window, "ImportButton").IsEnabled);

        Hit(window, PhysicalKey.Enter);
        Assert.False(closed());

        Hit(window, PhysicalKey.Escape);
        Assert.True(closed());
        Assert.False(window.Accepted);
    }

    [AvaloniaFact]
    public void The_search_walks_with_the_arrows_and_enter_in_the_box_loads()
    {
        var window = ScreenBenchTests.SearchWindow();
        var closed = Open(window);
        window.SearchNow();
        Dispatcher.UIThread.RunJobs();
        var box = Named<TextBox>(window, "QueryBox");
        var list = Named<ListBox>(window, "ResultsList");
        Assert.True(box.IsFocused);
        Assert.Equal(LibrarySamples.SearchQuery.Length, box.CaretIndex);
        Assert.Equal(0, list.SelectedIndex);

        Hit(window, PhysicalKey.ArrowDown);
        Hit(window, PhysicalKey.ArrowDown);
        Hit(window, PhysicalKey.ArrowUp);

        Assert.Equal(1, list.SelectedIndex);
        // The arrows leave the cursor where it was, in the box.
        Assert.True(box.IsFocused);
        Assert.Equal(LibrarySamples.SearchQuery.Length, box.CaretIndex);

        Hit(window, PhysicalKey.Enter);

        Assert.True(closed());
        Assert.Equal(LibrarySamples.SearchHits(LibrarySamples.SearchQuery)[1], window.Result);
    }

    [AvaloniaFact]
    public void The_search_enter_in_the_list_loads_the_chosen_one()
    {
        var window = ScreenBenchTests.SearchWindow();
        var closed = Open(window);
        window.SearchNow();
        Dispatcher.UIThread.RunJobs();
        var list = Named<ListBox>(window, "ResultsList");
        list.SelectedIndex = 1;
        list.ContainerFromIndex(1)!.Focus();

        Hit(window, PhysicalKey.Enter);

        Assert.True(closed());
        Assert.Equal(LibrarySamples.SearchHits(LibrarySamples.SearchQuery)[1], window.Result);
    }

    [AvaloniaFact]
    public void The_search_loads_with_a_double_click()
    {
        var window = ScreenBenchTests.SearchWindow();
        var closed = Open(window);
        window.SearchNow();
        Dispatcher.UIThread.RunJobs();
        var row = Named<ListBox>(window, "ResultsList").ContainerFromIndex(1)!;
        var middle = row.TranslatePoint(new Point(row.Bounds.Width / 2, row.Bounds.Height / 2), window)!.Value;

        for (var click = 0; click < 2; click++)
        {
            window.MouseDown(middle, MouseButton.Left);
            if (!closed())
                window.MouseUp(middle, MouseButton.Left);
        }

        Dispatcher.UIThread.RunJobs();
        Assert.True(closed());
        Assert.Equal(LibrarySamples.SearchHits(LibrarySamples.SearchQuery)[1], window.Result);
    }

    [AvaloniaFact]
    public void The_search_escape_closes_it_with_nothing()
    {
        var window = ScreenBenchTests.SearchWindow();
        var closed = Open(window);
        window.SearchNow();
        Dispatcher.UIThread.RunJobs();

        Hit(window, PhysicalKey.Escape);

        Assert.True(closed());
        Assert.Null(window.Result);
    }

    [AvaloniaTheory]
    [InlineData(PhysicalKey.Enter)]
    [InlineData(PhysicalKey.Escape)]
    public void The_trash_closes_with_enter_and_with_escape(PhysicalKey key)
    {
        var window = ScreenBenchTests.Trash(empty: false);
        var closed = Open(window);
        var list = Named<ListBox>(window, "TrashList");
        list.SelectedIndex = 0;
        list.ContainerFromIndex(0)!.Focus();
        Assert.True(Named<Button>(window, "RestoreButton").IsEnabled);
        Assert.True(Named<Button>(window, "PurgeButton").IsEnabled);

        // «Cerrar» is the default button and the one of Esc, as in WPF.
        Hit(window, key);

        Assert.True(closed());
    }

    [AvaloniaFact]
    public void The_web_search_enter_in_the_box_searches_and_does_not_close()
    {
        var window = ScreenBenchTests.WebSearch();
        var closed = Open(window);
        var box = Named<TextBox>(window, "QueryBox");
        Assert.True(box.IsFocused);
        box.Text = "renuévame";

        Hit(window, PhysicalKey.Enter);

        Assert.False(closed());
        var list = Named<ListBox>(window, "ResultsList");
        Assert.Equal(3, list.ItemCount);
        Assert.Equal(0, list.SelectedIndex);
        Assert.True(Named<Button>(window, "ImportButton").IsEnabled);
        Assert.StartsWith("2 diapositivas.", Named<TextBlock>(window, "PreviewInfoText").Text);

        // Enter in the lyrics writes a line: it neither imports nor closes.
        var lyrics = Named<TextBox>(window, "LyricsBox");
        lyrics.Focus();
        lyrics.CaretIndex = lyrics.Text!.Length;
        Hit(window, PhysicalKey.Enter);
        Assert.False(closed());
        Assert.EndsWith("\n", lyrics.Text);

        // What the operator rewrites is counted again.
        lyrics.Text = "uno\n\ndos\n\ntres";
        Dispatcher.UIThread.RunJobs();
        Assert.StartsWith("3 diapositivas.", Named<TextBlock>(window, "PreviewInfoText").Text);

        Hit(window, PhysicalKey.Escape);
        Assert.True(closed());
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void The_help_with_the_bible_formats_closes_with_escape(bool insideAnExample)
    {
        var window = ScreenBenchTests.HelpWindow();
        var closed = Open(window);
        if (insideAnExample)
            window.GetVisualDescendants().OfType<TextBox>().First().Focus();

        Hit(window, PhysicalKey.Enter);
        Assert.False(closed());

        Hit(window, PhysicalKey.Escape);
        Assert.True(closed());
        Assert.False(window.ImportRequested);
    }

    [AvaloniaFact]
    public void The_list_of_bibles_enter_does_nothing_and_escape_cancels()
    {
        var window = new BiblePackageWindow(BiblePackageDialog.ForExport(LibrarySamples.InstalledVersions()));
        var closed = Open(window);
        window.GetVisualDescendants().OfType<CheckBox>().First().Focus();

        Hit(window, PhysicalKey.Enter);
        // As in WPF, where no button is the default.
        Assert.False(closed());

        Hit(window, PhysicalKey.Escape);
        Assert.True(closed());
        Assert.False(window.Accepted);
    }

    [AvaloniaTheory]
    [InlineData("NameBox")]
    [InlineData("AbbreviationBox")]
    [InlineData("LanguageBox")]
    public void Importing_a_bible_enter_does_nothing_and_escape_cancels(string box)
    {
        var window = new BibleImportWindow("Mi Biblia");
        var closed = Open(window);
        Named<TextBox>(window, "AbbreviationBox").Text = "MB";
        Named<TextBox>(window, box).Focus();

        Hit(window, PhysicalKey.Enter);
        // As in WPF, where «Importar» is not the default button.
        Assert.False(closed());

        Hit(window, PhysicalKey.Escape);
        Assert.True(closed());
        Assert.Null(window.Result);
    }
}
