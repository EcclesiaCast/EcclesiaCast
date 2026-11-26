using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace EcclesiaCast.App.Services;

/// <summary>
/// Only one copy of the program per data folder. A second copy would take the
/// next port (so the OBS or phone address of the first one stops matching what
/// the operator sees) and fight over the same database. The data folder is
/// part of the name so a test build with ECCLESIACAST_DATA_DIR still runs next
/// to the real one.
/// </summary>
internal static class SingleInstance
{
    private static Mutex? _mutex;

    /// <summary>
    /// True when this is the only copy. Waits a moment so a copy that is still
    /// closing (a restart after an update) does not make the new one give up.
    /// The mutex is kept until the process ends, which releases it.
    /// </summary>
    public static bool TryAcquire(string dataDir, TimeSpan wait)
    {
        var hash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(dataDir).ToLowerInvariant())))[..12];

        _mutex = new Mutex(initiallyOwned: false, $"Local\\EcclesiaCast-{hash}");
        try
        {
            return _mutex.WaitOne(wait);
        }
        catch (AbandonedMutexException)
        {
            // The previous copy was killed: the lock is ours now.
            return true;
        }
    }

    /// <summary>Brings the window of the copy that is already open to the front. False when it cannot be found.</summary>
    public static bool FocusExisting()
    {
        using var me = Process.GetCurrentProcess();
        foreach (var other in Process.GetProcessesByName(me.ProcessName))
        {
            using (other)
            {
                if (other.Id == me.Id || other.MainWindowHandle == IntPtr.Zero)
                    continue;

                if (IsIconic(other.MainWindowHandle))
                    ShowWindow(other.MainWindowHandle, 9); // SW_RESTORE

                return SetForegroundWindow(other.MainWindowHandle);
            }
        }

        return false;
    }

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);
}
