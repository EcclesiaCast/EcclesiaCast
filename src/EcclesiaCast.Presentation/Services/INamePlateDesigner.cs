using EcclesiaCast.Core.Presentation;

namespace EcclesiaCast.Presentation.Services;

/// <summary>What the operator left in the name plate designer.</summary>
public sealed record NamePlateDesign(IReadOnlyList<NamePlateStyle> Styles, string ChosenStyle);

/// <summary>«✏ Diseñar…» of the «Placas» tab: the window with every option of the styles.</summary>
public interface INamePlateDesigner
{
    /// <summary>
    /// Shows it over <paramref name="sample"/> and waits; null when the
    /// operator cancels.
    /// </summary>
    NamePlateDesign? Design(NamePlate sample);
}
