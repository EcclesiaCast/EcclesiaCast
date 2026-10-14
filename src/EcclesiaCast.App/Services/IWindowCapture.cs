using EcclesiaCast.Core.Capture;

namespace EcclesiaCast.App.Services;

/// <summary>
/// Showing another program's window on the screen, live. The picture goes to
/// <see cref="WindowCaptureFeed"/>; this is only the part that starts, stops
/// and lists. Nothing leaves the computer: it is Windows' own window capture,
/// drawn into a bitmap in this process.
/// </summary>
public interface IWindowCapture
{
    /// <summary>False on a Windows too old to capture a window (before 10 version 1903).</summary>
    bool IsSupported { get; }

    /// <summary>True while a window is being captured.</summary>
    bool IsRunning { get; }

    /// <summary>True when the captured window is minimized: no new pictures arrive, the last one stays.</summary>
    bool IsMinimized { get; }

    /// <summary>The windows worth offering, by title; never one of this program's own.</summary>
    IReadOnlyList<RawWindow> ListWindows();

    /// <summary>Starts capturing <paramref name="window"/>, replacing whatever was being captured.</summary>
    bool Start(RawWindow window, bool showCursor, out string? error);

    /// <summary>
    /// Lets the operator choose in Windows' own picker (windows and screens)
    /// and captures the choice. Null title with no error: they cancelled.
    /// </summary>
    Task<(string? Title, string? Error)> StartFromPickerAsync(nint owner, bool showCursor);

    /// <summary>Stops, frees everything and empties the picture.</summary>
    void Stop();

    /// <summary>Raised on the UI thread when the captured window is closed.</summary>
    event EventHandler? Ended;

    /// <summary>Raised on the UI thread when the window is minimized or comes back.</summary>
    event EventHandler? MinimizedChanged;
}
