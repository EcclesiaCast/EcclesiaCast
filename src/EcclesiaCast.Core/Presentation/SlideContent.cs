namespace EcclesiaCast.Core.Presentation;

/// <summary>
/// What a slide displays: the main text plus an optional caption
/// (e.g. a Bible reference like "Juan 3:16 · RVR1960").
/// </summary>
public sealed record SlideContent(string MainText, string? Caption = null);
