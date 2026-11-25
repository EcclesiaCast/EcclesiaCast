using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Audio;
using EcclesiaCast.Core.Media;
using Serilog;

namespace EcclesiaCast.App.ViewModels;

/// <summary>
/// YouTube inside the Música tab, sound only: search an artist, a song or a
/// pad, hear it at once (over the internet, with no picture and no ads), add
/// it to the list, or save its sound to work without internet. Whatever is on
/// the screen (logo, lyrics on their video, the Bible) does not change.
/// </summary>
public sealed partial class MainViewModel
{
    private const string AudioRepeatKey = "audio.repeat";
    private const int YouTubeResultCount = 20;

    /// <summary>The play press that is current; an older YouTube lookup that finishes late is dropped.</summary>
    private int _audioPlayRequest;

    private CancellationTokenSource? _youTubeSearch;

    /// <summary>Videos whose sound is being saved right now, so a second click does not save it twice.</summary>
    private readonly HashSet<string> _youTubeSaving = [];

    /// <summary>What the operator types in the YouTube search of the Música tab.</summary>
    [ObservableProperty]
    private string _youTubeMusicQuery = string.Empty;

    public ObservableCollection<YouTubeSearchResult> YouTubeMusicResults { get; } = [];

    [ObservableProperty]
    private bool _hasYouTubeMusicResults;

    [ObservableProperty]
    private bool _isSearchingYouTubeMusic;

    /// <summary>The repeat button: no repeat, the whole list, or this song.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AudioRepeatGlyph), nameof(AudioRepeatTip), nameof(IsAudioRepeatOn))]
    private AudioRepeatMode _audioRepeat;

    public string AudioRepeatGlyph => AudioRepeat == AudioRepeatMode.One ? "🔂" : "🔁";

    public bool IsAudioRepeatOn => AudioRepeat != AudioRepeatMode.Off;

    public string AudioRepeatTip => AudioRepeat switch
    {
        AudioRepeatMode.All => "Repite la lista: después del último vuelve al primero. Clic: repetir sólo esta",
        AudioRepeatMode.One => "Repite esta canción una y otra vez (ideal para un pad). Clic: sin repetir",
        _ => "Sin repetir: la música para después del último. Clic: repetir la lista",
    };

    partial void OnAudioRepeatChanged(AudioRepeatMode value) =>
        _settings.Set(AudioRepeatKey, value.ToString());

    private void InitializeYouTubeMusic()
    {
        AudioRepeat = Enum.TryParse<AudioRepeatMode>(_settings.Get(AudioRepeatKey), out var mode)
            ? mode
            : AudioRepeatMode.Off;
        YouTubeMusicResults.CollectionChanged += (_, _) => HasYouTubeMusicResults = YouTubeMusicResults.Count > 0;
    }

    [RelayCommand]
    private void CycleAudioRepeat()
    {
        AudioRepeat = AudioNavigator.Cycle(AudioRepeat);
        StatusText = AudioRepeat switch
        {
            AudioRepeatMode.All => "Repetir la lista: después del último audio vuelve al primero.",
            AudioRepeatMode.One => "Repetir esta: el audio que suena vuelve a empezar al terminar.",
            _ => "Sin repetir: la música para después del último audio de la lista.",
        };
    }

    /// <summary>Progress of yt-dlp (its own download, its update, the search) shown in the status bar.</summary>
    private IProgress<(double Percent, string Message)> StatusProgress() =>
        new Progress<(double Percent, string Message)>(p => StatusText = p.Message);

    // ── Buscar ───────────────────────────────────────────────────

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task SearchYouTubeMusicAsync()
    {
        var query = YouTubeMusicQuery.Trim();
        if (query.Length == 0)
        {
            StatusText = "Escribí un artista, una canción o, por ejemplo, «pad en Re».";
            return;
        }

        // A pasted link is that video: no search, only its title and length.
        var linkedId = query.Length > 11 ? YouTubeUrl.TryParseVideoId(query) : null;

        _youTubeSearch?.Cancel();
        var search = new CancellationTokenSource();
        _youTubeSearch = search;
        IsSearchingYouTubeMusic = true;

        try
        {
            IReadOnlyList<YouTubeSearchResult> results = linkedId is not null
                ? [await YtDlp.LookUpAsync(linkedId, StatusProgress(), search.Token)]
                : await YtDlp.SearchAsync(query, YouTubeResultCount, StatusProgress(), search.Token);
            if (search.IsCancellationRequested)
                return;

            YouTubeMusicResults.Clear();
            foreach (var result in results)
                YouTubeMusicResults.Add(result);

            StatusText = linkedId is not null
                ? "Link de YouTube: tocá ▶ para escucharlo, ➕ para sumarlo a la lista o 💾 para guardarlo."
                : results.Count > 0
                    ? $"{results.Count} resultado(s) de YouTube para «{query}». ▶ escucha, ➕ suma a la lista, 💾 guarda para usar sin internet."
                    : $"YouTube no encontró nada para «{query}».";

            // While the person reads the titles, the first ones are brought
            // ahead: pressing ▶ on one of them then plays at once.
            if (linkedId is null)
                PrefetchTopResults(results);
        }
        catch (OperationCanceledException)
        {
            // A newer search took its place.
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Falló la búsqueda de YouTube {Query}", query);
            StatusText = $"No se pudo buscar en YouTube. {YtDlp.ExplainMusic(ex)}";
        }
        finally
        {
            if (ReferenceEquals(_youTubeSearch, search))
            {
                IsSearchingYouTubeMusic = false;
                _youTubeSearch = null;
            }

            search.Dispose();
        }
    }

    /// <summary>The tone buttons under the search box.</summary>
    public IReadOnlyList<string> PadKeys => YouTubeAudio.PadKeys;

    /// <summary>A tone button: searches YouTube for pads in that tone.</summary>
    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task SearchPadAsync(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return Task.CompletedTask;

        YouTubeMusicQuery = YouTubeAudio.PadQuery(key);
        return SearchYouTubeMusicAsync();
    }

    [RelayCommand]
    private void ClearYouTubeMusicResults()
    {
        _youTubeSearch?.Cancel();
        YouTubeMusicResults.Clear();
        YouTubeMusicQuery = string.Empty;
    }

    // ── Escuchar, sumar a la lista, guardar ──────────────────────

    /// <summary>▶ on a result: plays it now without touching the list.</summary>
    [RelayCommand]
    private void ListenYouTubeResult(YouTubeSearchResult? result)
    {
        if (result is null)
            return;

        // Already in the list: that row plays, so ⏭ and the repeat follow the list.
        if (AudioTracks.FirstOrDefault(t => YouTubeAudio.IsTrackOf(t.Path, result.VideoId)) is { } inList)
        {
            PlayAudio(inList);
            return;
        }

        PlayAudio(new AudioTrackItem(new AudioTrack
        {
            Id = 0,
            Name = result.Title,
            Path = YouTubeUrl.WatchUrl(result.VideoId),
        }));
    }

    /// <summary>➕ on a result: goes to the end of the list, played over the internet.</summary>
    [RelayCommand]
    private void AddYouTubeResult(YouTubeSearchResult? result)
    {
        if (result is null)
            return;

        StatusText = AddYouTubeTracks([result]) > 0
            ? $"«{result.Title}» se sumó a la lista (suena por internet; con 💾 queda guardado para usar sin internet)."
            : $"«{result.Title}» ya estaba en la lista.";
    }

    /// <summary>«Sumar todos»: every result to the list, for an hour of background music in one click.</summary>
    [RelayCommand]
    private void AddAllYouTubeResults()
    {
        if (YouTubeMusicResults.Count == 0)
            return;

        var added = AddYouTubeTracks(YouTubeMusicResults.ToList());
        var repeated = YouTubeMusicResults.Count - added;
        StatusText = $"{added} audio(s) de YouTube sumado(s) a la lista."
                     + (repeated > 0 ? $" {repeated} ya estaba(n)." : string.Empty);
    }

    private int AddYouTubeTracks(IReadOnlyList<YouTubeSearchResult> results)
    {
        var added = 0;
        foreach (var result in results)
        {
            if (AudioTracks.Any(t => YouTubeAudio.IsTrackOf(t.Path, result.VideoId)))
                continue;

            _audios.Add(new AudioTrack { Name = result.Title, Path = YouTubeUrl.WatchUrl(result.VideoId) });
            added++;
        }

        if (added > 0)
            LoadAudioTracks();
        return added;
    }

    /// <summary>💾 on a result: saves its sound and puts it in the list as a file.</summary>
    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task SaveYouTubeResultAsync(YouTubeSearchResult? result) =>
        result is null ? Task.CompletedTask : SaveYouTubeSoundAsync(result.VideoId, result.Title);

    /// <summary>💾 of the list: the selected YouTube song is saved to this computer, in its same place.</summary>
    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task SaveSelectedAudioAsync()
    {
        if (SelectedAudio is not { } item || YouTubeAudio.TrackVideoId(item.Path) is not { } videoId)
        {
            StatusText = "Elegí en la lista un audio de YouTube (los que tienen 🌐) para guardarlo en la computadora.";
            return Task.CompletedTask;
        }

        return SaveYouTubeSoundAsync(videoId, item.Name);
    }

    private async Task SaveYouTubeSoundAsync(string videoId, string title)
    {
        if (!_youTubeSaving.Add(videoId))
        {
            StatusText = $"«{title}» ya se está guardando…";
            return;
        }

        try
        {
            var progress = new Progress<(double Percent, string Message)>(p =>
                StatusText = p.Percent >= 0
                    ? $"Guardando «{title}»… {p.Percent:0}%"
                    : p.Message);

            var file = await YtDlp.SaveAudioAsync(videoId, progress);
            if (file is null || !File.Exists(file))
            {
                StatusText = $"No se pudo guardar «{title}».";
                return;
            }

            // The YouTube row of the list becomes this file, in its same place;
            // with no row, the file goes to the end of the list.
            var row = AudioTracks.FirstOrDefault(t => YouTubeAudio.TrackVideoId(t.Path) == videoId);
            if (row is not null)
            {
                row.Track.Path = file;
                _audios.Update(row.Track);
            }
            else if (!AudioTracks.Any(t => string.Equals(t.Path, file, StringComparison.OrdinalIgnoreCase)))
            {
                _audios.Add(new AudioTrack { Name = title, Path = file });
            }

            LoadAudioTracks();
            StatusText = $"«{title}» quedó guardado: ahora funciona sin internet (está en Música › EcclesiaCast).";
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "No se pudo guardar el audio de YouTube {Id}", videoId);
            StatusText = $"No se pudo guardar «{title}». {YtDlp.ExplainMusic(ex)}";
        }
        finally
        {
            _youTubeSaving.Remove(videoId);
        }
    }

    // ── Reproducir por internet ──────────────────────────────────

    /// <summary>
    /// Plays a YouTube song at once. From a file if its sound is already in the
    /// cache; otherwise straight from the internet: the address of the sound
    /// (already asked for while the results were shown, or asked now) is read
    /// as it plays, with no download first. Only if that fails does it bring
    /// the file, as before. Meanwhile the button already shows ⏸ and a newer
    /// press replaces this one.
    /// </summary>
    private async Task PlayYouTubeTrackAsync(AudioTrackItem item, string videoId, int request, bool allowStream = true)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();

        // What was sounding stops now: the click is answered at once.
        _audioPlayer.Stop();
        _streamingVideoId = null;
        CurrentAudio = item;
        if (!item.IsPreview)
            SelectedAudio = item;
        SetAudioProgress(TimeSpan.Zero, TimeSpan.Zero);
        _audioOpeningUntil = DateTime.UtcNow + TimeSpan.FromSeconds(75);
        IsAudioPlaying = true;
        StatusText = $"Buscando el audio de «{item.Name}» en YouTube…";

        try
        {
            if (allowStream && YtDlp.CachedAudio(videoId) is null)
            {
                try
                {
                    var url = await YtDlp.ResolveStreamUrlAsync(videoId);
                    var resolved = clock.ElapsedMilliseconds;
                    if (request != _audioPlayRequest)
                        return;

                    var stream = await Task.Run(() => new RangedHttpStream(url));
                    if (request != _audioPlayRequest)
                    {
                        stream.Dispose();
                        return;
                    }

                    if (StartAudio(item, url, AudioOpeningGrace, stream))
                    {
                        _streamingVideoId = videoId;
                        Log.Information("YouTube {Id}: dirección {Resolved} ms, sonando {Total} ms después de ▶",
                            videoId, resolved, clock.ElapsedMilliseconds);
                        PrepareNextYouTubeTrack();
                        return;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // The file way below still has a chance.
                    Log.Warning(ex, "No se pudo escuchar {Id} por internet directo; se baja el archivo", videoId);
                    YtDlp.ForgetStreamUrl(videoId);
                    if (request != _audioPlayRequest)
                        return;
                }
            }

            var file = await YtDlp.GetAudioFileAsync(videoId, new Progress<(double Percent, string Message)>(p =>
                StatusText = p.Percent >= 0 ? $"Trayendo «{item.Name}» de YouTube… {p.Percent:0}%" : p.Message));
            if (request != _audioPlayRequest)
                return;

            if (StartAudio(item, file, AudioOpeningGrace))
            {
                Log.Information("YouTube {Id}: sonando del archivo {Total} ms después de ▶", videoId, clock.ElapsedMilliseconds);
                PrepareNextYouTubeTrack();
            }
        }
        catch (Exception ex)
        {
            if (request != _audioPlayRequest)
                return;

            Log.Warning(ex, "No se pudo reproducir de YouTube {Id}", videoId);
            var name = item.Name;
            StopAudio();
            StatusText = $"No se pudo reproducir «{name}». {YtDlp.ExplainMusic(ex)}";
        }
    }

    /// <summary>The video that is sounding straight from the internet (not from a file), or null.</summary>
    private string? _streamingVideoId;

    /// <summary>How many of the first results get their address asked ahead of time.</summary>
    private const int PrefetchResults = 5;

    private CancellationTokenSource? _prefetch;

    /// <summary>
    /// Asks, in one yt-dlp and while the person reads the titles, the address
    /// of the sound of the first results: ▶ on one of them then starts at
    /// once. A newer search stops it.
    /// </summary>
    private void PrefetchTopResults(IReadOnlyList<YouTubeSearchResult> results)
    {
        _prefetch?.Cancel();
        var cancel = new CancellationTokenSource();
        _prefetch = cancel;

        var ids = results.Where(r => r.Duration is not null).Take(PrefetchResults).Select(r => r.VideoId).ToList();
        _ = YtDlp.PreResolveStreamUrlsAsync(ids, cancel.Token);
    }

    /// <summary>
    /// Brings ahead the sound of the YouTube song that comes after this one,
    /// so background music goes from song to song without a long silence.
    /// </summary>
    private void PrepareNextYouTubeTrack()
    {
        if (CurrentAudio is not { IsPreview: false } current)
            return;

        var index = AudioTracks.IndexOf(current);
        if (AudioNavigator.AfterEnd(AudioTracks.Count, index, AudioRepeat) is not { } next || next == index)
            return;

        if (YouTubeAudio.TrackVideoId(AudioTracks[next].Path) is not { } videoId)
            return;

        _ = PrepareAsync();

        async Task PrepareAsync()
        {
            try
            {
                await YtDlp.GetAudioFileAsync(videoId, new Progress<(double, string)>());
            }
            catch (Exception ex)
            {
                // It is asked again when its turn comes.
                Log.Debug(ex, "No se pudo preparar el próximo audio de YouTube {Id}", videoId);
            }
        }
    }
}
