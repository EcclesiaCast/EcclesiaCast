using System.Runtime.InteropServices;
using Serilog;

namespace EcclesiaCast.App.Services;

/// <summary>
/// Asks Windows not to run this process in "efficiency mode" (EcoQoS).
///
/// On the balanced power plan Windows slows down a process whose window is
/// not in front — and the projector window never takes focus on purpose, so
/// as soon as the operator clicked elsewhere the video could start to
/// stutter. A projection program is exactly the kind of process that must
/// keep its pace. This affects only EcclesiaCast, never the system's plan.
/// </summary>
public static class PowerThrottling
{
    public static void OptOut()
    {
        try
        {
            var state = new ProcessPowerThrottlingState
            {
                Version = ProcessPowerThrottlingCurrentVersion,
                // Control the execution-speed throttle and turn it off.
                ControlMask = ProcessPowerThrottlingExecutionSpeed,
                StateMask = 0,
            };

            if (!SetProcessInformation(GetCurrentProcess(), ProcessPowerThrottlingClass,
                    ref state, Marshal.SizeOf<ProcessPowerThrottlingState>()))
                Log.Warning("Windows no aceptó desactivar el modo eficiencia (error {Code})",
                    Marshal.GetLastWin32Error());
        }
        catch (Exception ex)
        {
            // Older Windows without the API: nothing to opt out of.
            Log.Warning(ex, "No se pudo desactivar el modo eficiencia de Windows");
        }
    }

    private const int ProcessPowerThrottlingClass = 4;
    private const uint ProcessPowerThrottlingCurrentVersion = 1;
    private const uint ProcessPowerThrottlingExecutionSpeed = 0x1;

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessPowerThrottlingState
    {
        public uint Version;
        public uint ControlMask;
        public uint StateMask;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetProcessInformation(IntPtr process, int informationClass,
        ref ProcessPowerThrottlingState information, int size);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();
}
