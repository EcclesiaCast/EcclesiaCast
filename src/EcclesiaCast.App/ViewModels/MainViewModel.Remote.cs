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
    // ── Control desde el celular ─────────────────────────────────

    private const string RemotePinKey = "remote.pin";
    private const string RemotePortKey = "remote.port";
    private const string RemoteEnabledKey = "remote.enabled";

    private RemoteControlServer? _remote;

    [ObservableProperty]
    private bool _isRemoteRunning;

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
        if (string.IsNullOrWhiteSpace(pin) || pin.Length != 4)
        {
            pin = Random.Shared.Next(1000, 10000).ToString();
            _settings.Set(RemotePinKey, pin);
        }

        var port = int.TryParse(_settings.Get(RemotePortKey), out var saved) ? saved : 8080;

        if (!_remote.Start(port, pin))
            return false;

        _settings.Set(RemotePortKey, _remote.Port.ToString());
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
            MessageBox.Show(
                "No se pudo abrir ningún puerto para el control remoto. "
                + "Puede que otro programa los esté usando.",
                "EcclesiaCast", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var window = new Views.RemoteControlWindow(_remote!)
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
    RemoteOutput IRemoteHost.GetOutput()
    {
        var slide = Projection.Slide;
        var theme = slide?.Theme ?? SlideTheme.Fallback;
        var over = slide?.Override;
        var countdown = _presentation.Countdown;

        return new RemoteOutput(
            ShowText: IsProjecting && _presentation.State == OutputState.Content && slide is not null,
            MainText: slide?.MainText ?? string.Empty,
            SecondaryText: slide?.SecondaryText ?? string.Empty,
            Caption: theme.ShowCaption ? slide?.Caption ?? string.Empty : string.Empty,
            Overlay: _presentation.OverlayMessage ?? string.Empty,
            Countdown: countdown?.Format(DateTimeOffset.Now) ?? string.Empty,
            CountdownHeading: countdown is null || countdown.HasFinished(DateTimeOffset.Now)
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
            SecondaryMarks: Marks(_presentation.Highlight.Resolve(slide?.SecondaryText, includeSpans: false)));

        static IReadOnlyList<int[]> Marks(IReadOnlyList<TextSpan> spans) =>
            spans.Select(s => new[] { s.Start, s.Length }).ToList();
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
