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

    /// <summary>The Bible's own background, if it has one and it is still in the library.</summary>
    public MediaItem? BibleBackgroundMedia =>
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
            BibleBackgroundHint = "La Biblia no tiene un fondo propio: se queda el que esté puesto. Clic para elegir uno de la biblioteca o usar el que está en pantalla (también: con la pestaña Biblia abierta, clic derecho en una imagen o video de Medios → «Usar como fondo de la Biblia»).";
        }
    }

    /// <summary>The backgrounds of the library (pictures and videos) by tab, for the «choose a background» menus.</summary>
    /// <remarks>
    /// The ones shown on top of everything are listed too, marked: hiding them
    /// looked like the program had lost them. Picking one says why it can't be.
    /// A tab that goes on top is marked itself; in the others, each media that
    /// goes on top on its own is.
    /// </remarks>
    public IReadOnlyList<(string Tab, bool TabOnTop, IReadOnlyList<(MediaItem Media, bool OnTop)> Items)> BackgroundChoicesByTab() =>
        _allMedia
            .Where(m => m.Type is MediaType.Image or MediaType.Video)
            .GroupBy(m => m.Category, StringComparer.OrdinalIgnoreCase)
            .Select(g => (g.Key,
                IsOnTopMediaTab(g.Key),
                (IReadOnlyList<(MediaItem, bool)>)g
                    .Select(m => (m, Playable(m).Behavior == MediaBehavior.Foreground))
                    .ToList()))
            .ToList();

    /// <summary>
    /// A media that goes on top of everything can't be a background. Said in a
    /// window: in the status bar alone it looked like the program was broken.
    /// </summary>
    private void ExplainWhyNotABackground(MediaItem media, string covered)
    {
        const string move = "Para usarlo de fondo: movelo a otra pestaña (clic derecho → «Mover a la pestaña…»)";
        var fix = IsPresentationTab(media.Category)
            ? $"La pestaña «{media.Category}» es una presentación: sus páginas van encima de todo.\n\n{move}."
            : TabRule(media.Category).Mode == MediaTabMode.Foreground
                ? $"La pestaña «{media.Category}» está configurada como «Todo va en primer plano», o sea encima de todo.\n\n"
                  + $"{move}, o cambiá la pestaña (clic derecho en su nombre → «Configurar…»)."
                : "En sus Propiedades está puesto como «primer plano», o sea encima de todo.\n\n"
                  + "Para usarlo de fondo: clic derecho → «Propiedades» y cambialo a «Fondo».";

        StatusText = $"«{media.Name}» no se puede usar de fondo: va encima de todo y taparía {covered}.";
        Services.Notice.Show(
            $"«{media.Name}» taparía {covered}, por eso no se puede usar de fondo.\n\n{fix}\n\n"
            + $"Las carpetas que tienen {Services.Notice.OnTopIconMark} al lado del nombre van encima de todo: "
            + "así las reconocés de un vistazo.",
            "No se puede usar de fondo", Services.NoticeKind.Warning);
    }

    /// <summary>A background picked for the Bible (from the menu of the button over the verses, or a media card's right click).</summary>
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
            ExplainWhyNotABackground(media, "el texto");
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
        if (media is not null && current!.Behavior == MediaBehavior.Foreground)
        {
            ExplainWhyNotABackground(media, "el texto");
            return;
        }

        if (media is null)
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
