using EcclesiaCast.Core.Abstractions;

namespace EcclesiaCast.Core.Feedback;

/// <summary>When to ask «¿Querés enviarnos algún comentario?»: rarely, and never twice for the same version.</summary>
public static class FeedbackPolicy
{
    public const string NeverKey = "feedback.never";
    public const string LastVersionKey = "feedback.lastVersion";

    /// <summary>An accidental open and close is not a service: ask only after a real session.</summary>
    public const int MinSessionMinutes = 5;

    public static bool ShouldAskOnClose(ISettingsStore settings, string version, DateTime now, int sessionMinutes) =>
        now <= FeedbackForm.AutoPromptUntil
        && sessionMinutes >= MinSessionMinutes
        && settings.Get(NeverKey) != "1"
        && settings.Get(LastVersionKey) != version;

    public static void MarkAsked(ISettingsStore settings, string version) =>
        settings.Set(LastVersionKey, version);

    public static void NeverAgain(ISettingsStore settings) =>
        settings.Set(NeverKey, "1");
}
