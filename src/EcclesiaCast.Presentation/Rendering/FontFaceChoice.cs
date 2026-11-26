namespace EcclesiaCast.Presentation.Rendering;

/// <summary>
/// Which face of a font WPF draws for a weight the font may not have, and
/// whether it thickens it itself. The Avalonia interface follows this rule
/// instead of its own, which picks a different face depending on what was
/// asked for before (Segoe UI «Medium» came out Semibold or Regular).
/// </summary>
/// <remarks>
/// Seen in the WPF bench: Segoe UI has no Medium (500) and WPF draws Semibold
/// (600), the heavier of the two nearest; Impact has only Regular and, asked
/// for Semibold, WPF thickens it and widens every letter that is not a space
/// by <see cref="SimulatedBoldAdvance"/> of the font size (0.0200 measured in
/// both text logos of the bench).
/// </remarks>
public static class FontFaceChoice
{
    /// <summary>What WPF adds to each letter of a thickened face, as a share of the font size.</summary>
    public const double SimulatedBoldAdvance = 0.02;

    /// <summary>From this weight on (Semibold) WPF thickens a face lighter than it.</summary>
    public const int SimulatedBoldFrom = 600;

    /// <summary>
    /// The face drawn for <paramref name="requested"/> among the weights the
    /// font really has: the nearest one, the heavier on a tie from Medium up
    /// (the lighter below); thickened when Semibold or more is asked for and
    /// only a lighter face is there.
    /// </summary>
    public static FaceChoice Choose(IEnumerable<int> realWeights, int requested)
    {
        var weights = realWeights.Distinct().ToList();
        if (weights.Count == 0)
            return new FaceChoice(requested, false);

        var best = weights
            .OrderBy(w => Math.Abs(w - requested))
            .ThenBy(w => requested >= 500 ? -w : w)
            .First();
        return new FaceChoice(best, requested >= SimulatedBoldFrom && best < SimulatedBoldFrom);
    }
}

/// <param name="Weight">The weight of the face really drawn.</param>
/// <param name="SimulatedBold">Whether that face is thickened, as WPF does.</param>
public readonly record struct FaceChoice(int Weight, bool SimulatedBold);
