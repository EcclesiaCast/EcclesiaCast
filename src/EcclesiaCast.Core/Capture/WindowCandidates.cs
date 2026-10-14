namespace EcclesiaCast.Core.Capture;

/// <summary>
/// One top-level window as Windows reports it, before deciding whether it is
/// worth offering. Plain data, so the choice can be tested without a desktop.
/// </summary>
/// <param name="Handle">The window handle (HWND).</param>
/// <param name="ProcessId">Process that owns the window.</param>
/// <param name="ProcessName">Executable name, like «explorer.exe».</param>
/// <param name="Title">The caption.</param>
/// <param name="IsVisible">Visible on the desktop (not hidden with ShowWindow).</param>
/// <param name="IsCloaked">Hidden by the desktop manager (another virtual desktop, a suspended app).</param>
/// <param name="IsToolWindow">A palette, tooltip or tray helper (WS_EX_TOOLWINDOW).</param>
/// <param name="HasOwner">Belongs to another window (a dialog, a popup).</param>
/// <param name="Width">Width in pixels.</param>
/// <param name="Height">Height in pixels.</param>
public sealed record RawWindow(
    long Handle,
    int ProcessId,
    string ProcessName,
    string Title,
    bool IsVisible = true,
    bool IsCloaked = false,
    bool IsToolWindow = false,
    bool HasOwner = false,
    int Width = 800,
    int Height = 600);

/// <summary>
/// Which windows the operator is offered for projecting: the ones a person
/// would call «a window» (an application's main window), not the hundreds of
/// invisible helpers Windows keeps around.
/// </summary>
public static class WindowCandidates
{
    /// <summary>
    /// Keeps the windows that are visible, not hidden by the desktop manager,
    /// not tool windows, with no owner, a title and a size — and never those of
    /// <paramref name="ownProcessId"/>: projecting this very program would make
    /// the screen show itself showing itself, forever. Sorted by title.
    /// </summary>
    public static IReadOnlyList<RawWindow> Filter(IEnumerable<RawWindow> windows, int ownProcessId) =>
        windows
            .Where(w => w.IsVisible
                        && !w.IsCloaked
                        && !w.IsToolWindow
                        && !w.HasOwner
                        && w.ProcessId != ownProcessId
                        && !string.IsNullOrWhiteSpace(w.Title)
                        && w.Width > 0
                        && w.Height > 0)
            .OrderBy(w => w.Title, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(w => w.Handle)
            .ToList();
}
