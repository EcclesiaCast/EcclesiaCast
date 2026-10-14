namespace EcclesiaCast.App.Services;

/// <summary>
/// The web panel of the central box as the view model sees it: which page is
/// open, and a photo of it. The panel owns the browser; the view model only
/// decides what to do with the page (project it, save it).
/// </summary>
public interface IWebPageHost
{
    /// <summary>True once the browser exists and has started: a photo can be taken.</summary>
    bool IsReady { get; }

    /// <summary>The address of the page being shown, or null when none is open yet.</summary>
    string? CurrentAddress { get; }

    /// <summary>The title of the page being shown, or null while it has none.</summary>
    string? CurrentTitle { get; }

    /// <summary>Raised when the operator picks «Agregar a medios» on a picture of the page.</summary>
    event EventHandler<WebImageChosen>? ImageChosen;

    /// <summary>Opens a page in the panel (it is created on the first call).</summary>
    void Open(string address);

    /// <summary>Takes a picture of what the page shows right now and writes it to <paramref name="pngPath"/>.</summary>
    Task<bool> CaptureAsync(string pngPath);
}

/// <summary>
/// A picture the operator picked on a page («Agregar a medios» in the browser's
/// right-click menu): where the picture is, and the page it was on.
/// </summary>
public sealed record WebImageChosen(string Source, string? Page, bool IsThumbnail = false);
