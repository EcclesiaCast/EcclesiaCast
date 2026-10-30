using System.Windows;
using EcclesiaCast.App.Views;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Feedback;
using Serilog;

namespace EcclesiaCast.App.Services;

/// <summary>
/// Knows when to offer the comments window: after a real session when the
/// program closes (once per version, and never again if the person says so),
/// after a run that did not end on its own, and always from Ayuda. It also
/// counts the opens and minutes that go in the usage summary.
/// </summary>
public static class FeedbackCoordinator
{
    private static ISettingsStore? _settings;
    private static UsageStats? _stats;
    private static string _dataDir = "";
    private static DateTime _startedAt;

    /// <summary>
    /// Set before the program closes itself to update or to restore a copy:
    /// nobody is leaving, so nobody should be asked for an opinion.
    /// </summary>
    public static bool Quiet { get; set; }

    public static void Start(ISettingsStore settings, string dataDir)
    {
        _settings = settings;
        _dataDir = dataDir;
        _stats = new UsageStats(settings);
        _stats.RecordStart();
        _startedAt = DateTime.Now;
    }

    /// <summary>How many times the program has been opened, this one included.</summary>
    public static int Opens => _stats?.Opens ?? 0;

    public static int SessionMinutes => (int)(DateTime.Now - _startedAt).TotalMinutes;

    /// <summary>Adds this session to the total; called once, when the program exits.</summary>
    public static void RecordEnd() => _stats?.AddMinutes(SessionMinutes);

    public static void ShowFromMenu(Window owner) => Show(owner, FeedbackReason.Menu);

    public static void AskAfterCrash(Window owner) => Show(owner, FeedbackReason.AfterCrash);

    /// <summary>Asks, if it is the right moment, before the main window closes.</summary>
    public static void AskOnClosing(Window owner)
    {
        if (Quiet || _settings is null)
            return;
        if (!FeedbackPolicy.ShouldAskOnClose(_settings, AppInfo.Version, DateTime.Now, SessionMinutes))
            return;

        // Marked right away, like the other one-time notices: closing the
        // program in the middle must not bring it back on the next close.
        FeedbackPolicy.MarkAsked(_settings, AppInfo.Version);
        Show(owner, FeedbackReason.Closing);
    }

    private static void Show(Window owner, FeedbackReason reason)
    {
        if (_settings is null || _stats is null)
            return;
        try
        {
            new FeedbackWindow(_settings, _stats, _dataDir, reason, SessionMinutes) { Owner = owner }.ShowDialog();
        }
        catch (Exception ex)
        {
            // Asking for an opinion is never worth getting in the way of closing.
            Log.Warning(ex, "No se pudo mostrar la ventana de comentarios");
        }
    }
}
