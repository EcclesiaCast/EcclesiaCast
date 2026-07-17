using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace EcclesiaCast.App.Services;

/// <summary>
/// Generates a poster thumbnail for a media file using the Windows shell
/// (the same image Explorer shows for videos and pictures) and saves it as
/// a PNG in the app's thumbnails folder.
/// </summary>
public static class ShellThumbnail
{
    private static string ThumbsDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "EcclesiaCast", "thumbnails");

    /// <summary>Returns the saved thumbnail path, or null if it couldn't be generated.</summary>
    public static string? Save(string sourcePath, int width = 320, int height = 180)
    {
        try
        {
            var source = GetThumbnail(sourcePath, width, height);
            if (source is null)
                return null;

            Directory.CreateDirectory(ThumbsDir);
            var target = Path.Combine(ThumbsDir, $"{Guid.NewGuid():N}.png");

            using var stream = File.Create(target);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(source));
            encoder.Save(stream);
            return target;
        }
        catch
        {
            return null;
        }
    }

    private static BitmapSource? GetThumbnail(string path, int width, int height)
    {
        var riid = typeof(IShellItemImageFactory).GUID;
        SHCreateItemFromParsingName(path, IntPtr.Zero, ref riid, out var factory);
        if (factory is null)
            return null;

        var hbitmap = IntPtr.Zero;
        try
        {
            factory.GetImage(new SIZE { cx = width, cy = height }, SIIGBF.ResizeToFit, out hbitmap);
            if (hbitmap == IntPtr.Zero)
                return null;

            var source = Imaging.CreateBitmapSourceFromHBitmap(
                hbitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            source.Freeze();
            return source;
        }
        finally
        {
            if (hbitmap != IntPtr.Zero)
                DeleteObject(hbitmap);
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
