using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Feedback;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Presentation.Services;

/// <summary>Why the comments window is open: it decides the wording and whether it offers to stop asking.</summary>
public enum FeedbackReason
{
    /// <summary>The person chose Ayuda → Enviar comentarios.</summary>
    Menu,

    /// <summary>The program is closing after a real session.</summary>
    Closing,

    /// <summary>The last run did not end on its own.</summary>
    AfterCrash,
}

/// <summary>
/// One run of the program as the comments see it: it counts the opens and
/// minutes that go in the usage summary, knows when to offer the comments
/// window (after a real session when the program closes, once per version and
/// never again if the person says so, and after a run that did not end on its
/// own) and makes the form the window shows. Made once, at start-up.
/// </summary>
public sealed class FeedbackSession
{
    private static readonly Lazy<HttpClient> Client = new(CreateClient);

    private readonly ISettingsStore _settings;
    private readonly string _dataFolder;
    private readonly string _systemLabel;
    private readonly Func<DateTime> _now;
    private readonly Func<FeedbackMessage, Task<bool>> _send;
    private readonly UsageStats _stats;
    private readonly DateTime _startedAt;

    /// <param name="systemLabel">How the usage summary names the line of the system.</param>
    /// <param name="now">The clock (the tests set their own).</param>
    /// <param name="send">How a comment goes out (the tests set their own).</param>
    public FeedbackSession(
        ISettingsStore settings, string dataFolder, string systemLabel = "Windows",
        Func<DateTime>? now = null, Func<FeedbackMessage, Task<bool>>? send = null)
    {
        _settings = settings;
        _dataFolder = dataFolder;
        _systemLabel = systemLabel;
        _now = now ?? (() => DateTime.Now);
        _send = send ?? (message => new FeedbackSender(Client.Value).SendAsync(message));
        _stats = new UsageStats(settings, _now);
        _stats.RecordStart();
        _startedAt = _now();
    }

    /// <summary>
    /// Set before the program closes itself to update or to restore a copy:
    /// nobody is leaving, so nobody should be asked for an opinion.
    /// </summary>
    public bool Quiet { get; set; }

    /// <summary>How many times the program has been opened, this one included.</summary>
    public int Opens => _stats.Opens;

    public int SessionMinutes => (int)(_now() - _startedAt).TotalMinutes;

    /// <summary>Adds this session to the total; called once, when the program exits.</summary>
    public void RecordEnd() => _stats.AddMinutes(SessionMinutes);

    /// <summary>
    /// True when, as the main window closes, it is the right moment to ask.
    /// It is marked as asked right away, like the other one-time notices:
    /// closing the program in the middle must not bring it back on the next close.
    /// </summary>
    public bool TakeClosingTurn()
    {
        if (Quiet || !FeedbackPolicy.ShouldAskOnClose(_settings, AppInfo.Version, _now(), SessionMinutes))
            return false;

        FeedbackPolicy.MarkAsked(_settings, AppInfo.Version);
        return true;
    }

    /// <summary>
    /// Whether to ask about a run that did not end on its own: not on the very
    /// first start (the person has not used it yet), not past the day the
    /// temporary notices stop and not after «no volver a mostrar».
    /// </summary>
    public bool ShouldAskAfterCrash(bool lastRunCrashed) =>
        lastRunCrashed && _now() <= FeedbackForm.AutoPromptUntil
        && _settings.Get(FeedbackPolicy.NeverKey) != "1" && Opens > 1;

    /// <summary>A form ready to be filled, with the usage summary as it would go.</summary>
    public FeedbackViewModel CreateForm(FeedbackReason reason)
    {
        var errors = RecentErrors.Read(
            Path.Combine(_dataFolder, "logs"),
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        var summary = _stats.BuildSummary(
            AppInfo.Version, RuntimeInformation.OSDescription, SessionMinutes, errors, _systemLabel);
        return new FeedbackViewModel(_settings, reason, summary, _send);
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"EcclesiaCast/{AppInfo.Version}");
        return client;
    }
}
