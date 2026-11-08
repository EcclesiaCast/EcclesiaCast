using LibVLCSharp.Shared;
using Serilog;

namespace EcclesiaCast.App.Services;

/// <summary>
/// Copies VLC's own errors into the program's log, so the report a church
/// sends says why a video had no sound or did not open (a codec VLC lacks,
/// a sound device that refused to start). Only errors, and the warnings of
/// the sound output; at most <see cref="MaxLines"/> per run, without repeats.
/// </summary>
public static class VlcLog
{
    private const int MaxLines = 300;

    private static int _written;
    private static string? _last;
    private static readonly object Sync = new();

    public static void Attach(LibVLC libVlc)
    {
        try
        {
            libVlc.Log += OnLog;
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "No se pudo escuchar el registro de VLC");
        }
    }

    private static void OnLog(object? sender, LogEventArgs e)
    {
        if (_written >= MaxLines)
            return;

        var module = e.Module ?? string.Empty;
        var isSound = module.Contains("audio", StringComparison.OrdinalIgnoreCase)
                      || module.Contains("aout", StringComparison.OrdinalIgnoreCase)
                      || module.Contains("mmdevice", StringComparison.OrdinalIgnoreCase)
                      || module.Contains("wasapi", StringComparison.OrdinalIgnoreCase)
                      || module.Contains("directsound", StringComparison.OrdinalIgnoreCase);
        if (e.Level != LogLevel.Error && !(e.Level == LogLevel.Warning && isSound))
            return;

        var line = $"{module}: {e.Message}";
        lock (Sync)
        {
            if (line == _last || _written >= MaxLines)
                return;
            _last = line;
            _written++;
        }

        if (e.Level == LogLevel.Error)
            Log.Warning("VLC: {Line}", line);
        else
            Log.Information("VLC: {Line}", line);
    }
}
