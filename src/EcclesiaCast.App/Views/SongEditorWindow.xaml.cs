using System.Windows;
using EcclesiaCast.Core.Songs;

namespace EcclesiaCast.App.Views;

public partial class SongEditorWindow : Window
{
    private readonly int _songId;

    public SongEditorWindow(Song? existing)
    {
        InitializeComponent();

        if (existing is not null)
        {
            _songId = existing.Id;
            Title = "Editar canción";
            TitleBox.Text = existing.Title;
            ArtistBox.Text = existing.Artist;
            CopyrightBox.Text = existing.Copyright ?? string.Empty;
            LyricsBox.Text = LyricsParser.ToTaggedText(existing.Sections);
        }
        else
        {
            Title = "Nueva canción";
        }

        TitleBox.Focus();
    }

    public Song? Result { get; private set; }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var title = TitleBox.Text.Trim();
        if (title.Length == 0)
        {
            ErrorText.Text = "La canción necesita un título.";
            TitleBox.Focus();
            return;
        }

        var sections = LyricsParser.Parse(LyricsBox.Text);
        if (sections.Count == 0)
        {
            ErrorText.Text = "La letra está vacía.";
            LyricsBox.Focus();
            return;
        }

        Result = new Song
        {
            Id = _songId,
            Title = title,
            Artist = ArtistBox.Text.Trim(),
            Copyright = string.IsNullOrWhiteSpace(CopyrightBox.Text) ? null : CopyrightBox.Text.Trim(),
            Sections = sections,
        };
        DialogResult = true;
    }
}
