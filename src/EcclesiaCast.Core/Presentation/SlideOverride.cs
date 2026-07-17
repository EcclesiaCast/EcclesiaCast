using EcclesiaCast.Core.Themes;

namespace EcclesiaCast.Core.Presentation;

/// <summary>
/// Per-slide design overrides (the ProPresenter-style slide editor): an
/// optional text box (position/size over the 1920×1080 canvas) and optional
/// format tweaks. Null properties fall back to the slide's theme.
/// </summary>
public sealed record SlideOverride(
    double? BoxX = null,
    double? BoxY = null,
    double? BoxWidth = null,
    double? BoxHeight = null,
    double? FontSize = null,
    bool? Bold = null,
    bool? Italic = null,
    string? TextColor = null,
    HAlign? AlignH = null,
    VAlign? AlignV = null)
{
    public bool HasBox => BoxX is not null && BoxY is not null && BoxWidth is not null && BoxHeight is not null;

    /// <summary>True when nothing is overridden (equivalent to null).</summary>
    public bool IsEmpty => this == new SlideOverride();
}
