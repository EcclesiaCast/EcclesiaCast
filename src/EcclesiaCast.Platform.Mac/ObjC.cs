using System.Runtime.InteropServices;

namespace EcclesiaCast.Platform.Mac;

/// <summary>
/// Plain Objective-C messages (<c>objc_msgSend</c>) from libobjc, one
/// signature per shape of call. Only ever reached on a Mac.
/// </summary>
internal static class ObjC
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CGRect
    {
        public double X, Y, Width, Height;
    }

    private const string Library = "/usr/lib/libobjc.A.dylib";

    [DllImport(Library, CharSet = CharSet.Ansi)]
    public static extern IntPtr objc_getClass(string name);

    [DllImport(Library, CharSet = CharSet.Ansi)]
    private static extern IntPtr sel_registerName(string name);

    [DllImport(Library, EntryPoint = "objc_msgSend")]
    private static extern IntPtr MsgPtr(IntPtr receiver, IntPtr selector);

    [DllImport(Library, EntryPoint = "objc_msgSend")]
    private static extern long MsgLong(IntPtr receiver, IntPtr selector);

    [DllImport(Library, EntryPoint = "objc_msgSend")]
    private static extern ulong MsgULong(IntPtr receiver, IntPtr selector);

    [DllImport(Library, EntryPoint = "objc_msgSend")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool MsgBoolResult(IntPtr receiver, IntPtr selector);

    [DllImport(Library, EntryPoint = "objc_msgSend")]
    private static extern CGRect MsgRect(IntPtr receiver, IntPtr selector);

    [DllImport(Library, EntryPoint = "objc_msgSend")]
    private static extern void MsgSetLong(IntPtr receiver, IntPtr selector, long value);

    [DllImport(Library, EntryPoint = "objc_msgSend")]
    private static extern void MsgSetULong(IntPtr receiver, IntPtr selector, ulong value);

    [DllImport(Library, EntryPoint = "objc_msgSend")]
    private static extern void MsgSetBool(IntPtr receiver, IntPtr selector, [MarshalAs(UnmanagedType.I1)] bool value);

    [DllImport(Library, EntryPoint = "objc_msgSend")]
    private static extern void MsgSetRectBool(IntPtr receiver, IntPtr selector, CGRect rect, [MarshalAs(UnmanagedType.I1)] bool value);

    [DllImport(Library, EntryPoint = "objc_msgSend")]
    private static extern IntPtr MsgPtrPtr(IntPtr receiver, IntPtr selector, IntPtr argument);

    [DllImport(Library, EntryPoint = "objc_msgSend")]
    private static extern void MsgVoidPtr(IntPtr receiver, IntPtr selector, IntPtr argument);

    [DllImport(Library, EntryPoint = "objc_msgSend")]
    private static extern IntPtr MsgPtrULongPtr(IntPtr receiver, IntPtr selector, ulong options, IntPtr argument);

    public static IntPtr SendPtr(IntPtr receiver, string selector) => MsgPtr(receiver, sel_registerName(selector));
    public static long SendLong(IntPtr receiver, string selector) => MsgLong(receiver, sel_registerName(selector));
    public static ulong SendULong(IntPtr receiver, string selector) => MsgULong(receiver, sel_registerName(selector));
    public static bool SendBoolResult(IntPtr receiver, string selector) => MsgBoolResult(receiver, sel_registerName(selector));
    public static CGRect SendRect(IntPtr receiver, string selector) => MsgRect(receiver, sel_registerName(selector));
    public static void Send(IntPtr receiver, string selector, long value) => MsgSetLong(receiver, sel_registerName(selector), value);
    public static void Send(IntPtr receiver, string selector, ulong value) => MsgSetULong(receiver, sel_registerName(selector), value);
    public static void SendBool(IntPtr receiver, string selector, bool value) => MsgSetBool(receiver, sel_registerName(selector), value);

    public static void SendRectBool(IntPtr receiver, string selector, CGRect rect, bool value) =>
        MsgSetRectBool(receiver, sel_registerName(selector), rect, value);

    public static IntPtr SendPtrPtr(IntPtr receiver, string selector, IntPtr argument) =>
        MsgPtrPtr(receiver, sel_registerName(selector), argument);

    public static void SendVoidPtr(IntPtr receiver, string selector, IntPtr argument) =>
        MsgVoidPtr(receiver, sel_registerName(selector), argument);

    public static IntPtr SendPtrULongPtr(IntPtr receiver, string selector, ulong options, IntPtr argument) =>
        MsgPtrULongPtr(receiver, sel_registerName(selector), options, argument);

    /// <summary>An autoreleased NSString with <paramref name="text"/>.</summary>
    public static IntPtr NSString(string text)
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
}
