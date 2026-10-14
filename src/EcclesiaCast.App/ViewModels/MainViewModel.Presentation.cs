using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EcclesiaCast.Core.Media;
using EcclesiaCast.Core.Playlists;
using EcclesiaCast.Core.Presentation;

namespace EcclesiaCast.App.ViewModels;

/// <summary>
/// A media tab opened as a presentation: its pages (a PDF or a deck imported
/// into a tab of its own, or any set of pictures and videos) fill the central
/// grid the way the lyrics of a song do, and the arrows walk them. The words
/// of a song or a verse can not be put over a page.
/// </summary>
public sealed partial class MainViewModel
{
    private static string PresentationKey(string tab) => $"media.tab.{tab.ToLowerInvariant()}.presentation";

    /// <summary>The tabs marked as a presentation, read once per reload of the library (the settings are a database).</summary>
    private HashSet<string> _presentationTabs = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The tab whose pages are in the grid; null when the grid holds anything else.</summary>
    private string? _presentationTab;

    /// <summary>What the grid was built from, to rebuild it only when the tab really changed.</summary>
    private string _presentationSignature = string.Empty;

    /// <summary>Id of the page on the screen; 0 when the screen shows no page of a presentation.</summary>
    private int _livePageId;

    /// <summary>What was behind before the first page took the screen, to put it back when words return.</summary>
    private MediaItem? _backgroundBeforePages;

    /// <summary>True while the grid holds a presentation: it enables «➕ Playlist» beside the title.</summary>
    [ObservableProperty]
    private bool _isPresentationOpen;

    /// <summary>
    /// The page that comes after the one on screen. The PREVIEW draws it as a
    /// picture (there is no text to preview in a presentation).
    /// </summary>
    [ObservableProperty]
    private MediaItem? _previewPageMedia;

    /// <summary>True for a tab marked as a presentation (also read by the tab's menu).</summary>
    public bool IsPresentationTab(string? tab) =>
        !string.IsNullOrWhiteSpace(tab) && _presentationTabs.Contains(tab);

    private void RefreshPresentationTabs() =>
        _presentationTabs = new HashSet<string>(
            MediaTabs.Where(t => _settings.Get(PresentationKey(t)) == "1"),
            StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// A page of a presentation always takes the whole screen and stays until
    /// the operator moves on: the tab's rule and the seconds an image would
    /// have on its own do not apply, and a video can not hand over to a media
    /// outside the presentation.
    /// </summary>
    private static MediaItem AsPage(MediaItem item)
    {
        if (item.Type is not (MediaType.Image or MediaType.Video))
            return item;

        var needsChange = item.Behavior != MediaBehavior.Foreground
            || (item.Type == MediaType.Image && item.ShowSeconds != 0)
            || item.EndBehavior == VideoEndBehavior.NextMedia;
        if (!needsChange)
            return item;

        var page = item.Clone();
        page.Behavior = MediaBehavior.Foreground;
        if (page.Type == MediaType.Image)
            page.ShowSeconds = 0;
        if (page.EndBehavior == VideoEndBehavior.NextMedia)
            page.EndBehavior = VideoEndBehavior.Stop;
        return page;
    }

    /// <summary>The grid stops holding a presentation (something else is being loaded into it).</summary>
    private void LeavePresentation()
    {
        _presentationTab = null;
        _presentationSignature = string.Empty;
        IsPresentationOpen = false;
        PreviewPageMedia = null;
    }

    /// <summary>Right click on a tab → «Abrir como presentación».</summary>
    [RelayCommand]
    private void OpenPresentationTab(string? tab)
    {
        if (string.IsNullOrWhiteSpace(tab))
            return;

        if (!IsPresentationTab(tab))
            SetPresentationTab(tab, true);

        OpenPresentation(tab, null);
    }

    /// <summary>Marks or unmarks a tab as a presentation; unmarking closes it if it is in the grid.</summary>
    private void SetPresentationTab(string tab, bool on)
    {
        _settings.Set(PresentationKey(tab), on ? "1" : "0");
        RefreshPresentationTabs();
        if (!on)
            RefreshOpenPresentation();
    }

    /// <summary>
    /// Puts the pages of a tab in the grid. Opening what is already open
    /// changes nothing, so a click on a card of the tab that is on screen
    /// does not rebuild the grid under the operator's hand.
    /// </summary>
    private bool OpenPresentation(string tab, int? startMediaId)
    {
        var pages = PresentationPages.For(tab, _allMedia);
        if (pages.Count == 0)
        {
            StatusText = $"«{tab}» no tiene imágenes ni videos para armar la presentación.";
            return false;
        }

        var signature = PresentationPages.Signature(pages);
        var alreadyOpen = string.Equals(_presentationTab, tab, StringComparison.OrdinalIgnoreCase)
            && signature == _presentationSignature
            && Slides.Count == pages.Count;
        if (!alreadyOpen)
        {
            NoteAction($"Abrir la presentación «{tab}»");

            // The same clearing as loading a song: the song list stops
            // pointing at what the grid no longer shows.
            SelectedSong = null;
            Slides.Clear();
            LiveSlideIndex = -1;
            PreviewSlideIndex = -1;
            _currentPassage = null;
            PreviewSlide = null;
            PreviewMedia = null;
            IsWebPanelOpen = false;
            if (!_openingPlaylistItem)
                _currentPlaylistIndex = -1;

            _presentationTab = tab;
            _presentationSignature = signature;
            IsPresentationOpen = true;
            GridTitle = "📑 " + tab;
            BuildPageCards(pages);
            StatusText = $"Presentación «{tab}»: {pages.Count} página(s). Clic en una la proyecta · flechas ←→ pasan de página.";
        }

        var start = Slides.FirstOrDefault(s => s.Page?.Id == startMediaId) ?? Slides.FirstOrDefault();
        if (!alreadyOpen || LiveSlideIndex < 0)
            PreviewPageMedia = start?.Page;
        return true;
    }

    private void BuildPageCards(IReadOnlyList<MediaItem> pages)
    {
        Slides.Clear();
        for (var i = 0; i < pages.Count; i++)
        {
            Slides.Add(new SlideItemViewModel(
                i, $"Página {i + 1}", new SlideContent($"Página {i + 1}"))
            {
                Page = pages[i],
            });
        }
    }

    /// <summary>
    /// The library was reloaded: the grid follows the tab (a page added,
    /// moved or removed, a new poster) and the page on screen keeps its mark.
    /// A tab that is no longer a presentation, or has no pages left, closes.
    /// </summary>
    private void RefreshOpenPresentation()
    {
        if (_presentationTab is not { } tab)
            return;

        var pages = IsPresentationTab(tab) ? PresentationPages.For(tab, _allMedia) : [];
        if (pages.Count == 0)
        {
            Slides.Clear();
            LiveSlideIndex = -1;
            PreviewSlideIndex = -1;
            GridTitle = null;
            LeavePresentation();
            return;
        }

        var signature = PresentationPages.Signature(pages);
        if (signature == _presentationSignature)
            return;

        var liveId = LiveSlideIndex >= 0 && LiveSlideIndex < Slides.Count ? Slides[LiveSlideIndex].Page?.Id : null;
        _presentationSignature = signature;
        BuildPageCards(pages);

        var live = liveId is null ? null : Slides.FirstOrDefault(s => s.Page?.Id == liveId);
        LiveSlideIndex = live?.Index ?? -1;
        foreach (var slide in Slides)
            slide.IsLive = slide.Index == LiveSlideIndex;

        PreviewPageMedia = (live is null ? Slides.FirstOrDefault() : Slides.ElementAtOrDefault(live.Index + 1))?.Page;
    }

    /// <summary>
    /// A click on a media of a tab marked as a presentation: the pages open in
    /// the grid and that one goes on screen.
    /// </summary>
    private void ShowPage(MediaItem media)
    {
        if (!OpenPresentation(media.Category, media.Id))
            return;

        var card = Slides.FirstOrDefault(s => s.Page?.Id == media.Id);
        if (card is not null)
            GoLiveSlide(card.Index);
    }

    private bool IsPresentationPageOnAir =>
        _livePageId != 0 && _presentation.Background?.Id == _livePageId;

    /// <summary>
    /// Takes the page off the screen because words are about to go up: what
    /// was behind before the presentation started comes back (if it was
    /// something that sits behind text), or nothing.
    /// </summary>
    private void RemovePageForText()
    {
        var restore = NextSlideBackground.Resolve(_backgroundBeforePages);
        _presentation.SetBackground(restore, restore is null ? null : ResolveFill(restore));
        _livePageId = 0;
        _backgroundBeforePages = null;
    }

    /// <summary>
    /// A page of the presentation goes on screen: a foreground picture or
    /// video, with no words. The words that were up are forgotten, so Clear
    /// (F2) does not bring them back on top.
    /// </summary>
    private void GoLivePage(int index, MediaItem page)
    {
        NoteAction($"Proyectar la página {index + 1} de {Slides.Count}: {page.Name}");

        if (!IsPresentationPageOnAir)
            _backgroundBeforePages = _presentation.Background;

        _presentation.ClearText();
        ApplyBackgroundCore(page);
        _livePageId = page.Id;
        _settings.Set(OutputDisplayKey, SelectedDisplay!.Info.DeviceName);

        LiveTitle = GridTitle;
        LiveSlideIndex = index;
        PreviewSlideIndex = -1;
        foreach (var slide in Slides)
        {
            slide.IsLive = slide.Index == index;
            slide.IsPreviewed = false;
        }

        PreviewSlide = null;
        var next = Slides.ElementAtOrDefault(index + 1);
        PreviewPageMedia = next?.Page;

        Projection.SlideLabel = $"Página {index + 1} de {Slides.Count}";
        Projection.NextSlide = next?.Slide;

        StatusText = $"En vivo: página {index + 1} de {Slides.Count}. Flechas ←→ para pasar de página · F1 Black · F3 Logo · F4 quita la página.";
    }

    /// <summary>
    /// «➕ Playlist» beside the title of the grid, or «Agregar a la playlist
    /// como presentación» on a tab: the whole tab goes in as one entry, which
    /// the arrows walk page by page before moving on to the next entry.
    /// </summary>
    [RelayCommand]
    private void AddPresentationToPlaylist(string? tab)
    {
        tab ??= _presentationTab;
        if (string.IsNullOrWhiteSpace(tab))
            return;

        var pages = PresentationPages.For(tab, _allMedia);
        if (pages.Count == 0)
        {
            StatusText = $"«{tab}» no tiene imágenes ni videos para agregar.";
            return;
        }

        if (!EnsurePlaylist())
            return;

        PlaylistItems.Add(new PlaylistItem
        {
            Type = PlaylistItemType.Presentation,
            Caption = tab,
            MediaId = pages[0].Id,
        });
        SavePlaylist();
        StatusText = $"Presentación «{tab}» agregada a la playlist.";
    }
}
