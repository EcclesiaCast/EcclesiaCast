using CommunityToolkit.Mvvm.ComponentModel;
using EcclesiaCast.Core.Media;

namespace EcclesiaCast.Presentation.ViewModels;

public sealed partial class MainViewModel
{
    // ── Volumen del video en pantalla y volumen de toda la salida ──
    // Under the LIVE controls: the video's own volume (kept in its Propiedades)
    // and one volume for everything EcclesiaCast plays (videos and music list).

    private const string OutputVolumeKey = "output.volume";

    private bool _syncingVolume;

    /// <summary>Everything EcclesiaCast plays, 0 to 100 (the Windows volume is not touched).</summary>
    [ObservableProperty]
    private int _outputVolume = 100;

    /// <summary>The volume of the video on screen, 0 to 100 (its own, saved in its Propiedades).</summary>
    [ObservableProperty]
    private int _backgroundVolume = 100;

    partial void OnOutputVolumeChanged(int value)
    {
        // Applied at once to every player (it is the program's Windows audio
        // session, which moves smoothly); only saving waits for the slider to rest.
        PlayerSound.Master = Math.Clamp(value, 0, 100);
        if (_startingUp)
            return;

        _outputVolumeSave ??= NewRestTimer(TimeSpan.FromMilliseconds(500), () =>
            _settings.Set(OutputVolumeKey, OutputVolume.ToString()));
        Restart(_outputVolumeSave);
    }

    private IUiTimer? _outputVolumeSave;
    private IUiTimer? _backgroundVolumeApply;
    private IUiTimer? _backgroundVolumeSave;
    private MediaItem? _backgroundVolumeItem;

    partial void OnBackgroundVolumeChanged(int value)
    {
        if (_syncingVolume)
            return;

        if (_presentation.Background is not { Type: MediaType.Video, Id: > 0 } background
            || _allMedia.FirstOrDefault(m => m.Id == background.Id) is not { } original)
            return;

        var wasSilent = original.Muted || original.Volume <= 0;
        original.Volume = Math.Clamp(value, 0, 100);
        // Raising the volume of a silenced video is asking for its sound.
        if (original.Volume > 0 && original.Muted)
            original.Muted = false;
        _backgroundVolumeItem = original;

        // A video's own volume is an equalizer of its player: changing it on
        // every step of the slider made the sound stutter. It is applied a few
        // times a second while the slider moves (at once when the video goes
        // from silent to sounding or back), and saved when it rests.
        var nowSilent = original.Muted || original.Volume <= 0;
        _backgroundVolumeApply ??= NewRestTimer(TimeSpan.FromMilliseconds(150), ApplyBackgroundVolume);
        if (wasSilent != nowSilent)
            ApplyBackgroundVolume();
        else if (!_backgroundVolumeApply.IsEnabled)
            _backgroundVolumeApply.Start();

        _backgroundVolumeSave ??= NewRestTimer(TimeSpan.FromMilliseconds(600), () =>
        {
            if (_backgroundVolumeItem is { } item)
                _media.Update(item);
        });
        Restart(_backgroundVolumeSave);
    }

    private void ApplyBackgroundVolume()
    {
        _backgroundVolumeApply?.Stop();
        if (_backgroundVolumeItem is not { } item || _presentation.Background?.Id != item.Id)
            return;

        _presentation.RefreshBackground(Resolved(item).Clone(), ResolveFill(item));
        UpdateBackgroundSoundFlags();
    }

    private IUiTimer NewRestTimer(TimeSpan interval, Action action)
    {
        var timer = _timers.Create(interval);
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            action();
        };
        return timer;
    }

    private static void Restart(IUiTimer timer)
    {
        timer.Stop();
        timer.Start();
    }

    private void LoadOutputVolume()
    {
        OutputVolume = int.TryParse(_settings.Get(OutputVolumeKey), out var volume)
            ? Math.Clamp(volume, 0, 100)
            : 100;
        PlayerSound.Master = OutputVolume;
        PlayerSound.SnapSession();
    }

    private void SyncBackgroundVolume()
    {
        _syncingVolume = true;
        try
        {
            BackgroundVolume = _presentation.Background is { Type: MediaType.Video or MediaType.YouTube } b
                ? Math.Clamp(b.Volume, 0, 100)
                : 100;
        }
        finally
        {
            _syncingVolume = false;
        }
    }
}
