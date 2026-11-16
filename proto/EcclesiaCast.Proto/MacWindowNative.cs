using System.Runtime.InteropServices;
using Avalonia.Controls;
using Serilog;

namespace EcclesiaCast.Proto;

/// <summary>
/// What the output window needs from macOS that Avalonia does not offer:
/// a window level above the menu bar and the Dock, staying on every Space
/// without moving, and keeping App Nap away while projecting. Everything is
/// a plain Objective-C message (<c>objc_msgSend</c>) on the main thread.
/// Elsewhere every method does nothing.
/// </summary>
public static class MacWindowNative
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

    // NSActivityOptions: NSActivityUserInitiated | NSActivityLatencyCritical.
    private const ulong ActivityUserInitiated = 0x00FFFFFFUL | (1UL << 20);
    private const ulong ActivityLatencyCritical = 0xFF00000000UL;

    private static IntPtr _activity;

    /// <summary>
    /// Raises the output window above the menu bar and the Dock, on every
    /// Space, and stretches it over its whole screen (menu bar area included).
    /// Call once the window is shown.
    /// </summary>
    public static void MakeOutputWindow(Window window)
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

            Describe(window, "salida preparada");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Mac: falló la llamada nativa para la ventana de salida");
        }
    }

    /// <summary>Writes the native level, behaviour and frame of a window in the log.</summary>
    public static void Describe(Window window, string when)
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

    /// <summary>Tells macOS a projection is going on, so App Nap does not slow the program down.</summary>
    public static void BeginActivity()
    {
        if (!OperatingSystem.IsMacOS() || _activity != IntPtr.Zero)
            return;

        try
        {
            var info = SendPtr(objc_getClass("NSProcessInfo"), "processInfo");
            var reason = NSString("EcclesiaCast está proyectando");
            var token = SendPtrULongPtr(info, "beginActivityWithOptions:reason:",
                ActivityUserInitiated | ActivityLatencyCritical, reason);
            // The token comes autoreleased: keep it until EndActivity.
            _activity = SendPtr(token, "retain");
            Log.Information("Mac: actividad iniciada (sin App Nap)");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Mac: no se pudo iniciar la actividad");
        }
    }

    public static void EndActivity()
    {
        if (!OperatingSystem.IsMacOS() || _activity == IntPtr.Zero)
            return;

        try
        {
            var info = SendPtr(objc_getClass("NSProcessInfo"), "processInfo");
            SendVoidPtr(info, "endActivity:", _activity);
            SendPtr(_activity, "release");
            Log.Information("Mac: actividad terminada");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Mac: no se pudo terminar la actividad");
        }
        finally
        {
            _activity = IntPtr.Zero;
        }
    }

    /// <summary>
    /// Avalonia's handle on a Mac is the NSView («NSView») or the NSWindow
    /// («NSWindow»), depending on the version: both lead to the window.
    /// </summary>
    private static IntPtr NSWindowOf(Window window)
    {
        var handle = window.TryGetPlatformHandle();
        if (handle is null || handle.Handle == IntPtr.Zero)
            return IntPtr.Zero;

        return handle.HandleDescriptor switch
        {
            "NSWindow" => handle.Handle,
            "NSView" => SendPtr(handle.Handle, "window"),
            _ => LogUnknown(handle.HandleDescriptor),
        };
    }

    private static IntPtr LogUnknown(string? descriptor)
    {
        Log.Error("Mac: manejador de ventana desconocido: {Descriptor}", descriptor);
        return IntPtr.Zero;
    }

    private static IntPtr NSString(string text)
    {
        var utf8 = Marshal.StringToCoTaskMemUTF8(text);
        try
        {
            return SendPtrPtr(objc_getClass("NSString"), "stringWithUTF8String:", utf8);
        }
        finally
        {
            Marshal.FreeCoTaskMem(utf8);
        }
    }

    // ── Objective-C ──────────────────────────────────────────────

    [StructLayout(LayoutKind.Sequential)]
    private struct CGRect
    {
        public double X, Y, Width, Height;
    }

    private const string ObjC = "/usr/lib/libobjc.A.dylib";

    [DllImport(ObjC, CharSet = CharSet.Ansi)]
    private static extern IntPtr objc_getClass(string name);

    [DllImport(ObjC, CharSet = CharSet.Ansi)]
    private static extern IntPtr sel_registerName(string name);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern IntPtr MsgPtr(IntPtr receiver, IntPtr selector);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern long MsgLong(IntPtr receiver, IntPtr selector);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern ulong MsgULong(IntPtr receiver, IntPtr selector);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool MsgBoolResult(IntPtr receiver, IntPtr selector);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern CGRect MsgRect(IntPtr receiver, IntPtr selector);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern void MsgSetLong(IntPtr receiver, IntPtr selector, long value);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern void MsgSetULong(IntPtr receiver, IntPtr selector, ulong value);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern void MsgSetBool(IntPtr receiver, IntPtr selector, [MarshalAs(UnmanagedType.I1)] bool value);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern void MsgSetRectBool(IntPtr receiver, IntPtr selector, CGRect rect, [MarshalAs(UnmanagedType.I1)] bool value);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern IntPtr MsgPtrPtr(IntPtr receiver, IntPtr selector, IntPtr argument);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern void MsgVoidPtr(IntPtr receiver, IntPtr selector, IntPtr argument);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern IntPtr MsgPtrULongPtr(IntPtr receiver, IntPtr selector, ulong options, IntPtr argument);

    private static IntPtr SendPtr(IntPtr receiver, string selector) => MsgPtr(receiver, sel_registerName(selector));
    private static long SendLong(IntPtr receiver, string selector) => MsgLong(receiver, sel_registerName(selector));
    private static ulong SendULong(IntPtr receiver, string selector) => MsgULong(receiver, sel_registerName(selector));
    private static bool SendBoolResult(IntPtr receiver, string selector) => MsgBoolResult(receiver, sel_registerName(selector));
    private static CGRect SendRect(IntPtr receiver, string selector) => MsgRect(receiver, sel_registerName(selector));
    private static void Send(IntPtr receiver, string selector, long value) => MsgSetLong(receiver, sel_registerName(selector), value);
    private static void Send(IntPtr receiver, string selector, ulong value) => MsgSetULong(receiver, sel_registerName(selector), value);
    private static void SendBool(IntPtr receiver, string selector, bool value) => MsgSetBool(receiver, sel_registerName(selector), value);
    private static void SendRectBool(IntPtr receiver, string selector, CGRect rect, bool value) =>
        MsgSetRectBool(receiver, sel_registerName(selector), rect, value);
    private static IntPtr SendPtrPtr(IntPtr receiver, string selector, IntPtr argument) =>
        MsgPtrPtr(receiver, sel_registerName(selector), argument);
    private static void SendVoidPtr(IntPtr receiver, string selector, IntPtr argument) =>
        MsgVoidPtr(receiver, sel_registerName(selector), argument);
    private static IntPtr SendPtrULongPtr(IntPtr receiver, string selector, ulong options, IntPtr argument) =>
        MsgPtrULongPtr(receiver, sel_registerName(selector), options, argument);
}
