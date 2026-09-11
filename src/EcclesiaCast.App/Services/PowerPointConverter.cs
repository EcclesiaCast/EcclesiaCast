using System.IO;
using System.Runtime.InteropServices;
using Serilog;

namespace EcclesiaCast.App.Services;

/// <summary>
/// Saves a PowerPoint deck as PDF using the copy of PowerPoint the church
/// already has.
///
/// Nothing else in EcclesiaCast needs Office, and nothing here installs it:
/// if PowerPoint is not on the machine this says so and the operator exports
/// the deck to PDF themselves, which every version of PowerPoint can do.
/// Driving it through COM keeps the fidelity — fonts, animations flattened,
/// the church's own template — that any third-party reader would lose.
/// </summary>
public static class PowerPointConverter
{
    /// <summary>PowerPoint's own "save as PDF" format id.</summary>
    private const int PpSaveAsPdf = 32;

    /// <summary>Returns the PDF's path, or null when PowerPoint is not installed.</summary>
    public static string? ToPdf(string deckPath)
    {
        var type = Type.GetTypeFromProgID("PowerPoint.Application");
        if (type is null)
        {
            Log.Information("No hay PowerPoint instalado: la presentación no se puede convertir sola");
            return null;
        }

        object? application = null;
        object? presentations = null;
        object? presentation = null;

        try
        {
            application = Activator.CreateInstance(type);
            if (application is null)
                return null;

            var target = Path.Combine(Path.GetTempPath(),
                $"ecclesiacast-{Path.GetFileNameWithoutExtension(deckPath)}-{Guid.NewGuid():N}.pdf");

            presentations = application.GetType().InvokeMember(
                "Presentations", System.Reflection.BindingFlags.GetProperty, null, application, null);

            // ReadOnly, no window: the operator should never see PowerPoint
            // flash open in the middle of a service.
            presentation = presentations?.GetType().InvokeMember(
                "Open", System.Reflection.BindingFlags.InvokeMethod, null, presentations,
                [deckPath, true, false, false]);

            presentation?.GetType().InvokeMember(
                "SaveAs", System.Reflection.BindingFlags.InvokeMethod, null, presentation,
                [target, PpSaveAsPdf, 0]);

            presentation?.GetType().InvokeMember(
                "Close", System.Reflection.BindingFlags.InvokeMethod, null, presentation, null);

            return File.Exists(target) ? target : null;
        }
        catch (Exception ex) when (ex is COMException or MissingMethodException or InvalidCastException)
        {
            Log.Warning(ex, "PowerPoint no pudo convertir {Path}", deckPath);
            return null;
        }
        finally
        {
            Release(presentation);
            Release(presentations);

            if (application is not null)
            {
                try
                {
                    application.GetType().InvokeMember(
                        "Quit", System.Reflection.BindingFlags.InvokeMethod, null, application, null);
                }
                catch (COMException) { /* ya se fue */ }

                Release(application);
            }
        }
    }

    private static void Release(object? com)
    {
        if (com is not null && Marshal.IsComObject(com))
            Marshal.FinalReleaseComObject(com);
    }
}
