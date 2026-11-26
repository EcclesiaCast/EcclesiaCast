namespace EcclesiaCast.Core.Media;

/// <summary>
/// The drawing that marks what goes on top of everything: the corner of a
/// frame behind, and a solid square over it. A drawing and not an emoji so it
/// looks the same in the program (where WPF paints emoji in one color) and in
/// the phone's page, and so it is told apart from 🔇 and 🖼 at a glance.
/// </summary>
public static class OnTopIcon
{
    /// <summary>Path data in a 16 × 16 box, read the same by WPF and by SVG.</summary>
    public const string PathData = "M1,1 H11 V4 H9 V3 H3 V9 H4 V11 H1 Z M6,6 H15 V15 H6 Z";

    public const string Explanation =
        "Va encima de todo: se proyecta en primer plano, tapando lo demás, y no sirve de fondo.";
}
