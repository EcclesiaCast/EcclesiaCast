namespace EcclesiaCast.Core.Bible;

/// <summary>
/// A resolved reference like "Juan 3:16" or "Salmos 23" (whole chapter).
/// Null <see cref="VerseStart"/> means the whole chapter.
/// </summary>
public sealed record BibleReference(int BookNumber, int Chapter, int? VerseStart, int? VerseEnd);
