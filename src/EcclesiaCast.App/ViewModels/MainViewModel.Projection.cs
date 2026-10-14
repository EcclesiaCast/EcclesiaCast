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
    // ── Proyección de slides ─────────────────────────────────────

    [RelayCommand]
    private void ProjectSlide(SlideItemViewModel item) => GoLiveSlide(item.Index);

    [RelayCommand]
    private void ProjectFirstSlide() => GoLiveSlide(0);

    [RelayCommand]
    private void NextSlide() => Navigate(+1, isRepeat: false);

    [RelayCommand]
    private void PreviousSlide() => Navigate(-1, isRepeat: false);

    private readonly ArrowStepGate _arrowGate = new();

    /// <summary>
    /// One arrow press. A press that comes from a key held down (the keyboard's
    /// own repetition) never leaves the playlist item: it stops at the edge and
    /// says what another press does, so a held key can't run through the whole
    /// service, putting backgrounds on as it goes.
    /// </summary>
    public void Navigate(int direction, bool isRepeat)
    {
        var neighbour = _currentPlaylistIndex + direction;
        var hasNeighbour = _currentPlaylistIndex >= 0 && neighbour >= 0 && neighbour < PlaylistItems.Count;

        // A media item has no slides of its own: the arrows move between
        // playlist items (the slide grid still shows the previous item), so
        // every press is at the edge.
        // With nothing live it starts from the verse marked in the search (or
        // the first real slide), never from a chapter-jump card.
        var target = SlideStep.Target(LiveSlideIndex, PreviewSlideIndex, direction,
            Slides.Select(s => s.JumpTarget is not null).ToList());
        var atEdge = CurrentPlaylistItemIsMedia
            || target < 0 || target >= Slides.Count || LeavesPassage(direction);

        // At the end of the item, the arrow continues to the next playlist
        // item. For a Bible passage the item ends at its last planned verse,
        // not at the chapter's end (the whole chapter is loaded around it).
        // When there is no next item, the operator can keep reading past the
        // passage.
        switch (_arrowGate.Decide(atEdge, hasNeighbour, isRepeat))
        {
            case ArrowAction.Ignore:
                return;

            case ArrowAction.Hold:
                StatusText = EdgeMessage(direction, PlaylistItems[neighbour]);
                return;

            case ArrowAction.Cross:
                TryAdvancePlaylist(direction);
                return;

            default:
                if (!CurrentPlaylistItemIsMedia)
                    GoLiveSlide(target);
                return;
        }
    }

    /// <summary>What the status bar says when an arrow stops at the edge of an item.</summary>
    private string EdgeMessage(int direction, PlaylistItem neighbour)
    {
        var current = _currentPlaylistIndex >= 0 && _currentPlaylistIndex < PlaylistItems.Count
            ? PlaylistItems[_currentPlaylistIndex].Type
            : PlaylistItemType.Media;
        var edge = (direction > 0 ? "Fin" : "Principio") + current switch
        {
            PlaylistItemType.Song => " de la canción",
            PlaylistItemType.BiblePassage => " del pasaje",
            PlaylistItemType.Presentation => " de la presentación",
            _ => " del elemento",
        };
        var arrow = direction > 0 ? "→" : "←";
        var verb = direction > 0 ? "pasa a" : "vuelve a";
        return $"{edge}: {arrow} otra vez {verb} «{neighbour.Caption}».";
    }

    private bool CurrentPlaylistItemIsMedia
    {
        get
        {
            if (_currentPlaylistIndex < 0
                || _currentPlaylistIndex >= PlaylistItems.Count
                || PlaylistItems[_currentPlaylistIndex] is not { Type: PlaylistItemType.Media } item)
                return false;

            // A page of a presentation added on its own: projecting it opened
            // the whole presentation in the grid, so the arrows walk its pages
            // like in any presentation instead of jumping to the next entry.
            return !(IsPresentationOpen && Slides.Any(s => s.Page?.Id == item.MediaId));
        }
    }

    /// <summary>
    /// True when the live slide sits on the edge of the current playlist
    /// item's verse range and the next arrow press in this direction would
    /// step outside the planned passage.
    /// </summary>
    private bool LeavesPassage(int direction)
    {
        if (_currentPlaylistIndex < 0 || _currentPlaylistIndex >= PlaylistItems.Count)
            return false;

        // The edge in this direction is where entering from the opposite
        // direction would land (forward exit = VerseEnd, backward = VerseStart).
        var edge = PassageBoundarySlideIndex(PlaylistItems[_currentPlaylistIndex], -direction);
        return edge is int index && LiveSlideIndex == index;
    }

    /// <summary>Moves to the adjacent playlist item and projects it. False if not applicable.</summary>
    private bool TryAdvancePlaylist(int direction)
    {
        if (_currentPlaylistIndex < 0)
            return false;

        var target = _currentPlaylistIndex + direction;
        if (target < 0 || target >= PlaylistItems.Count)
            return false;

        var item = PlaylistItems[target];
        // An item that could not be loaded leaves the previous one in the
        // grid: nothing is projected, and its message stays in the status bar.
        // It is still the current entry, so the next arrow goes on past it.
        if (!LoadPlaylistItem(item, projectMedia: true))
            return true;

        // A media item projects itself when opened (ApplyBackground); putting a
        // slide live on top of it would paint the previous item's text over it.
        if (item.Type != PlaylistItemType.Media)
        {
            // Forward lands on the first slide, backward on the last, so
            // reading through the service is continuous in both directions. A
            // Bible passage bounds that to its own verse range (its item loads
            // the whole chapter; landing on verse 1 would be wrong).
            var projectable = Slides.Where(s => s.JumpTarget is null).ToList();
            var landing = PassageBoundarySlideIndex(item, direction)
                ?? (projectable.Count > 0
                    ? (direction > 0 ? projectable[0].Index : projectable[^1].Index)
                    : null);
            if (landing is int index)
                GoLiveSlide(index);
        }

        StatusText = $"Playlist: {item.Caption}";
        return true;
    }

    /// <summary>
    /// The slide to land on when arrowing into a Bible-passage playlist item:
    /// its first verse going forward, its last verse going backward. Null for
    /// other item types (or when the verse isn't in the loaded chapter).
    /// </summary>
    private int? PassageBoundarySlideIndex(PlaylistItem item, int direction)
    {
        if (item.Type != PlaylistItemType.BiblePassage || item.Chapter is not int chapter)
            return null;

        var verse = direction > 0 ? item.VerseStart : item.VerseEnd ?? item.VerseStart;
        if (verse is not int number)
            return null;

        var index = FindVerseSlide(chapter, number, lastPart: direction < 0);
        return index >= 0 ? index : null;
    }

    /// <summary>
    /// The slide showing a verse of the loaded chapter, or -1. A verse can
    /// share its slide with others (a paragraph of the TLA) or be split over
    /// several: then it is the first part, or the last one going backward.
    /// </summary>
    private int FindVerseSlide(int chapter, int verse, bool lastPart = false)
    {
        var matches = Slides.Where(s => s.Verses is { } v && v.Chapter == chapter && v.Contains(verse));
        return (lastPart ? matches.LastOrDefault() : matches.FirstOrDefault())?.Index ?? -1;
    }

    private void GoLiveSlide(int index)
    {
        if (SelectedDisplay is null)
        {
            Log.Warning("GoLiveSlide({Index}) rechazado: no hay pantalla de salida seleccionada", index);
            StatusText = "No hay pantalla de salida seleccionada.";
            return;
        }

        if (index < 0 || index >= Slides.Count)
        {
            Log.Debug("GoLiveSlide({Index}) fuera de rango (slides: {Count})", index, Slides.Count);
            return;
        }

        var item = Slides[index];

        // A card of a presentation is a picture, never text.
        if (item.Page is { } page)
        {
            GoLivePage(index, page);
            return;
        }

        NoteAction($"Proyectar la diapositiva {index + 1} de {Slides.Count}: {item.Label}");

        // A foreground media takes the whole screen and hides the text: it is
        // an announcement, not a background. Projecting lyrics on top of one
        // means the operator is done with it, so it comes off.
        if (_presentation.Background is { Behavior: MediaBehavior.Foreground } foreground
            && item.JumpTarget is null)
        {
            // A presentation gives back what was behind before it.
            if (IsPresentationPageOnAir)
                RemovePageForText();
            else
                _presentation.SetBackground(null);
            Log.Debug("Fondo de primer plano «{Name}» retirado al proyectar texto", foreground.Name);
        }

        // Chapter jump cards never project themselves: they load the target
        // passage and go live on its first verse (next) or its last verse
        // (previous, so backward reading is continuous) in one motion.
        if (item.JumpTarget is { } jump)
        {
            var jumpToEnd = item.JumpToEnd;

            if (SelectedBibleBook?.Number != jump.BookNumber)
                SelectedBibleBook = BibleBooksAvailable.FirstOrDefault(b => b.Number == jump.BookNumber);

            LoadBiblePassage(jump);

            var landing = jumpToEnd
                ? Slides.LastOrDefault(s => s.JumpTarget is null)
                : Slides.FirstOrDefault(s => s.JumpTarget is null);
            if (landing is not null)
                GoLiveSlide(landing.Index);
            return;
        }

        _liveSlideOrigin = item.Verses is not null ? SlideOrigin.Bible
            : item.SectionId != 0 ? SlideOrigin.Song
            : SlideOrigin.None;
        _textLiveTitle = GridTitle;
        _presentation.GoLive(item.Slide);
        SyncHighlightBox();
        LiveTitle = GridTitle;
        _projection.EnsureVisible(SelectedDisplay.Info);
        _settings.Set(OutputDisplayKey, SelectedDisplay.Info.DeviceName);
        IsProjecting = true;

        // A song reaching the screen may draw its own background. Done after
        // going live so the slide already exists and the output doesn't dip
        // into "background only" on the way.
        if (item.SectionId != 0)
        {
            ApplyRandomBackgroundForSong();
            NoteSongProjected();
        }

        LiveSlideIndex = index;
        PreviewSlideIndex = -1;
        foreach (var slide in Slides)
        {
            slide.IsLive = slide.Index == index;
            slide.IsPreviewed = false;
        }

        RefreshSlideOnAir();
        PreviewSlide = index + 1 < Slides.Count ? Slides[index + 1].Slide : null;

        // The stage display needs the label of what is live and the text of
        // what follows, skipping the Bible's chapter-jump cards.
        Projection.SlideLabel = item.Label;
        Projection.NextSlide = Slides
            .Skip(index + 1)
            .FirstOrDefault(s => s.JumpTarget is null)?.Slide;

        Log.Debug("Slide {Index} en vivo: {Label}", index, item.Label);
        StatusText = $"En vivo: diapositiva {index + 1} de {Slides.Count}. Flechas ←→ para navegar · F1 Black · F2 Clear · F3 Logo.";
    }

    // ── Texto rápido ─────────────────────────────────────────────

    partial void OnQuickTextChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            PreviewSlide = QuickSlide(value.Trim());
    }

    partial void OnQuickTextFontChanged(string value)
    {
        _settings.Set(QuickTextFontKey, value);
        if (!string.IsNullOrWhiteSpace(QuickText))
            PreviewSlide = QuickSlide(QuickText.Trim());
    }

    /// <summary>
    /// The quick text as a slide: the songs' theme, with the typeface chosen
    /// for the quick text when there is one.
    /// </summary>
    private SlideContent QuickSlide(string text) =>
        new(text, Theme: DefaultSongTheme,
            Override: string.IsNullOrWhiteSpace(QuickTextFont) ? null : new SlideOverride(FontFamily: QuickTextFont.Trim()));

    [RelayCommand]
    private void GoLive()
    {
        if (string.IsNullOrWhiteSpace(QuickText) || SelectedDisplay is null)
            return;

        // A page of a presentation would hide the words: it comes off, as it
        // does for a song.
        if (IsPresentationPageOnAir)
            RemovePageForText();

        _liveSlideOrigin = SlideOrigin.QuickText;
        _textLiveTitle = "Texto rápido";
        _presentation.GoLive(QuickSlide(QuickText.Trim()));
        SyncHighlightBox();
        _projection.EnsureVisible(SelectedDisplay.Info);
        _settings.Set(OutputDisplayKey, SelectedDisplay.Info.DeviceName);
        IsProjecting = true;

        LiveSlideIndex = -1;
        foreach (var slide in Slides)
            slide.IsLive = false;
        RefreshSlideOnAir();

        // Quick text stands alone: nothing follows it on the stage display.
        Projection.SlideLabel = "Texto rápido";
        LiveTitle = "Texto rápido";
        Projection.NextSlide = null;

        StatusText = "Texto rápido en vivo. F1 Black · F2 Clear · F3 Logo · Esc apaga la salida.";
    }

    // ── Notas para la plataforma ─────────────────────────────────

    /// <summary>
    /// The preacher's outline. It only ever reaches the stage display, so
    /// unlike everything else in this panel it does not need the projector on.
    /// </summary>
    [RelayCommand]
    private void ShowStageNotes()
    {
        if (string.IsNullOrWhiteSpace(StageNotesText))
            return;

        var notes = StageNotesText.Trim();
        _presentation.SetStageNotes(notes);
        _settings.Set(StageNotesKey, notes);

        StatusText = IsStageVisible
            ? "Notas en la pantalla de escenario."
            : "Notas guardadas. Se ven al prender la pantalla de escenario (🎭).";
    }

    [RelayCommand]
    private void HideStageNotes()
    {
        _presentation.SetStageNotes(null);
        StatusText = "Notas fuera del escenario.";
    }

    // ── Aviso al pie ─────────────────────────────────────────────

    [RelayCommand]
    private void ShowOverlay()
    {
        if (string.IsNullOrWhiteSpace(OverlayText) || SelectedDisplay is null)
            return;

        _presentation.ShowOverlay(OverlayText.Trim());
        _projection.EnsureVisible(SelectedDisplay.Info);
        IsProjecting = true;
        StatusText = "Aviso al pie visible sobre la salida.";
    }

    [RelayCommand]
    private void HideOverlay()
    {
        _presentation.HideOverlay();
        StatusText = "Aviso al pie retirado.";
    }

    // ── Estados de salida ────────────────────────────────────────

    /// <summary>Turns the output window on (if a display is selected).</summary>
    private bool EnsureOutputOn()
    {
        if (SelectedDisplay is null)
        {
            StatusText = "No hay pantalla de salida seleccionada.";
            return false;
        }

        _projection.EnsureVisible(SelectedDisplay.Info);
        IsProjecting = true;
        return true;
    }

    [RelayCommand]
    private void ToggleOutput()
    {
        if (IsProjecting)
        {
            _projection.HideOutput();
            IsProjecting = false;
            StatusText = "Salida apagada.";
        }
        else if (EnsureOutputOn())
        {
            StatusText = "Salida en vivo.";
        }
    }

    [RelayCommand]
    private void ToggleClear()
    {
        _presentation.ToggleClear();
        EnsureOutputOn();
    }

    [RelayCommand]
    private void ToggleBlack()
    {
        NoteAction("Pantalla negra (Black)");
        _presentation.ToggleBlack();
        EnsureOutputOn();
    }

    [RelayCommand]
    private void ToggleLogo()
    {
        _presentation.ToggleLogo();
        EnsureOutputOn();
    }

    [RelayCommand]
    private void HideOutput()
    {
        _projection.HideOutput();
        IsProjecting = false;
        StatusText = "Salida apagada.";
    }

    private void UpdateStateFlags()
    {
        IsClearActive = _presentation.State == OutputState.Clear;
        IsBlackActive = _presentation.State == OutputState.Black;
        IsLogoActive = _presentation.State == OutputState.Logo;
        IsOverlayActive = _presentation.OverlayMessage is not null;
        IsCountdownRunning = _presentation.Countdown is not null;
        IsStageNotesActive = _presentation.StageNotes is not null;
        HasBackground = _presentation.Background is not null;

        // A page of a presentation stops being «on screen» the moment
        // anything else takes the background.
        if (_livePageId != 0 && _presentation.Background?.Id != _livePageId)
            _livePageId = 0;

        UpdateOnAir();

        // The live page switches itself off the moment anything else takes the
        // background (or it is cleared): the button must follow.
        IsWebLive = _presentation.Background is { Type: MediaType.Web };
        IsWindowLive = _presentation.Background is { Type: MediaType.Window };

        // Show the blur the current background carries, without saving it back.
        var blur = _presentation.Background?.Blur ?? 0;
        if (Math.Abs(BackgroundBlur - blur) > 0.01)
            BackgroundBlur = blur;

        SyncPlaybackTimer();
    }
}
