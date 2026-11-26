using System.Windows;
using EcclesiaCast.App.Views;
using EcclesiaCast.Core.Abstractions;
using Serilog;

namespace EcclesiaCast.App.Services;

/// <summary>
/// Opens the comments window over a window of WPF. When to offer it is
/// decided by <see cref="FeedbackSession"/>, which the Mac program uses too.
/// </summary>
public static class FeedbackCoordinator
{
    private static FeedbackSession? _session;

    /// <inheritdoc cref="FeedbackSession.Quiet"/>
    public static bool Quiet
    {
        get => _session?.Quiet ?? false;
        set
        {
            if (_session is not null)
                _session.Quiet = value;
        }
    }

    public static void Start(ISettingsStore settings, string dataDir) =>
        _session = new FeedbackSession(settings, dataDir);

    /// <inheritdoc cref="FeedbackSession.RecordEnd"/>
    public static void RecordEnd() => _session?.RecordEnd();

    public static void ShowFromMenu(Window owner) => Show(owner, FeedbackReason.Menu);

    /// <summary>Asks about the last run, if it ended badly and it is still time to ask.</summary>
    public static void AskAfterCrash(Window owner, bool lastRunCrashed)
    {
        if (_session?.ShouldAskAfterCrash(lastRunCrashed) == true)
            Show(owner, FeedbackReason.AfterCrash);
    }

    /// <summary>Asks, if it is the right moment, before the main window closes.</summary>
    public static void AskOnClosing(Window owner)
    {
        if (_session?.TakeClosingTurn() == true)
            Show(owner, FeedbackReason.Closing);
    }

    private static void Show(Window owner, FeedbackReason reason)
    {
        if (_session is null)
            return;
        try
        {
            new FeedbackWindow(_session.CreateForm(reason)) { Owner = owner }.ShowDialog();
        }
        catch (Exception ex)
        {
            // Asking for an opinion is never worth getting in the way of closing.
            Log.Warning(ex, "No se pudo mostrar la ventana de comentarios");
        }
    }
}
