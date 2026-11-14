using EcclesiaCast.Core.Media;

namespace EcclesiaCast.Core.Logos;

public enum LogoKind
{
    /// <summary>A still image — the usual church logo.</summary>
    Image,

    /// <summary>A video, looped for as long as the logo is up.</summary>
    Video,

    /// <summary>Free text drawn over a plain colour (no file needed).</summary>
    Text,
}

/// <summary>
/// What the output shows in the Logo state (F3). A church usually keeps
/// several: one for the general service, one for the youth meeting, one for
/// the women's meeting — so the operator picks one instead of editing a
/// setting between services.
/// </summary>
public sealed class Logo
{
    public int Id { get; set; }

    /// <summary>Shown in the picker: "General", "Jóvenes", "Mujeres"…</summary>
    public string Name { get; set; } = string.Empty;

    public int Order { get; set; }

    public LogoKind Kind { get; set; }

    /// <summary>Absolute path to the image or video; null for a text logo.</summary>
    public string? Path { get; set; }

    /// <summary>Generated still of a video logo, so the previews can show it.</summary>
    public string? PosterPath { get; set; }

    /// <summary>The words shown when <see cref="Kind"/> is Text.</summary>
    public string Text { get; set; } = string.Empty;

    public string FontFamily { get; set; } = "Segoe UI";
    public double FontSize { get; set; } = 120;
    public bool Bold { get; set; } = true;
    public string TextColor { get; set; } = "#FFFFFF";

    /// <summary>Fills the screen behind the logo (and around a fitted image).</summary>
    public string BackgroundColor { get; set; } = "#10141E";

    /// <summary>How a non-16:9 image or video is fitted to the screen.</summary>
    public MediaScaling Scaling { get; set; } = MediaScaling.Fit;

    public bool Muted { get; set; } = true;

    /// <summary>Blur used when this logo is put behind the lyrics as a background, 0–100.</summary>
    public double BackgroundBlur { get; set; } = 40;

    // ── Encuadre: the same framing a media item has ──────────────

    /// <summary>Manual zoom over the framing, 1 = as the scaling mode left it.</summary>
    public double Zoom { get; set; } = 1;

    /// <summary>Horizontal shift, as a fraction of the screen width.</summary>
    public double OffsetX { get; set; }

    /// <summary>Vertical shift, as a fraction of the screen height.</summary>
    public double OffsetY { get; set; }

    /// <summary>Fixed size on the 1920×1080 canvas; null uses the whole screen.</summary>
    public int? FrameWidth { get; set; }

    public int? FrameHeight { get; set; }

    /// <summary>
    /// What fills the screen around the picture instead of
    /// <see cref="BackgroundColor"/>: <see cref="MediaItem.FillWithSelfId"/>
    /// for a blurred copy of the logo itself, or an image of the media library.
    /// </summary>
    public int? FillMediaId { get; set; }

    /// <summary>
    /// The logo as a media item carrying its framing, so the same code that
    /// frames the backgrounds frames the logo. The id is negative: it never
    /// collides with a row of the media library.
    /// </summary>
    public MediaItem AsFramedMedia() => new()
    {
        Id = -1000 - Id,
        Name = Name,
        Path = Path ?? string.Empty,
        Type = Kind == LogoKind.Video ? MediaType.Video : MediaType.Image,
        ThumbnailPath = Kind == LogoKind.Video ? PosterPath : Path,
        Scaling = Scaling,
        Zoom = Zoom,
        OffsetX = OffsetX,
        OffsetY = OffsetY,
        FrameWidth = FrameWidth,
        FrameHeight = FrameHeight,
        FillColor = BackgroundColor,
        FillMediaId = FillMediaId,
        Muted = Muted,
        EndBehavior = VideoEndBehavior.Loop,
    };

    /// <summary>True when it can act as a background layer (it has a file).</summary>
    public bool CanBeBackground => Kind is LogoKind.Image or LogoKind.Video
        && !string.IsNullOrWhiteSpace(Path);

    public Logo Clone() => (Logo)MemberwiseClone();
}
