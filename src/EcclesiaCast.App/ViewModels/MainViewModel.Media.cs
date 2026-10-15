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
    // ── Medios (biblioteca con tabs por categoría) ───────────────

    /// <summary>All media, unfiltered; the bar shows the current tab's items.</summary>
    private readonly List<MediaItem> _allMedia = [];

    /// <summary>Items shown in the media bar (filtered by the current tab).</summary>
    public ObservableCollection<MediaItem> MediaItems { get; } = [];

    /// <summary>The tabs across the media bar (categories).</summary>
    public ObservableCollection<string> MediaTabs { get; } = [];

    [ObservableProperty]
    private string _selectedMediaTab = "Fondos";

    /// <summary>
    /// When on, finishing a video in this tab starts the next one, looping
    /// round at the end — the tab behaves like a playlist. Kept per tab in the
    /// settings, since tabs are just the media's category.
    /// </summary>
    [ObservableProperty]
    private bool _continuousPlayback;

    private static string ContinuousKey(string tab) => $"media.tab.{tab.ToLowerInvariant()}.continuous";

    private const string MediaTabsKey = "media.tabs";

    private static string RuleKey(string tab) => $"media.tab.{tab.ToLowerInvariant()}.rule";

    /// <summary>What the tab decides for its media (everything a foreground, say); each media's own setting when it decides nothing.</summary>
    private MediaTabRule TabRule(string? tab) =>
        string.IsNullOrWhiteSpace(tab) ? MediaTabRule.Default : MediaTabRule.Parse(_settings.Get(RuleKey(tab)));

    /// <summary>The media as it is to play: with the rule of its tab applied.</summary>
    private MediaItem Playable(MediaItem item)
    {
        var resolved = TabRule(item.Category).Resolve(item);

        // The pages of a presentation are not decoration: always the whole screen.
        return IsPresentationTab(item.Category) ? AsPage(resolved) : resolved;
    }

    /// <summary>
    /// Names of the tabs whose media play without sound. The tab list shows a
    /// speaker on each; the set is replaced (never edited) so the bindings see
    /// it change.
    /// </summary>
    [ObservableProperty]
    private HashSet<string> _mutedMediaTabs = new(StringComparer.OrdinalIgnoreCase);

    private void RefreshMutedMediaTabs() =>
        MutedMediaTabs = new HashSet<string>(
            MediaTabs.Where(t => TabRule(t).Muted), StringComparer.OrdinalIgnoreCase);

    /// <summary>True when the tab is silenced; the menu of the tab asks it to word its entry.</summary>
    public bool IsMediaTabMuted(string? tab) =>
        !string.IsNullOrWhiteSpace(tab) && TabRule(tab).Muted;

    /// <summary>
    /// Keeps the rule of a tab. When its sound changed and the media on screen
    /// belongs to it, the screen gets the media again so the change is heard
    /// now, not at the next click (the video keeps playing: only its mute
    /// switches).
    /// </summary>
    private void SaveMediaTabRule(string tab, MediaTabRule rule)
    {
        var soundChanged = TabRule(tab).Muted != rule.Muted;
        _settings.Set(RuleKey(tab), rule.ToSetting());
        RefreshMutedMediaTabs();

        if (soundChanged
            && _presentation.Background is { } onScreen
            && _allMedia.FirstOrDefault(m => m.Id == onScreen.Id) is { } original
            && string.Equals(original.Category, tab, StringComparison.OrdinalIgnoreCase))
            _presentation.RefreshBackground(Resolved(original).Clone(), ResolveFill(original));
    }

    /// <summary>Right click on a tab → «Silenciar la pestaña» / «Quitar el silencio».</summary>
    [RelayCommand]
    private void ToggleMediaTabMute(string? tab)
    {
        if (string.IsNullOrWhiteSpace(tab))
            return;

        var rule = TabRule(tab);
        SaveMediaTabRule(tab, rule with { Muted = !rule.Muted });
        StatusText = rule.Muted
            ? $"«{tab}»: ahora suena (cada medio con lo que dice en sus Propiedades)."
            : $"«{tab}»: silenciada, sus medios se proyectan sin sonido.";
    }

    private void SaveMediaTabs(IEnumerable<string> tabs) =>
        _settings.Set(MediaTabsKey, MediaTabNames.Serialize(tabs));

    private bool IsContinuousTab(string? tab) =>
        !string.IsNullOrWhiteSpace(tab) && _settings.Get(ContinuousKey(tab)) == "1";

    partial void OnContinuousPlaybackChanged(bool value)
    {
        if (_loadingMediaTab)
            return;

        _settings.Set(ContinuousKey(SelectedMediaTab), value ? "1" : "0");
        StatusText = value
            ? $"«{SelectedMediaTab}»: los videos se reproducen uno atrás del otro."
            : $"«{SelectedMediaTab}»: reproducción continua desactivada.";
    }

    /// <summary>True while the tab's own setting is being read into the checkbox.</summary>
    private bool _loadingMediaTab;

    // ── Fondo al azar ────────────────────────────────────────────

    private const string RandomEnabledKey = "media.random.enabled";
    private const string RandomTabKey = "media.random.tab";

    /// <summary>Draws backgrounds without repeating; see <see cref="ShuffleBag"/>.</summary>
    private readonly ShuffleBag _backgroundBag = new();

    /// <summary>The song the current random background was drawn for; 0 when none.</summary>
    private int _randomBackgroundSongId;

    /// <summary>
    /// Id of the last background the draw put up; 0 when none. Anything else
    /// on screen was chosen by hand, and the draw leaves it alone.
    /// </summary>
    private int _randomBackgroundId;

    /// <summary>
    /// When on, every song that goes live gets a different background from the
    /// chosen tab. The tab is the one the box was ticked in, so no extra
    /// setting is needed to say where the backgrounds come from.
    /// </summary>
    [ObservableProperty]
    private bool _randomBackgroundPerSong;

    /// <summary>Tab the automatic draw takes from ("Fondos" unless changed).</summary>
    private string RandomTab => _settings.Get(RandomTabKey) ?? "Fondos";

    partial void OnRandomBackgroundPerSongChanged(bool value)
    {
        if (_loadingMediaTab)
            return;

        _settings.Set(RandomEnabledKey, value ? "1" : "0");
        if (value)
            _settings.Set(RandomTabKey, SelectedMediaTab);

        _backgroundBag.Reset();
        _randomBackgroundSongId = 0;

        StatusText = value
            ? $"Cada canción se lleva un fondo distinto de «{SelectedMediaTab}»."
            : "Fondo al azar por canción desactivado.";
    }

    /// <summary>
    /// Backgrounds worth drawing from a tab: the ones that sit behind the text.
    /// A foreground item would cover the lyrics, which is never what someone
    /// asking for a random background wants.
    /// </summary>
    private List<MediaItem> BackgroundPool(string tab) =>
        _allMedia
            .Where(m => string.Equals(m.Category, tab, StringComparison.OrdinalIgnoreCase))
            .Where(m => Playable(m).Behavior == MediaBehavior.Background)
            .ToList();

    /// <summary>Applies a background drawn at random from the tab on screen.</summary>
    [RelayCommand]
    private void RandomBackground()
    {
        var pool = BackgroundPool(SelectedMediaTab);
        if (pool.Count == 0)
        {
            StatusText = $"«{SelectedMediaTab}» no tiene fondos para sortear.";
            return;
        }

        var id = _backgroundBag.Take(pool.Select(m => m.Id).ToList(), Random.Shared);
        if (pool.FirstOrDefault(m => m.Id == id) is not { } picked)
            return;

        ApplyBackground(picked);
        _randomBackgroundId = picked.Id;
        StatusText = $"Fondo al azar: {picked.Name}.";
    }

    /// <summary>
    /// Gives a song its own background when the feature is on. Called as a
    /// slide goes live, but it only draws once per song: moving back and forth
    /// through the verses must not keep changing the background.
    /// </summary>
    /// <summary>
    /// Notes that this song has been sung, which is what the "songs we have
    /// stopped singing" list reads. Only the first slide of a run counts:
    /// walking the verses of one song is one use, not eight.
    /// </summary>
    private void NoteSongProjected()
    {
        if (SelectedSong is not { } song || song.Id <= 0 || _lastNotedSongId == song.Id)
            return;

        _lastNotedSongId = song.Id;
        var now = DateTime.Now;
        song.LastProjectedAt = now;

        try
        {
            _songs.MarkProjected(song.Id, now);
        }
        catch (Exception ex)
        {
            // Bookkeeping must never take the projection down mid-service.
            Log.Warning(ex, "No se pudo anotar el uso de la canción {Song}", song.Title);
        }
    }

    private int _lastNotedSongId;

    private void ApplyRandomBackgroundForSong()
    {
        if (!RandomBackgroundPerSong || SelectedSong is not { } song)
            return;

        if (song.Id == _randomBackgroundSongId)
            return;

        // A background picked from the media bar stays until the operator
        // changes it: the draw only fills an empty screen, or replaces a
        // background that was itself drawn.
        if (_presentation.Background is { } current && current.Id != _randomBackgroundId)
            return;

        var pool = BackgroundPool(RandomTab);
        if (pool.Count == 0)
            return;

        // Mark it drawn even if nothing lands, so an empty tab doesn't retry
        // on every single slide.
        _randomBackgroundSongId = song.Id;

        var id = _backgroundBag.Take(pool.Select(m => m.Id).ToList(), Random.Shared);
        if (pool.FirstOrDefault(m => m.Id == id) is { } picked)
        {
            ApplyBackground(picked);
            _randomBackgroundId = picked.Id;
        }
    }

    /// <summary>The item after this one in its tab, wrapping around at the end.</summary>
    private MediaItem? NextInTab(MediaItem current)
    {
        var siblings = _allMedia
            .Where(m => string.Equals(m.Category, current.Category, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (siblings.Count <= 1)
            return null;

        var index = siblings.FindIndex(m => m.Id == current.Id);
        return index < 0 ? siblings[0] : siblings[(index + 1) % siblings.Count];
    }

    /// <param name="selectTab">The tab to leave open; the one that was open when null.</param>
    private void LoadMedia(string? selectTab = null)
    {
        _allMedia.Clear();
        _allMedia.AddRange(_media.GetAll());

        // A poster whose event is over goes to «Eventos pasados» by itself.
        var overdue = _allMedia.Where(m => MediaExpiry.IsDue(m, DateTime.Today)).ToList();
        foreach (var old in overdue)
        {
            old.Category = MediaExpiry.ArchiveTab;
            // It has done its job: with the date cleared it will not jump back
            // if someone moves it out of the archive on purpose.
            old.EventDate = null;
            _media.Update(old);
        }

        if (overdue.Count > 0)
        {
            StatusText = overdue.Count == 1
                ? $"«{overdue[0].Name}» ya pasó (su evento terminó) y se movió a «{MediaExpiry.ArchiveTab}»."
                : $"{overdue.Count} medios ya pasaron (sus eventos terminaron) y se movieron a «{MediaExpiry.ArchiveTab}».";
        }

        // An item with no tab name belongs to no tab and would never be shown
        // anywhere: send it to the first one.
        foreach (var orphan in _allMedia.Where(m => string.IsNullOrWhiteSpace(m.Category)))
        {
            orphan.Category = "Fondos";
            _media.Update(orphan);
        }

        // The tabs are the saved list (so an empty or reordered one is kept,
        // and a deleted one stays deleted) plus any category in use that the
        // list does not have yet.
        var keepTab = selectTab ?? SelectedMediaTab;
        var saved = MediaTabNames.Deserialize(_settings.Get(MediaTabsKey));
        var tabs = MediaTabNames.Merge(saved, _allMedia.Select(m => m.Category));
        MediaTabs.Clear();
        foreach (var tab in tabs)
            MediaTabs.Add(tab);
        if (saved is null || !saved.SequenceEqual(tabs))
            SaveMediaTabs(tabs);

        SelectedMediaTab = MediaTabs.Contains(keepTab) ? keepTab : MediaTabs.FirstOrDefault() ?? "Fondos";
        FilterMediaByTab();
        SyncMediaTabOptions();
        RefreshMutedMediaTabs();

        // A presentation open in the grid follows what the tab holds now.
        RefreshPresentationTabs();
        RefreshOpenPresentation();
    }

    partial void OnSelectedMediaTabChanged(string value)
    {
        FilterMediaByTab();
        SyncMediaTabOptions();
    }

    /// <summary>
    /// Reads the current tab's own switches into the checkboxes. Called from
    /// <see cref="LoadMedia"/> too: on startup the tab is assigned its existing
    /// value, so the property-changed hook never fires and the boxes would
    /// stay blank while the settings said otherwise.
    /// </summary>
    private void SyncMediaTabOptions()
    {
        _loadingMediaTab = true;
        ContinuousPlayback = IsContinuousTab(SelectedMediaTab);
        // The box is ticked only on the tab the draw actually takes from, so
        // walking the tabs shows at a glance where the backgrounds come from.
        RandomBackgroundPerSong = _settings.Get(RandomEnabledKey) == "1"
            && string.Equals(SelectedMediaTab, RandomTab, StringComparison.OrdinalIgnoreCase);
        _loadingMediaTab = false;
    }

    private void FilterMediaByTab()
    {
        MediaItems.Clear();
        foreach (var item in _allMedia.Where(m =>
                     string.Equals(m.Category, SelectedMediaTab, StringComparison.OrdinalIgnoreCase)))
            MediaItems.Add(item);

        // The bar is rebuilt after almost every change: what was picked stays
        // picked as long as it is still in this tab.
        MediaSelection.Retain(MediaItems.Select(m => m.Id));
        MediaSelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    // ── Selección de medios, como en el Explorador ───────────────

    /// <summary>The cards picked in the bar. Keyed by id: the items are rebuilt on every reload.</summary>
    public MediaSelection MediaSelection { get; } = new();

    /// <summary>Raised when the picked cards change, so the bar can redraw their frames.</summary>
    public event EventHandler? MediaSelectionChanged;

    private void NotifyMediaSelection() => MediaSelectionChanged?.Invoke(this, EventArgs.Empty);

    /// <summary>A library item by id, for the cards whose fill is another media.</summary>
    public MediaItem? FindMedia(int id) => _allMedia.FirstOrDefault(m => m.Id == id);

    private List<int> MediaOrder() => MediaItems.Select(m => m.Id).ToList();

    /// <summary>The picked media, in the order the bar shows them.</summary>
    public IReadOnlyList<MediaItem> SelectedMedia =>
        MediaItems.Where(m => MediaSelection.Contains(m.Id)).ToList();

    /// <summary>
    /// What a command started from one card acts on: the whole selection when
    /// that card is part of it, otherwise just the card. Same as right-clicking
    /// a file in the Explorer.
    /// </summary>
    public IReadOnlyList<MediaItem> MediaTargets(MediaItem? item)
    {
        if (item is null)
            return [];

        return MediaSelection.Count > 1 && MediaSelection.Contains(item.Id)
            ? SelectedMedia
            : [item];
    }

    /// <summary>
    /// A click on a card. Returns true when it should also project it — only a
    /// plain click does; Ctrl and Shift just pick.
    /// </summary>
    public bool ClickMedia(MediaItem item, bool ctrl, bool shift)
    {
        if (shift)
            MediaSelection.Range(MediaOrder(), item.Id, additive: ctrl);
        else if (ctrl)
            MediaSelection.Toggle(item.Id);
        else
            MediaSelection.Only(item.Id);

        NotifyMediaSelection();
        return !ctrl && !shift;
    }

    /// <summary>Right click on a card that is not picked: it becomes the only one, as in the Explorer.</summary>
    public void SelectMediaForMenu(MediaItem item)
    {
        if (MediaSelection.Contains(item.Id))
            return;

        MediaSelection.Only(item.Id);
        NotifyMediaSelection();
    }

    public void SelectAllMedia()
    {
        MediaSelection.Set(MediaItems.Select(m => m.Id), additive: false);
        NotifyMediaSelection();
    }

    public void ClearMediaSelection()
    {
        if (MediaSelection.Count == 0)
            return;

        MediaSelection.Clear();
        NotifyMediaSelection();
    }

    /// <summary>The rectangle dragged over the empty space of the bar.</summary>
    public void SelectMediaIn(IEnumerable<MediaItem> items, bool additive)
    {
        MediaSelection.Set(items.Select(m => m.Id), additive);
        NotifyMediaSelection();
    }

    /// <summary>Supr on the bar: removes what is picked, after asking once.</summary>
    public void DeleteSelectedMedia()
    {
        var picked = SelectedMedia;
        if (picked.Count > 0)
            RemoveMedia(picked, alwaysAsk: true);
    }

    [RelayCommand]
    private void NewMediaTab()
    {
        var name = _textPrompt.Ask("Nueva pestaña", "Nombre de la pestaña (ej. Jóvenes):");
        if (string.IsNullOrWhiteSpace(name))
            return;
        name = name.Trim();
        if (!MediaTabs.Contains(name, StringComparer.OrdinalIgnoreCase))
        {
            MediaTabs.Add(name);
            SaveMediaTabs(MediaTabs);
        }
        SelectedMediaTab = MediaTabs.FirstOrDefault(t => string.Equals(t, name, StringComparison.OrdinalIgnoreCase)) ?? name;
    }

    /// <summary>
    /// Right click on a tab → «Cambiar nombre». A tab is only the category
    /// written on its media, so renaming it rewrites that on each one, and its
    /// own switches (continuous play, rule) move to the new name.
    /// </summary>
    [RelayCommand]
    private void RenameMediaTab(string? tab)
    {
        if (string.IsNullOrWhiteSpace(tab))
            return;

        var typed = _textPrompt.Ask("Cambiar nombre de la pestaña", "Nombre nuevo (se pueden usar emojis):", tab);
        if (typed is null || typed.Trim() == tab)
            return;

        var name = typed.Trim();
        if (MediaTabNames.WhyNotRename(MediaTabs, tab, name) is { } why)
        {
            StatusText = why;
            return;
        }

        NoteAction($"Cambiar el nombre de la pestaña «{tab}» a «{name}»");
        foreach (var item in _allMedia.Where(m => string.Equals(m.Category, tab, StringComparison.OrdinalIgnoreCase)).ToList())
        {
            item.Category = name;
            _media.Update(item);
        }

        // Its switches follow it; the old name is left switched off.
        if (_settings.Get(ContinuousKey(tab)) is { } continuous)
        {
            _settings.Set(ContinuousKey(name), continuous);
            _settings.Set(ContinuousKey(tab), "0");
        }
        if (_settings.Get(RuleKey(tab)) is { } rule)
        {
            _settings.Set(RuleKey(name), rule);
            _settings.Set(RuleKey(tab), MediaTabRule.Default.ToSetting());
        }
        if (_settings.Get(PresentationKey(tab)) is { } presentation)
        {
            _settings.Set(PresentationKey(name), presentation);
            _settings.Set(PresentationKey(tab), "0");
        }
        if (string.Equals(_presentationTab, tab, StringComparison.OrdinalIgnoreCase))
        {
            // The grid keeps showing the same pages under the new name.
            if (LiveTitle == GridTitle)
                LiveTitle = "📑 " + name;
            _presentationTab = name;
            GridTitle = "📑 " + name;
        }
        if (string.Equals(_settings.Get(RandomTabKey), tab, StringComparison.OrdinalIgnoreCase))
            _settings.Set(RandomTabKey, name);

        SaveMediaTabs(MediaTabs.Select(t => string.Equals(t, tab, StringComparison.OrdinalIgnoreCase) ? name : t));
        LoadMedia(string.Equals(SelectedMediaTab, tab, StringComparison.OrdinalIgnoreCase) ? name : null);
        StatusText = $"La pestaña «{tab}» ahora se llama «{name}».";
    }

    /// <summary>
    /// Right click on a tab → «Eliminar». The media leave the library; their
    /// files stay where they are.
    /// </summary>
    [RelayCommand]
    private void DeleteMediaTab(string? tab)
    {
        if (string.IsNullOrWhiteSpace(tab))
            return;

        var items = _allMedia
            .Where(m => string.Equals(m.Category, tab, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var answer = _textPrompt.Choose(
            $"Eliminar la pestaña «{tab}»",
            "Los medios de esta pestaña ya no se van a ver en el programa (los archivos no se borran)."
            + (items.Count > 0 ? $"\n\nSon {items.Count} medio(s)." : string.Empty),
            ["Eliminar la pestaña"]);
        if (answer != 0)
            return;

        NoteAction($"Eliminar la pestaña «{tab}»");
        foreach (var item in items)
            RemoveMediaCore(item);

        _settings.Set(ContinuousKey(tab), "0");
        _settings.Set(RuleKey(tab), MediaTabRule.Default.ToSetting());
        _settings.Set(PresentationKey(tab), "0");
        SaveMediaTabs(MediaTabs.Where(t => !string.Equals(t, tab, StringComparison.OrdinalIgnoreCase)));
        LoadMedia(string.Equals(SelectedMediaTab, tab, StringComparison.OrdinalIgnoreCase)
            ? MediaTabs.FirstOrDefault(t => !string.Equals(t, tab, StringComparison.OrdinalIgnoreCase))
            : null);
        StatusText = $"Pestaña «{tab}» eliminada.";
    }

    /// <summary>Right click on a tab → «Configurar…»: where everything in it goes, and what follows.</summary>
    [RelayCommand]
    private void ConfigureMediaTab(string? tab)
    {
        if (string.IsNullOrWhiteSpace(tab))
            return;

        var presentation = IsPresentationTab(tab);
        if (_mediaInspector.EditTabRule(tab, TabRule(tab), ref presentation) is not { } rule)
            return;

        SaveMediaTabRule(tab, rule);
        if (presentation != IsPresentationTab(tab))
        {
            SetPresentationTab(tab, presentation);
            if (presentation)
            {
                StatusText = $"«{tab}» es una presentación: un clic en una de sus tarjetas la abre en el cuadro central.";
                return;
            }
        }

        StatusText = rule.Mode switch
        {
            MediaTabMode.Background => $"«{tab}»: todo va como fondo.",
            MediaTabMode.Foreground => rule.LogoAtEnd
                ? $"«{tab}»: todo va en primer plano y al terminar se muestra el logo."
                : $"«{tab}»: todo va en primer plano.",
            _ => $"«{tab}»: cada medio dice cómo va.",
        };
        if (rule.Muted)
            StatusText += " Sin sonido.";
    }

    [RelayCommand]
    private void ImportMedia()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = $"Agregar a «{SelectedMediaTab}»",
            Filter = "Imágenes, videos y documentos"
                   + "|*.jpg;*.jpeg;*.png;*.bmp;*.webp;*.gif;*.mp4;*.mov;*.m4v;*.avi;*.mkv;*.wmv;*.webm;*.pdf;*.pptx;*.ppt"
                   + "|Imágenes|*.jpg;*.jpeg;*.png;*.bmp;*.webp;*.gif"
                   + "|Videos|*.mp4;*.mov;*.m4v;*.avi;*.mkv;*.wmv;*.webm"
                   + "|PDF y PowerPoint|*.pdf;*.pptx;*.ppt",
            Multiselect = true,
        };
        if (dialog.ShowDialog() != true)
            return;

        ImportMediaFiles(dialog.FileNames);
    }

    /// <summary>
    /// Brings files into the tab that is open: the part of «Agregar» that
    /// starts from paths, so dropping files from Explorer onto the media bar
    /// does exactly the same. Whatever the library does not take is counted
    /// and said in the status bar instead of being skipped in silence.
    /// </summary>
    public void ImportMediaFiles(IEnumerable<string> paths)
    {
        var added = 0;
        var skipped = 0;
        var documents = new List<string>();
        var newVideos = new List<(string Path, string Name)>();
        var animatedGifs = new List<string>();
        var stillGifs = 0;

        foreach (var path in paths)
        {
            // An animated GIF would show only its first frame as a picture:
            // with ffmpeg it is made into a looping video once the rest is in.
            if (Path.GetExtension(path).Equals(".gif", StringComparison.OrdinalIgnoreCase)
                && File.Exists(path) && GifFile.IsAnimated(path))
            {
                if (GifImporter.IsAvailable)
                {
                    animatedGifs.Add(path);
                    continue;
                }

                stillGifs++;
            }

            // A PDF or a deck becomes one image per page, but drawing them
            // takes a moment, so they are done after this loop rather than
            // freezing the window in the middle of an import.
            var kind = MediaFiles.Classify(path);
            if (kind == MediaFileKind.Document && File.Exists(path))
            {
                documents.Add(path);
                continue;
            }

            MediaType? type = kind switch
            {
                MediaFileKind.Image => MediaType.Image,
                MediaFileKind.Video => MediaType.Video,
                _ => null,
            };
            if (type is null || !File.Exists(path))
            {
                skipped++;
                continue;
            }

            // Videos the shell can't decode get their poster from VLC afterwards,
            // in the background, so a batch import doesn't freeze the window.
            _media.Add(new MediaItem
            {
                Name = Path.GetFileNameWithoutExtension(path),
                Path = path,
                Type = type.Value,
                ThumbnailPath = MediaThumbnails.Create(path, type.Value, videoEngine: null),
                Category = SelectedMediaTab,
                // Videos with audio default to a foreground announcement; silent
                // ones and images to a background — the operator can change it.
                Behavior = MediaBehavior.Background,

                // A picture goes in whole («Ajustar»), and the bars the screen shape
                // leaves are filled with a blurred copy of itself, so it looks like
                // one picture. A video keeps what it always had.
                Scaling = type == MediaType.Image ? MediaScaling.Fit : default,
                FillMediaId = type == MediaType.Image ? MediaItem.FillWithSelfId : null,
            });
            added++;
            if (type == MediaType.Video)
                newVideos.Add((path, Path.GetFileNameWithoutExtension(path)));
        }

        LoadMedia();
        var ignored = skipped > 0
            ? $" {skipped} archivo(s) no se agregaron: sólo entran imágenes, videos, PDF y PowerPoint."
            : string.Empty;
        var gifNote = animatedGifs.Count > 0
            ? $" Convirtiendo {animatedGifs.Count} GIF animado(s) en video…"
            : stillGifs > 0
                ? $" {stillGifs} GIF animado(s) quedan quietos: para moverse hace falta ffmpeg y no se encontró en esta computadora."
                : string.Empty;
        StatusText = documents.Count > 0
            ? $"{added} medio(s) agregado(s). Preparando {documents.Count} documento(s)…{ignored}{gifNote}"
            : added == 0 && skipped > 0 && animatedGifs.Count == 0
                ? $"No se agregó nada: sólo entran imágenes, videos, PDF y PowerPoint."
                : $"{added} medio(s) agregado(s) a «{SelectedMediaTab}».{ignored}{gifNote}";
        _ = RefreshVideoThumbnailsAsync();

        if (animatedGifs.Count > 0)
            _ = ImportGifsAsync(animatedGifs, SelectedMediaTab);

        // The new videos wait for their answer before the background work may
        // make (or skip) a copy of them.
        LightVideoCache.Hold(newVideos.Select(v => v.Path));
        RefreshLightCopies();
        if (newVideos.Count > 0)
            _ = OfferLightCopiesAsync(newVideos);

        if (documents.Count > 0)
            _ = ImportDocumentsAsync(documents);
    }

    /// <summary>
    /// Makes a looping, silent video of each animated GIF, in the background
    /// (encoding takes a few seconds each), and adds it to the tab the GIFs
    /// were brought into. A GIF that cannot be converted goes in as the still
    /// picture it was, so nothing the operator dropped is lost.
    /// </summary>
    private async Task ImportGifsAsync(IReadOnlyList<string> gifs, string tab)
    {
        var converted = 0;
        var kept = 0;
        try
        {
            foreach (var gif in gifs)
            {
                var name = Path.GetFileNameWithoutExtension(gif);
                var video = await GifImporter.ConvertAsync(gif);
                if (video is null)
                {
                    // Left as a picture: it shows its first frame, as it always did.
                    _media.Add(new MediaItem
                    {
                        Name = name,
                        Path = gif,
                        Type = MediaType.Image,
                        ThumbnailPath = MediaThumbnails.Create(gif, MediaType.Image, videoEngine: null),
                        Category = tab,
                        Behavior = MediaBehavior.Background,
                        Scaling = MediaScaling.Fit,
                        FillMediaId = MediaItem.FillWithSelfId,
                    });
                    kept++;
                    continue;
                }

                _media.Add(new MediaItem
                {
                    Name = name,
                    Path = video,
                    Type = MediaType.Video,
                    Category = tab,
                    Behavior = MediaBehavior.Background,

                    // A GIF has no sound and never ends: it loops, silent.
                    Muted = true,
                    EndBehavior = VideoEndBehavior.Loop,
                });
                converted++;
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "No se pudieron convertir los GIF");
        }

        LoadMedia();
        _ = RefreshVideoThumbnailsAsync();
        RefreshLightCopies();

        StatusText = kept == 0
            ? $"{converted} GIF animado(s) convertido(s) en video, en bucle y sin sonido, en «{tab}»."
            : $"{converted} GIF convertido(s) en video; {kept} no se pudo convertir y quedó como imagen fija (el detalle está en el registro).";
    }

    /// <summary>
    /// A 4K video, or one of 50 frames or more, costs the projection computer
    /// several times what a 1080p one does and looks the same on the projector.
    /// For each such video just brought in, asks whether to make a lighter
    /// copy; yes makes it now, with a bar. The original stays where it is and
    /// stays in the library: the copy is only the file played in its place.
    /// </summary>
    private async Task OfferLightCopiesAsync(IReadOnlyList<(string Path, string Name)> videos)
    {
        try
        {
            if (!LightVideoCache.IsAvailable)
                return;

            var heavy = new List<(string Path, string Name, VideoStreamInfo Info)>();
            foreach (var (path, name) in videos)
            {
                var info = await LightVideoCache.ProbeAsync(path);
                if (info is not null && LightCopyPolicy.IsHeavy(info))
                    heavy.Add((path, name, info));
            }

            int? sameForAll = null;
            for (var i = 0; i < heavy.Count; i++)
            {
                var (path, name, info) = heavy[i];

                int choice;
                if (sameForAll is { } remembered)
                {
                    choice = remembered;
                }
                else
                {
                    var more = i < heavy.Count - 1;
                    var (index, repeat) = _textPrompt.ChooseAndRemember(
                        "Video muy pesado",
                        "Este video es muy pesado para proyectar. ¿Hago una copia más liviana?\n\n"
                        + $"«{name}» ({LightCopyPolicy.Describe(info)})\n\n"
                        + "La copia se parece al original y es la que se proyecta; tu archivo queda como está "
                        + "y la biblioteca guarda los dos. Sin copia, en una computadora lenta el video puede andar a los saltos.",
                        [
                            "Sí, hacer la copia ahora (tarda unos minutos)",
                            "No, proyectar el original",
                        ],
                        more ? "Hacer lo mismo con los demás videos pesados" : null);
                    choice = index;
                    if (repeat)
                        sameForAll = index;
                }

                if (choice == 0)
                    MakeLightCopy(path, name);
                else
                    LightVideoCache.Decline(path);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "No se pudo ofrecer la copia liviana de los videos nuevos");
        }
        finally
        {
            LightVideoCache.Release(videos.Select(v => v.Path));
            RefreshLightCopies();
        }
    }

    /// <summary>Runs the window that makes the copy; a cancelled copy is remembered as "no".</summary>
    private void MakeLightCopy(string path, string name)
    {
        var window = new Views.LightCopyWindow(path, name) { Owner = Application.Current.MainWindow };
        var done = window.ShowDialog() == true;

        if (done)
        {
            StatusText = $"Copia liviana lista: «{name}». La biblioteca guarda el original y la copia.";
            return;
        }

        if (window.Cancelled)
            LightVideoCache.Decline(path);

        StatusText = window.Cancelled
            ? $"Sin copia liviana de «{name}»: se proyecta el original. Clic derecho sobre el video → Hacer una copia liviana, si querés hacerla después."
            : $"No se pudo hacer la copia liviana de «{name}»: se proyecta el original.";
    }

    /// <summary>
    /// The way back for a video that was left without its copy: right click →
    /// «Hacer una copia liviana». Tells the operator when it is not needed.
    /// </summary>
    [RelayCommand]
    private async Task MakeLightCopyOf(MediaItem? item)
    {
        if (item is not { Type: MediaType.Video } || !File.Exists(item.Path))
            return;

        if (!LightVideoCache.IsAvailable)
        {
            StatusText = "Para hacer copias livianas hace falta ffmpeg, y no se encontró en esta computadora.";
            return;
        }

        var info = await LightVideoCache.ProbeAsync(item.Path);
        if (info is null)
        {
            StatusText = $"No se pudo leer «{item.Name}».";
            return;
        }

        if (!LightCopyPolicy.IsHeavy(info))
        {
            StatusText = $"«{item.Name}» ({LightCopyPolicy.Describe(info)}) ya es liviano: no hace falta una copia.";
            return;
        }

        if (LightVideoCache.HasCopy(item.Path))
        {
            StatusText = $"«{item.Name}» ya tiene su copia liviana.";
            return;
        }

        MakeLightCopy(item.Path, item.Name);
        RefreshLightCopies();
    }

    /// <summary>
    /// Brings in a PDF or a PowerPoint deck as one background per page, in
    /// order. The pages are ordinary images from then on: they go in the
    /// playlist, take a theme's text over them, and need nothing installed to
    /// show — which is the point of rendering them now rather than on Sunday.
    /// </summary>
    private async Task ImportDocumentsAsync(IReadOnlyList<string> paths)
    {
        var currentTab = SelectedMediaTab;
        var pagesAdded = 0;
        string? failure = null;
        string? presentationToOpen = null;
        string? lastTab = null;
        int? sameForAll = null;

        for (var n = 0; n < paths.Count; n++)
        {
            var path = paths[n];
            var name = Path.GetFileNameWithoutExtension(path);

            // A document is best kept in a tab of its own, opened as a
            // presentation: its pages then work in the central grid like a
            // song does. The other way leaves them in the tab that is open.
            int choice;
            if (sameForAll is { } remembered)
            {
                choice = remembered;
            }
            else
            {
                var more = n < paths.Count - 1;
                var (index, repeat) = _textPrompt.ChooseAndRemember(
                    "Dónde poner las páginas",
                    $"«{Path.GetFileName(path)}» se va a importar como una imagen por página. ¿Dónde las pongo?",
                    [
                        $"📑 Pestaña nueva «{name}», como presentación (recomendado)",
                        $"Agregar las páginas a «{currentTab}»",
                    ],
                    more ? "Hacer lo mismo con los demás documentos" : null);
                choice = index;
                if (repeat)
                    sameForAll = index;
            }

            if (choice < 0)
                continue;

            var result = await DocumentImporter.ImportAsync(path);

            if (result.Error is not null)
            {
                failure = result.Error;
                continue;
            }

            var tab = currentTab;
            if (choice == 0)
            {
                tab = MediaTabNames.Unique(MediaTabs, name);
                MediaTabs.Add(tab);
                SaveMediaTabs(MediaTabs);
                _settings.Set(PresentationKey(tab), "1");
                presentationToOpen = tab;
            }
            lastTab = tab;

            for (var page = 0; page < result.Pages.Count; page++)
            {
                _media.Add(new MediaItem
                {
                    // Numbered so the pages sort and read in order in the tab.
                    Name = $"{name} · {page + 1}",
                    Path = result.Pages[page],
                    Type = MediaType.Image,
                    ThumbnailPath = MediaThumbnails.Create(result.Pages[page], MediaType.Image, videoEngine: null),
                    Category = tab,
                    Behavior = MediaBehavior.Foreground,

                    // A page is read, not decorated: all of it has to show.
                    // Filling the screen cropped a portrait page to a strip.
                    Scaling = MediaScaling.Fit,
                });
                pagesAdded++;
            }
        }

        // The new presentation shows its tab, and its pages fill the grid
        // (without projecting anything).
        LoadMedia(presentationToOpen);

        if (presentationToOpen is not null)
        {
            OpenPresentation(presentationToOpen, null);
            StatusText = failure is not null
                ? failure
                : $"{pagesAdded} página(s) en la pestaña «{presentationToOpen}», abierta como presentación: clic en una página la proyecta y las flechas pasan de a una.";
            return;
        }

        StatusText = failure is not null
            ? failure
            : lastTab is null
                ? "No se importó ningún documento."
                : $"{pagesAdded} página(s) agregada(s) a «{lastTab}». Cada una es un fondo: arrastralas a la playlist para pasarlas con las flechas.";
    }

    /// <summary>
    /// Fills in the video posters the Windows shell couldn't produce, decoding a
    /// frame with VLC instead. Runs off the UI thread because each file costs a
    /// couple hundred milliseconds.
    /// </summary>
    /// <remarks>
    /// Until this existed, those videos showed the generic player icon (the VLC
    /// cone). The first run after the fix therefore rebuilds every video poster
    /// once, since we can't tell an old icon from a real frame after the fact.
    /// </remarks>
    /// <summary>
    /// Hands every video the program can project — the library, which holds
    /// the YouTube downloads too, and the video logos — to the light-copy
    /// cache. What is already done is skipped, so calling it after any
    /// change to the library is cheap.
    /// </summary>
    private void RefreshLightCopies()
    {
        var videos = _media.GetAll()
            .Where(m => m.Type == MediaType.Video)
            .Select(m => m.Path)
            .Concat(_logos.GetAll()
                .Where(l => l.Kind == LogoKind.Video && !string.IsNullOrWhiteSpace(l.Path))
                .Select(l => l.Path!));

        // IsProjecting is a plain field read: the cache asks from its own thread.
        LightVideoCache.Refresh(videos, () => IsProjecting);
    }

    private async Task RefreshVideoThumbnailsAsync()
    {
        // Videos need the VLC engine to get a poster; images and document
        // pages do not, so a missing engine must not stop their posters.
        var engine = App.VideoEngine;

        var rebuildAll = _settings.Get(VideoThumbnailsRebuiltKey) is null;
        var reshapeImages = _settings.Get(ImageThumbnailsShapedKey) is null;

        // Images are included: their poster is a PNG kept apart from the
        // picture, and when that file disappears the card in the media bar
        // stays blank after every restart (it was only ever rebuilt for
        // videos).
        var pending = _media.GetAll()
            .Where(m => m.Type switch
            {
                MediaType.Video => engine is not null && !string.IsNullOrWhiteSpace(m.Path)
                                   && (rebuildAll || PosterPolicy.NeedsPoster(m, File.Exists)),
                MediaType.Image => (reshapeImages && File.Exists(m.Path)) || PosterPolicy.NeedsPoster(m, File.Exists),
                _ => false,
            })
            .ToList();

        if (pending.Count > 0)
        {
            var rebuilt = await Task.Run(() =>
            {
                var count = 0;
                foreach (var item in pending)
                {
                    if (rebuildAll && item.Type == MediaType.Video)
                        MediaThumbnails.Delete(item.ThumbnailPath);

                    var replaced = item.ThumbnailPath;
                    var thumbnail = MediaThumbnails.Create(item.Path, item.Type, engine);
                    if (thumbnail is null)
                        continue;

                    item.ThumbnailPath = thumbnail;
                    _media.Update(item);
                    count++;

                    // The old poster goes only once the new one is in place.
                    if (item.Type == MediaType.Image && reshapeImages && replaced != thumbnail)
                        MediaThumbnails.Delete(replaced);
                }
                return count;
            });

            if (rebuilt > 0)
                LoadMedia();
        }

        // Images and pages do not need the engine: once is enough.
        if (reshapeImages)
            _settings.Set(ImageThumbnailsShapedKey, "1");

        // Only once the video engine could do the job; otherwise the next
        // run still has to rebuild the old generic icons.
        if (engine is not null)
            _settings.Set(VideoThumbnailsRebuiltKey, "1");

        // Sweep posters left behind by earlier rebuilds.
        var current = _media.GetAll().Select(m => m.ThumbnailPath).ToList();
        await Task.Run(() => MediaThumbnails.DeleteOrphans(current));
    }

    /// <summary>
    /// Click a media: background layers behind the text, foreground takes the
    /// screen. A saved web page is the exception: it opens in the web panel and
    /// is projected from its buttons, not by the click.
    /// </summary>
    [RelayCommand]
    private void ApplyBackground(MediaItem? item)
    {
        if (item is null)
            return;

        if (item.Type == MediaType.Web)
        {
            OpenWebPage(item.Path, item.Name);
            return;
        }

        // In a tab marked as a presentation the card is a page: the pages
        // open in the grid and this one goes on screen.
        if (item.Type is MediaType.Image or MediaType.Video && IsPresentationTab(item.Category))
        {
            ShowPage(item);
            return;
        }

        ApplyBackgroundCore(item);
    }

    /// <summary>
    /// The media exactly as it goes to the output: what its tab decides
    /// (everything a foreground, say) comes before what the media says, and a
    /// tab set to play through can't have its videos looping, or they would
    /// never reach the end that triggers the next one. Always a copy when it
    /// changes anything, so the library's item stays as the operator left it.
    /// Every path that puts a media on the output, or puts it there again
    /// after a change, goes through this.
    /// </summary>
    private MediaItem Resolved(MediaItem item)
    {
        item = Playable(item);
        if (item.Type != MediaType.Image
            && item.EndBehavior == VideoEndBehavior.Loop
            && IsContinuousTab(item.Category))
        {
            item = item.Clone();
            item.EndBehavior = VideoEndBehavior.Stop;
        }

        return item;
    }

    /// <summary>Puts a media on the output, whatever it is (also what the web panel projects).</summary>
    private void ApplyBackgroundCore(MediaItem item)
    {
        // A saved window card is only the name of a window: it has to be found
        // among the open ones and captured before there is anything to show.
        if (item.Type == MediaType.Window && item.Id != WindowLiveId)
        {
            ProjectSavedWindow(item);
            return;
        }

        item = Resolved(item);

        NoteAction($"Poner el fondo «{item.Name}»");
        _presentation.SetBackground(item, ResolveFill(item));

        // A media on the whole screen is what is live now: «En vivo: …» says it.
        if (item.Behavior == MediaBehavior.Foreground)
            LiveTitle = item.Name;

        // Foreground = the media alone (no text). Background with no live slide
        // also shows alone; Black/Logo would hide it, so drop back to showing
        // the media immediately.
        if (item.Behavior == MediaBehavior.Foreground
            || _presentation.CurrentSlide is null
            || _presentation.State is OutputState.Black or OutputState.Logo)
            _presentation.ShowBackgroundOnly();

        if (SelectedDisplay is not null)
        {
            _projection.EnsureVisible(SelectedDisplay.Info);
            IsProjecting = true;
        }
        StatusText = item.Behavior == MediaBehavior.Foreground
            ? $"Primer plano: {item.Name} (a pantalla completa)."
            : $"Fondo: {item.Name}.";

        StartAfterImageTimer(item);
    }

    // ── Qué sigue después de un medio ────────────────────────────

    /// <summary>
    /// The time of the image on the screen when it has seconds set, and the
    /// image itself. It drives the same playback bar as a video (see
    /// <see cref="OnPlaybackTick"/>): it can be paused, moved and started over.
    /// </summary>
    private ImageClock? _imageClock;

    private MediaItem? _imageClockItem;

    private void StopAfterImageTimer()
    {
        _imageClock = null;
        _imageClockItem = null;
    }

    /// <summary>
    /// An image with seconds set goes on to what follows it once they are up,
    /// as a video does when it ends, and the playback bar shows them going by.
    /// Anything else that takes the screen in the meantime cancels it.
    /// </summary>
    private void StartAfterImageTimer(MediaItem item)
    {
        StopAfterImageTimer();
        if (item.Type != MediaType.Image || item.ShowSeconds <= 0)
            return;

        _imageClock = new ImageClock(TimeSpan.FromSeconds(item.ShowSeconds), DateTime.UtcNow);
        _imageClockItem = item;
        SyncPlaybackTimer();
        OnPlaybackTick(null, EventArgs.Empty);
    }

    /// <summary>The image's seconds are up: what its Propiedades say comes next.</summary>
    private void FinishImage(MediaItem item)
    {
        StopAfterImageTimer();
        NoteAction($"Pasar de la imagen «{item.Name}» a lo que sigue");
        if (item.EndBehavior == VideoEndBehavior.NextMedia && GoToNextMedia(item))
            return;

        if (item.EndBehavior == VideoEndBehavior.Logo)
        {
            _presentation.SetBackground(null);
            if (!IsLogoActive)
                _presentation.ToggleLogo();
            StatusText = $"\"{item.Name}\" terminó → logo.";
        }
        else
        {
            ClearBackground();
        }
    }

    /// <summary>Puts on the media that follows <paramref name="media"/>; false when none is chosen or it is gone.</summary>
    private bool GoToNextMedia(MediaItem media)
    {
        if (media.NextMediaId is not int id || id == media.Id
            || _allMedia.FirstOrDefault(m => m.Id == id) is not { } next)
            return false;

        ApplyBackground(next);
        StatusText = $"\"{media.Name}\" terminó → {next.Name}.";
        return true;
    }

    /// <summary>
    /// Takes the image/video off the output and leaves the text where it was.
    /// Applying a background parks the output on "background only", so without
    /// this the lyrics stayed hidden after the background was removed.
    /// </summary>
    [RelayCommand]
    private void ClearBackground()
    {
        NoteAction("Sacar el fondo");
        StopAfterImageTimer();
        _presentation.SetBackground(null);

        if (_presentation.State == OutputState.Clear && _presentation.CurrentSlide is not null)
            _presentation.ToggleClear();

        StatusText = "Fondo quitado; la letra sigue en pantalla.";
    }

    /// <summary>A non-looping video finished: honour its end behaviour.</summary>
    private void OnProjectedVideoEnded()
    {
        var background = _presentation.Background;
        if (background is null || background.EndBehavior == VideoEndBehavior.Loop)
            return;

        // A media that names what follows it goes there before anything else.
        if (background.EndBehavior == VideoEndBehavior.NextMedia && GoToNextMedia(background))
            return;

        // The tab plays through: hand over to the next item in it.
        if (IsContinuousTab(background.Category) && NextInTab(background) is { } next)
        {
            ApplyBackground(next);
            StatusText = $"Reproducción continua en «{background.Category}» → {next.Name}.";
            return;
        }

        if (background.EndBehavior == VideoEndBehavior.Logo)
        {
            _presentation.SetBackground(null);
            if (!IsLogoActive)
                _presentation.ToggleLogo();
            StatusText = $"\"{background.Name}\" terminó → logo.";
        }
        else
        {
            StatusText = $"\"{background.Name}\" terminó.";
        }
    }
}
