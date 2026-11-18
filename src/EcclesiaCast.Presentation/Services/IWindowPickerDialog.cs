using EcclesiaCast.Core.Capture;
using EcclesiaCast.Presentation.Platform;

namespace EcclesiaCast.Presentation.Services;

/// <summary>What the operator chose in the «Proyectar otra ventana» list.</summary>
/// <param name="Window">The window picked, or null when the system's own picker is to be used.</param>
/// <param name="UseWindowsPicker">Open the system's picker (windows and whole screens) instead.</param>
/// <param name="ShowCursor">Draw the mouse pointer on the captured picture.</param>
/// <param name="SaveToMedia">Keep the window as a card in the media bar instead of projecting it now.</param>
public sealed record WindowPick(RawWindow? Window, bool UseWindowsPicker, bool ShowCursor, bool SaveToMedia = false);

/// <summary>Opens the list of windows that can be projected.</summary>
public interface IWindowPickerDialog
{
    /// <summary>Shows it modally; null when the operator closed it without choosing.</summary>
    WindowPick? Pick(IWindowCapture capture, bool showCursor);
}
