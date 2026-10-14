namespace EcclesiaCast.Core.Capture;

/// <summary>
/// Which window a saved «window» card points at. A window handle dies with
/// the window, so what is kept is the program and the title it had
/// («proceso.exe|Título»); the next time, the window is looked for again.
/// </summary>
public sealed record WindowTarget(string Process, string Title)
{
    private const char Separator = '|';

    /// <summary>The text kept in the media's path.</summary>
    public static string Format(string process, string title) => $"{process}{Separator}{title}";

    public override string ToString() => Format(Process, Title);

    /// <summary>Reads what <see cref="Format"/> wrote. False for empty text or one without a program.</summary>
    public static bool TryParse(string? text, out WindowTarget target)
    {
        target = new WindowTarget(string.Empty, string.Empty);
        if (string.IsNullOrWhiteSpace(text))
            return false;

        // A program's file name cannot hold a bar, a title can: split at the first.
        var cut = text.IndexOf(Separator);
        if (cut <= 0)
            return false;

        target = new WindowTarget(text[..cut], text[(cut + 1)..]);
        return true;
    }

    /// <summary>
    /// The open window that is the one the target pointed at, or null. Same
    /// program and same title; failing that, same program and a title that
    /// contains the saved one (a document that got «[modificado]» appended);
    /// failing that, the program's only window. Several windows and no clear
    /// answer: none, better a message than the wrong window on the screen.
    /// </summary>
    public static RawWindow? Best(IEnumerable<RawWindow> candidates, WindowTarget target)
    {
        var sameProcess = candidates
            .Where(w => string.Equals(w.ProcessName, target.Process, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (sameProcess.FirstOrDefault(w => w.Title == target.Title) is { } exact)
            return exact;

        if (target.Title.Length > 0
            && sameProcess.FirstOrDefault(w => w.Title.Contains(target.Title, StringComparison.OrdinalIgnoreCase)) is { } partial)
            return partial;

        return sameProcess.Count == 1 ? sameProcess[0] : null;
    }
}
