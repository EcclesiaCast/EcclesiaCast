namespace EcclesiaCast.Core.Bible;

/// <summary>
/// Writes a reference the way <see cref="BibleReferenceParser"/> reads it
/// («Juan 8:5-10», «Salmos 23»), so a text box can be pre-filled with what
/// would be added and the operator edits it from there.
/// </summary>
public static class BibleReferenceFormatter
{
    public static string Format(BibleReference reference)
    {
        var book = BibleBookCatalog.FindByNumber(reference.BookNumber)?.Name ?? $"Libro {reference.BookNumber}";
        if (reference.VerseStart is not int start)
            return $"{book} {reference.Chapter}";

        return reference.VerseEnd is int end && end != start
            ? $"{book} {reference.Chapter}:{start}-{end}"
            : $"{book} {reference.Chapter}:{start}";
    }
}
