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
using EcclesiaCast.Data;
using EcclesiaCast.Data.Persistence;
using Serilog;

namespace EcclesiaCast.App.ViewModels;

public sealed partial class MainViewModel
{
    // ── Pestañas de biblioteca ───────────────────────────────────

    [RelayCommand]
    private void ShowSongsTab() => IsBibleTabActive = false;

    [RelayCommand]
    private void ShowBibleTab() => IsBibleTabActive = true;

    // ── Canciones ────────────────────────────────────────────────

    private const string SongMaxLinesKey = "songs.maxLinesPerSlide";

    /// <summary>
    /// The most lines a slide of a song shows (1 to 4; 0 is all of the block).
    /// The song keeps its blocks whole: only the slides are cut, so the editor,
    /// the stream and the stage all see the same pieces.
    /// </summary>
    [ObservableProperty]
    private int _songMaxLines;

    /// <summary>The grid holds a song: the only case where the lines-per-slide choice applies.</summary>
    [ObservableProperty]
    private bool _isSongOpen;

    private void LoadSongMaxLines()
    {
        // The field, not the property: nothing is open yet to rebuild.
        _songMaxLines = int.TryParse(_settings.Get(SongMaxLinesKey), out var lines)
            ? Math.Clamp(lines, 0, SlideSplitter.MaxLinesLimit)
            : 0;
    }

    partial void OnSongMaxLinesChanged(int value)
    {
        _settings.Set(SongMaxLinesKey, value.ToString());
        if (SelectedSong is null || !IsSongOpen)
            return;

        // A song in the grid is cut again, keeping the playlist place and the live slide.
        var wasOpening = _openingPlaylistItem;
        _openingPlaylistItem = true;
        try
        {
            RebuildSlidesPreservingLive(BuildSongSlides);
        }
        finally
        {
            _openingPlaylistItem = wasOpening;
        }

        StatusText = value == 0
            ? "Las canciones se ven con todas sus líneas por diapositiva."
            : $"Las canciones se parten en diapositivas de hasta {value} {(value == 1 ? "línea" : "líneas")}.";
    }

    partial void OnSearchTextChanged(string value) => LoadSongs();

    partial void OnSelectedSongChanged(Song? value) => BuildSongSlides();

    private void LoadSongs()
    {
        var keepId = SelectedSong?.Id;
        Songs.Clear();
        foreach (var song in _songs.Search(SearchText))
            Songs.Add(song);
        SelectedSong = Songs.FirstOrDefault(s => s.Id == keepId) ?? SelectedSong;
    }

    private void BuildSongSlides()
    {
        NoteAction($"Abrir la canción «{SelectedSong?.Title}»");
        Slides.Clear();
        LiveSlideIndex = -1;
        PreviewSlideIndex = -1;
        _currentPassage = null;
        IsWebPanelOpen = false;
        PreviewMedia = null;
        GridTitle = null;
        LeavePresentation();
        if (!_openingPlaylistItem)
            _currentPlaylistIndex = -1;

        if (SelectedSong is null)
            return;

        var theme = ResolveSongTheme(SelectedSong);
        var caption = string.IsNullOrWhiteSpace(SelectedSong.Artist)
            ? SelectedSong.Title
            : $"{SelectedSong.Title} — {SelectedSong.Artist}";
        GridTitle = caption;

        foreach (var section in SelectedSong.Sections)
        {
            foreach (var piece in SlideSplitter.Split(section.Text, SongMaxLines))
            {
                Slides.Add(new SlideItemViewModel(
                    Slides.Count,
                    section.Label,
                    new SlideContent(piece, caption, Theme: theme, Override: section.GetOverride()),
                    sectionId: section.Id));
            }
        }

        PreviewSlide = Slides.FirstOrDefault()?.Slide;
    }

    private SongSection? SectionOf(SlideItemViewModel? item) =>
        item is null || SelectedSong is null || item.SectionId == 0
            ? null
            : SelectedSong.Sections.FirstOrDefault(s => s.Id == item.SectionId);

    /// <summary>Opens the full-song ProPresenter-style designer (right-click → Editar canción / Diseñar).</summary>
    [RelayCommand]
    private void EditSongDesign(SlideItemViewModel? item)
    {
        if (SelectedSong is null || (item is not null && item.SectionId == 0))
        {
            StatusText = "El diseño es para canciones; la Biblia usa su tema global (🎨 Temas).";
            return;
        }

        var index = item is not null
            ? SelectedSong.Sections.FindIndex(s => s.Id == item.SectionId)
            : 0;
        if (index < 0)
            index = 0;

        var theme = ResolveSongTheme(SelectedSong);
        if (!_songDesigner.Edit(SelectedSong, theme, index))
            return;

        NoteAction($"Guardar la canción «{SelectedSong.Title}»");
        var saved = _songs.Save(SelectedSong);
        RebuildSlidesPreservingLive(() =>
        {
            LoadSongs();
            SelectedSong = Songs.FirstOrDefault(s => s.Id == saved.Id);
        });
        StatusText = "Diseño de la canción guardado.";
    }

    /// <summary>Right-click → "Editar como texto plano": the lyrics editor.</summary>
    [RelayCommand]
    private void EditSongAsText(SlideItemViewModel? item)
    {
        if (SelectedSong is not null)
            EditSong();
    }

    /// <summary>Right-click on a slide → "Edición rápida": corrects just this slide's text.</summary>
    [RelayCommand]
    private void QuickEditSlide(SlideItemViewModel? item)
    {
        var section = SectionOf(item);
        if (section is null)
            return;

        var edited = _quickTextEditor.Edit(section.Text);
        if (edited is null || edited == section.Text)
            return;

        if (edited.Length == 0)
        {
            StatusText = "El texto no puede quedar vacío.";
            return;
        }

        section.Text = edited;
        NoteAction($"Guardar la canción «{SelectedSong!.Title}»");
        var saved = _songs.Save(SelectedSong!);
        RebuildSlidesPreservingLive(() =>
        {
            LoadSongs();
            SelectedSong = Songs.FirstOrDefault(s => s.Id == saved.Id);
        });
        StatusText = "Texto de la diapositiva corregido.";
    }

    // ── Copiar / pegar / duplicar / eliminar diapositivas ────────

    [RelayCommand]
    private void CopySlide(SlideItemViewModel? item)
    {
        var section = SectionOf(item);
        if (section is null)
            return;
        _clipboardSlide = (section.Label, section.Text, section.StyleJson);
        StatusText = "Diapositiva copiada.";
    }

    [RelayCommand]
    private void PasteSlide(SlideItemViewModel? item)
    {
        if (_clipboardSlide is null || SelectedSong is null)
            return;

        var at = item is not null
            ? SelectedSong.Sections.FindIndex(s => s.Id == item.SectionId) + 1
            : SelectedSong.Sections.Count;
        if (at <= 0)
            at = SelectedSong.Sections.Count;

        var c = _clipboardSlide.Value;
        SelectedSong.Sections.Insert(at,
            new SongSection { Label = c.Label, Text = c.Text, StyleJson = c.StyleJson });
        ReindexAndSaveSong("Diapositiva pegada.");
    }

    [RelayCommand]
    private void DuplicateSlide(SlideItemViewModel? item)
    {
        var section = SectionOf(item);
        if (section is null || SelectedSong is null)
            return;

        var at = SelectedSong.Sections.FindIndex(s => s.Id == section.Id) + 1;
        SelectedSong.Sections.Insert(at,
            new SongSection { Label = section.Label, Text = section.Text, StyleJson = section.StyleJson });
        ReindexAndSaveSong("Diapositiva duplicada.");
    }

    [RelayCommand]
    private void DeleteSlide(SlideItemViewModel? item)
    {
        var section = SectionOf(item);
        if (section is null || SelectedSong is null)
            return;

        if (SelectedSong.Sections.Count <= 1)
        {
            StatusText = "La canción debe tener al menos una diapositiva.";
            return;
        }

        SelectedSong.Sections.RemoveAll(s => s.Id == section.Id);
        ReindexAndSaveSong("Diapositiva eliminada.");
    }

    /// <summary>
    /// Drag and drop in the slide grid: the dragged slide takes the place of
    /// the one it was dropped on, and the song keeps the new order. Only song
    /// slides move — a chapter's verses have an order of their own.
    /// </summary>
    /// <summary>
    /// Puts a dragged slide in the gap before the slide at <paramref name="gap"/>
    /// (0 = first, the number of slides = after the last), the way a word is
    /// dropped between two others: nothing else trades places.
    /// </summary>
    public void MoveSlideTo(SlideItemViewModel dragged, int gap)
    {
        var from = SectionOf(dragged);
        if (from is null || SelectedSong is null)
            return;

        var sections = SelectedSong.Sections;
        var fromIndex = sections.FindIndex(s => s.Id == from.Id);
        if (fromIndex < 0)
            return;

        // Taking it out shifts every gap after it one place to the left.
        var toIndex = Math.Clamp(gap > fromIndex ? gap - 1 : gap, 0, sections.Count - 1);
        if (toIndex == fromIndex)
            return;

        sections.RemoveAt(fromIndex);
        sections.Insert(toIndex, from);
        ReindexAndSaveSong($"«{from.Label}» pasó al lugar {toIndex + 1}.");
    }

    /// <summary>True for slides that can be dragged to a new place.</summary>
    public bool CanMoveSlide(SlideItemViewModel? item) => SectionOf(item) is not null;

    private void ReindexAndSaveSong(string status)
    {
        if (SelectedSong is null)
            return;

        for (var i = 0; i < SelectedSong.Sections.Count; i++)
            SelectedSong.Sections[i].Order = i;

        NoteAction($"Guardar la canción «{SelectedSong.Title}»");
        var saved = _songs.Save(SelectedSong);
        LoadSongs();
        SelectedSong = Songs.FirstOrDefault(s => s.Id == saved.Id);
        StatusText = status;
    }

    // ── Menú contextual de la lista de canciones ─────────────────

    [RelayCommand]
    private void EditSongDesignFor(Song? song)
    {
        if (song is null)
            return;
        SelectedSong = Songs.FirstOrDefault(s => s.Id == song.Id) ?? song;
        EditSongDesign(null);
    }

    [RelayCommand]
    private void EditSongTextFor(Song? song)
    {
        if (song is null)
            return;
        SelectedSong = Songs.FirstOrDefault(s => s.Id == song.Id) ?? song;
        EditSong();
    }

    [RelayCommand]
    private void DuplicateSong(Song? song)
    {
        var source = song is null ? null : _songs.Get(song.Id);
        if (source is null)
            return;

        var copy = new Song
        {
            Title = $"{source.Title} (copia)",
            Artist = source.Artist,
            Copyright = source.Copyright,
            ThemeId = source.ThemeId,
            Sections = source.Sections
                .Select(s => new SongSection { Order = s.Order, Label = s.Label, Text = s.Text, StyleJson = s.StyleJson })
                .ToList(),
        };

        NoteAction($"Guardar la canción «{copy.Title}»");
        var saved = _songs.Save(copy);
        LoadSongs();
        SelectedSong = Songs.FirstOrDefault(s => s.Id == saved.Id);
        StatusText = $"Canción duplicada: \"{saved.Title}\".";
    }

    [RelayCommand]
    private void DeleteSongFor(Song? song)
    {
        if (song is null)
            return;
        SelectedSong = Songs.FirstOrDefault(s => s.Id == song.Id) ?? song;
        DeleteSong();
    }

    [RelayCommand]
    private void NewSong()
    {
        var created = _songEditor.Edit(null);
        if (created is null)
            return;

        NoteAction($"Guardar la canción «{created.Title}»");
        var outcome = _songSaver.Begin().Save(created);
        if (outcome.Song is not { } saved)
        {
            StatusText = $"No se guardó «{created.Title}»: ya había una canción con ese título.";
            return;
        }

        LoadSongs();
        SelectedSong = Songs.FirstOrDefault(s => s.Id == saved.Id);
        StatusText = outcome.Result switch
        {
            SongSaveResult.Replaced => $"Canción \"{saved.Title}\" reemplazada.",
            SongSaveResult.SavedAsCopy => $"Canción guardada como \"{saved.Title}\".",
            _ => $"Canción \"{saved.Title}\" guardada.",
        };
    }

    [RelayCommand]
    private void EditSong()
    {
        if (SelectedSong is null)
            return;

        var edited = _songEditor.Edit(SelectedSong);
        if (edited is null)
            return;

        NoteAction($"Guardar la canción «{edited.Title}»");
        var saved = _songs.Save(edited);
        LoadSongs();
        SelectedSong = Songs.FirstOrDefault(s => s.Id == saved.Id);
        StatusText = $"Canción \"{saved.Title}\" actualizada.";
    }

    [RelayCommand]
    private void DeleteSong()
    {
        if (SelectedSong is null)
            return;

        var confirm = MessageBox.Show(
            $"¿Eliminar \"{SelectedSong.Title}\" de la biblioteca?",
            "EcclesiaCast",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes)
            return;

        _songs.Delete(SelectedSong.Id);
        SelectedSong = null;
        LoadSongs();
        StatusText = "Canción eliminada.";
    }

    /// <summary>Brings the whole ProPresenter library over in one step.</summary>
    [RelayCommand]
    private void ImportFromProPresenter()
    {
        if (_proPresenterImport.Show() is not { } summary)
            return;

        LoadSongs();
        StatusText = summary;
    }

    /// <summary>Finds songs on the internet (musica.com, LRCLIB) and imports them.</summary>
    [RelayCommand]
    private void SearchSongsOnline()
    {
        var imported = _webSongSearch.Show();
        if (imported.Count == 0)
            return;

        // Clear the filter so the new song is in the list to be selected.
        SearchText = string.Empty;
        LoadSongs();
        SelectedSong = Songs.FirstOrDefault(s => s.Id == imported[^1].Id) ?? SelectedSong;
        StatusText = imported.Count == 1
            ? $"«{imported[0].Title}» importada de internet."
            : $"{imported.Count} canciones importadas de internet.";
    }

    [RelayCommand]
    private void ImportSongs()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Importar canciones",
            Filter = "Canciones (*.txt, *.pro)|*.txt;*.pro|Texto plano (*.txt)|*.txt|ProPresenter 7 (*.pro)|*.pro",
            Multiselect = true,
        };
        if (dialog.ShowDialog() != true)
            return;

        int imported = 0, replaced = 0, skipped = 0, discarded = 0, failed = 0;

        // One question about a repeated title is asked once per song, unless
        // the operator ticks "the same for the rest".
        var session = _songSaver.Begin(several: dialog.FileNames.Length > 1);

        foreach (var path in dialog.FileNames)
        {
            try
            {
                var song = Path.GetExtension(path).ToLowerInvariant() == ".pro"
                    ? ProPresenterImporter.FromFile(Path.GetFileName(path), File.ReadAllBytes(path))
                    : TxtSongImporter.FromText(Path.GetFileName(path), ReadTextFile(path));

                if (song.Sections.Count == 0)
                {
                    Log.Warning("Importación sin texto: {Path}", path);
                    skipped++;
                    continue;
                }

                switch (session.Save(song).Result)
                {
                    case SongSaveResult.Replaced: replaced++; break;
                    case SongSaveResult.Discarded: discarded++; break;
                    default: imported++; break;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Falló la importación de {Path}", path);
                failed++;
            }
        }

        LoadSongs();

        var parts = new List<string> { $"{imported} canciones importadas" };
        if (replaced > 0)
            parts.Add($"{replaced} reemplazadas");
        if (discarded > 0)
            parts.Add($"{discarded} descartadas por repetidas");
        if (skipped > 0)
            parts.Add($"{skipped} omitidas (vacías)");
        if (failed > 0)
            parts.Add($"{failed} con error (ver log)");
        StatusText = string.Join(" · ", parts) + ".";
    }

    /// <summary>
    /// Imports the library of EasyWorship 6 or 7: the operator picks its
    /// <c>Songs.db</c> and the words (<c>SongWords.db</c>) are read from beside it.
    /// </summary>
    [RelayCommand]
    private void ImportEasyWorship()
    {
        var common = new[]
        {
            @"C:\Users\Public\Documents\Softouch\EasyWorship\Default\Databases\Data",
            @"C:\Users\Public\Documents\Softouch\EasyWorship 6\Default\Databases\Data",
        };
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Importar de EasyWorship: elegí el archivo Songs.db",
            Filter = "Canciones de EasyWorship (Songs.db)|Songs.db",
            InitialDirectory = common.FirstOrDefault(Directory.Exists) ?? string.Empty,
        };
        if (dialog.ShowDialog() != true)
            return;

        var files = EasyWorshipDatabase.FindFiles(Path.GetDirectoryName(dialog.FileName) ?? string.Empty);
        if (files is null)
        {
            ShowStatusError("No encontré SongWords.db al lado de Songs.db: tienen que estar en la misma carpeta.");
            return;
        }

        List<Song> songs;
        int unreadable;
        try
        {
            songs = EasyWorshipDatabase.ReadSongs(files.Value.Songs, files.Value.Words, out unreadable);
        }
        catch (Exception ex) when (ex is InvalidDataException or Microsoft.Data.Sqlite.SqliteException or IOException)
        {
            Log.Error(ex, "No se pudo leer la base de EasyWorship {Path}", dialog.FileName);
            ShowStatusError("No se pudo leer esa base de EasyWorship (ver el registro). ¿Está EasyWorship abierto?");
            return;
        }

        if (songs.Count == 0)
        {
            StatusText = "La base de EasyWorship no tiene canciones con letra.";
            return;
        }

        int imported = 0, replaced = 0, discarded = 0, failed = 0;
        var session = _songSaver.Begin(several: songs.Count > 1);
        foreach (var song in songs)
        {
            try
            {
                switch (session.Save(song).Result)
                {
                    case SongSaveResult.Replaced: replaced++; break;
                    case SongSaveResult.Discarded: discarded++; break;
                    default: imported++; break;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Falló la importación de «{Title}» de EasyWorship", song.Title);
                failed++;
            }
        }

        LoadSongs();

        var parts = new List<string> { $"{imported} canciones importadas de EasyWorship" };
        if (replaced > 0)
            parts.Add($"{replaced} reemplazadas");
        if (discarded > 0)
            parts.Add($"{discarded} descartadas por repetidas");
        if (unreadable + failed > 0)
            parts.Add($"{unreadable + failed} con error (ver log)");
        StatusText = string.Join(" · ", parts) + ".";
    }

    /// <summary>Reads a text file as UTF-8, falling back to Latin-1.</summary>
    private static string ReadTextFile(string path)
    {
        var bytes = File.ReadAllBytes(path);
        try
        {
            return new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            return Encoding.Latin1.GetString(bytes);
        }
    }
}
