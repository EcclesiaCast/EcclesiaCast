using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Presentation.Rendering;

namespace EcclesiaCast.App.Platform;

/// <summary>The measuring of the slide view (<see cref="SlideTextFit"/>) behind <see cref="ITextMeasurer"/>.</summary>
public sealed class WpfTextMeasurer : ITextMeasurer
{
    public IReadOnlyList<(string Main, string? Secondary)> SplitToFit(
        SlideContent slide, Func<string, string>? label = null, Func<string, string>? secondaryLabel = null,
        int maxParts = 6) =>
        SlideTextFit.SplitToFit(WpfTextMetrics.Instance, slide, label, secondaryLabel, maxParts);
}
