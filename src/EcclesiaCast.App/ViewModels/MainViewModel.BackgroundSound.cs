using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EcclesiaCast.Core.Media;

namespace EcclesiaCast.App.ViewModels;

public sealed partial class MainViewModel
{
    // ── Sonido del video que está en pantalla ────────────────────
    // A video comes in silent (backgrounds are), and the only way to give it
    // its sound was its Propiedades, or the menu of its tab: someone who did
    // not know that had a video that «would not sound» and no way out. The
    // bar of the background on screen now shows whether it sounds, and one
    // click switches it.

    /// <summary>True while the background on screen is a video (one that can sound).</summary>
    [ObservableProperty]
    private bool _isBackgroundVideo;

    /// <summary>True while that video plays without its sound.</summary>
    [ObservableProperty]
    private bool _isBackgroundMuted;

    private void UpdateBackgroundSoundFlags()
    {
        var background = _presentation.Background;
        IsBackgroundVideo = background?.Type is MediaType.Video or MediaType.YouTube;
        IsBackgroundMuted = IsBackgroundVideo && (background!.Muted || background.Volume <= 0);
    }

    /// <summary>The 🔇 / 🔊 button of the bar of the background on screen.</summary>
    [RelayCommand]
    private void ToggleBackgroundSound()
    {
        if (_presentation.Background is not { Type: MediaType.Video or MediaType.YouTube } background)
            return;

        if (background.Id <= 0)
        {
            StatusText = "Este video es un logo: su sonido se cambia en el administrador de logos.";
            return;
        }

        if (_allMedia.FirstOrDefault(m => m.Id == background.Id) is not { } original)
            return;

        var wasMuted = background.Muted || background.Volume <= 0;
        if (wasMuted)
        {
            original.Muted = false;
            if (original.Volume <= 0)
                original.Volume = 100;
            _media.Update(original);

            // Its tab may be the one silencing it: then the tab is lifted too,
            // or the click would seem to do nothing.
            var tabNote = string.Empty;
            if (IsMediaTabMuted(original.Category))
            {
                var tab = original.Category!;
                SaveMediaTabRule(tab, TabRule(tab) with { Muted = false });
                tabNote = $" También se le quitó el silencio a la pestaña «{tab}».";
            }

            _presentation.RefreshBackground(Resolved(original).Clone(), ResolveFill(original));
            StatusText = $"«{original.Name}» suena (queda así en sus Propiedades).{tabNote}";
        }
        else
        {
            original.Muted = true;
            _media.Update(original);
            _presentation.RefreshBackground(Resolved(original).Clone(), ResolveFill(original));
            StatusText = $"«{original.Name}» sin sonido (queda así en sus Propiedades).";
        }

        UpdateBackgroundSoundFlags();
    }
}
