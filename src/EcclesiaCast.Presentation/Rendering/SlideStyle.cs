using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Core.Themes;

namespace EcclesiaCast.Presentation.Rendering;

/// <summary>
/// How a slide's words look: its theme with the slide's own changes applied
/// (the slide editor's font, colour, outline…). The same reading the WPF
/// slide view does with its «Effective…» properties, written once so the
/// Avalonia one cannot read it differently.
/// </summary>
public sealed class SlideStyle(SlideContent? slide)
{
    public SlideTheme Theme { get; } = slide?.Theme ?? SlideTheme.Fallback;

    private SlideOverride? Override => slide?.Override;

    public string FontFamily => Override?.FontFamily ?? Theme.FontFamily;
    public bool Bold => Override?.Bold ?? Theme.Bold;
    public bool Italic => Override?.Italic ?? Theme.Italic;
    public bool Underline => Override?.Underline ?? false;
    public bool Strikethrough => Override?.Strikethrough ?? false;
    public bool Shadow => Override?.Shadow ?? Theme.Shadow;
    public double ShadowOpacity => Override?.ShadowOpacity ?? Theme.ShadowOpacity;
    public double OutlineWidth => Override?.OutlineWidth ?? Theme.OutlineWidth;
    public string OutlineColor => Override?.OutlineColor ?? Theme.OutlineColor;
    public string TextColor => Override?.TextColor ?? Theme.TextColor;
    public HAlign AlignH => Override?.AlignH ?? Theme.AlignH;
    public VAlign AlignV => Override?.AlignV ?? Theme.AlignV;
    public double? LineSpacing => Override?.LineSpacing;
    public bool FitToWidth => Override?.FitToWidth ?? Theme.FitToWidth;
    public TextCase Case => Override?.Case ?? Theme.TextCase;

    /// <summary>The weight the main text is drawn with: a «bold» theme is semibold.</summary>
    public int Weight => Bold ? SlideTextFit.BoldWeight : SlideTextFit.NormalWeight;

    /// <summary>The size of the second version next to the main one's.</summary>
    public double SecondarySize(double mainSize) =>
        Math.Max(20, mainSize * (Theme.SecondaryMatchesPrimary ? 1.0 : Theme.SecondaryScale));

    /// <summary>The theme's transition time, held at five seconds: a longer one outlasts the change it covers.</summary>
    public double TransitionMs => Math.Clamp(Theme.TransitionMs, 0, 5000);
}
