using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EcclesiaCast.App.Remote;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Bible;
using EcclesiaCast.Core.Diagnostics;
using EcclesiaCast.Core.Displays;
using EcclesiaCast.Core.Logos;
using EcclesiaCast.Core.Media;
using EcclesiaCast.Core.Performance;
using EcclesiaCast.Core.Playlists;
using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Core.Songs;
using EcclesiaCast.Core.Stage;
using EcclesiaCast.Core.Timing;
using EcclesiaCast.Core.Themes;
using EcclesiaCast.Data.Persistence;
using Serilog;

namespace EcclesiaCast.App.ViewModels;

public sealed partial class MainViewModel
{
    // ── Playlist del servicio ────────────────────────────────────

    public ObservableCollection<Playlist> Playlists { get; } = [];
    public ObservableCollection<PlaylistItem> PlaylistItems { get; } = [];

    [ObservableProperty]
    private Playlist? _selectedPlaylist;

    [ObservableProperty]
    private PlaylistItem? _selectedPlaylistItem;

    /// <summary>Index in <see cref="PlaylistItems"/> of the item currently driving the grid; -1 if none.</summary>
    private int _currentPlaylistIndex = -1;

    /// <summary>True while OpenPlaylistItem loads content, so trackers aren't reset.</summary>
    private bool _openingPlaylistItem;

    private void LoadPlaylists(int? keepId)
    {
        keepId ??= SelectedPlaylist?.Id;
        Playlists.Clear();
        foreach (var playlist in _playlists.GetAll())
            Playlists.Add(playlist);
        SelectedPlaylist = Playlists.FirstOrDefault(p => p.Id == keepId) ?? Playlists.FirstOrDefault();
    }

    partial void OnSelectedPlaylistChanged(Playlist? value)
    {
        PlaylistItems.Clear();

        if (value is null)
            return;

        foreach (var item in value.Items.OrderBy(i => i.Order))
            PlaylistItems.Add(item);
    }

    /// <summary>
    /// Writes what the program is about to do on the trail the watchdog reads,
    /// so a freeze in the log says what it was busy with.
    /// </summary>
    private static void NoteAction(string what) => ActionTrail.Shared.Note(what);

    private void SavePlaylist()
    {
        if (SelectedPlaylist is null)
            return;

        NoteAction($"Guardar la playlist «{SelectedPlaylist.Name}»");

        for (var i = 0; i < PlaylistItems.Count; i++)
            PlaylistItems[i].Order = i;
        SelectedPlaylist.Items = PlaylistItems.ToList();
        var saved = _playlists.Save(SelectedPlaylist);
        LoadPlaylists(saved.Id);
    }

    [RelayCommand]
    private void NewPlaylist()
    {
        var name = _textPrompt.Ask("Nueva playlist", "Nombre (ej. Domingo 20/7):");
        if (string.IsNullOrWhiteSpace(name))
            return;
        var saved = _playlists.Save(new Playlist { Name = name.Trim() });
        LoadPlaylists(saved.Id);
        StatusText = $"Playlist \"{saved.Name}\" creada.";
    }

    [RelayCommand]
    private void RenamePlaylist()
    {
        if (SelectedPlaylist is null)
            return;

        var name = _textPrompt.Ask("Renombrar playlist", "Nuevo nombre:", SelectedPlaylist.Name);
        if (string.IsNullOrWhiteSpace(name))
            return;
        SelectedPlaylist.Name = name.Trim();
        SavePlaylist();
    }

    [RelayCommand]
    private void DuplicatePlaylist()
    {
        if (SelectedPlaylist is null)
            return;

        var copy = new Playlist
        {
            Name = $"{SelectedPlaylist.Name} (copia)",
            Items = SelectedPlaylist.Items
                .Select(i => new PlaylistItem
                {
                    Order = i.Order, Type = i.Type, Caption = i.Caption,
                    SongId = i.SongId, BibleVersionId = i.BibleVersionId,
                    BookNumber = i.BookNumber, Chapter = i.Chapter,
                    VerseStart = i.VerseStart, VerseEnd = i.VerseEnd, MediaId = i.MediaId,
                })
                .ToList(),
        };
        var saved = _playlists.Save(copy);
        LoadPlaylists(saved.Id);
        StatusText = $"Playlist duplicada: \"{saved.Name}\".";
    }

    [RelayCommand]
    private void DeletePlaylist()
    {
        if (SelectedPlaylist is null)
            return;
        var confirm = MessageBox.Show(
            $"¿Eliminar la playlist \"{SelectedPlaylist.Name}\"?",
            "EcclesiaCast", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes)
            return;
        _playlists.Delete(SelectedPlaylist.Id);
        SelectedPlaylist = null;
        LoadPlaylists(null);
        StatusText = "Playlist eliminada.";
    }

    private bool EnsurePlaylist()
    {
        if (SelectedPlaylist is not null)
            return true;
        NewPlaylist();
        return SelectedPlaylist is not null;
    }

    [RelayCommand]
    private void AddSongToPlaylist(Song? song) => InsertSongInPlaylist(song, PlaylistItems.Count);

    /// <summary>A song dragged from the list lands where it was dropped.</summary>
    public void InsertSongInPlaylist(Song? song, int index)
    {
        if (song is null || !EnsurePlaylist())
            return;

        index = Math.Clamp(index, 0, PlaylistItems.Count);
        if (index <= _currentPlaylistIndex)
            _currentPlaylistIndex++;

        PlaylistItems.Insert(index, new PlaylistItem
        {
            Type = PlaylistItemType.Song,
            Caption = string.IsNullOrWhiteSpace(song.Artist) ? song.Title : $"{song.Title} — {song.Artist}",
            SongId = song.Id,
        });
        SavePlaylist();
        StatusText = $"\"{song.Title}\" agregada a la playlist.";
    }

    // ── The ➕ Playlist box of the Bible panel ──

    /// <summary>The little box that asks which passage goes to the playlist.</summary>
    [ObservableProperty]
    private bool _isAddPassageOpen;

    /// <summary>The reference in the box; starts with what the ➕ would have added.</summary>
    [ObservableProperty]
    private string _addPassageText = string.Empty;

    /// <summary>«Juan 8:5-10 · RVR1960», or what is wrong with the text.</summary>
    [ObservableProperty]
    private string _addPassageHeading = string.Empty;

    /// <summary>The words of the passage, so it can be checked before it is added.</summary>
    [ObservableProperty]
    private string _addPassagePreview = string.Empty;

    [ObservableProperty]
    private bool _canAddPassage;

    partial void OnAddPassageTextChanged(string value) => UpdateAddPassagePreview();

    /// <summary>
    /// What the ➕ proposes, in order: a range typed in the search box
    /// («juan 8 5-10»), else the verse on screen (or marked as next), else a
    /// single verse typed, else the whole chapter. Null when there is nothing
    /// to propose.
    /// </summary>
    private BibleReference? DefaultPassageToAdd()
    {
        var typed = BibleReferenceParser.TryParse(BibleQuery);
        if (typed is { VerseStart: int start, VerseEnd: int end } && end > start)
            return typed;

        if (_currentPassage is null)
            return typed;

        var shown = BibleSlideAt(LiveSlideIndex) ?? BibleSlideAt(PreviewSlideIndex);
        if (shown is not null)
            return new BibleReference(_currentPassage.BookNumber, shown.Chapter, shown.FirstVerse, shown.LastVerse);
        if (typed?.VerseStart is not null)
            return typed;
        return _currentPassage with { VerseStart = null, VerseEnd = null };
    }

    [RelayCommand]
    private void OpenAddPassage()
    {
        if (PrimaryVersion is null)
        {
            StatusText = "Primero elegí una versión de la Biblia.";
            return;
        }

        var proposal = DefaultPassageToAdd();
        // Setting the same text again would not refresh the preview (the
        // version may have changed since the box was last open).
        AddPassageText = proposal is null ? string.Empty : BibleReferenceFormatter.Format(proposal);
        UpdateAddPassagePreview();
        IsAddPassageOpen = true;
    }

    private void UpdateAddPassagePreview()
    {
        CanAddPassage = false;
        AddPassagePreview = string.Empty;

        if (string.IsNullOrWhiteSpace(AddPassageText))
        {
            AddPassageHeading = "Escribí una cita, por ejemplo «juan 8 del 5 al 10».";
            return;
        }

        var reference = BibleReferenceParser.TryParse(AddPassageText);
        if (reference is null)
        {
            AddPassageHeading = "No se entiende la cita. Probá con «juan 8 del 5 al 10» o «sal 23».";
            return;
        }

        var version = PrimaryVersion;
        if (version is null)
            return;

        var heading = $"{BibleReferenceFormatter.Format(reference)} · {version.Abbreviation}";
        var verses = BibleTextCleaner.ForList(_bibles.GetPassage(version.Id, reference), ShowSelahAndTitles);
        if (verses.Count == 0)
        {
            AddPassageHeading = $"{heading} — no existe en esta versión.";
            return;
        }

        var text = string.Join("  ", verses.Select(v => $"{v.Verse} {v.Text.Trim()}"));
        const int limit = 900;
        if (text.Length > limit)
            text = text[..limit].TrimEnd() + "…";

        AddPassageHeading = heading;
        AddPassagePreview = text;
        CanAddPassage = true;
    }

    [RelayCommand]
    private void ConfirmAddPassage()
    {
        var reference = BibleReferenceParser.TryParse(AddPassageText);
        if (reference is null || !CanAddPassage)
            return;

        if (AddPassageToPlaylist(reference))
            IsAddPassageOpen = false;
    }

    /// <summary>Right click on a verse card: just that verse.</summary>
    [RelayCommand]
    private void AddVerseSlideToPlaylist(SlideItemViewModel? slide)
    {
        if (slide?.Verses is { } verses && _currentPassage is not null)
            AddPassageToPlaylist(new BibleReference(_currentPassage.BookNumber, verses.Chapter, verses.FirstVerse, verses.LastVerse));
    }

    /// <summary>
    /// Right click on a verse card → "Corregir el texto…": a typo in the Bible
    /// itself. What opens is the stored text of the main version, the one the
    /// card was made from, and the fix is saved there for good. A translation
    /// that tells several verses as one paragraph keeps that paragraph under
    /// each of them, so the same fix goes to all of the card's verses.
    /// </summary>
    [RelayCommand]
    private void CorrectVerseText(SlideItemViewModel? slide)
    {
        if (slide?.Verses is not { } group || _currentPassage is null || PrimaryVersion is not { } version)
            return;

        var stored = _bibles.GetPassage(
            version.Id, new BibleReference(_currentPassage.BookNumber, group.Chapter, group.FirstVerse, group.LastVerse));
        var texts = stored.Select(v => v.Text.Trim()).Distinct().ToList();
        if (texts.Count == 0)
            return;

        // The card can bring together verses that only the second version
        // repeats; saving one text over all of them would flatten the main one.
        if (texts.Count > 1)
        {
            StatusText = $"En {version.Abbreviation} estos versículos se guardan con textos distintos: corregí de a uno.";
            return;
        }

        var reference = $"{stored[0].BookName} {group.Chapter}:{group.Numbers}";
        var message = SecondaryVersion is null
            ? $"Texto guardado de {reference} en {version.Abbreviation}. Lo que cambies queda en la Biblia, no sólo en esta diapositiva."
            : $"Se corrige la versión principal ({version.Abbreviation}), no la segunda. Texto guardado de {reference}; lo que cambies queda en la Biblia, no sólo en esta diapositiva.";
        var edited = _quickTextEditor.Edit(texts[0], "Corregir el texto", message);
        if (edited is null || edited == texts[0])
            return;

        if (edited.Length == 0)
        {
            StatusText = "El texto no puede quedar vacío.";
            return;
        }

        NoteAction($"Corregir el texto de {reference} ({version.Abbreviation})");
        _bibles.UpdateVerseText(version.Id, _currentPassage.BookNumber, group.Chapter, group.FirstVerse, group.LastVerse, edited);
        var passage = _currentPassage;
        RebuildSlidesPreservingLive(() => LoadBiblePassage(passage));
        StatusText = $"Texto de {reference} corregido en {version.Abbreviation}.";
    }

    /// <summary>Right click on a verse card: from the verse on screen (or marked) to this one.</summary>
    [RelayCommand]
    private void AddRangeToSlideToPlaylist(SlideItemViewModel? slide)
    {
        if (_currentPassage is not null && RangeFromLive(slide) is var (first, last))
            AddPassageToPlaylist(new BibleReference(_currentPassage.BookNumber, slide!.Verses!.Chapter, first, last));
    }

    /// <summary>
    /// The verses between the one on screen and <paramref name="slide"/>, in
    /// either direction; null when there is no verse on screen in this
    /// chapter or it is this same one. Feeds the "from there to here" item.
    /// </summary>
    public (int First, int Last)? RangeFromLive(SlideItemViewModel? slide)
    {
        var live = BibleSlideAt(LiveSlideIndex) ?? BibleSlideAt(PreviewSlideIndex);
        if (slide?.Verses is not { } here || live is null || live.Chapter != here.Chapter
            || live.FirstVerse == here.FirstVerse)
            return null;

        return (Math.Min(live.FirstVerse, here.FirstVerse), Math.Max(live.LastVerse, here.LastVerse));
    }

    /// <summary>The verses behind the card at an index, if it is a Bible card.</summary>
    private VerseGroup? BibleSlideAt(int index) =>
        index >= 0 && index < Slides.Count ? Slides[index].Verses : null;

    /// <summary>A passage's name as the playlist shows it: «Juan 8:5-10 (RVR1960)».</summary>
    private string PassageCaption(BibleReference reference)
    {
        var bookName = BibleBookCatalog.FindByNumber(reference.BookNumber)?.Name ?? $"Libro {reference.BookNumber}";
        var verses = reference.VerseStart is int vs
            ? reference.VerseEnd is int ve && ve != vs ? $":{vs}-{ve}" : $":{vs}"
            : string.Empty;
        var version = PrimaryVersion is { } primary ? $" ({primary.Abbreviation})" : string.Empty;
        return $"{bookName} {reference.Chapter}{verses}{version}";
    }

    private bool AddPassageToPlaylist(BibleReference reference)
    {
        if (PrimaryVersion is null || !EnsurePlaylist())
            return false;

        if (_bibles.GetPassage(PrimaryVersion.Id, reference).Count == 0)
        {
            StatusText = $"{PassageCaption(reference)} no existe en esta versión.";
            return false;
        }

        var caption = PassageCaption(reference);
        PlaylistItems.Add(new PlaylistItem
        {
            Type = PlaylistItemType.BiblePassage,
            Caption = caption,
            BibleVersionId = PrimaryVersion.Id,
            BookNumber = reference.BookNumber,
            Chapter = reference.Chapter,
            VerseStart = reference.VerseStart,
            VerseEnd = reference.VerseEnd,
        });
        SavePlaylist();
        StatusText = $"«{caption}» agregado a la playlist.";
        return true;
    }

    /// <summary>Right click → «Agregar a la playlist»: the whole selection when the card belongs to it.</summary>
    [RelayCommand]
    private void AddMediaToPlaylist(MediaItem? item) =>
        InsertMediaInPlaylist(MediaTargets(item), PlaylistItems.Count);

    /// <summary>A media dragged from the bar lands where it was dropped.</summary>
    public void InsertMediaInPlaylist(MediaItem? item, int index) =>
        InsertMediaInPlaylist(item is null ? [] : [item], index);

    /// <summary>
    /// Several media dropped together keep the order the bar shows them in and
    /// go in with a single save: the playlist is rebuilt on every save, and a
    /// tab of announcements is a dozen pages.
    /// </summary>
    public void InsertMediaInPlaylist(IReadOnlyList<MediaItem> items, int index)
    {
        if (items.Count == 0 || !EnsurePlaylist())
            return;

        index = Math.Clamp(index, 0, PlaylistItems.Count);
        if (index <= _currentPlaylistIndex)
            _currentPlaylistIndex += items.Count;

        for (var i = 0; i < items.Count; i++)
        {
            PlaylistItems.Insert(index + i, new PlaylistItem
            {
                Type = PlaylistItemType.Media,
                Caption = items[i].Name,
                MediaId = items[i].Id,
            });
        }

        SavePlaylist();
        StatusText = items.Count == 1
            ? $"\"{items[0].Name}\" agregado a la playlist."
            : $"{items.Count} medios agregados a la playlist.";
    }

    /// <summary>
    /// Media dragged onto another tab of the bar move there: the tab is just
    /// the media's category. The files themselves stay where they are.
    /// </summary>
    public void MoveMediaToTab(MediaItem? item, string tab) =>
        MoveMediaToTab(item is null ? [] : [item], tab);

    public void MoveMediaToTab(IReadOnlyList<MediaItem> items, string tab)
    {
        if (string.IsNullOrWhiteSpace(tab))
            return;

        var moving = items
            .Where(m => !string.Equals(m.Category, tab, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (moving.Count == 0)
            return;

        NoteAction(moving.Count == 1
            ? $"Mover el medio «{moving[0].Name}» a la pestaña «{tab}»"
            : $"Mover {moving.Count} medios a la pestaña «{tab}»");
        foreach (var item in moving)
        {
            item.Category = tab;
            _media.Update(item);
        }

        LoadMedia();
        StatusText = moving.Count == 1
            ? $"«{moving[0].Name}» pasó a la pestaña «{tab}»."
            : $"{moving.Count} medios pasaron a la pestaña «{tab}».";
    }

    [RelayCommand]
    private void RemovePlaylistItem(PlaylistItem? item)
    {
        if (item is null)
            return;

        // The arrows walk the playlist from the item in use: removing one
        // above it shifts it up, and removing that very one leaves none.
        var index = PlaylistItems.IndexOf(item);
        if (index >= 0 && index < _currentPlaylistIndex)
            _currentPlaylistIndex--;
        else if (index == _currentPlaylistIndex)
            _currentPlaylistIndex = -1;

        PlaylistItems.Remove(item);
        SavePlaylist();
        StatusText = $"«{item.Caption}» quitado de la playlist.";
    }

    [RelayCommand]
    private void MovePlaylistItemUp(PlaylistItem? item)
    {
        if (item is null)
            return;
        var index = PlaylistItems.IndexOf(item);
        if (index <= 0)
            return;
        PlaylistItems.Move(index, index - 1);
        SavePlaylist();
        SelectedPlaylistItem = PlaylistItems.FirstOrDefault(i => i.Caption == item.Caption && i.Order == index - 1);
    }

    [RelayCommand]
    private void MovePlaylistItemDown(PlaylistItem? item)
    {
        if (item is null)
            return;
        var index = PlaylistItems.IndexOf(item);
        if (index < 0 || index >= PlaylistItems.Count - 1)
            return;
        PlaylistItems.Move(index, index + 1);
        SavePlaylist();
    }

    /// <summary>
    /// One click: loads the item into the operator without projecting it. A
    /// media item is only shown in the centre, since applying it would put it
    /// on the screen.
    /// </summary>
    [RelayCommand]
    private void OpenPlaylistItem(PlaylistItem? item) => LoadPlaylistItem(item, projectMedia: false);

    /// <summary>
    /// Loads a playlist item. <paramref name="projectMedia"/> says whether a
    /// media item goes on screen (double click, arrows) or just to the centre.
    /// </summary>
    /// <returns>
    /// False when there was nothing to load (the song, media or presentation
    /// is gone): the grid still holds the previous item, so the caller must
    /// not project from it. The entry is still the current one, so the arrows
    /// go on past it.
    /// </returns>
    private bool LoadPlaylistItem(PlaylistItem? item, bool projectMedia)
    {
        if (item is null)
            return false;

        bool loaded;
        _openingPlaylistItem = true;
        try
        {
            loaded = OpenPlaylistItemCore(item, projectMedia);
        }
        finally
        {
            _openingPlaylistItem = false;
        }

        _currentPlaylistIndex = PlaylistItems.IndexOf(item);
        SelectedPlaylistItem = item;
        return loaded;
    }

    private bool OpenPlaylistItemCore(PlaylistItem item, bool projectMedia)
    {
        switch (item.Type)
        {
            case PlaylistItemType.Song:
                IsBibleTabActive = false;
                var song = Songs.FirstOrDefault(s => s.Id == item.SongId);

                // The song list may be filtered by whatever was searched last;
                // the playlist is what the operator is following now, so drop
                // the filter rather than report the song as missing.
                if (song is null && !string.IsNullOrEmpty(SearchText))
                {
                    SearchText = string.Empty;
                    song = Songs.FirstOrDefault(s => s.Id == item.SongId);
                }

                if (song is null)
                {
                    StatusText = $"La canción \"{item.Caption}\" ya no está en la biblioteca.";
                    return false;
                }

                // Choosing the song that is already selected changes nothing,
                // so its slides have to be rebuilt by hand.
                if (ReferenceEquals(SelectedSong, song))
                    BuildSongSlides();
                else
                    SelectedSong = song;
                break;

            case PlaylistItemType.BiblePassage:
                IsBibleTabActive = true;
                var option = BibleVersionOptions.FirstOrDefault(o => o.Info.Id == item.BibleVersionId);
                if (option is not null && PrimaryVersion?.Id != option.Info.Id)
                    SelectSoloVersion(option);
                if (item.BookNumber is int book && item.Chapter is int chapter)
                {
                    SelectedBibleBook = BibleBooksAvailable.FirstOrDefault(b => b.Number == book);
                    LoadBiblePassage(new BibleReference(book, chapter, null, null));
                    if (item.VerseStart is int verse)
                    {
                        var index = FindVerseSlide(chapter, verse);
                        SetPreviewIndex(index);
                    }
                }
                break;

            case PlaylistItemType.Media:
                var media = _allMedia.FirstOrDefault(m => m.Id == item.MediaId);
                if (media is null)
                {
                    StatusText = $"El medio \"{item.Caption}\" ya no está en la biblioteca.";
                    return false;
                }

                // A saved page opens in the web panel; the double click (or the
                // arrows) also put it live. Nothing else of it goes to the centre.
                if (media.Type == MediaType.Web)
                {
                    OpenWebPage(media.Path, media.Name);
                    if (projectMedia)
                        StartWebLive(media.Path, media.Name);
                }
                else if (projectMedia)
                    ProjectMedia(media);
                else
                    ShowMediaInCentre(media);
                break;

            case PlaylistItemType.Presentation:
                // The entry remembers the first page; the tab is wherever that
                // page is now (the tab may have been renamed since), or the
                // name written when it was added. The page may also have been
                // moved to a tab that is not a presentation: then the name
                // written is the one to go by. A tab is never marked here —
                // someone who unmarked it meant it.
                var pageTab = _allMedia.FirstOrDefault(m => m.Id == item.MediaId)?.Category;
                var tab = IsPresentationTab(pageTab) ? pageTab! : item.Caption;
                if (!IsPresentationTab(tab))
                {
                    StatusText = $"«{item.Caption}» ya no es una presentación. Para volver a usarla: clic derecho en su pestaña → «Usar como presentación y abrirla».";
                    return false;
                }

                if (!OpenPresentation(tab, null))
                {
                    StatusText = $"La presentación \"{item.Caption}\" ya no tiene páginas en la biblioteca.";
                    return false;
                }
                break;
        }

        return true;
    }

    /// <summary>
    /// Puts a media of the playlist in the centre, the way a song shows its
    /// slides: the operator sees what comes before it is on the screen.
    /// </summary>
    private void ShowMediaInCentre(MediaItem media)
    {
        // Leaving the song selected would make clicking it again do nothing,
        // since the grid only rebuilds when the selection changes.
        SelectedSong = null;
        Slides.Clear();
        LiveSlideIndex = -1;
        PreviewSlideIndex = -1;
        _currentPassage = null;
        PreviewSlide = null;

        IsWebPanelOpen = false;
        LeavePresentation();
        PreviewMedia = media;
        GridTitle = media.Name;
        StatusText = $"«{media.Name}» listo: doble clic en la playlist (o el botón Proyectar) lo pone en pantalla.";
    }

    /// <summary>Puts a media on screen and names it above the grid when it takes the whole screen.</summary>
    private void ProjectMedia(MediaItem media)
    {
        ApplyBackground(media);

        // A page of a presentation already named the screen after its tab
        // (the red border and the phone mark follow that title).
        var isPage = media.Type is MediaType.Image or MediaType.Video && IsPresentationTab(media.Category);
        if (media.Behavior == MediaBehavior.Foreground && !isPage)
            LiveTitle = media.Name;
    }

    /// <summary>The «Proyectar» button under a media shown in the centre.</summary>
    [RelayCommand]
    private void ProjectPreviewMedia()
    {
        if (PreviewMedia is { } media)
            ProjectMedia(media);
    }

    /// <summary>Double-click: load the item AND put it live.</summary>
    [RelayCommand]
    private void ProjectPlaylistItem(PlaylistItem? item)
    {
        if (item is null)
            return;

        // The grid still holds the previous item when this one is gone: its
        // first slide must not go up in its place.
        if (!LoadPlaylistItem(item, projectMedia: true))
            return;

        if (item.Type is PlaylistItemType.Song or PlaylistItemType.BiblePassage or PlaylistItemType.Presentation)
        {
            if (item.Type == PlaylistItemType.BiblePassage && PreviewSlideIndex >= 0)
                GoLiveSlide(PreviewSlideIndex);
            else if (Slides.FirstOrDefault(s => s.JumpTarget is null) is { } first)
                GoLiveSlide(first.Index);
        }
    }
}
