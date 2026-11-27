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
