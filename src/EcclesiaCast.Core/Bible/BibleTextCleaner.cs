using System.Text.RegularExpressions;
using EcclesiaCast.Core.Abstractions;

namespace EcclesiaCast.Core.Bible;

/// <summary>
/// Takes out of a verse what a Bible file carries from its printed edition
/// but nobody wants on the wall: the rule of dashes before a footnote, the
/// footnote itself, section labels in brackets («[Final largo de Marcos]»),
/// the Hebrew numbering the TLA leaves in the middle of a verse
/// («43b (44) Sin embargo…») and, if asked, «Selah».
///
/// It only runs on the way to the screen. The stored text is never touched,
/// so the file can be exported unchanged and the search still sees the same
/// words. And it is deliberately timid: a paragraph in brackets that is part
/// of the text itself (1 Samuel 10:27 in the NTV, taken from the Dead Sea
/// scrolls) is Scripture and stays.
/// </summary>
public static partial class BibleTextCleaner
{
    /// <summary>Book number of Salmos in the canon (1–66).</summary>
    private const int PsalmsBook = 19;

    /// <summary>A label in brackets has few words; a longer one is a sentence.</summary>
    private const int MaxLabelWords = 6;

    /// <summary>
    /// The text as it is shown. <paramref name="showSelah"/> keeps the word
    /// «Selah» (the theme's checkbox); everything else always goes. Text that
    /// has nothing to clean comes back untouched, spaces and all.
    /// </summary>
    public static string Clean(string? text, bool showSelah = true)
    {
        if (string.IsNullOrEmpty(text))
            return text ?? string.Empty;

        // Each rule reads the text as the one before left it, which is why
        // the label «[Final breve de Marcos]» is already at the start of a
        // sentence once the rule and its note are gone.
        var result = RuleAndNote().Replace(text, " ");
        var labeled = result;
        result = BracketLabel().Replace(labeled, label => IsSectionLabel(labeled, label) ? " " : label.Value);
        result = MiddleVerseNumbers().Replace(result, " ");

        if (!showSelah)
        {
            var withSelah = result;
            result = SelahWord().Replace(withSelah, selah =>
                DropsPunctuation(withSelah, selah) ? string.Empty : selah.Groups["after"].Value);
        }

        if (result == text)
            return text;

        // Whatever was cut leaves its spaces behind.
        result = SpaceRun().Replace(result, " ");
        result = SpaceBeforePunctuation().Replace(result, "$1");
        return result.Trim();
    }

    /// <summary>
    /// True for what some files put as verse 0 of a Psalm: its heading («Al
    /// músico principal…»). Not part of the verses, so a theme can hide it.
    /// </summary>
    public static bool IsPsalmTitle(int bookNumber, int verse) => bookNumber == PsalmsBook && verse == 0;

    /// <summary>
    /// The verses a list is allowed to show: without the Psalm headings when
    /// the theme hides them. The text is left as stored; slides group it
    /// first and clean it after.
    /// </summary>
    public static IReadOnlyList<BibleVerseResult> WithoutHiddenTitles(
        IReadOnlyList<BibleVerseResult> verses, bool showSelahAndTitles) =>
        showSelahAndTitles ? verses : verses.Where(v => !IsPsalmTitle(v.BookNumber, v.Verse)).ToList();

    /// <summary>
    /// One verse for a list or a preview (search results, the playlist box):
    /// hidden headings dropped, the TLA's leading numbers and everything else
    /// cleaned off. Slides do this in two steps instead, see above.
    /// </summary>
    public static IReadOnlyList<BibleVerseResult> ForList(
        IReadOnlyList<BibleVerseResult> verses, bool showSelahAndTitles) =>
        WithoutHiddenTitles(verses, showSelahAndTitles)
            .Select(v => v with { Text = Clean(VerseGrouping.StripVerseNumbers(v.Text, v.Verse), showSelahAndTitles) })
            .ToList();

    // Four or more dashes (or underscores) in a row, and the note in brackets
    // that comes right after them with the full stop that closes it.
    [GeneratedRegex(@"[-_]{4,}(?:\s*\[[^\[\]]*\]\.?)?")]
    private static partial Regex RuleAndNote();

    [GeneratedRegex(@"\[(?<inside>[^\[\]]*)\]")]
    private static partial Regex BracketLabel();

    /// <summary>
    /// A label is a few capitalised words with no sentence punctuation that
    /// starts a sentence of its own. «Dios» between brackets in the middle of
    /// a sentence is a word the translator added, and stays.
    /// </summary>
    private static bool IsSectionLabel(string text, Match label)
    {
        var inside = label.Groups["inside"].Value.Trim();
        if (inside.Length == 0 || !char.IsUpper(inside[0]))
            return false;
        if (inside.IndexOfAny(['.', ',', ';', ':', '!', '?', '¿', '¡', '«', '»', '"', '“', '”', '(', ')']) >= 0
            || inside.Any(char.IsDigit))
            return false;

        var words = inside.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        if (words < 2 || words > MaxLabelWords)
            return false;

        var before = text[..label.Index].TrimEnd();
        return before.Length == 0 || before[^1] is '.' or '!' or '?' or '»' or '”' or '"' or ']';
    }

    // «43b (44) », «1 (21.1) », «4b (5) »: a number (maybe with a letter or a
    // range) and the other numbering in parentheses, after the end of a
    // clause. A bare «7.000 hombres» or «(Sal 23)» has no pair and stays.
    [GeneratedRegex(@"(?<=[.!?;:,»”""\]])\s+\d+[a-z]?(?:\s*[-–]\s*\d+[a-z]?)?\s+\(\d+(?:[.:]\d+)?[a-z]?(?:\s*[-–]\s*\d+(?:[.:]\d+)?[a-z]?)?\)(?=\s|$)")]
    private static partial Regex MiddleVerseNumbers();

    [GeneratedRegex(@"[ \t]*[\[(]?(?<![\p{L}\p{N}])Selah(?![\p{L}\p{N}])[\])]?(?<after>[.,;:]?)", RegexOptions.IgnoreCase)]
    private static partial Regex SelahWord();

    /// <summary>
    /// «Jehová. Selah.» loses the second full stop, but «Jehová Selah.» keeps
    /// the one that closes the sentence.
    /// </summary>
    private static bool DropsPunctuation(string text, Match selah)
    {
        if (selah.Groups["after"].Length == 0)
            return true;

        var before = text[..selah.Index].TrimEnd();
        return before.Length == 0 || before[^1] is '.' or '!' or '?' or ';' or ':' or ',' or '»' or '”';
    }

    [GeneratedRegex(@"[ \t]{2,}")]
    private static partial Regex SpaceRun();

    [GeneratedRegex(@"[ \t]+([.,;:!?»”])")]
    private static partial Regex SpaceBeforePunctuation();
}
