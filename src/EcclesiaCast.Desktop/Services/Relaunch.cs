using System.Diagnostics;

namespace EcclesiaCast.Desktop.Services;

/// <summary>Opens the program again, after a backup was restored.</summary>
public static class Relaunch
{
    private const string InsideBundle = ".app/Contents/MacOS/";

    /// <summary>
    /// How the program at <paramref name="programPath"/> is started again. On
    /// a Mac, a program inside an application bundle is opened by the system
    /// («open -n»), as the Finder would; started by hand it would be left
    /// without its icon and name in the Dock.
    /// </summary>
    public static ProcessStartInfo How(string programPath, bool isMac)
    {
        var inside = isMac ? programPath.IndexOf(InsideBundle, StringComparison.Ordinal) : -1;
        if (inside < 0)
            return new ProcessStartInfo(programPath);

        var open = new ProcessStartInfo("/usr/bin/open");
        open.ArgumentList.Add("-n");
        open.ArgumentList.Add(programPath[..(inside + ".app".Length)]);
        return open;
    }

    public static void Start()
    {
        if (Environment.ProcessPath is { } program)
            Process.Start(How(program, OperatingSystem.IsMacOS()))?.Dispose();
    }
}
