using System.Text.RegularExpressions;

namespace EcclesiaCast.Core.Search;

/// <summary>
/// Shared rules for looking a phrase up inside stored text. Lyrics and some
/// Bible files keep the line breaks of the original, so a phrase the operator
/// types as one line («las cadenas de la muerte») sits in the database split
/// in two («las cadenas⏎de la muerte») and a plain LIKE never finds it.
/// </summary>
public static partial class TextMatching
{
    /// <summary>
    /// Turns every run of whitespace (spaces, tabs, line breaks) into a single
    /// space and trims the ends. The repositories apply the same idea to the
    /// stored column with SQL replace(), so both sides compare like with like.
    /// </summary>
    public static string CollapseWhitespace(string? text) =>
        string.IsNullOrEmpty(text) ? string.Empty : WhitespaceRun().Replace(text, " ").Trim();

    /// <summary>The LIKE pattern for a typed query, or null when there is nothing to look for.</summary>
    public static string? ContainsPattern(string? query)
    {
        var collapsed = CollapseWhitespace(query);
        return collapsed.Length == 0 ? null : $"%{collapsed}%";
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRun();
}
