using System.Windows;
using EcclesiaCast.Core.Songs;
using EcclesiaCast.Core.Themes;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.App.Views;

/// <summary>
/// The song editor. What it starts with, what is missing when it cannot be
/// saved and the song it makes are in <see cref="SongEditorViewModel"/>; this
/// window places the texts in the boxes and moves the cursor.
/// </summary>
public partial class SongEditorWindow : Window
{
    private readonly SongEditorViewModel _viewModel;

    public SongEditorWindow(Song? existing, IReadOnlyList<SlideTheme> themes)
    {
        InitializeComponent();
        _viewModel = new SongEditorViewModel(existing, themes);

        ThemeCombo.ItemsSource = _viewModel.Themes;
        ThemeCombo.SelectedItem = _viewModel.SelectedTheme;

        Title = _viewModel.WindowTitle;
        TitleBox.Text = _viewModel.InitialTitle;
        ArtistBox.Text = _viewModel.InitialArtist;
        CopyrightBox.Text = _viewModel.InitialCopyright;
        LyricsBox.Text = _viewModel.InitialLyrics;

        TitleBox.Focus();
    }

    public Song? Result => _viewModel.Result;

    /// <summary>
    /// «Coro», «Estrofa»…: writes the tag line above the paragraph the cursor
    /// is in. Done as a replacement of the selection so Ctrl+Z takes it back.
    /// </summary>
    private void Tag_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not string kind)
            return;

        var change = LyricsTags.Insert(LyricsBox.Text, LyricsBox.CaretIndex, kind);
        LyricsBox.Focus();
        LyricsBox.Select(change.Start, change.Length);
        LyricsBox.SelectedText = change.Text;
        LyricsBox.Select(change.Caret, 0);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.SelectedTheme = ThemeCombo.SelectedItem as SongThemeChoice;
        var missing = _viewModel.TrySave(TitleBox.Text, ArtistBox.Text, CopyrightBox.Text, LyricsBox.Text);
        if (missing is not null)
        {
            ErrorText.Text = _viewModel.Error;
            (missing == SongEditorField.Title ? TitleBox : LyricsBox).Focus();
            return;
        }

        DialogResult = true;
    }
}
