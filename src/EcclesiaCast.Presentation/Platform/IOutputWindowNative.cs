using EcclesiaCast.Core.Displays;

namespace EcclesiaCast.Presentation.Platform;

/// <summary>
/// A window as the system knows it: a window handle (HWND) on Windows; on a
/// Mac the NSWindow or its NSView, as <see cref="Kind"/> says («NSWindow» or
/// «NSView»). Zero when the system window does not exist yet.
/// </summary>
public readonly record struct NativeWindowHandle(IntPtr Handle, string? Kind = null)
{
    public bool IsEmpty => Handle == IntPtr.Zero;
}

/// <summary>
/// What the output and the stage windows need from the system and the
/// interfaces do not offer: covering a whole screen, above everything (on a
/// Mac, above the menu bar and the Dock too), without ever taking the
/// keyboard from the operator's window.
/// </summary>
public interface IOutputWindowNative
{
    /// <summary>
    /// Once, as soon as the system window exists and before it is placed:
    /// on Windows the window is marked so it never takes the keyboard.
    /// </summary>
    void Prepare(NativeWindowHandle window);

    /// <summary>
    /// Puts the window over the whole of <paramref name="display"/>, shown,
    /// without taking the keyboard. Called every time it is (re)shown.
    /// </summary>
    void Place(NativeWindowHandle window, DisplayInfo display);
}
