using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Audio;
using EcclesiaCast.Core.Media;
using Serilog;

namespace EcclesiaCast.App.ViewModels;

/// <summary>
/// The Música tab's own list of audio files and the player that goes with it
/// (its own LibVLC player, apart from the one of the background video, with
/// its own volume). The Spotify keys next to it are unrelated.
/// </summary>
public sealed partial class MainViewModel
{
    private const string AudioVolumeKey = "audio.volume";

    private readonly IAudioRepository _audios;
    private readonly IAudioPlayer _audioPlayer;

    /// <summary>Asks the player how far along it is while a track is loaded.</summary>
    private readonly System.Windows.Threading.DispatcherTimer _audioTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(250),
    };

    private bool _updatingAudio;

    /// <summary>
    /// The player reads «stopped» for a moment after Play (the first track of
    /// a session can take a second or two to open). Until then the track
    /// counts as playing: the button shows pause instead of inviting a second
    /// click that would start the opening over.
    /// </summary>
    private DateTime _audioOpeningUntil = DateTime.MinValue;

    private static readonly TimeSpan AudioOpeningGrace = TimeSpan.FromSeconds(4);

    private bool AudioIsOpening => _audioPlayer.State == AudioPlayerState.Stopped && DateTime.UtcNow < _audioOpeningUntil;

    public ObservableCollection<AudioTrackItem> AudioTracks { get; } = [];

    [ObservableProperty]
    private bool _hasAudios;

    /// <summary>The row the operator has selected (to move it or take it out).</summary>
    [ObservableProperty]
    private AudioTrackItem? _selectedAudio;

    /// <summary>The track loaded in the player: playing, paused, or just finished.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AudioNowPlayingText))]
    private AudioTrackItem? _currentAudio;

    [ObservableProperty]
    private bool _isAudioPlaying;

    [ObservableProperty]
    private double _audioPositionSeconds;

    [ObservableProperty]
    private double _audioDurationSeconds = 0.001;

    [ObservableProperty]
    private string _audioTimeText = "0:00 / 0:00";

    /// <summary>The audio's own volume, 0 to 100: it does not touch the video's or Windows'.</summary>
    [ObservableProperty]
    private int _audioVolume = 80;

    public string AudioNowPlayingText => CurrentAudio is { } track ? track.Name : "Elegí un audio de la lista";

    partial void OnCurrentAudioChanged(AudioTrackItem? oldValue, AudioTrackItem? newValue)
    {
        if (oldValue is not null)
            oldValue.IsCurrent = false;
        if (newValue is not null)
            newValue.IsCurrent = true;
    }

    partial void OnAudioVolumeChanged(int value)
    {
        // Heard at once; the database is written when the bar rests (a write
        // on every step made the bar lag behind the hand).
        _audioPlayer.Volume = value;
        _audioVolumeSave ??= NewRestTimer(TimeSpan.FromMilliseconds(500), () =>
            _settings.Set(AudioVolumeKey, AudioVolume.ToString()));
        Restart(_audioVolumeSave);
    }

    private System.Windows.Threading.DispatcherTimer? _audioVolumeSave;

    partial void OnAudioPositionSecondsChanged(double value)
    {
        // Only a move the operator makes on the bar: the timer's own updates
        // must not seek the player back to where it already is.
        if (_updatingAudio || CurrentAudio is null || _audioPlayer.State == AudioPlayerState.Stopped)
            return;

        _audioPlayer.SeekTo(TimeSpan.FromSeconds(value));
    }

    private void InitializeAudio()
    {
        // The volume the operator left last time (the setter hands it to the player).
        AudioVolume = int.TryParse(_settings.Get(AudioVolumeKey), out var volume) ? Math.Clamp(volume, 0, 100) : 80;
        _audioPlayer.Volume = AudioVolume;

        AudioTracks.CollectionChanged += (_, _) => HasAudios = AudioTracks.Count > 0;
        _audioTimer.Tick += (_, _) => RefreshAudioProgress();
        _audioPlayer.Ended += (_, _) => OnAudioEnded();
        _audioPlayer.Failed += (_, _) => OnAudioFailed();
        LoadAudioTracks();
        InitializeYouTubeMusic();
    }

    /// <summary>Reads the list from the library again, keeping the selected and the loaded track.</summary>
    private void LoadAudioTracks()
    {
        var selectedId = SelectedAudio?.Id;
        var current = CurrentAudio;

        AudioTracks.Clear();
        foreach (var track in _audios.GetAll())
            AudioTracks.Add(new AudioTrackItem(track));

        SelectedAudio = AudioTracks.FirstOrDefault(t => t.Id == selectedId);
        CurrentAudio = current switch
        {
            null => null,
            // A song heard from the YouTube search is not a row: it stays the one
            // sounding, and becomes its row once it is added to the list.
            { IsPreview: true } preview => YouTubeAudio.TrackVideoId(preview.Path) is { } videoId
                && AudioTracks.FirstOrDefault(t => YouTubeAudio.IsTrackOf(t.Path, videoId)) is { } row
                    ? row
                    : preview,
            _ => AudioTracks.FirstOrDefault(t => t.Id == current.Id),
        };
    }

    // ── Agregar, quitar y ordenar ────────────────────────────────

    [RelayCommand]
    private void ImportAudio()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Agregar audios",
            Filter = "Audios (mp3, wav, m4a, ogg, flac)|*.mp3;*.wav;*.m4a;*.ogg;*.flac",
            Multiselect = true,
        };
        if (dialog.ShowDialog() != true)
            return;

        AddAudioFiles(dialog.FileNames);
    }

    /// <summary>
    /// The part of «Agregar» that starts from paths, so dropping files from
    /// Explorer onto the list does exactly the same. What is not taken (not an
    /// audio file, or already in the list) is counted and said in the status bar.
    /// </summary>
    public void AddAudioFiles(IEnumerable<string> paths)
    {
        var known = AudioTracks.Select(t => t.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var added = 0;
        var notAudio = 0;
        var repeated = 0;

        foreach (var path in paths)
        {
            if (!AudioFiles.IsAudio(path) || !File.Exists(path))
            {
                notAudio++;
                continue;
            }

            if (!known.Add(path))
            {
                repeated++;
                continue;
            }

            _audios.Add(new AudioTrack { Name = AudioFiles.DisplayName(path), Path = path });
            added++;
        }

        LoadAudioTracks();

        var extra = (notAudio > 0 ? $" {notAudio} archivo(s) no se agregaron: sólo entran mp3, wav, m4a, ogg y flac." : string.Empty)
                    + (repeated > 0 ? $" {repeated} ya estaba(n) en la lista." : string.Empty);
        StatusText = added > 0
            ? $"{added} audio(s) agregado(s) a la lista.{extra}"
            : $"No se agregó ningún audio.{extra}";
    }

    [RelayCommand]
    private void RemoveAudio()
    {
        if (SelectedAudio is not { } item)
        {
            StatusText = "Elegí en la lista el audio que querés quitar.";
            return;
        }

        if (ReferenceEquals(item, CurrentAudio))
            StopAudio();

        _audios.Delete(item.Id);
        var index = AudioTracks.IndexOf(item);
        LoadAudioTracks();

        // Leaves the selection where the removed row was, to take out the next one.
        if (AudioTracks.Count > 0)
            SelectedAudio = AudioTracks[Math.Min(index, AudioTracks.Count - 1)];

        StatusText = item.IsYouTube
            ? $"«{item.Name}» salió de la lista."
            : $"«{item.Name}» salió de la lista. El archivo sigue en su lugar.";
    }

    [RelayCommand]
    private void MoveAudioUp() => MoveSelectedAudio(-1);

    [RelayCommand]
    private void MoveAudioDown() => MoveSelectedAudio(+1);

    private void MoveSelectedAudio(int delta)
    {
        if (SelectedAudio is not { } item)
            return;

        var index = AudioTracks.IndexOf(item);
        var moved = ListOrder.Move(AudioTracks.Select(t => t.Id).ToList(), index, delta);
        if (moved < 0)
            return;

        AudioTracks.Move(index, moved);
        SelectedAudio = item;
        SaveAudioOrder();
    }

    [RelayCommand]
    private void SortAudioByName()
    {
        var sorted = AudioTracks
            .OrderBy(t => t.Name, Comparer<string>.Create(ListOrder.CompareNatural))
            .ToList();

        var selected = SelectedAudio;
        AudioTracks.Clear();
        foreach (var item in sorted)
            AudioTracks.Add(item);

        SelectedAudio = selected;
        SaveAudioOrder();
        StatusText = "Audios ordenados por nombre.";
    }

    // ── Clic derecho en un audio de la lista ─────────────────────

    /// <summary>Right click → «Reproducir en bucle sólo este»: starts it and repeats it until the repeat is changed.</summary>
    [RelayCommand]
    private void PlayAudioLooping(AudioTrackItem? item)
    {
        if (item is null)
            return;

        AudioRepeat = AudioRepeatMode.One;
        PlayAudio(item);
        StatusText = $"«{item.Name}» suena en bucle. Con 🔁 se vuelve a cambiar.";
    }

    /// <summary>Right click → «Guardar para usar sin internet»: the same as 💾, on the row that was clicked.</summary>
    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task SaveAudioItemAsync(AudioTrackItem? item)
    {
        if (item is null || YouTubeAudio.TrackVideoId(item.Path) is not { } videoId)
            return Task.CompletedTask;

        return SaveYouTubeSoundAsync(videoId, item.Name);
    }

    [RelayCommand]
    private void RemoveAudioItem(AudioTrackItem? item)
    {
        if (item is null)
            return;

        SelectedAudio = item;
        RemoveAudio();
    }

    [RelayCommand]
    private void MoveAudioItemUp(AudioTrackItem? item)
    {
        SelectedAudio = item;
        MoveSelectedAudio(-1);
    }

    [RelayCommand]
    private void MoveAudioItemDown(AudioTrackItem? item)
    {
        SelectedAudio = item;
        MoveSelectedAudio(+1);
    }

    [RelayCommand]
    private void ShowAudioInFolder(AudioTrackItem? item)
    {
        if (item is null || !File.Exists(item.Path))
        {
            StatusText = "No se encuentra el archivo.";
            return;
        }

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"/select,\"{item.Path}\"",
            UseShellExecute = true,
        });
    }

    [RelayCommand]
    private void CopyAudioLink(AudioTrackItem? item)
    {
        if (item is null || YouTubeAudio.TrackVideoId(item.Path) is not { } videoId)
            return;

        try
        {
            System.Windows.Clipboard.SetText($"https://www.youtube.com/watch?v={videoId}");
            StatusText = "El link de YouTube quedó copiado.";
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "No se pudo copiar el link");
            StatusText = "No se pudo copiar el link: el portapapeles está ocupado.";
        }
    }

    private void SaveAudioOrder() => _audios.SaveOrder(AudioTracks.Select(t => t.Id).ToList());

    // ── Reproducir ───────────────────────────────────────────────

    [RelayCommand]
    private void PlayAudio(AudioTrackItem? item)
    {
        if (item is null)
            return;

        if (!_audioPlayer.IsAvailable)
        {
            StatusText = "El reproductor de audio no arrancó en esta computadora: no se puede reproducir.";
            return;
        }

        // A newer press wins over a YouTube song still being looked up.
        var request = ++_audioPlayRequest;

        if (YouTubeAudio.TrackVideoId(item.Path) is { } videoId)
        {
            _ = PlayYouTubeTrackAsync(item, videoId, request);
            return;
        }

        if (!File.Exists(item.Path))
        {
            StatusText = $"No se encuentra el archivo de «{item.Name}»: ¿lo movieron o lo borraron?";
            return;
        }

        StartAudio(item, item.Path, AudioOpeningGrace);
    }

    /// <summary>Hands the player the file to play (for a YouTube song, its sound brought to this computer) and shows it as loaded.</summary>
    private bool StartAudio(AudioTrackItem item, string source, TimeSpan grace, Stream? stream = null)
    {
        try
        {
            if (stream is not null)
                _audioPlayer.PlayStream(stream);
            else
                _audioPlayer.Play(source);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "No se pudo reproducir el audio {Path}", item.Path);
            StatusText = $"No se pudo reproducir «{item.Name}».";
            return false;
        }

        CurrentAudio = item;
        if (!item.IsPreview)
            SelectedAudio = item;
        _audioOpeningUntil = DateTime.UtcNow + grace;
        _audioTimer.Start();
        RefreshAudioProgress();
        StatusText = item.IsYouTube
            ? $"Sonando de YouTube: «{item.Name}»."
            : $"Sonando: «{item.Name}».";
        return true;
    }

    /// <summary>The ⏯ button: pause, resume, start over a track that ended, or start the selected one.</summary>
    [RelayCommand]
    private void ToggleAudioPlayPause()
    {
        if (CurrentAudio is null)
        {
            var first = SelectedAudio ?? AudioTracks.FirstOrDefault();
            if (first is null)
            {
                StatusText = "Agregá audios a la lista con ➕ para reproducirlos.";
                return;
            }

            PlayAudio(first);
            return;
        }

        // Still opening: a second press would only start it over.
        if (AudioIsOpening)
            return;

        switch (_audioPlayer.State)
        {
            case AudioPlayerState.Playing:
                _audioPlayer.Pause();
                break;
            case AudioPlayerState.Paused:
                _audioPlayer.Resume();
                break;
            default:
                PlayAudio(CurrentAudio);
                return;
        }

        RefreshAudioProgress();
    }

    [RelayCommand]
    private void PreviousAudio()
    {
        var index = CurrentAudio is null ? -1 : AudioTracks.IndexOf(CurrentAudio);
        var position = _audioPlayer.State == AudioPlayerState.Stopped ? 0 : _audioPlayer.Position.TotalSeconds;

        if (AudioNavigator.Previous(AudioTracks.Count, index, position) is { } target)
            PlayAudio(AudioTracks[target]);
    }

    [RelayCommand]
    private void NextAudio()
    {
        var index = CurrentAudio is null ? -1 : AudioTracks.IndexOf(CurrentAudio);
        if (AudioNavigator.NextPressed(AudioTracks.Count, index, AudioRepeat) is { } target)
            PlayAudio(AudioTracks[target]);
        else if (AudioTracks.Count > 0)
            StatusText = "Es el último audio de la lista.";
    }

    [RelayCommand]
    private void SkipAudioBack() => SkipAudio(TimeSpan.FromSeconds(-10));

    [RelayCommand]
    private void SkipAudioForward() => SkipAudio(TimeSpan.FromSeconds(10));

    private void SkipAudio(TimeSpan delta)
    {
        if (CurrentAudio is null || _audioPlayer.State == AudioPlayerState.Stopped)
            return;

        var target = _audioPlayer.Position + delta;
        var length = _audioPlayer.Duration;

        // Short of the very end: skipping past it would end the track by accident.
        if (length > TimeSpan.Zero && target > length - TimeSpan.FromMilliseconds(500))
            target = length - TimeSpan.FromMilliseconds(500);

        _audioPlayer.SeekTo(target < TimeSpan.Zero ? TimeSpan.Zero : target);
        RefreshAudioProgress();
    }

    /// <summary>Stops the sound and lets go of the track. Also called when the program closes.</summary>
    public void StopAudio()
    {
        _audioPlayRequest++;
        _audioTimer.Stop();
        _audioPlayer.Stop();
        _audioOpeningUntil = DateTime.MinValue;
        IsAudioPlaying = false;
        SetAudioProgress(TimeSpan.Zero, TimeSpan.Zero);
        CurrentAudio = null;
    }

    private void OnAudioEnded()
    {
        // A song heard from the search, outside the list: it only goes on with
        // «repetir esta»; it does not start the list by itself.
        if (CurrentAudio is { IsPreview: true } preview)
        {
            if (AudioRepeat == AudioRepeatMode.One)
            {
                PlayAudio(preview);
                return;
            }

            FinishAudio("Terminó el audio de YouTube.");
            return;
        }

        var index = CurrentAudio is null ? -1 : AudioTracks.IndexOf(CurrentAudio);

        if (AudioNavigator.AfterEnd(AudioTracks.Count, index, AudioRepeat) is { } next)
        {
            PlayAudio(AudioTracks[next]);
            return;
        }

        // The last one, and no repeat: the music does not start over by itself.
        FinishAudio("Terminó el último audio de la lista.");
    }

    private void FinishAudio(string message)
    {
        _audioTimer.Stop();
        _audioOpeningUntil = DateTime.MinValue;
        IsAudioPlaying = false;
        SetAudioProgress(TimeSpan.Zero, TimeSpan.Zero);
        StatusText = message;
    }

    private void OnAudioFailed()
    {
        var current = CurrentAudio;
        var name = current?.Name ?? "el audio";
        StopAudio();

        if (current is not null && YouTubeAudio.TrackVideoId(current.Path) is { } videoId)
        {
            if (_streamingVideoId == videoId)
            {
                // Straight from the internet failed (the address expired or was refused):
                // once more, bringing the file.
                YtDlp.ForgetStreamUrl(videoId);
                _streamingVideoId = null;
                _ = PlayYouTubeTrackAsync(current, videoId, ++_audioPlayRequest, allowStream: false);
                return;
            }

            // The sound brought from YouTube came damaged (a download cut short):
            // it is thrown away so the next ▶ brings it again.
            YtDlp.ForgetCachedAudio(videoId);
            StatusText = $"No se pudo reproducir «{name}»: el audio llegó incompleto de YouTube. Tocá ▶ de nuevo y se vuelve a traer.";
            return;
        }

        StatusText = $"No se pudo reproducir «{name}»: el archivo puede estar dañado.";
    }

    private void RefreshAudioProgress()
    {
        var state = _audioPlayer.State;
        IsAudioPlaying = state == AudioPlayerState.Playing || AudioIsOpening;
        if (state != AudioPlayerState.Stopped)
            _audioOpeningUntil = DateTime.MinValue;

        // Stopped is also what a track reads for the instant after Play, while
        // the file is still being opened: nothing to show yet, but the timer
        // must keep going or the bar would never start moving.
        if (state == AudioPlayerState.Stopped)
            return;

        SetAudioProgress(_audioPlayer.Position, _audioPlayer.Duration);
    }

    private void SetAudioProgress(TimeSpan position, TimeSpan duration)
    {
        _updatingAudio = true;
        AudioDurationSeconds = Math.Max(duration.TotalSeconds, 0.001);
        AudioPositionSeconds = Math.Clamp(position.TotalSeconds, 0, AudioDurationSeconds);
        _updatingAudio = false;

        AudioTimeText = duration > TimeSpan.Zero
            ? $"{Clock(position)} / {Clock(duration)}"
            : $"{Clock(position)} / --:--";
    }
}
