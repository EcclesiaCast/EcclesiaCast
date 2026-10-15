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
    // ── Reproducción del fondo (barra de transporte) ─────────────

    /// <summary>Polls the output while a video plays; the transport bar binds to the result.</summary>
    private readonly System.Windows.Threading.DispatcherTimer _playbackTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(300),
    };

    /// <summary>True while the timer writes the position, so it isn't read back as a seek.</summary>
    private bool _updatingPlayback;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowTransportInPreview))]
    private bool _hasVideoPlayback;

    [ObservableProperty]
    private bool _isVideoPlaying;

    [ObservableProperty]
    private double _playbackPositionSeconds;

    [ObservableProperty]
    private double _playbackDurationSeconds;

    [ObservableProperty]
    private string _playbackTimeText = "0:00 / 0:00";

    /// <summary>Blur over the background, 0–100; applies live and sticks to the media.</summary>
    [ObservableProperty]
    private double _backgroundBlur;

    private void OnPlaybackTick(object? sender, EventArgs e)
    {
        // An image with seconds set uses the same bar: its time going by.
        if (_imageClock is { } clock && _imageClockItem is { } image)
        {
            if (_presentation.Background?.Id != image.Id)
            {
                // Something else took the screen: the image's time is over.
                StopAfterImageTimer();
                SyncPlaybackTimer();
                return;
            }

            var now = DateTime.UtcNow;
            var elapsed = clock.Elapsed(now);
            HasVideoPlayback = !IsLogoActive;
            IsVideoPlaying = clock.IsRunning;

            _updatingPlayback = true;
            PlaybackDurationSeconds = clock.Duration.TotalSeconds;
            PlaybackPositionSeconds = elapsed.TotalSeconds;
            _updatingPlayback = false;
            PlaybackTimeText = $"{Clock(elapsed)} / {Clock(clock.Duration)}";

            if (clock.IsOver(now))
                FinishImage(image);
            return;
        }

        var state = _projection.Playback;

        // With the logo up the background video keeps running behind it, but
        // it isn't what the congregation sees, so its controls step aside.
        HasVideoPlayback = state.HasVideo && !IsLogoActive;
        IsVideoPlaying = state.IsPlaying;

        _updatingPlayback = true;
        PlaybackDurationSeconds = Math.Max(state.Duration.TotalSeconds, 0.001);
        PlaybackPositionSeconds = Math.Clamp(state.Position.TotalSeconds, 0, PlaybackDurationSeconds);
        _updatingPlayback = false;

        PlaybackTimeText = state.CanSeek
            ? $"{Clock(state.Position)} / {Clock(state.Duration)}"
            : state.HasVideo ? Clock(state.Position) : "0:00 / 0:00";
    }

    private static string Clock(TimeSpan value) =>
        value.TotalHours >= 1 ? value.ToString(@"h\:mm\:ss") : value.ToString(@"m\:ss");

    /// <summary>Starts polling when a video (or an image with seconds) goes on the output, and stops when none is left.</summary>
    private void SyncPlaybackTimer()
    {
        var isVideo = _presentation.Background?.Type is MediaType.Video or MediaType.YouTube;
        var timed = isVideo || _imageClock is not null;
        if (timed && !_playbackTimer.IsEnabled)
            _playbackTimer.Start();
        else if (!timed && _playbackTimer.IsEnabled)
        {
            _playbackTimer.Stop();
            HasVideoPlayback = false;
            PlaybackTimeText = "0:00 / 0:00";
        }
    }

    partial void OnPlaybackPositionSecondsChanged(double value)
    {
        if (_updatingPlayback)
            return;

        if (_imageClock is { } clock)
        {
            clock.Seek(TimeSpan.FromSeconds(value), DateTime.UtcNow);
            return;
        }

        _projection.SeekTo(TimeSpan.FromSeconds(value));
    }

    partial void OnBackgroundBlurChanged(double value)
    {
        var amount = Math.Clamp(value, 0, 100);
        Projection.BackgroundBlur = amount;

        // Remember it, so the same background comes back blurred. A negative id
        // means the background is really a logo, which owns its own setting.
        if (_presentation.Background is not { } background || Math.Abs(background.Blur - amount) <= 0.01)
            return;

        background.Blur = amount;

        if (background.Id > 0)
        {
            _media.Update(background);
        }
        else if (Logos.FirstOrDefault(l => -1000 - l.Id == background.Id) is { } logo)
        {
            logo.BackgroundBlur = amount;
            _logos.Save(logo);
        }
    }

    [RelayCommand]
    private void TogglePlayPause()
    {
        if (_imageClock is { } clock)
        {
            clock.TogglePause(DateTime.UtcNow);
            OnPlaybackTick(null, EventArgs.Empty);
            return;
        }

        // Pressing play with the output off would send sound to a dark screen.
        if (!IsVideoPlaying && !IsProjecting && !EnsureOutputOn())
            return;

        _projection.TogglePlayPause();
        OnPlaybackTick(null, EventArgs.Empty);
    }

    [RelayCommand]
    private void SkipBackward() => Skip(TimeSpan.FromSeconds(-10));

    [RelayCommand]
    private void SkipForward() => Skip(TimeSpan.FromSeconds(10));

    private void Skip(TimeSpan by)
    {
        if (_imageClock is { } clock)
        {
            var now = DateTime.UtcNow;
            clock.Seek(clock.Elapsed(now) + by, now);
            OnPlaybackTick(null, EventArgs.Empty);
            return;
        }

        _projection.Skip(by);
    }

    [RelayCommand]
    private void RestartVideo()
    {
        if (_imageClock is { } clock)
        {
            clock.Seek(TimeSpan.Zero, DateTime.UtcNow);
            OnPlaybackTick(null, EventArgs.Empty);
            return;
        }

        _projection.SeekTo(TimeSpan.Zero);
    }

    [RelayCommand]
    private void ClearBlur() => BackgroundBlur = 0;
}
