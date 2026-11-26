using EcclesiaCast.Core.Audio;
using EcclesiaCast.Presentation.Platform;
using LibVLCSharp.Shared;
using Serilog;

namespace EcclesiaCast.Presentation.Services;

/// <summary>
/// Plays the Música tab's audio with LibVLC, on a player of its own: the one
/// that draws the background video is never shared, so pausing, changing or
/// muting one cannot reach the other. The engine (the same LibVLC the video
/// uses) is looked up when the first track plays, so a computer where it did
/// not start simply has no audio list instead of failing at start-up.
///
/// It sends its sound through DirectSound, not through LibVLC's usual Windows
/// output, so that the music list's bar acts while it is being dragged: the
/// usual output has one volume for the whole program, and the equalizer (the
/// other way to turn one player down) is only heard about 1.2 s later, because
/// LibVLC passes the sound through it that long before it plays.
/// Each song gets a new player: one that was stopped and played again came
/// back at full volume and no longer obeyed (measured on LibVLC 3.0.23).
/// </summary>
public sealed class VlcAudioPlayer(Func<LibVLC?> engine) : IAudioPlayer
{
    private MediaPlayer? _player;
    private readonly LivePlayerVolume _volume = new(80);

    /// <summary>The file being played, to reopen it in place when the output device changes.</summary>
    private string? _path;

    /// <summary>The <see cref="AudioOutputChanges.Generation"/> this player's output was opened under.</summary>
    private int _audioGeneration;

    private bool _listening;

    /// <summary>
    /// Bumped by every Play and Stop. An «ended» that VLC raised for a track
    /// which was replaced before the UI thread got to it carries the old
    /// number and is dropped: it would otherwise skip the track just chosen.
    /// </summary>
    private int _generation;

    public event EventHandler? Ended;

    public event EventHandler? Failed;

    public bool IsAvailable => engine() is not null;

    public AudioPlayerState State => _player?.State switch
    {
        VLCState.Opening or VLCState.Buffering or VLCState.Playing => AudioPlayerState.Playing,
        VLCState.Paused => AudioPlayerState.Paused,
        _ => AudioPlayerState.Stopped,
    };

    public TimeSpan Position =>
        _player is { } player ? TimeSpan.FromMilliseconds(Math.Max(0, player.Time)) : TimeSpan.Zero;

    public TimeSpan Duration =>
        _player is { } player ? TimeSpan.FromMilliseconds(Math.Max(0, player.Length)) : TimeSpan.Zero;

    /// <summary>
    /// Set straight on the player, in the same call that moves the bar: with
    /// DirectSound this is the volume of this player's own sound buffer, heard
    /// right away (measured: about 0.06 s, against 1.2 to 1.3 s through the
    /// equalizer). Nothing else touches the player from another thread.
    /// </summary>
    public int Volume
    {
        get => _volume.Value;
        set => _volume.Value = value;
    }

    public void Play(string path) => PlayFrom(path, TimeSpan.Zero);

    private void PlayFrom(string path, TimeSpan start)
    {
        if (engine() is not { } libVlc)
            throw new InvalidOperationException("El motor de audio no está disponible.");

        var player = NewPlayer(libVlc);

        using var media = new Media(libVlc, new Uri(path));
        // A cover picture or a video track inside the file must not open a window.
        media.AddOption(":no-video");
        if (start > TimeSpan.Zero)
            media.AddOption($":start-time={start.TotalSeconds.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)}");
        _path = path;
        player.Play(media);
    }

    /// <summary>The input LibVLC reads the current stream from; it must outlive the media.</summary>
    private StreamMediaInput? _input;

    public void PlayStream(Stream stream)
    {
        if (engine() is not { } libVlc)
        {
            stream.Dispose();
            throw new InvalidOperationException("El motor de audio no está disponible.");
        }

        var player = NewPlayer(libVlc);

        _input = new StreamMediaInput(stream);
        using var media = new Media(libVlc, _input);
        media.AddOption(":no-video");
        // A sound that came by the internet is not reopened when the device changes.
        _path = null;
        player.Play(media);
    }

    private void ReleaseInput()
    {
        var input = _input;
        _input = null;
        try { input?.Dispose(); } catch { /* ya cerrado */ }
    }

    public void Pause() => SetPaused(true);

    public void Resume()
    {
        // Paused while Windows changed the device: the old output is not
        // trusted, so it is reopened at the same spot instead of unpaused.
        if (_player is not null && _path is not null && State == AudioPlayerState.Paused
            && _audioGeneration != AudioOutputChanges.Generation)
        {
            PlayFrom(_path, Position);
            return;
        }

        SetPaused(false);
    }

    /// <summary>
    /// Windows moved the sound to another device (or the operator asked to
    /// restart it): what is playing is reopened on the new one, in the same place.
    /// </summary>
    private void OnOutputChanged(object? sender, EventArgs e)
    {
        if (_player is null || _path is null || State != AudioPlayerState.Playing)
            return;

        try
        {
            Log.Information("Se reabre la salida del audio en {Position}", Position);
            PlayFrom(_path, Position);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "No se pudo reabrir la salida del audio");
        }
    }

    /// <summary>Stops the player and lets it go, with the stream it was reading.</summary>
    private void DiscardPlayer()
    {
        _volume.Detach();
        var old = _player;
        _player = null;
        // Whatever the old player was about to report is not about the track
        // that starts next.
        Interlocked.Increment(ref _generation);
        if (old is not null)
        {
            try { old.Stop(); } catch { /* el motor ya lo cerró */ }
            try { old.Dispose(); } catch { /* ignore */ }
        }

        // Only now: the player was reading it.
        ReleaseInput();
    }

    private void SetPaused(bool paused)
    {
        if (_player is not { } player || !player.CanPause)
            return;

        try { player.SetPause(paused); } catch { /* el motor ya lo cerró */ }
    }

    public void Stop()
    {
        Interlocked.Increment(ref _generation);
        if (_player is { } player && player.State is not (VLCState.Stopped or VLCState.NothingSpecial))
        {
            try { player.Stop(); } catch { /* el motor ya lo cerró */ }
        }
    }

    public void SeekTo(TimeSpan position)
    {
        if (_player is not { } player || !player.IsSeekable)
            return;

        try { player.Time = (long)Math.Max(0, position.TotalMilliseconds); } catch { /* el motor ya lo cerró */ }
    }

    /// <summary>
    /// A fresh player for the song about to start, with its DirectSound output
    /// and the volume already set, so it never sounds at another one. Being
    /// new, it also opens the sound device Windows has now.
    /// </summary>
    private MediaPlayer NewPlayer(LibVLC libVlc)
    {
        DiscardPlayer();
        _audioGeneration = AudioOutputChanges.Generation;

        var player = new MediaPlayer(libVlc);
        // Windows only; elsewhere LibVLC's own output is the right one.
        if (OperatingSystem.IsWindows() && !player.SetAudioOutput("directsound"))
            Log.Warning("No se pudo abrir DirectSound para los audios");

        // The general volume, which Windows applies on top of the player's own.
        PlayerSound.ApplySession();
        _volume.Attach(volume => player.Volume = WithGeneralVolume(volume));

        // VLC raises these on its own thread, where calling back into the
        // player would deadlock: they are handed to the UI thread first.
        player.EndReached += (_, _) =>
        {
            var generation = Volatile.Read(ref _generation);
            OnUiThread(() =>
            {
                if (generation == Volatile.Read(ref _generation))
                    Ended?.Invoke(this, EventArgs.Empty);
            });
        };
        player.EncounteredError += (_, _) =>
        {
            Log.Error("VLC no pudo reproducir un audio");
            var generation = Volatile.Read(ref _generation);
            OnUiThread(() =>
            {
                if (generation == Volatile.Read(ref _generation))
                    Failed?.Invoke(this, EventArgs.Empty);
            });
        };

        _player = player;
        if (!_listening)
        {
            _listening = true;
            AudioOutputChanges.OutputChanged += OnOutputChanged;
            PlayerSound.MasterChanged += OnMasterChanged;
        }

        return player;
    }

    /// <summary>
    /// Where the system has no volume for the whole program, this player's
    /// own volume also carries the general «Volumen» bar.
    /// </summary>
    private static int WithGeneralVolume(int volume) =>
        PlatformServices.ProgramVolume is null ? volume * PlayerSound.Master / 100 : volume;

    private void OnMasterChanged(object? sender, EventArgs e)
    {
        // With a system volume, that one already moved: the player stays as it is.
        if (PlatformServices.ProgramVolume is null)
            _volume.Value = _volume.Value;
    }

    private static void OnUiThread(Action action)
    {
        if (PlatformServices.Dispatcher is { } dispatcher)
            dispatcher.Post(action);
        else
            action();
    }

    public void Dispose()
    {
        AudioOutputChanges.OutputChanged -= OnOutputChanged;
        PlayerSound.MasterChanged -= OnMasterChanged;
        _listening = false;
        DiscardPlayer();
    }
}
