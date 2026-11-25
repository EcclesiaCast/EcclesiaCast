using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EcclesiaCast.Core.Media;
using EcclesiaCast.Core.Presentation;

namespace EcclesiaCast.App.ViewModels;

/// <summary>
/// A background of its own for the Bible, the way a song can have one: picked
/// once, it goes on the screen by itself when a verse is projected after
/// something else, instead of keeping whatever background was left.
/// </summary>
public sealed partial class MainViewModel
{
    private const string BibleBackgroundKey = "bible.background.mediaid";

    /// <summary>The grid holds Bible verses: where the «🖼 Fondo de la Biblia» button shows.</summary>
    [ObservableProperty]
    private bool _isBibleOpen;

    [ObservableProperty]
    private string _bibleBackgroundLabel = "🖼 Biblia: sin fondo propio";

    [ObservableProperty]
    private string _bibleBackgroundHint = string.Empty;

    [ObservableProperty]
    private bool _bibleHasOwnBackground;

    private int? BibleBackgroundId => int.TryParse(_settings.Get(BibleBackgroundKey), out var id) && id > 0 ? id : null;

    private MediaItem? BibleBackgroundMedia =>
        BibleBackgroundId is int id ? _allMedia.FirstOrDefault(m => m.Id == id) : null;

    private void UpdateBibleBackgroundLabel()
    {
        if (BibleBackgroundMedia is { } media)
        {
            BibleHasOwnBackground = true;
            BibleBackgroundLabel = $"🖼 Biblia: {media.Name}";
            BibleBackgroundHint = $"Cuando proyectás un versículo después de otra cosa, se pone solo de fondo «{media.Name}». Clic para cambiarlo o quitarlo.";
        }
        else
        {
            BibleHasOwnBackground = false;
            BibleBackgroundLabel = "🖼 Biblia: sin fondo propio";
            BibleBackgroundHint = "La Biblia no tiene un fondo propio: se queda el que esté puesto. Clic para elegir uno de la biblioteca.";
        }
    }

    /// <summary>The backgrounds of the library (pictures and videos) by tab, for the «choose a background» menus.</summary>
    public IReadOnlyList<(string Tab, IReadOnlyList<MediaItem> Items)> BackgroundChoicesByTab() =>
        _allMedia
            .Where(m => m.Type is MediaType.Image or MediaType.Video)
            .Where(m => Playable(m).Behavior == MediaBehavior.Background)
            .GroupBy(m => m.Category, StringComparer.OrdinalIgnoreCase)
            .Select(g => (g.Key, (IReadOnlyList<MediaItem>)g.ToList()))
            .ToList();

    /// <summary>A background picked for the Bible (from the button's menu or a media card's right click).</summary>
    [RelayCommand]
    private void UseMediaAsBibleBackground(MediaItem? media)
    {
        if (media is null)
            return;

        if (media.Type is not (MediaType.Image or MediaType.Video or MediaType.YouTube))
        {
            StatusText = "Como fondo de la Biblia sirve una imagen, un video o un YouTube.";
            return;
        }

        if (Playable(media).Behavior == MediaBehavior.Foreground)
        {
            StatusText = $"«{media.Name}» está como primer plano: taparía el texto. Cambialo a «Fondo» en sus Propiedades.";
            return;
        }

        NoteAction($"Fondo de la Biblia: {media.Name}");
        _settings.Set(BibleBackgroundKey, media.Id.ToString());
        UpdateBibleBackgroundLabel();
        StatusText = $"La Biblia va a usar el fondo «{media.Name}» cada vez que proyectes un versículo después de otra cosa.";

        if (_liveSlideOrigin == SlideOrigin.Bible && _presentation.Background?.Id != media.Id)
        {
            ApplyBackground(media);
            _randomBackgroundId = media.Id;
        }
    }

    [RelayCommand]
    private void UseCurrentBackgroundForBible()
    {
        var current = _presentation.Background;
        var media = current is { Id: > 0 } ? _allMedia.FirstOrDefault(m => m.Id == current.Id) : null;
        if (media is null || current!.Behavior == MediaBehavior.Foreground)
        {
            StatusText = "No hay una imagen o un video de fondo en pantalla. Elegí uno de la lista o poné uno de Medios.";
            return;
        }

        UseMediaAsBibleBackground(media);
    }

    [RelayCommand]
    private void RemoveBibleBackground()
    {
        if (BibleBackgroundId is null)
            return;

        _settings.Set(BibleBackgroundKey, string.Empty);
        UpdateBibleBackgroundLabel();
        StatusText = "La Biblia ya no tiene un fondo propio.";
    }

    /// <summary>Called when a verse goes live after something that was not the Bible: puts the Bible's own background.</summary>
    private void ApplyBibleOwnBackground()
    {
        if (BibleBackgroundMedia is not { } media || _presentation.Background?.Id == media.Id)
            return;

        Serilog.Log.Information("Fondo propio de la Biblia: {Media}", media.Name);
        ApplyBackground(media);
        _randomBackgroundId = media.Id;
    }
}
