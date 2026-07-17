using EcclesiaCast.Core.Themes;

namespace EcclesiaCast.Core.Presentation;

/// <summary>
/// What a slide displays: the main text, an optional caption (e.g. a Bible
/// reference like "Juan 3:16 · RVC"), an optional secondary text — the same
/// verse in a second Bible version — and the theme that styles it all.
/// A null theme renders with <see cref="SlideTheme.Fallback"/>.
/// </summary>
public sealed record SlideContent(
    string MainText,
    string? Caption = null,
    string? SecondaryText = null,
    SlideTheme? Theme = null,
    SlideOverride? Override = null);
