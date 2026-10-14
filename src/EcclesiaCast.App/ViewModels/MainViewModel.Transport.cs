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

    /// <summary>Starts polling when a video goes on the output (and stops when none is left).</summary>
    private void SyncPlaybackTimer()
    {
        var isVideo = _presentation.Background?.Type is MediaType.Video or MediaType.YouTube;
        if (isVideo && !_playbackTimer.IsEnabled)
            _playbackTimer.Start();
        else if (!isVideo && _playbackTimer.IsEnabled)
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
        // Pressing play with the output off would send sound to a dark screen.
        if (!IsVideoPlaying && !IsProjecting && !EnsureOutputOn())
            return;

        _projection.TogglePlayPause();
        OnPlaybackTick(null, EventArgs.Empty);
    }

    [RelayCommand]
    private void SkipBackward() => _projection.Skip(TimeSpan.FromSeconds(-10));

    [RelayCommand]
    private void SkipForward() => _projection.Skip(TimeSpan.FromSeconds(10));

    [RelayCommand]
    private void RestartVideo() => _projection.SeekTo(TimeSpan.Zero);

    [RelayCommand]
    private void ClearBlur() => BackgroundBlur = 0;
}
