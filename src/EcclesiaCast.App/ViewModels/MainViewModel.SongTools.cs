using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EcclesiaCast.Core.Songs;

namespace EcclesiaCast.App.ViewModels;

/// <summary>
/// The bar over a song's slides (asked for in GitHub #32): the lock that keeps
/// the slides from being moved by accident, Ctrl+Z / Ctrl+Y for the song's
/// changes, a new empty slide, and «Buscar en la letra».
/// </summary>
public sealed partial class MainViewModel
{
    // ── Candado del orden ────────────────────────────────────────

    private const string SlidesOrderLockedKey = "slides.order.locked";

    /// <summary>
    /// The slides of a song cannot be dragged to another place while this is
    /// on. It starts on: someone moved slides by mistake more than once and did
    /// not know how to put them back. The program remembers how it was left.
    /// </summary>
    [ObservableProperty]
    private bool _slidesOrderLocked = true;

    private void LoadSlidesOrderLock() =>
        _slidesOrderLocked = _settings.Get(SlidesOrderLockedKey) != "0";

    partial void OnSlidesOrderLockedChanged(bool value)
    {
        _settings.Set(SlidesOrderLockedKey, value ? "1" : "0");
        StatusText = value
            ? "🔒 Orden trabado: las diapositivas ya no se mueven al arrastrarlas."
            : "🔓 Orden destrabado: arrastrá una diapositiva para cambiarla de lugar. Tocá el candado otra vez para trabarlo.";
    }

    [RelayCommand]
    private void ToggleSlidesOrderLock() => SlidesOrderLocked = !SlidesOrderLocked;

    /// <summary>Someone tried to drag a slide with the order locked: says how to unlock it.</summary>
    public void TellSlidesOrderIsLocked() =>
        StatusText = "🔒 El orden de las diapositivas está trabado. Para moverlas, tocá «🔒 Orden trabado» arriba de las diapositivas.";

    /// <summary>A song slide that could be moved if the order were unlocked.</summary>
    public bool IsSongSlide(SlideItemViewModel? item) => SectionOf(item) is not null;

    // ── Deshacer / rehacer ───────────────────────────────────────

    private readonly SongHistory _songHistory = new();

    [ObservableProperty]
    private bool _canUndoSongChange;

    [ObservableProperty]
    private bool _canRedoSongChange;

    [ObservableProperty]
    private string _undoSongHint = "Deshacer el último cambio de la canción (Ctrl+Z)";

    [ObservableProperty]
    private string _redoSongHint = "Rehacer lo que se deshizo (Ctrl+Y)";

    private void UpdateSongUndoState()
    {
        var id = SelectedSong?.Id ?? 0;
        CanUndoSongChange = _songHistory.CanUndo(id);
        CanRedoSongChange = _songHistory.CanRedo(id);
        UndoSongHint = _songHistory.NextUndo(id) is { } undo
            ? $"Deshacer: {undo} (Ctrl+Z)"
            : "No hay cambios para deshacer en esta canción (Ctrl+Z)";
        RedoSongHint = _songHistory.NextRedo(id) is { } redo
            ? $"Rehacer: {redo} (Ctrl+Y)"
            : "No hay nada para rehacer (Ctrl+Y)";
    }

    /// <summary>
    /// Keeps the step a change to the open song is about to save, so Ctrl+Z
    /// can take it back. Called after changing the song and before saving it;
    /// a change that left everything the same is not a step.
    /// </summary>
    private void RememberSongChange(SongSnapshot before, string what)
    {
        if (SelectedSong is not { Id: > 0 } song || before.SameAs(SongSnapshot.Of(song)))
            return;

        _songHistory.Record(song.Id, before, what);
    }

    [RelayCommand]
    private void UndoSongChange()
    {
        if (SelectedSong is not { Id: > 0 } song || _songHistory.Undo(song.Id, SongSnapshot.Of(song)) is not { } step)
        {
            StatusText = "No hay cambios para deshacer en esta canción.";
            return;
        }

        PutSongState(song, step.State);
        StatusText = $"↶ Se deshizo: {step.What}. Ctrl+Y lo vuelve a hacer.";
    }

    [RelayCommand]
    private void RedoSongChange()
    {
        if (SelectedSong is not { Id: > 0 } song || _songHistory.Redo(song.Id, SongSnapshot.Of(song)) is not { } step)
        {
            StatusText = "No hay nada para rehacer en esta canción.";
            return;
        }

        PutSongState(song, step.State);
        StatusText = $"↷ Se volvió a hacer: {step.What}.";
    }

    private void PutSongState(Song song, SongSnapshot state)
    {
        state.ApplyTo(song);
        NoteAction($"Guardar la canción «{song.Title}» (deshacer/rehacer)");
        var saved = _songs.Save(song);
        ReloadSongKeepingPlace(saved.Id);
        UpdateSongUndoState();
    }

    // ── Diapositiva nueva ────────────────────────────────────────

    /// <summary>
    /// A new slide written from scratch, after the one right-clicked (or at the
    /// end, from the bar over the slides). Before, the only way was to
    /// duplicate one and change its text.
    /// </summary>
    [RelayCommand]
    private void NewSlide(SlideItemViewModel? after)
    {
        if (SelectedSong is not { } song)
            return;

        var text = _quickTextEditor.Edit(
            string.Empty,
            "Nueva diapositiva",
            "Escribí la letra de la diapositiva nueva. Un renglón en blanco no la parte: queda todo en esta.");
        if (string.IsNullOrWhiteSpace(text))
            return;

        var before = SongSnapshot.Of(song);
        var at = SectionOf(after) is { } section
            ? song.Sections.FindIndex(s => s.Id == section.Id) + 1
            : song.Sections.Count;
        song.Sections.Insert(Math.Clamp(at, 0, song.Sections.Count),
            new SongSection { Label = "Nueva", Text = text.Trim() });
        ReindexAndSaveSong("Diapositiva nueva agregada.", before, "agregar una diapositiva nueva");
    }

    // ── Buscar en la letra ───────────────────────────────────────

    /// <summary>What is typed in «Buscar en la letra».</summary>
    [ObservableProperty]
    private string _lyricsSearchText = string.Empty;

    /// <summary>«2 de 3», «Ninguna», or nothing while the box is empty.</summary>
    [ObservableProperty]
    private string _lyricsSearchStatus = string.Empty;

    private List<SlideItemViewModel> _lyricsMatches = [];
    private int _lyricsMatchIndex = -1;

    /// <summary>Raised with the slide found, so the grid scrolls to it.</summary>
    public event EventHandler<SlideItemViewModel>? LyricsMatchFocused;

    partial void OnLyricsSearchTextChanged(string value) => RunLyricsSearch(keepPlace: false);

    /// <summary>Marks the slides that have the words and goes to the first one.</summary>
    private void RunLyricsSearch(bool keepPlace)
    {
        foreach (var slide in Slides)
        {
            slide.IsSearchMatch = false;
            slide.IsSearchCurrent = false;
        }

        var songSlides = Slides.Where(s => s.SectionId != 0).ToList();
        var found = LyricsSearch.Matches(songSlides.Select(s => s.PreviewText).ToList(), LyricsSearchText);
        _lyricsMatches = found.Select(i => songSlides[i]).ToList();

        if (string.IsNullOrWhiteSpace(LyricsSearchText))
        {
            _lyricsMatchIndex = -1;
            LyricsSearchStatus = string.Empty;
            return;
        }

        if (_lyricsMatches.Count == 0)
        {
            _lyricsMatchIndex = -1;
            LyricsSearchStatus = "Ninguna";
            return;
        }

        foreach (var match in _lyricsMatches)
            match.IsSearchMatch = true;

        _lyricsMatchIndex = keepPlace ? Math.Clamp(_lyricsMatchIndex, 0, _lyricsMatches.Count - 1) : 0;
        FocusLyricsMatch();
    }

    private void FocusLyricsMatch()
    {
        foreach (var match in _lyricsMatches)
            match.IsSearchCurrent = false;

        var current = _lyricsMatches[_lyricsMatchIndex];
        current.IsSearchCurrent = true;
        LyricsSearchStatus = $"{_lyricsMatchIndex + 1} de {_lyricsMatches.Count}";
        LyricsMatchFocused?.Invoke(this, current);
    }

    /// <summary>↓ or F3 in the box: the next slide that has the words (back to the first after the last).</summary>
    [RelayCommand]
    private void NextLyricsMatch()
    {
        if (_lyricsMatches.Count == 0)
            return;
        _lyricsMatchIndex = (_lyricsMatchIndex + 1) % _lyricsMatches.Count;
        FocusLyricsMatch();
    }

    [RelayCommand]
    private void PreviousLyricsMatch()
    {
        if (_lyricsMatches.Count == 0)
            return;
        _lyricsMatchIndex = (_lyricsMatchIndex - 1 + _lyricsMatches.Count) % _lyricsMatches.Count;
        FocusLyricsMatch();
    }

    /// <summary>Enter in the box: the slide found goes on the screen.</summary>
    [RelayCommand]
    private void ProjectLyricsMatch()
    {
        if (_lyricsMatchIndex < 0 || _lyricsMatchIndex >= _lyricsMatches.Count)
            return;

        var slide = _lyricsMatches[_lyricsMatchIndex];
        ProjectSlideCommand.Execute(slide);
    }

    /// <summary>Esc in the box: the search is cleared.</summary>
    [RelayCommand]
    private void ClearLyricsSearch() => LyricsSearchText = string.Empty;

    /// <summary>
    /// After the grid is filled again: a new song starts with an empty box; the
    /// same song (saved, or its lines per slide changed) keeps the search.
    /// </summary>
    private int _lyricsSearchSongId;

    private void AfterSongSlidesBuilt()
    {
        UpdateSongUndoState();
        var id = SelectedSong?.Id ?? 0;
        if (id != _lyricsSearchSongId)
        {
            _lyricsSearchSongId = id;
            _lyricsMatchIndex = -1;
            if (LyricsSearchText.Length > 0)
            {
                LyricsSearchText = string.Empty; // runs the search, which clears the marks
                return;
            }
        }

        RunLyricsSearch(keepPlace: true);
    }
}
