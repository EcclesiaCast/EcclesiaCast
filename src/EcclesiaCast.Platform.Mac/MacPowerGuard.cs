using System.Diagnostics;
using EcclesiaCast.Presentation.Platform;
using Serilog;
using static EcclesiaCast.Platform.Mac.ObjC;

namespace EcclesiaCast.Platform.Mac;

/// <summary>
/// While the output is on, a Mac must neither sleep nor dim the screen, nor
/// slow the program down with App Nap (the output never takes the keyboard,
/// so to macOS the program is in the background). From the prototype of
/// batch 1 (<c>KeepAwake</c> and <c>MacWindowNative</c>):
/// <list type="bullet">
/// <item><c>/usr/bin/caffeinate -d -i -w &lt;pid&gt;</c>, which comes with
/// macOS and ends by itself if the program dies;</item>
/// <item>an activity of <c>NSProcessInfo</c>, «user initiated» and «latency
/// critical», which keeps App Nap away.</item>
/// </list>
/// On any other system it does nothing.
/// </summary>
public sealed class MacPowerGuard : IPowerGuard, IDisposable
{
    // NSActivityOptions: NSActivityUserInitiated | NSActivityLatencyCritical.
    private const ulong ActivityUserInitiated = 0x00FFFFFFUL | (1UL << 20);
    private const ulong ActivityLatencyCritical = 0xFF00000000UL;

    private Process? _caffeinate;
    private IntPtr _activity;

    /// <summary>Nothing for the whole run: everything here is only while projecting.</summary>
    public void Start()
    {
    }

    public void BeginProjecting()
    {
        if (!OperatingSystem.IsMacOS())
            return;

        StartCaffeinate();
        BeginActivity();
    }

    public void EndProjecting()
    {
        if (!OperatingSystem.IsMacOS())
            return;

        EndActivity();
        StopCaffeinate();
    }

    public void Dispose() => EndProjecting();

    private void StartCaffeinate()
    {
        if (_caffeinate is { HasExited: false })
            return;

        try
        {
            var info = new ProcessStartInfo("/usr/bin/caffeinate", $"-d -i -w {Environment.ProcessId}")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            _caffeinate = Process.Start(info);
            Log.Information("caffeinate prendido (proceso {Pid})", _caffeinate?.Id);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "No se pudo lanzar caffeinate");
        }
    }

    private void StopCaffeinate()
    {
        var process = _caffeinate;
        _caffeinate = null;
        if (process is null)
            return;

        try
        {
            if (!process.HasExited)
                process.Kill();
            Log.Information("caffeinate apagado");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "No se pudo terminar caffeinate");
        }
        finally
        {
            process.Dispose();
        }
    }

    /// <summary>Tells macOS a projection is going on, so App Nap does not slow the program down.</summary>
    private void BeginActivity()
    {
        if (_activity != IntPtr.Zero)
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

    private void EndActivity()
    {
        if (_activity == IntPtr.Zero)
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
}
