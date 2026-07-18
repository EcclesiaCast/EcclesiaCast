using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Serilog;

namespace EcclesiaCast.App.Services;

/// <summary>
/// Asks the Windows shell for the poster image it shows in Explorer.
/// </summary>
public static class ShellThumbnail
{
    /// <summary>
    /// Returns the shell's thumbnail, or null when it has none. Note this never
    /// falls back to the file-type icon: a picture of the VLC cone is worse than
    /// no thumbnail at all, and <see cref="VideoThumbnail"/> can usually decode
    /// the frame the shell couldn't.
    /// </summary>
    public static BitmapSource? TryGet(string path, int width, int height)
    {
        IShellItemImageFactory? factory = null;
        var hbitmap = IntPtr.Zero;
        try
        {
            var riid = typeof(IShellItemImageFactory).GUID;
            SHCreateItemFromParsingName(path, IntPtr.Zero, ref riid, out factory);
            if (factory is null)
                return null;

            factory.GetImage(
                new SIZE { cx = width, cy = height },
                SIIGBF.ResizeToFit | SIIGBF.ThumbnailOnly,
                out hbitmap);
            if (hbitmap == IntPtr.Zero)
                return null;

            var source = Imaging.CreateBitmapSourceFromHBitmap(
                hbitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            source.Freeze();
            return source;
        }
        catch (Exception ex)
        {
            // Expected for formats Windows can't decode (QuickTime-branded mp4,
            // ProRes...); the caller falls back to LibVLC.
            Log.Debug(ex, "El shell no tiene miniatura para {Path}", path);
            return null;
        }
        finally
        {
            if (hbitmap != IntPtr.Zero)
                DeleteObject(hbitmap);
            if (factory is not null)
                Marshal.ReleaseComObject(factory);
        }
    }

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    private static extern void SHCreateItemFromParsingName(
        string pszPath, IntPtr pbc, ref Guid riid, out IShellItemImageFactory ppv);

    [StructLayout(LayoutKind.Sequential)]
    private struct SIZE
    {
        public int cx;
        public int cy;
    }

    [Flags]
    private enum SIIGBF
    {
        ResizeToFit = 0x00,
        BiggerSizeOk = 0x01,
        ThumbnailOnly = 0x08,
    }

    [ComImport]
    [Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItemImageFactory
    {
        void GetImage(SIZE size, SIIGBF flags, out IntPtr phbm);
    }
}
