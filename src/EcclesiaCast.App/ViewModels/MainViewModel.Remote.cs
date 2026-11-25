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
using EcclesiaCast.Core.Remote;
using EcclesiaCast.Core.Songs;
using EcclesiaCast.Core.Stage;
using EcclesiaCast.Core.Timing;
using EcclesiaCast.Core.Themes;
using EcclesiaCast.Data.Persistence;
using Serilog;

namespace EcclesiaCast.App.ViewModels;

public sealed partial class MainViewModel
{
    // ── Control desde el celular ─────────────────────────────────

    private const string RemotePinKey = "remote.pin";
    private const string RemotePortKey = "remote.port";
    private const string RemoteEnabledKey = "remote.enabled";

    private RemoteControlServer? _remote;

    [ObservableProperty]
    private bool _isRemoteRunning;

    /// <summary>
    /// «Transmisión → Salida para OBS o vMix…»: the addresses of the stream's
    /// layers. Turns the server on if it was off and leaves it on, since a
    /// Browser Source in OBS is useless with nobody listening.
    /// </summary>
    [RelayCommand]
    private void OpenBroadcast()
    {
        if (!StartRemote())
        {
            Notice.Show(
                "Puede que otro programa los esté usando.",
                "No se pudo abrir ningún puerto para la salida de OBS",
                NoticeKind.Warning);
            return;
        }

        new Views.BroadcastWindow(_remote!, _settings) { Owner = Application.Current.MainWindow }.ShowDialog();
    }

    /// <summary>«Transmisión → Estilos de la transmisión…».</summary>
    [RelayCommand]
    private void OpenBroadcastStyles() =>
        new Views.BroadcastStyleWindow(_settings) { Owner = Application.Current.MainWindow }.ShowDialog();

    /// <summary>
    /// Starts listening. Left on between sessions on purpose: whoever set the
    /// phone up on Sunday shouldn't have to do it again next Sunday.
    /// </summary>
    private bool StartRemote()
    {
        _remote ??= new RemoteControlServer(this);
        if (_remote.IsRunning)
            return true;

        var pin = _settings.Get(RemotePinKey);
        if (!RemotePin.IsValid(pin))
        {
            pin = RemotePin.Generate();
            _settings.Set(RemotePinKey, pin);
        }

        var port = int.TryParse(_settings.Get(RemotePortKey), out var saved) ? saved : 8080;

        if (!_remote.Start(port, pin))
            return false;

        // Si el puerto elegido estaba ocupado y se usó otro, no se pisa el
        // guardado: la próxima vez se vuelve a intentar con el de siempre, así
        // la dirección pegada en OBS no se corre para siempre.
        if (_remote.Port == port || _settings.Get(RemotePortKey) is null)
            _settings.Set(RemotePortKey, _remote.Port.ToString());
        else
            Log.Warning("El puerto {Wanted} estaba ocupado: se usa el {Used} esta vez", port, _remote.Port);
        _settings.Set(RemoteEnabledKey, "1");
        IsRemoteRunning = true;
        return true;
    }

    /// <summary>Brings the remote back up when it was left on last time.</summary>
    private void RestoreRemote()
    {
        if (_settings.Get(RemoteEnabledKey) != "1")
            return;

        if (StartRemote())
            Log.Information("Control remoto restaurado en {Address}", _remote!.Address);
    }

    /// <summary>Turns the phone remote on and shows how to connect to it.</summary>
    [RelayCommand]
    private void OpenRemote()
    {
        if (!StartRemote())
        {
            Notice.Show(
                "Puede que otro programa los esté usando.",
                "No se pudo abrir ningún puerto para el control remoto",
                NoticeKind.Warning);
            return;
        }

        var window = new Views.RemoteControlWindow(_remote!, newPin => _settings.Set(RemotePinKey, newPin))
        {
            Owner = Application.Current.MainWindow,
        };
        window.ShowDialog();

        if (window.StopRequested)
        {
            _remote!.Stop();
            _settings.Set(RemoteEnabledKey, "0");
            IsRemoteRunning = false;
            StatusText = "Control desde el celular apagado.";
        }
        else
        {
            StatusText = $"Control desde el celular activo en {_remote!.Address}.";
        }
    }

    /// <summary>Shuts the server down when the app closes.</summary>
    public void StopRemote()
    {
        _remote?.Dispose();
        _remote = null;
        IsRemoteRunning = false;
    }

    RemoteState IRemoteHost.GetState()
    {
        var slides = Slides
            .Where(s => s.JumpTarget is null)
            .Select(s => new RemoteSlide(
                s.Index, s.Label, Shorten(s.Slide.MainText), s.IsLive && IsGridOnAir, s.Page?.Id))
            .ToList();

        var presentations = PresentationsForRemote()
            .Select((p, index) => new RemotePresentation(index, p.Tab, p.Pages))
            .ToList();

        var playlist = PlaylistItems
            .Select((item, index) => new RemotePlaylistItem(
                index,
                item.Caption,
                item.Type switch
                {
                    PlaylistItemType.BiblePassage => "📖",
                    PlaylistItemType.Media => "🎞",
                    PlaylistItemType.Presentation => "📑",
                    _ => "🎵",
                }))
            .ToList();

        return new RemoteState(
            IsProjecting: IsProjecting,
            OutputState: _presentation.State.ToString(),
            SlideLabel: Projection.SlideLabel ?? "En vivo",
            LiveText: Projection.Slide?.MainText ?? string.Empty,
            NextText: Projection.NextSlide?.MainText ?? string.Empty,
            Status: StatusText,
            Slides: slides,
            Playlist: playlist,
            SongTitle: SelectedSong?.Title ?? (IsBibleTabActive ? "Biblia" : "EcclesiaCast"),
            Presentations: presentations,
            // A page (or a media) on the air shows as its poster on the phone,
            // not as the words that were up before it.
            LiveMediaId: OnAir is OnAirSource.Presentation or OnAirSource.Media && LiveMediaId > 0 ? LiveMediaId : null,
            NextMediaId: IsPresentationOpen ? PreviewPageMedia?.Id : null);
    }

    /// <summary>What the last phone search found, so a result can be opened by its place in the list.</summary>
    private IReadOnlyList<SearchHit> _remoteHits = [];

    IReadOnlyList<RemoteHit> IRemoteHost.Search(string query)
    {
        query = query.Trim();
        if (query.Length < 2)
        {
            _remoteHits = [];
            return [];
        }

        // Songs and verses only: a phone has no use for a video in the middle of a search.
        _remoteHits = SearchEverywhere(query[..Math.Min(query.Length, 80)])
            .Where(h => h.Kind is SearchHitKind.Song or SearchHitKind.Verse)
            .Take(30)
            .ToList();

        return _remoteHits
            .Select((h, index) => new RemoteHit(
                index, h.Kind == SearchHitKind.Song ? "song" : "verse", h.Title, h.Detail))
            .ToList();
    }

    /// <summary>The marked tabs that have pages, in the order of the tabs: what the phone offers to open.</summary>
    private List<(string Tab, int Pages)> PresentationsForRemote() =>
        MediaTabs
            .Where(IsPresentationTab)
            .Select(t => (Tab: t, Pages: PresentationPages.For(t, _allMedia).Count))
            .Where(p => p.Pages > 0)
            .ToList();

    string? IRemoteHost.GetThumbnailPath(int mediaId)
    {
        var poster = _allMedia.FirstOrDefault(m => m.Id == mediaId)?.ThumbnailPath;
        if (string.IsNullOrWhiteSpace(poster) || !File.Exists(poster))
            return null;

        // Only what the program itself drew into its posters folder: whatever
        // else a media's poster field might point to is not for the network.
        var folder = Path.GetFullPath(MediaThumbnails.Directory).TrimEnd('\\') + "\\";
        return Path.GetFullPath(poster).StartsWith(folder, StringComparison.OrdinalIgnoreCase) ? poster : null;
    }

    /// <summary>
    /// What the streaming page draws: the words as the congregation sees
    /// them, with the theme's styling, and nothing else. Clear, Black and
    /// Logo all mean "no words on screen" here — a clean feed follows the
    /// projector rather than second-guessing it.
    /// </summary>
    RemoteOutput IRemoteHost.GetOutput(OutputChannel channel)
    {
        var slide = Projection.Slide;
        var theme = slide?.Theme ?? SlideTheme.Fallback;
        var over = slide?.Override;
        var countdown = _presentation.Countdown;

        // Each page of the stream shows only its own kind of thing: the words
        // of a verse go to the Bible page and nowhere else, and so on. The page
        // with everything keeps showing what it always did.
        var belongs = channel == OutputChannel.All || OutputChannels.Shows(channel, OnAir);
        var overlays = OutputChannels.ShowsOverlays(channel);
        var style = StyleOf(channel);

        // The name plate goes where the «aviso al pie» goes: the page with
        // everything and the announcements page.
        var plate = overlays && IsProjecting && _presentation.NamePlate is { Look.OnStream: true } shown ? shown : null;
        var plateFile = plate is null ? null : ((IRemoteHost)this).GetNamePlateFile();

        return new RemoteOutput(
            ShowText: belongs && IsProjecting && _presentation.State == OutputState.Content && slide is not null,
            MainText: slide?.MainText ?? string.Empty,
            SecondaryText: slide?.SecondaryText ?? string.Empty,
            Caption: theme.ShowCaption ? slide?.Caption ?? string.Empty : string.Empty,
            Overlay: overlays ? _presentation.OverlayMessage ?? string.Empty : string.Empty,
            Countdown: overlays ? countdown?.Format(DateTimeOffset.Now) ?? string.Empty : string.Empty,
            CountdownHeading: !overlays || countdown is null || countdown.HasFinished(DateTimeOffset.Now)
                ? string.Empty
                : countdown.Heading,
            FontFamily: over?.FontFamily ?? theme.FontFamily,
            FontSize: over?.FontSize ?? theme.MaxFontSize,
            Bold: over?.Bold ?? theme.Bold,
            Italic: over?.Italic ?? theme.Italic,
            Color: over?.TextColor ?? theme.TextColor,
            OutlineColor: over?.OutlineColor ?? theme.OutlineColor,
            OutlineWidth: over?.OutlineWidth ?? theme.OutlineWidth,
            AlignH: (over?.AlignH ?? theme.AlignH).ToString().ToLowerInvariant(),
            AlignV: (over?.AlignV ?? theme.AlignV).ToString().ToLowerInvariant(),
            MainMarks: Marks(_presentation.Highlight.Resolve(slide?.MainText)),
            SecondaryMarks: Marks(_presentation.Highlight.Resolve(slide?.SecondaryText, includeSpans: false)),
            Media: channel == OutputChannel.Announcements ? OnAirMedia() : null,
            Style: style is { FollowProjector: false } ? style with { BackgroundFile = Path.GetFileName(style.BackgroundFile) } : null,
            Blank: _presentation.State != OutputState.Content,
            Label: OnAir switch
            {
                OnAirSource.Song => (Projection.SlideLabel ?? string.Empty).ToUpperInvariant(),
                OnAirSource.Bible => slide?.Caption ?? string.Empty,
                _ => string.Empty,
            },
            // The name plate goes where the «aviso al pie» goes: the page with
            // everything and the announcements page.
            PlateName: plate?.Name ?? string.Empty,
            PlateRole: plate?.Role ?? string.Empty,
            PlateImage: plateFile is null ? string.Empty : NamePlateMedia.IsVideo(plateFile) ? "video" : "imagen",
            PlateImageKey: plateFile is null ? string.Empty : PlateFileKey(plateFile),
            PlateImageRight: plate?.ImageRight == true,
            PlateAnimation: plate?.Look.Enter switch
            {
                NamePlateAnimation.Rise => "subir",
                NamePlateAnimation.Wipe => "persiana",
                NamePlateAnimation.Fade => "aparecer",
                _ => "deslizar",
            },
            PlateStyle: plate?.Look);

        // The page asks for the picture again only when it is another file.
        static string PlateFileKey(string file) =>
            $"{Path.GetFileName(file).GetHashCode():x}-{new FileInfo(file).Length}";

        static IReadOnlyList<int[]> Marks(IReadOnlyList<TextSpan> spans) =>
            spans.Select(s => new[] { s.Start, s.Length }).ToList();
    }

    /// <summary>
    /// The picture or video that is alone on the screen right now, if it is one
    /// of the library's (a photo of a web page, a logo or a captured window has
    /// no file to hand over). The page draws it behind the words, fitted the
    /// way the projector fits it.
    /// </summary>
    private RemoteOutputMedia? OnAirMedia()
    {
        if (OnAir is not (OnAirSource.Media or OnAirSource.Presentation)
            || _presentation.Background is not { Id: > 0 } media
            || media.Type is not (MediaType.Image or MediaType.Video)
            || ((IRemoteHost)this).GetMediaFile(media.Id) is null)
            return null;

        return new RemoteOutputMedia(
            media.Id,
            media.Type == MediaType.Video ? "video" : "image",
            media.Scaling switch
            {
                MediaScaling.Fit => "contain",
                MediaScaling.Stretch => "fill",
                _ => "cover",
            },
            Loop: media.Type == MediaType.Video && media.EndBehavior == VideoEndBehavior.Loop,
            Backdrop: media.Scaling == MediaScaling.Fit && (media.FillsWithSelf || media.FillsWithOwnVideo));
    }

    /// <summary>
    /// The design a layer of the stream is drawn with. The page with everything
    /// takes the one of whatever is on air (the Bible, a song, or the rest).
    /// </summary>
    private BroadcastStyle StyleOf(OutputChannel channel)
    {
        if (channel == OutputChannel.All)
        {
            channel = OnAir switch
            {
                OnAirSource.Bible => OutputChannel.Bible,
                OnAirSource.Song => OutputChannel.Songs,
                _ => OutputChannel.Announcements,
            };
        }

        return BroadcastStyleSettings.Current.For(channel);
    }

    string? IRemoteHost.GetBackgroundFile(OutputChannel channel)
    {
        var style = StyleOf(channel);
        var path = style.BackgroundFile;
        return !style.FollowProjector
               && style.Background == BroadcastBackground.File
               && !string.IsNullOrWhiteSpace(path)
               && StreamableExtensions.Contains(Path.GetExtension(path))
               && File.Exists(path)
            ? path
            : null;
    }

    /// <summary>The kinds of file the stream page may be handed: what a browser plays.</summary>
    private static readonly HashSet<string> StreamableExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".mp4", ".m4v", ".webm", ".mov",
    };

    string? IRemoteHost.GetNamePlateFile() =>
        _presentation.NamePlate?.ImagePath is { } path
        && StreamableExtensions.Contains(Path.GetExtension(path))
        && File.Exists(path)
            ? path
            : null;

    string? IRemoteHost.GetMediaFile(int mediaId)
    {
        var media = _allMedia.FirstOrDefault(m => m.Id == mediaId);
        if (media is null || media.Type is not (MediaType.Image or MediaType.Video))
            return null;

        var path = media.Path;
        return !string.IsNullOrWhiteSpace(path)
               && StreamableExtensions.Contains(Path.GetExtension(path))
               && File.Exists(path)
            ? path
            : null;
    }

    void IRemoteHost.Execute(string action, int? index)
    {
        switch (action)
        {
            case "next": NextSlide(); break;
            case "prev": PreviousSlide(); break;
            case "clear": ToggleClear(); break;
            case "black": ToggleBlack(); break;
            case "logo": ToggleLogo(); break;
            case "output": ToggleOutput(); break;
            case "nobackground": ClearBackground(); break;
            case "slide" when index is int slide: GoLiveSlide(slide); break;
            case "presentation" when index is int shown:
                // The phone opens it on the computer: the pages come back with the next poll.
                if (PresentationsForRemote() is var offered && shown >= 0 && shown < offered.Count)
                    OpenPresentation(offered[shown].Tab, null);
                break;
            case "hit" when index is int hit && hit >= 0 && hit < _remoteHits.Count:
                // Loaded, not projected: its slides come back with the next poll for the phone to pick.
                OpenSearchHit(_remoteHits[hit]);
                break;
            case "playlist" when index is int item && item >= 0 && item < PlaylistItems.Count:
                ProjectPlaylistItem(PlaylistItems[item]);
                break;
            default:
                Log.Debug("El celular pidió una acción desconocida: {Action}", action);
                break;
        }
    }

    /// <summary>Slide previews travel on every poll, so they are kept short.</summary>
    private static string Shorten(string text)
    {
        var single = text.Replace('\n', ' ').Replace('\r', ' ').Trim();
        return single.Length <= 70 ? single : single[..70] + "…";
    }
}
