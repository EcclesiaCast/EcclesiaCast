using CommunityToolkit.Mvvm.ComponentModel;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Media;

namespace EcclesiaCast.App.ViewModels;

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
        value = Math.Clamp(value, 0, 100);
        PlayerSound.Master = value;
        if (_startingUp)
            return;

        _settings.Set(OutputVolumeKey, value.ToString());

        // Re-apply to what is already playing.
        _audioPlayer.Volume = AudioVolume;
        if (_presentation.Background is { Type: MediaType.Video, Id: > 0 } background
            && _allMedia.FirstOrDefault(m => m.Id == background.Id) is { } original)
            _presentation.RefreshBackground(Resolved(original).Clone(), ResolveFill(original));
    }

    partial void OnBackgroundVolumeChanged(int value)
    {
        if (_syncingVolume)
            return;

        if (_presentation.Background is not { Type: MediaType.Video, Id: > 0 } background
            || _allMedia.FirstOrDefault(m => m.Id == background.Id) is not { } original)
            return;

        original.Volume = Math.Clamp(value, 0, 100);
        // Raising the volume of a silenced video is asking for its sound.
        if (original.Volume > 0 && original.Muted)
            original.Muted = false;
        _media.Update(original);
        _presentation.RefreshBackground(Resolved(original).Clone(), ResolveFill(original));
        UpdateBackgroundSoundFlags();
    }

    private void LoadOutputVolume()
    {
        OutputVolume = int.TryParse(_settings.Get(OutputVolumeKey), out var volume)
            ? Math.Clamp(volume, 0, 100)
            : 100;
        PlayerSound.Master = OutputVolume;
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
