using EcclesiaCast.Core.Media;
using MediaType = EcclesiaCast.Core.Media.MediaType;

namespace EcclesiaCast.Presentation.ViewModels;

/// <summary>What the fields of the media properties window hold, as the operator left them.</summary>
public sealed record MediaForm(
    DateTime? EventDate,
    double ArchiveDays,
    string Name,
    string Category,
    int BehaviorIndex,
    int ScalingIndex,
    int EndIndex,
    int ImageEndIndex,
    double ShowSeconds,
    int? NextMediaId,
    bool Muted,
    double Volume,
    double Blur,
    double ZoomPercent,
    double OffsetXPercent,
    double OffsetYPercent,
    bool FixedSize,
    double FrameWidth,
    double FrameHeight,
    double TrimStart,
    double TrimEnd,
    string FillColor,
    int? FillMediaId);

/// <summary>What the preview of the window draws: a copy of the item with the fields on it.</summary>
public sealed record MediaPreview(MediaItem Media, MediaItem? FillMedia, double Blur);

/// <summary>
/// Backs the media properties window (framing, trim, what happens at the end,
/// the day it expires…). The window works on the item itself: <see cref="Apply"/>
/// writes the fields into it only when the operator saves; cancelling leaves
/// it as it was, and the preview shows a copy.
/// </summary>
public sealed class MediaInspectorViewModel
{
    private readonly IReadOnlyList<MediaItem> _library;
    private readonly IColorPicker _colorPicker;

    public MediaInspectorViewModel(
        MediaItem item, IReadOnlyList<string> categories, IReadOnlyList<MediaItem> library, IColorPicker colorPicker)
    {
        Item = item;
        Categories = categories;
        _library = library;
        _colorPicker = colorPicker;

        // "Sin nada" first, then everything except this very item — filling a
        // background with itself would be a mirror facing a mirror.
        var options = new List<MediaItem> { new() { Id = 0, Name = "(sólo el color)" } };

        // A picture or a video can fill its own bars with a soft copy of itself,
        // which makes it look like one picture with no border. The video's copy
        // is made from the frames it already decodes: no second player.
        if (item.Type == MediaType.Image)
            options.Add(new MediaItem { Id = MediaItem.FillWithSelfId, Name = "(la misma imagen, desenfocada)" });
        else if (item.Type == MediaType.Video)
            options.Add(new MediaItem { Id = MediaItem.FillWithSelfId, Name = "(el mismo video, desenfocado)" });

        options.AddRange(library
            .Where(m => m.Id != item.Id && m.Type != MediaType.YouTube)
            .OrderBy(m => m.Name));
        FillOptions = options;

        // What can follow: any other media of the library.
        Followers = library.Where(m => m.Id != item.Id).OrderBy(m => m.Name).ToList();
    }

    public MediaItem Item { get; }

    public IReadOnlyList<string> Categories { get; }

    /// <summary>The choices for what fills the bars: the colour, the picture itself, or another one of the library.</summary>
    public IReadOnlyList<MediaItem> FillOptions { get; }

    /// <summary>The media that can follow this one.</summary>
    public IReadOnlyList<MediaItem> Followers { get; }

    public bool IsVideo => Item.Type is MediaType.Video or MediaType.YouTube;

    public bool IsVideoFile => Item.Type == MediaType.Video;

    /// <summary>YouTube always fills the screen with its own player.</summary>
    public bool CanChangeScaling => Item.Type != MediaType.YouTube;

    public string TypeText => Item.Type switch
    {
        MediaType.Video => "Video",
        MediaType.YouTube => $"YouTube · {Item.YouTubeId}",
        _ => "Imagen",
    };

    /// <summary>The choice in the list of fills for an id (the first one, the colour, when it is not there).</summary>
    public MediaItem FillOptionFor(int? id) => FillOptions.FirstOrDefault(m => m.Id == (id ?? 0)) ?? FillOptions[0];

    /// <summary>The media that follows, if it is in the library.</summary>
    public MediaItem? FollowerFor(int? id) => Followers.FirstOrDefault(m => m.Id == id);

    // ── What the fields show when it opens ───────────────────────

    public MediaForm Initial()
    {
        var item = Item;
        return new MediaForm(
            item.EventDate?.Date,
            item.EventDate is null ? MediaExpiry.DefaultDays : item.ArchiveAfterDays,
            item.Name,
            item.Category,
            (int)item.Behavior,
            (int)item.Scaling,
            (int)item.EndBehavior,
            // An image has no loop: the default of a new one (Loop) means "nothing".
            item.EndBehavior switch
            {
                VideoEndBehavior.Logo => 1,
                VideoEndBehavior.NextMedia => 2,
                _ => 0,
            },
            item.ShowSeconds,
            item.NextMediaId,
            item.Muted,
            item.Volume,
            item.Blur,
            Math.Round((item.Zoom <= 0 ? 1 : item.Zoom) * 100),
            Math.Round(item.OffsetX * 100),
            Math.Round(item.OffsetY * 100),
            item.HasFrame,
            item.FrameWidth ?? 1920,
            item.FrameHeight ?? 1080,
            item.TrimStart,
            item.TrimEnd,
            string.IsNullOrWhiteSpace(item.FillColor) ? "#000000" : item.FillColor,
            item.FillMediaId);
    }

    // ── Small decisions ──────────────────────────────────────────

    /// <summary>
    /// Whether the box that picks the next media shows: only when «seguir con
    /// otro medio» is chosen (the fourth end of a video, the third of an image).
    /// An image's seconds count whether it goes in front of the words or behind them.
    /// </summary>
    public bool FollowsNextMedia(int endIndex, int imageEndIndex) =>
        IsVideo ? endIndex == 3 : imageEndIndex == 2;

    /// <summary>Says, under the date, what it will do: «pasa a Eventos pasados el 27/10/2026».</summary>
    public static string EventNote(DateTime? date, double archiveDays, DateTime today)
    {
        if (date is not { } chosen)
            return "Sin fecha: no vence. Con la fecha de un evento, pasa sola a «Eventos pasados» unos días después.";

        var day = MediaExpiry.MovesOn(chosen, (int)archiveDays)!.Value;
        return day <= today
            ? $"Esa fecha ya pasó: al guardar se mueve a «{MediaExpiry.ArchiveTab}»."
            : $"Pasa a «{MediaExpiry.ArchiveTab}» el {MediaExpiry.Format(day)}.";
    }

    /// <summary>Opens the colour picker on the colour shown; null when the operator cancelled.</summary>
    public string? PickFillColor(string current) => _colorPicker.Pick(current);

    // ── Preview ──────────────────────────────────────────────────

    /// <summary>
    /// What the preview draws from what the fields say right now — before
    /// saving — by handing it a copy of the item with those values on it.
    /// The real item is only touched by <see cref="Apply"/>.
    /// </summary>
    public MediaPreview PreviewOf(MediaForm form)
    {
        var shown = Item.Clone();
        shown.Scaling = (MediaScaling)Math.Max(0, form.ScalingIndex);
        shown.Zoom = Math.Clamp(form.ZoomPercent / 100, 0.1, 4);
        shown.OffsetX = Math.Clamp(form.OffsetXPercent / 100, -FillFraming.MaxOffset, FillFraming.MaxOffset);
        shown.OffsetY = Math.Clamp(form.OffsetYPercent / 100, -FillFraming.MaxOffset, FillFraming.MaxOffset);
        shown.FrameWidth = form.FixedSize ? (int)Math.Clamp(form.FrameWidth, 16, 1920) : null;
        shown.FrameHeight = form.FixedSize ? (int)Math.Clamp(form.FrameHeight, 16, 1080) : null;
        shown.FillColor = form.FillColor;

        var chosenFill = FillChosen(form.FillMediaId);
        shown.FillMediaId = chosenFill is { Id: not 0 } ? chosenFill.Id : null;
        var fillMedia = chosenFill is { Id: > 0 } fill ? fill : shown.FillsWithSelf || shown.FillsWithOwnVideo ? shown : null;
        return new MediaPreview(shown, fillMedia, form.Blur);
    }

    /// <summary>The option of the list the form points at, or null when it points at none.</summary>
    private MediaItem? FillChosen(int? id) => id is null ? null : FillOptions.FirstOrDefault(m => m.Id == id);

    // ── Save ─────────────────────────────────────────────────────

    /// <summary>Writes what the fields say into the item (this is «Guardar»).</summary>
    public void Apply(MediaForm form)
    {
        var item = Item;
        item.EventDate = form.EventDate?.Date;
        item.ArchiveAfterDays = (int)Math.Clamp(form.ArchiveDays, 0, 365);
        item.Name = string.IsNullOrWhiteSpace(form.Name) ? item.Name : form.Name.Trim();
        item.Category = string.IsNullOrWhiteSpace(form.Category) ? "Fondos" : form.Category.Trim();
        item.Behavior = (MediaBehavior)Math.Max(0, form.BehaviorIndex);
        item.Scaling = (MediaScaling)Math.Max(0, form.ScalingIndex);
        item.EndBehavior = IsVideo
            ? (VideoEndBehavior)Math.Max(0, form.EndIndex)
            : form.ImageEndIndex switch
            {
                1 => VideoEndBehavior.Logo,
                2 => VideoEndBehavior.NextMedia,
                _ => VideoEndBehavior.Stop,
            };

        // Seconds and a following media only mean something with the choice that uses them.
        item.ShowSeconds = IsVideo ? 0 : (int)Math.Clamp(form.ShowSeconds, 0, 3600);
        var next = form.NextMediaId;
        if (item.EndBehavior == VideoEndBehavior.NextMedia && next is null)
            item.EndBehavior = VideoEndBehavior.Stop; // none chosen: nothing follows
        item.NextMediaId = item.EndBehavior == VideoEndBehavior.NextMedia ? next : null;
        item.Muted = form.Muted;
        item.Volume = (int)form.Volume;
        item.Blur = form.Blur;

        item.Zoom = Math.Clamp(form.ZoomPercent / 100, 0.1, 4);
        item.OffsetX = Math.Clamp(form.OffsetXPercent / 100, -FillFraming.MaxOffset, FillFraming.MaxOffset);
        item.OffsetY = Math.Clamp(form.OffsetYPercent / 100, -FillFraming.MaxOffset, FillFraming.MaxOffset);

        if (form.FixedSize)
        {
            item.FrameWidth = (int)Math.Clamp(form.FrameWidth, 16, 1920);
            item.FrameHeight = (int)Math.Clamp(form.FrameHeight, 16, 1080);
        }
        else
        {
            item.FrameWidth = null;
            item.FrameHeight = null;
        }

        // Brillo y tinte se sacaron (15/10/2026): lo que hubiera quedado guardado se borra.
        item.Brightness = 0;
        item.Tint = null;
        item.TintStrength = 0;
        item.TrimStart = Math.Max(0, form.TrimStart);
        item.TrimEnd = Math.Max(0, form.TrimEnd);

        item.FillColor = form.FillColor;
        item.FillMediaId = form.FillMediaId is int fillId and not 0 ? fillId : null;
    }
}
