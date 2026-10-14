using System.Windows;
using LibVLCSharp.Shared;
using Serilog;

namespace EcclesiaCast.App.Services;

/// <summary>
/// Plays the Música tab's audio with LibVLC, on a player of its own: the one
/// that draws the background video is never shared, so pausing, changing or
/// muting one cannot reach the other. The engine (the same LibVLC the video
/// uses) is looked up when the first track plays, so a computer where it did
/// not start simply has no audio list instead of failing at start-up.
/// </summary>
public sealed class VlcAudioPlayer(Func<LibVLC?> engine) : IAudioPlayer
{
    private MediaPlayer? _player;
    private int _volume = 80;

    /// <summary>The file being played, to reopen it in place when the output device changes.</summary>
    private string? _path;

    /// <summary>The <see cref="AudioEndpointWatcher.Generation"/> this player's output was opened under.</summary>
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

    public int Volume
    {
        get => _volume;
        set
        {
            _volume = Math.Clamp(value, 0, 100);
            if (_player is { } player)
                player.Volume = _volume;
        }
    }

    public void Play(string path) => PlayFrom(path, TimeSpan.Zero);

    private void PlayFrom(string path, TimeSpan start)
    {
        if (engine() is not { } libVlc)
            throw new InvalidOperationException("El motor de audio no está disponible.");

        // Windows changed the audio device since this player opened its output:
        // LibVLC keeps that output inside the player and it can stay dead, so
        // this one is replaced by a new player (it has no video callbacks to
        // keep alive, unlike the video surface).
        if (_player is not null && _audioGeneration != AudioEndpointWatcher.Generation)
            DiscardPlayer();
        _audioGeneration = AudioEndpointWatcher.Generation;

        var player = EnsurePlayer(libVlc);

        // Stop the old input first, the way the video does: handing a player a
        // new one while the old is still running is what corrupted its memory.
        player.Stop();
        Interlocked.Increment(ref _generation);

        using var media = new Media(libVlc, new Uri(path));
        // A cover picture or a video track inside the file must not open a window.
        media.AddOption(":no-video");
        if (start > TimeSpan.Zero)
            media.AddOption($":start-time={start.TotalSeconds.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)}");
        player.Volume = _volume;
        _path = path;
        player.Play(media);
    }

    public void Pause() => SetPaused(true);

    public void Resume()
    {
        // Paused while Windows changed the device: the old output is not
        // trusted, so it is reopened at the same spot instead of unpaused.
        if (_player is not null && _path is not null && State == AudioPlayerState.Paused
            && _audioGeneration != AudioEndpointWatcher.Generation)
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

    private void DiscardPlayer()
    {
        var old = _player;
        _player = null;
        // Whatever the old player was about to report is not about the track
        // that starts next.
        Interlocked.Increment(ref _generation);
        if (old is null)
            return;

        try { old.Stop(); } catch { /* el motor ya lo cerró */ }
        try { old.Dispose(); } catch { /* ignore */ }
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

    private MediaPlayer EnsurePlayer(LibVLC libVlc)
    {
        if (_player is { } existing)
            return existing;

        var player = new MediaPlayer(libVlc) { Volume = _volume };

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
            AudioEndpointWatcher.OutputChanged += OnOutputChanged;
        }

        return player;
    }

    private static void OnUiThread(Action action)
    {
        if (Application.Current?.Dispatcher is { } dispatcher)
            dispatcher.BeginInvoke(action);
        else
            action();
    }

    public void Dispose()
    {
        AudioEndpointWatcher.OutputChanged -= OnOutputChanged;
        _listening = false;
        var player = _player;
        _player = null;
        if (player is null)
            return;

        try { player.Stop(); } catch { /* ignore */ }
        try { player.Dispose(); } catch { /* ignore */ }
    }
}
