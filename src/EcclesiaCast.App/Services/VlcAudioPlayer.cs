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

    public void Play(string path)
    {
        if (engine() is not { } libVlc)
            throw new InvalidOperationException("El motor de audio no está disponible.");

        var player = EnsurePlayer(libVlc);

        // Stop the old input first, the way the video does: handing a player a
        // new one while the old is still running is what corrupted its memory.
        player.Stop();
        Interlocked.Increment(ref _generation);

        using var media = new Media(libVlc, new Uri(path));
        // A cover picture or a video track inside the file must not open a window.
        media.AddOption(":no-video");
        player.Volume = _volume;
        player.Play(media);
    }

    public void Pause() => SetPaused(true);

    public void Resume() => SetPaused(false);

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
        var player = _player;
        _player = null;
        if (player is null)
            return;

        try { player.Stop(); } catch { /* ignore */ }
        try { player.Dispose(); } catch { /* ignore */ }
    }
}
