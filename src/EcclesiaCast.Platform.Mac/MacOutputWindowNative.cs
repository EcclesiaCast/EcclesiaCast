using EcclesiaCast.Core.Displays;
using EcclesiaCast.Presentation.Platform;
using Serilog;
using static EcclesiaCast.Platform.Mac.ObjC;

namespace EcclesiaCast.Platform.Mac;

/// <summary>
/// The output and the stage windows on a Mac, from the prototype of batch 1
/// (<c>MacWindowNative</c>): a window level above the menu bar and the Dock,
/// on every Space without moving, and the frame of the whole screen (the
/// menu bar's strip included). macOS's own full screen is never used: it
/// makes a Space of its own and leaves when another program comes forward.
/// On any other system it does nothing.
/// </summary>
public sealed class MacOutputWindowNative : IOutputWindowNative
{
    // CGWindowLevel.h: kCGMainMenuWindowLevel = 24, kCGStatusWindowLevel = 25,
    // kCGDockWindowLevel = 20. One above the menu bar covers it and the Dock.
    private const long MainMenuWindowLevel = 24;
    public const long OutputWindowLevel = MainMenuWindowLevel + 1;

    // NSWindowCollectionBehavior
    private const ulong CanJoinAllSpaces = 1 << 0;
    private const ulong Stationary = 1 << 4;
    private const ulong IgnoresCycle = 1 << 6;
    private const ulong FullScreenAuxiliary = 1 << 8;

    /// <summary>Nothing to do before it is shown: Avalonia already shows it without the keyboard.</summary>
    public void Prepare(NativeWindowHandle window)
    {
    }

    /// <summary>
    /// Raises the window above the menu bar and the Dock, on every Space,
    /// and stretches it over the whole frame of the screen it is on. The
    /// window must already be on <paramref name="display"/> (its position
    /// is set first); the frame is read from macOS, in its own points.
    /// </summary>
    public void Place(NativeWindowHandle window, DisplayInfo display)
    {
        if (!OperatingSystem.IsMacOS())
            return;

        try
        {
            var nsWindow = NSWindowOf(window);
            if (nsWindow == IntPtr.Zero)
            {
                Log.Error("Mac: no se encontró la NSWindow de la salida");
                return;
            }

            Send(nsWindow, "setLevel:", OutputWindowLevel);
            // FullScreenAuxiliary: it may also sit over another program's
            // full screen. (Auxiliary, Primary and None exclude each other:
            // two of them together make AppKit throw.)
            Send(nsWindow, "setCollectionBehavior:",
                CanJoinAllSpaces | Stationary | IgnoresCycle | FullScreenAuxiliary);
            SendBool(nsWindow, "setHidesOnDeactivate:", false);
            SendBool(nsWindow, "setCanHide:", false);

            // A borderless window may sit over the menu bar, but only if its
            // frame says so: take the whole frame of the screen it is on.
            var screen = SendPtr(nsWindow, "screen");
            if (screen != IntPtr.Zero)
            {
                var frame = SendRect(screen, "frame");
                SendRectBool(nsWindow, "setFrame:display:", frame, true);
            }

            Describe(window, $"puesta en {display.DeviceName}");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Mac: falló la llamada nativa para la ventana de salida");
        }
    }

    /// <summary>Writes the native level, behaviour and frame of a window in the log.</summary>
    public static void Describe(NativeWindowHandle window, string when)
    {
        if (!OperatingSystem.IsMacOS())
            return;

        try
        {
            var nsWindow = NSWindowOf(window);
            if (nsWindow == IntPtr.Zero)
                return;

            var level = SendLong(nsWindow, "level");
            var behavior = SendULong(nsWindow, "collectionBehavior");
            var frame = SendRect(nsWindow, "frame");
            var screen = SendPtr(nsWindow, "screen");
            var screenFrame = screen == IntPtr.Zero ? default : SendRect(screen, "frame");
            var key = SendBoolResult(nsWindow, "isKeyWindow");
            Log.Information(
                "Mac ({When}): nivel {Level} (quería {Wanted}), comportamiento {Behavior}, marco {X},{Y} {W}×{H}, " +
                "pantalla {SX},{SY} {SW}×{SH}, tiene el teclado: {Key}",
                when, level, OutputWindowLevel, behavior, frame.X, frame.Y, frame.Width, frame.Height,
                screenFrame.X, screenFrame.Y, screenFrame.Width, screenFrame.Height, key);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Mac: no se pudo describir la ventana");
        }
    }

    /// <summary>
    /// Avalonia's handle on a Mac is the NSView («NSView») or the NSWindow
    /// («NSWindow»), depending on the version: both lead to the window.
    /// </summary>
    private static IntPtr NSWindowOf(NativeWindowHandle window)
    {
        if (window.IsEmpty)
            return IntPtr.Zero;

        return window.Kind switch
        {
            "NSWindow" => window.Handle,
            "NSView" => SendPtr(window.Handle, "window"),
            _ => LogUnknown(window.Kind),
        };
    }

    private static IntPtr LogUnknown(string? kind)
    {
        Log.Error("Mac: manejador de ventana desconocido: {Kind}", kind);
        return IntPtr.Zero;
    }
}
