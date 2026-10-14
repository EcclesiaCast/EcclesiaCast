using System.IO;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using EcclesiaCast.Core.Bible;
using EcclesiaCast.Core.Media;
using EcclesiaCast.Core.Presentation;

namespace EcclesiaCast.App.ViewModels;

/// <summary>One card in the slide grid.</summary>
public sealed partial class SlideItemViewModel(
    int index, string label, SlideContent slide,
    BibleReference? jumpTarget = null, bool jumpToEnd = false, int sectionId = 0)
    : ObservableObject
{
    public int Index { get; } = index;
    public string Label { get; } = label;
    public SlideContent Slide { get; } = slide;

    /// <summary>Song section behind this slide (0 for Bible/quick-text slides).</summary>
    public int SectionId { get; } = sectionId;

    /// <summary>
    /// Verses behind a Bible slide; null for everything else. Several slides
    /// share it when a long verse is split over them.
    /// </summary>
    public VerseGroup? Verses { get; init; }

    /// <summary>
    /// When set, activating this card doesn't project it: it loads this
    /// passage instead (the previous/next chapter cards).
    /// </summary>
    public BibleReference? JumpTarget { get; } = jumpTarget;

    /// <summary>
    /// True on the "previous chapter" card: after the jump, go live on the
    /// last verse instead of the first, so backward reading is continuous.
    /// </summary>
    public bool JumpToEnd { get; } = jumpToEnd;

    public string PreviewText => Slide.MainText;

    /// <summary>
    /// The media behind a card of a presentation (a page of a PDF, a picture,
    /// a video); null on every other card. The <see cref="Slide"/> of such a
    /// card only says «Página 3» for the stage and the phone: it never goes
    /// on screen as text. Settable because a reload of the library hands out
    /// new objects for the same pages: the card follows them, so what it
    /// projects or edits is never an old copy.
    /// </summary>
    public MediaItem? Page { get; set; }

    public bool IsPage => Page is not null;

    private System.Windows.Media.ImageSource? _pageImage;
    private bool _pageImageLoaded;

    /// <summary>
    /// The picture of the card: the page's small poster, never the 4K file.
    /// Without a poster the picture itself is read at the width of a card.
    /// Read the first time the card is drawn.
    /// </summary>
    public System.Windows.Media.ImageSource? PageImage
    {
        get
        {
            if (!_pageImageLoaded)
            {
                _pageImageLoaded = true;
                _pageImage = Page is null ? null : LoadPageImage(Page);
            }

            return _pageImage;
        }
    }

    private static System.Windows.Media.ImageSource? LoadPageImage(MediaItem page)
    {
        var poster = !string.IsNullOrWhiteSpace(page.ThumbnailPath) && File.Exists(page.ThumbnailPath);
        var path = poster ? page.ThumbnailPath : page.Type == MediaType.Image ? page.Path : null;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return null;

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(path);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            if (!poster)
                bitmap.DecodePixelWidth = 220;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Tooltip of the grid card. Only song cards can be dragged to another
    /// place; Bible cards get the text that matches what their click and
    /// right click really do.
    /// </summary>
    public string CardToolTip
    {
        get
        {
            if (Page != null)
                return "Clic: proyectar esta página · Clic derecho: Propiedades";
            if (JumpTarget != null)
                return JumpToEnd
                    ? "Clic: abre el capítulo anterior en su último versículo"
                    : "Clic: abre el capítulo siguiente en su primer versículo";
            if (Verses != null)
                return "Clic: proyectar · Clic derecho: agregar a la playlist o corregir el texto";
            if (SectionId != 0)
                return "Clic: proyectar · Arrastrar: cambiarla de lugar · Clic derecho: más opciones";
            return "Clic: proyectar";
        }
    }

    /// <summary>The card last projected from this grid; it may no longer be on the air.</summary>
    [ObservableProperty]
    private bool _isLive;

    /// <summary>The card that is on the air right now.</summary>
    [ObservableProperty]
    private bool _isOnAir;

    /// <summary>Selected as "next up" (e.g. the verse a reference points to), not live yet.</summary>
    [ObservableProperty]
    private bool _isPreviewed;
}
