using EcclesiaCast.Core.Updates;

namespace EcclesiaCast.App.Services;

/// <summary>
/// The program looking for its own new versions, as the main window sees it:
/// whether there is one waiting, a way to open the notice, and the searches
/// the operator asks for from the Ayuda menu.
/// </summary>
public interface IUpdates
{
    /// <summary>A newer version found and not yet installed or skipped; null when there is none.</summary>
    UpdateOffer? Pending { get; }

    /// <summary>Raised when <see cref="Pending"/> changes.</summary>
    event EventHandler? PendingChanged;

    /// <summary>«Buscar actualizaciones al abrir» in the Ayuda menu.</summary>
    bool CheckOnStartup { get; set; }

    /// <summary>Opens the notice of the pending version (the «⬆ Versión nueva» button).</summary>
    void ShowPending();

    /// <summary>
    /// Searches right now, whatever was searched today and even for a skipped
    /// version, and says the result in a message.
    /// </summary>
    Task CheckNowAsync();
}
