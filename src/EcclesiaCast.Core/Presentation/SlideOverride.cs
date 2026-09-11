using EcclesiaCast.Core.Themes;

namespace EcclesiaCast.Core.Presentation;

/// <summary>
/// Per-slide design overrides (the ProPresenter-style slide editor): an
/// optional text box (position/size over the 1920×1080 canvas) plus optional
/// format tweaks. Null properties fall back to the slide's theme, so only the
/// things you actually change are stored.
/// </summary>
public sealed record SlideOverride(
    double? BoxX = null,
    double? BoxY = null,
    double? BoxWidth = null,
    double? BoxHeight = null,
    double? FontSize = null,
    string? FontFamily = null,
    bool? Bold = null,
    bool? Italic = null,
    bool? Underline = null,
    bool? Strikethrough = null,
    bool? Shadow = null,
    double? ShadowOpacity = null,
    double? OutlineWidth = null,
    string? OutlineColor = null,
    string? TextColor = null,
    HAlign? AlignH = null,
    VAlign? AlignV = null,
    TextCase? Case = null,
    double? LineSpacing = null,
    bool? FitToWidth = null,

    /// <summary>
    /// Extra text boxes drawn over this slide. Null or empty on almost every
    /// slide, which is why they hang off the slide's own design rather than
    /// off the theme.
    /// </summary>
    IReadOnlyList<SlideTextBox>? Boxes = null)
{
    public bool HasBox => BoxX is not null && BoxY is not null && BoxWidth is not null && BoxHeight is not null;

    /// <summary>The extra boxes, never null.</summary>
    public IReadOnlyList<SlideTextBox> TextBoxes => Boxes ?? [];

    /// <summary>
    /// True when nothing is overridden (equivalent to null). The boxes are
    /// checked by hand: a record compares lists by reference, and an empty
    /// list means the same as none at all.
    /// </summary>
    public bool IsEmpty => TextBoxes.Count == 0 && (this with { Boxes = null }) == new SlideOverride();
}
