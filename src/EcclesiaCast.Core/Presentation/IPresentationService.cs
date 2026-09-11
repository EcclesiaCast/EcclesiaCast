namespace EcclesiaCast.Core.Presentation;

/// <summary>
/// The single source of truth for what is live on the projection output.
/// The operator UI drives it; output surfaces (projector window, previews,
/// and later stage display or NDI) only listen to <see cref="Changed"/>.
/// </summary>
public interface IPresentationService
{
    SlideContent? CurrentSlide { get; }

    OutputState State { get; }

    /// <summary>
    /// Bottom-of-screen announcement (lower third) shown above everything
    /// else, independent of the output state. Null when hidden.
    /// </summary>
    string? OverlayMessage { get; }

    /// <summary>
    /// Word or phrase highlighted inside the projected text (like a marker
    /// pen over the live verse). Null when nothing is highlighted.
    /// </summary>
    string? HighlightTerm { get; }

    /// <summary>
    /// What shows behind the background where it does not reach — another
    /// library item, resolved from the background's fill setting. Null means
    /// the flat fill colour.
    /// </summary>
    Media.MediaItem? BackgroundFill { get; }

    /// <summary>
    /// Background layer behind the text (image or looping video), independent
    /// of the slide and its theme. Null shows the plain background. Persists
    /// across slide changes, like ProPresenter's backgrounds.
    /// </summary>
    Media.MediaItem? Background { get; }

    /// <summary>
    /// The logo shown in <see cref="OutputState.Logo"/>. Churches keep one per
    /// kind of meeting, so the operator switches it instead of editing it.
    /// Null falls back to the app's built-in wordmark.
    /// </summary>
    Logos.Logo? ActiveLogo { get; }

    /// <summary>
    /// The "we start in 5:00" screen, counting down over the background.
    /// Null when there is none running.
    /// </summary>
    Countdown? Countdown { get; }

    /// <summary>
    /// Notes only the stage display shows — an outline for whoever is
    /// preaching, or a reminder for the band. Never reaches the congregation.
    /// </summary>
    string? StageNotes { get; }

    /// <summary>Raised whenever the slide, the output state or the overlay changes.</summary>
    event EventHandler? Changed;

    /// <summary>Chooses which logo the Logo state shows.</summary>
    void SetActiveLogo(Logos.Logo? logo);

    /// <summary>Puts a slide live and switches the output to content.</summary>
    void GoLive(SlideContent slide);

    /// <summary>Toggles between background-only and the current content.</summary>
    void ToggleClear();

    /// <summary>Toggles between full black and the current content.</summary>
    void ToggleBlack();

    /// <summary>Toggles between the logo and the current content.</summary>
    void ToggleLogo();

    /// <summary>Shows an announcement at the bottom of the output.</summary>
    void ShowOverlay(string message);

    void HideOverlay();

    /// <summary>Highlights a word or phrase in the projected text; null or blank clears it.</summary>
    void SetHighlight(string? term);

    /// <summary>
    /// Sets the background layer; null clears it. <paramref name="fill"/> is
    /// the item that shows behind it, already looked up by the caller.
    /// </summary>
    void SetBackground(Media.MediaItem? background, Media.MediaItem? fill = null);

    /// <summary>Hides the text so only the background shows (for image/video-only slides).</summary>
    void ShowBackgroundOnly();

    /// <summary>Sets the notes the stage display shows; null or blank clears them.</summary>
    void SetStageNotes(string? notes);

    /// <summary>Puts a countdown on the output, replacing any earlier one.</summary>
    void StartCountdown(Countdown countdown);

    /// <summary>Takes the countdown off the output.</summary>
    void StopCountdown();
}
