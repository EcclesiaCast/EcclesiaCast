namespace EcclesiaCast.Core.Themes;

public enum ThemeKind
{
    Song,
    Bible,
}

public enum HAlign
{
    Left,
    Center,
    Right,
}

public enum VAlign
{
    Top,
    Center,
    Bottom,
}

public enum CaptionPosition
{
    TopLeft,
    TopCenter,
    TopRight,
    BottomLeft,
    BottomCenter,
    BottomRight,
}

/// <summary>How the text's casing is transformed, mirroring ProPresenter's options.</summary>
public enum TextCase
{
    None,
    Upper,
    Title,
    Sentence,
}

/// <summary>
/// Everything configurable about how a slide looks. Sizes and margins are
/// in pixels over the virtual 1920×1080 canvas that <c>SlideView</c> renders.
/// </summary>
public sealed class SlideTheme
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ThemeKind Kind { get; set; }

    // ── Texto principal ──────────────────────────────────────────
    public string FontFamily { get; set; } = "Segoe UI";

    /// <summary>Preferred size; auto-fit shrinks from here down to <see cref="MinFontSize"/>.</summary>
    public double MaxFontSize { get; set; } = 92;

    /// <summary>Floor of the auto-fit; below this the text just overflows.</summary>
    public double MinFontSize { get; set; } = 36;

    public bool Bold { get; set; } = true;
    public bool Italic { get; set; }
    public bool Uppercase { get; set; }
    public bool Shadow { get; set; } = true;
    public string TextColor { get; set; } = "#FFFFFF";
    public HAlign AlignH { get; set; } = HAlign.Center;
    public VAlign AlignV { get; set; } = VAlign.Center;

    // ── Márgenes (px sobre 1920×1080) ────────────────────────────
    public double MarginHorizontal { get; set; } = 110;
    public double MarginVertical { get; set; } = 80;

    // ── Fondo ────────────────────────────────────────────────────
    public string BackgroundColor { get; set; } = "#10141E";
    public string? BackgroundImagePath { get; set; }

    /// <summary>0–1: black layer over the background to keep text readable.</summary>
    public double BackgroundDim { get; set; }

    // ── Leyenda (canciones: título/artista · Biblia: referencia) ─
    public bool ShowCaption { get; set; } = true;
    public CaptionPosition CaptionPosition { get; set; } = CaptionPosition.BottomRight;
    public double CaptionFontSize { get; set; } = 40;

    // ── Solo Biblia ──────────────────────────────────────────────
    /// <summary>Include the version abbreviation in the reference caption.</summary>
    public bool ShowVersionName { get; set; } = true;

    /// <summary>Prefix each verse's text with its number.</summary>
    public bool ShowVerseNumbers { get; set; }

    /// <summary>Built-in look used when nothing is configured yet.</summary>
    public static SlideTheme Fallback { get; } = new() { Name = "(integrado)" };

    public SlideTheme Clone() => (SlideTheme)MemberwiseClone();
}
