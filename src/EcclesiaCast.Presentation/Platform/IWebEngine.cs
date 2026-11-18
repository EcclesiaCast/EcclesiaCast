namespace EcclesiaCast.Presentation.Platform;

/// <summary>
/// The browser the program embeds for YouTube and the web panel: whether the
/// system has it, and the one setting of the page's picture the view model owns.
/// </summary>
public interface IWebEngine
{
    /// <summary>True when the embedded browser can run on this computer (on Windows, the WebView2 runtime).</summary>
    bool IsRuntimeAvailable();

    /// <summary>The «Mostrar el puntero» box: draw the arrow where the operator points, on the screen.</summary>
    bool ShowPointer { get; set; }
}
