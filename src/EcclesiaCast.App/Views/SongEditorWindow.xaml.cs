using System.Windows;
using EcclesiaCast.Core.Songs;
using EcclesiaCast.Core.Themes;

namespace EcclesiaCast.App.Views;

public partial class SongEditorWindow : Window
{
    /// <summary>An entry in the theme dropdown; null Id means "use the default theme".</summary>
    public sealed record ThemeChoice(int? Id, string Label);

    private readonly int _songId;
    private readonly List<SongSection> _existingSections = [];

    public SongEditorWindow(Song? existing, IReadOnlyList<SlideTheme> themes)
    {
        InitializeComponent();

        var choices = new List<ThemeChoice> { new(null, "(Tema por defecto)") };
        choices.AddRange(themes.Select(t => new ThemeChoice(t.Id, t.Name)));
        ThemeCombo.ItemsSource = choices;
        ThemeCombo.SelectedItem = choices.FirstOrDefault(c => c.Id == existing?.ThemeId) ?? choices[0];

        if (existing is not null)
        {
            _songId = existing.Id;
            _existingSections = existing.Sections;
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

        // Best effort: los diseños por diapositiva sobreviven la edición de
        // la letra emparejando por posición.
        for (var i = 0; i < sections.Count && i < _existingSections.Count; i++)
            sections[i].StyleJson = _existingSections[i].StyleJson;

        Result = new Song
        {
            Id = _songId,
            Title = title,
            Artist = ArtistBox.Text.Trim(),
            Copyright = string.IsNullOrWhiteSpace(CopyrightBox.Text) ? null : CopyrightBox.Text.Trim(),
            ThemeId = (ThemeCombo.SelectedItem as ThemeChoice)?.Id,
            Sections = sections,
        };
        DialogResult = true;
    }
}
