using EcclesiaCast.Core.Presentation;

namespace EcclesiaCast.Presentation.Platform;

/// <summary>
/// Measures text the way the screen draws it (its typeface, its size), to
/// know whether it can be read in one slide.
/// </summary>
public interface ITextMeasurer
{
    /// <summary>
    /// Splits a text that can't be read in one slide into as few slides as
    /// make each one readable, cutting at the end of a sentence when possible.
    /// The second version, if any, is split in the same number of parts so
    /// the two keep walking together.
    /// </summary>
    IReadOnlyList<(string Main, string? Secondary)> SplitToFit(
        SlideContent slide, Func<string, string>? label = null, Func<string, string>? secondaryLabel = null,
        int maxParts = 6);
}
