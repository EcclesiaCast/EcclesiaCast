namespace EcclesiaCast.Presentation.Platform;

public enum NoticeKind { Info, Warning, Error, Question }

/// <summary>
/// Notices and questions in the program's own look. The static
/// <c>Notice</c> decides the buttons; this only shows them.
/// </summary>
public interface INotices
{
    /// <summary>
    /// Shows <paramref name="message"/> with <paramref name="buttons"/> and
    /// waits for the operator. Returns the index of the button chosen
    /// (<paramref name="cancelIndex"/> when the window was closed), or -1 when
    /// there is no screen to show it on. Callable from any thread.
    /// </summary>
    int Ask(string message, string? title, NoticeKind kind, IReadOnlyList<string> buttons,
        int defaultIndex, int cancelIndex, bool destructive);
}
