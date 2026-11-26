using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EcclesiaCast.Core.Media;
using EcclesiaCast.Core.Songs;
using Serilog;

namespace EcclesiaCast.App.ViewModels;

/// <summary>
/// A background of its own for each song (GitHub #32): picked once, it goes on
/// the screen by itself every time the song does, instead of keeping the
/// previous song's background and changing it by hand.
/// </summary>
public sealed partial class MainViewModel
{
    /// <summary>The open song, when the grid shows one: the one «Usar como fondo de…» is for.</summary>
    public Song? SongForBackground => IsSongOpen ? SelectedSong : null;

    /// <summary>«🖼 Fondo: Atardecer» or «🖼 Sin fondo propio», over the slides.</summary>
    [ObservableProperty]
    private string _songBackgroundLabel = "🖼 Sin fondo propio";

    [ObservableProperty]
    private string _songBackgroundHint = string.Empty;

    [ObservableProperty]
    private bool _songHasOwnBackground;

    /// <summary>The media a song's own background points at, if it is still in the library.</summary>
    private MediaItem? OwnBackgroundOf(Song? song) =>
        song?.BackgroundMediaId is int id ? _allMedia.FirstOrDefault(m => m.Id == id) : null;

    private void UpdateSongBackgroundLabel()
    {
        var song = SelectedSong;
        if (OwnBackgroundOf(song) is { } media)
        {
            SongHasOwnBackground = true;
            SongBackgroundLabel = $"🖼 Fondo: {media.Name}";
            SongBackgroundHint = $"Cuando proyectás esta canción se pone sola de fondo «{media.Name}». Clic para cambiarlo o quitarlo.";
        }
        else
        {
            SongHasOwnBackground = false;
            SongBackgroundLabel = "🖼 Sin fondo propio";
            SongBackgroundHint = "Esta canción no tiene un fondo propio: se queda el que esté puesto. Clic para elegir uno de la biblioteca o usar el que está en pantalla (también: clic derecho en una imagen o video de Medios → «Usar como fondo de…»).";
        }
    }

    /// <summary>Right click on a media card → «Usar como fondo de «…»».</summary>
    [RelayCommand]
    private void UseMediaAsSongBackground(MediaItem? media)
    {
        if (SongForBackground is not { } song || media is null)
            return;

        if (media.Type is not (MediaType.Image or MediaType.Video or MediaType.YouTube))
        {
            StatusText = "Como fondo de una canción sirve una imagen, un video o un YouTube.";
            return;
        }

        if (Playable(media).Behavior == MediaBehavior.Foreground)
        {
            ExplainWhyNotABackground(media, "la letra");
            return;
        }

        SetSongBackground(song, media);
    }

    /// <summary>The button over the slides → «Usar el fondo que está en pantalla».</summary>
    [RelayCommand]
    private void UseCurrentBackgroundForSong()
    {
        if (SongForBackground is not { } song)
            return;

        var current = _presentation.Background;
        var media = current is { Id: > 0 } ? _allMedia.FirstOrDefault(m => m.Id == current.Id) : null;
        if (media is not null && current!.Behavior == MediaBehavior.Foreground)
        {
            ExplainWhyNotABackground(media, "la letra");
            return;
        }

        if (media is null || media.Type is not (MediaType.Image or MediaType.Video or MediaType.YouTube))
        {
            StatusText = "No hay una imagen o un video de fondo en pantalla. Poné uno de Medios y volvé a tocar acá.";
            return;
        }

        SetSongBackground(song, media);
    }

    [RelayCommand]
    private void RemoveSongBackground()
    {
        if (SongForBackground is not { } song || song.BackgroundMediaId is null)
            return;

        _songs.SetBackground(song.Id, null);
        song.BackgroundMediaId = null;
        UpdateSongBackgroundLabel();
        StatusText = $"«{song.Title}» ya no tiene un fondo propio.";
    }

    private void SetSongBackground(Song song, MediaItem media)
    {
        NoteAction($"Fondo propio de «{song.Title}»: {media.Name}");
        _songs.SetBackground(song.Id, media.Id);
        song.BackgroundMediaId = media.Id;
        UpdateSongBackgroundLabel();
        StatusText = $"«{song.Title}» va a usar el fondo «{media.Name}» cada vez que la proyectes.";

        // The song is on the screen right now: the background changes now too.
        if (IsGridOnAir && _presentation.Background?.Id != media.Id)
        {
            _songBackgroundRunId = song.Id;
            ApplyBackground(media);
            _randomBackgroundId = media.Id;
        }
    }

    /// <summary>The last song that went on the screen, so its own background is put once per turn.</summary>
    private int _songBackgroundRunId;

    /// <summary>
    /// Called as a slide of a song goes live. The first slide of the song's
    /// turn puts its own background (a background changed by hand meanwhile is
    /// respected until the next song). True when the song has one, so no random
    /// background is drawn for it.
    /// </summary>
    private bool ApplySongOwnBackground()
    {
        if (SelectedSong is not { } song)
            return false;

        var firstSlide = song.Id != _songBackgroundRunId;
        _songBackgroundRunId = song.Id;

        if (OwnBackgroundOf(song) is not { } media)
            return false;
        if (!firstSlide)
            return true;

        if (_presentation.Background?.Id != media.Id)
        {
            Log.Information("Fondo propio de la canción «{Song}»: {Media}", song.Title, media.Name);
            ApplyBackground(media);
        }

        // Counted as a drawn background: «al azar por canción» may replace it
        // when the next song (without one of its own) comes.
        _randomBackgroundId = media.Id;
        return true;
    }
}
