namespace EcclesiaCast.Core.Bible;

/// <summary>
/// A canonical book entry (1–66, Génesis–Apocalipsis), independent of any
/// imported version. Used to resolve references like "Juan 3:16" or "1 co 13"
/// typed by the operator, regardless of how a given version names its books.
/// </summary>
public sealed record BibleBookInfo(int Number, string Name, Testament Testament, string[] Aliases);
