using EcclesiaCast.Desktop.Views;
using EcclesiaCast.Presentation.Services;
using Serilog;

namespace EcclesiaCast.Desktop.Services;

/// <summary>
/// Opens the comments window. When to offer it is decided by
/// <see cref="FeedbackSession"/>, as in WPF; the operator's window of
/// Avalonia calls these (batch 19): from the menu, on starting after a run
/// that did not end on its own and before closing.
/// </summary>
public sealed class FeedbackDialogs(FeedbackSession session)
{
    public void ShowFromMenu() => Show(FeedbackReason.Menu);

    /// <summary>Asks about the last run, if it ended badly and it is still time to ask.</summary>
    public void AskAfterCrash(bool lastRunCrashed)
    {
        if (session.ShouldAskAfterCrash(lastRunCrashed))
            Show(FeedbackReason.AfterCrash);
    }

    /// <summary>Asks, if it is the right moment, before the main window closes.</summary>
    public void AskOnClosing()
    {
        if (session.TakeClosingTurn())
            Show(FeedbackReason.Closing);
    }

    private void Show(FeedbackReason reason)
    {
        try
        {
            Modal.Run(new FeedbackWindow(session.CreateForm(reason)));
        }
        catch (Exception ex)
        {
            // Asking for an opinion is never worth getting in the way of closing.
            Log.Warning(ex, "No se pudo mostrar la ventana de comentarios");
        }
    }
}
