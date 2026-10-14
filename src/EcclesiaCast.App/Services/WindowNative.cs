using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using EcclesiaCast.Core.Capture;

namespace EcclesiaCast.App.Services;

/// <summary>
/// What Windows says about the other programs' windows: the list of them, and
/// the small icon of the program each belongs to. Only reads: nothing here
/// touches, focuses or moves any window.
/// </summary>
internal static class WindowNative
{
    private const int GwlExStyle = -20;
    private const int WsExToolWindow = 0x80;
    private const uint GwOwner = 4;
    private const int DwmaCloaked = 14;
    private const uint ProcessQueryLimitedInformation = 0x1000;

    private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hwnd);

    [DllImport("user32.dll")]
    public static extern bool IsIconic(IntPtr hwnd);

    [DllImport("user32.dll")]
    public static extern bool IsWindow(IntPtr hwnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(IntPtr hwnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int maxCount);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out int processId);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindow(IntPtr hwnd, uint command);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hwnd, out NativeRect rect);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr hwnd, int attribute, out int value, int size);

    [DllImport("kernel32.dll")]
    private static extern IntPtr OpenProcess(uint access, bool inherit, int processId);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr handle);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern bool QueryFullProcessImageName(IntPtr process, int flags, StringBuilder name, ref int size);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SHGetFileInfo(string path, uint attributes, ref ShFileInfo info, uint size, uint flags);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr icon);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ShFileInfo
    {
        public IntPtr Icon;
        public int IconIndex;
        public uint Attributes;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string DisplayName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string TypeName;
    }

    /// <summary>
    /// Every top-level window, unfiltered; <see cref="WindowCandidates.Filter"/>
    /// decides which ones are worth offering.
    /// </summary>
    public static IReadOnlyList<RawWindow> EnumerateAll()
    {
        var windows = new List<RawWindow>();
        var processNames = new Dictionary<int, string>();

        EnumWindows((hwnd, _) =>
        {
            try
            {
                if (!IsWindowVisible(hwnd))
                    return true;

                var length = GetWindowTextLength(hwnd);
                if (length <= 0)
                    return true;

                var title = new StringBuilder(length + 1);
                GetWindowText(hwnd, title, title.Capacity);
                GetWindowThreadProcessId(hwnd, out var processId);

                if (!processNames.TryGetValue(processId, out var processName))
                    processNames[processId] = processName = ProcessExecutableName(processId);

                _ = DwmGetWindowAttribute(hwnd, DwmaCloaked, out var cloaked, sizeof(int));
                GetWindowRect(hwnd, out var rect);

                windows.Add(new RawWindow(
                    Handle: hwnd.ToInt64(),
                    ProcessId: processId,
                    ProcessName: processName,
                    Title: title.ToString(),
                    IsVisible: true,
                    IsCloaked: cloaked != 0,
                    IsToolWindow: (GetWindowLong(hwnd, GwlExStyle) & WsExToolWindow) != 0,
                    HasOwner: GetWindow(hwnd, GwOwner) != IntPtr.Zero,
                    Width: rect.Right - rect.Left,
                    Height: rect.Bottom - rect.Top));
            }
            catch
            {
                // A window that vanishes while it is being read is just not listed.
            }

            return true;
        }, IntPtr.Zero);

        return windows;
    }

    /// <summary>The file name of the program, like «explorer.exe»; «?» when Windows will not say.</summary>
    private static string ProcessExecutableName(int processId) =>
        ProcessImagePath(processId) is { } path ? System.IO.Path.GetFileName(path) : "?";

    private static string? ProcessImagePath(int processId)
    {
        var process = OpenProcess(ProcessQueryLimitedInformation, false, processId);
        if (process == IntPtr.Zero)
            return null;

        try
        {
            var name = new StringBuilder(1024);
            var size = name.Capacity;
            return QueryFullProcessImageName(process, 0, name, ref size) ? name.ToString(0, size) : null;
        }
        finally
        {
            CloseHandle(process);
        }
    }

    /// <summary>The small icon of the program that owns the window, or null. Call on the UI thread.</summary>
    public static ImageSource? IconOf(int processId)
    {
        if (ProcessImagePath(processId) is not { } path)
            return null;

        var info = new ShFileInfo();
        const uint icon = 0x100, small = 0x1;
        if (SHGetFileInfo(path, 0, ref info, (uint)Marshal.SizeOf<ShFileInfo>(), icon | small) == IntPtr.Zero
            || info.Icon == IntPtr.Zero)
            return null;

        try
        {
            var source = Imaging.CreateBitmapSourceFromHIcon(info.Icon, Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            source.Freeze();
            return source;
        }
        finally
        {
            DestroyIcon(info.Icon);
        }
    }
}
