using Avalonia.Controls;
using Avalonia.Interactivity;
using EcclesiaCast.Core.Songs;
using EcclesiaCast.Core.Themes;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Desktop.Views;

/// <summary>
/// The song editor. What it starts with, what is missing when it cannot be
/// saved and the song it makes are in <see cref="SongEditorViewModel"/>; this
/// window places the texts in the boxes and moves the cursor.
/// </summary>
public sealed partial class SongEditorWindow : Window
{
    private readonly SongEditorViewModel _viewModel;

    public SongEditorWindow(Song? existing, IReadOnlyList<SlideTheme> themes)
    {
        InitializeComponent();
        _viewModel = new SongEditorViewModel(existing, themes);
        DataContext = _viewModel;

        Title = _viewModel.WindowTitle;
        TitleBox.Text = _viewModel.InitialTitle;
        ArtistBox.Text = _viewModel.InitialArtist;
        CopyrightBox.Text = _viewModel.InitialCopyright;
        LyricsBox.Text = _viewModel.InitialLyrics;

        Opened += (_, _) => TitleBox.Focus();
    }

    /// <summary>The song as saved; null when the operator cancelled.</summary>
    public Song? Result => _viewModel.Result;

    /// <summary>
    /// «Coro», «Estrofa»…: writes the tag line above the paragraph the cursor
    /// is in. Done as a replacement of the selection so Ctrl+Z (⌘Z) takes it back.
    /// </summary>
    private void Tag_Click(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.Tag is not string kind)
            return;

        var change = LyricsTags.Insert(LyricsBox.Text ?? string.Empty, LyricsBox.CaretIndex, kind);
        LyricsBox.Focus();
        LyricsBox.SelectionStart = change.Start;
        LyricsBox.SelectionEnd = change.Start + change.Length;
        LyricsBox.SelectedText = change.Text;
        LyricsBox.CaretIndex = change.Caret;
        LyricsBox.SelectionStart = change.Caret;
        LyricsBox.SelectionEnd = change.Caret;
    }

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        var missing = _viewModel.TrySave(TitleBox.Text, ArtistBox.Text, CopyrightBox.Text, LyricsBox.Text);
        if (missing is { } field)
            (field == SongEditorField.Title ? TitleBox : LyricsBox).Focus();
        else
            Close();
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close();
}
