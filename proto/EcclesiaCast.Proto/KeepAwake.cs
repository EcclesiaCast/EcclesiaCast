using System.Diagnostics;
using Serilog;

namespace EcclesiaCast.Proto;

/// <summary>
/// Keeps a Mac from sleeping or dimming the screen while the output is on:
/// <c>/usr/bin/caffeinate -d -i -w &lt;pid&gt;</c>, which comes with macOS and
/// ends by itself if the program dies. Does nothing elsewhere.
/// </summary>
public sealed class KeepAwake : IDisposable
{
    private Process? _caffeinate;

    public void Start()
    {
        if (!OperatingSystem.IsMacOS() || _caffeinate is { HasExited: false })
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

        MacWindowNative.BeginActivity();
    }

    public void Stop()
    {
        MacWindowNative.EndActivity();

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

    public void Dispose() => Stop();
}
