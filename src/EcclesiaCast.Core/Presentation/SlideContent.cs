namespace EcclesiaCast.Core.Presentation;

/// <summary>
/// What a slide displays: the main text, an optional caption (e.g. a Bible
/// reference like "Juan 3:16 · RVC"), and an optional secondary text — the
/// same verse in a second Bible version, shown below the main one.
/// </summary>
public sealed record SlideContent(string MainText, string? Caption = null, string? SecondaryText = null);
