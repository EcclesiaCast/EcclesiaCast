using CommunityToolkit.Mvvm.ComponentModel;
using EcclesiaCast.Core.Songs;
using EcclesiaCast.Core.Themes;

namespace EcclesiaCast.Presentation.ViewModels;

/// <summary>An entry in the theme list of the song editor; a null Id means "use the default theme".</summary>
public sealed record SongThemeChoice(int? Id, string Label);

/// <summary>The boxes of the song editor that can be missing something.</summary>
public enum SongEditorField { Title, Lyrics }

/// <summary>
/// What the song editor decides: what its boxes and its theme list start
/// with, what is missing when it cannot be saved and the song it makes. The
/// text of the boxes belongs to the operator while they edit, so the window
/// hands it over to <see cref="TrySave"/>.
/// </summary>
public sealed partial class SongEditorViewModel : ObservableObject
{
    private readonly int _songId;
    private readonly List<SongSection> _existingSections = [];

    public SongEditorViewModel(Song? existing, IReadOnlyList<SlideTheme> themes)
    {
        var choices = new List<SongThemeChoice> { new(null, "(Tema principal)") };
        choices.AddRange(themes.Select(t => new SongThemeChoice(t.Id, t.Name)));

        // The song's own theme is not among the shared ones: offered apart, so
        // saving the lyrics does not take it away. Picking another one does.
        if (existing?.ThemeId is int own && choices.All(c => c.Id != own))
            choices.Insert(1, new SongThemeChoice(own, "(Tema propio de esta canción)"));

        Themes = choices;
        SelectedTheme = choices.FirstOrDefault(c => c.Id == existing?.ThemeId) ?? choices[0];

        if (existing is not null)
        {
            _songId = existing.Id;
            _existingSections = existing.Sections;
            WindowTitle = "Editar canción";
            InitialTitle = existing.Title;
            InitialArtist = existing.Artist;
            InitialCopyright = existing.Copyright ?? string.Empty;
            InitialLyrics = LyricsParser.ToTaggedText(existing.Sections);
        }
        else
        {
            WindowTitle = "Nueva canción";
        }
    }

    public string WindowTitle { get; }

    public string InitialTitle { get; } = string.Empty;

    public string InitialArtist { get; } = string.Empty;

    public string InitialCopyright { get; } = string.Empty;

    public string InitialLyrics { get; } = string.Empty;

    public IReadOnlyList<SongThemeChoice> Themes { get; }

    [ObservableProperty]
    private SongThemeChoice? _selectedTheme;

    /// <summary>Why the last <see cref="TrySave"/> did not save; empty before.</summary>
    [ObservableProperty]
    private string _error = string.Empty;

    /// <summary>The song as saved; null until <see cref="TrySave"/> accepts.</summary>
    public Song? Result { get; private set; }

    /// <summary>
    /// Makes the song with what the boxes hold. Returns null when it did
    /// (the song is in <see cref="Result"/>); otherwise the box that is
    /// missing something, for the window to put the cursor there, and
    /// <see cref="Error"/> says what.
    /// </summary>
    public SongEditorField? TrySave(string? titleText, string? artist, string? copyright, string? lyrics)
    {
        var title = (titleText ?? string.Empty).Trim();
        if (title.Length == 0)
        {
            Error = "La canción necesita un título.";
            return SongEditorField.Title;
        }

        var sections = LyricsParser.Parse(lyrics ?? string.Empty);
        if (sections.Count == 0)
        {
            Error = "La letra está vacía.";
            return SongEditorField.Lyrics;
        }

        // Best effort: los diseños por diapositiva sobreviven la edición de
        // la letra emparejando por posición.
        for (var i = 0; i < sections.Count && i < _existingSections.Count; i++)
            sections[i].StyleJson = _existingSections[i].StyleJson;

        Result = new Song
        {
            Id = _songId,
            Title = title,
            Artist = (artist ?? string.Empty).Trim(),
            Copyright = string.IsNullOrWhiteSpace(copyright) ? null : copyright.Trim(),
            ThemeId = SelectedTheme?.Id,
            Sections = sections,
        };
        return null;
    }
}
