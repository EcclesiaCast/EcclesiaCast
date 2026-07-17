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
    /// Background layer behind the text (image or looping video), independent
    /// of the slide and its theme. Null shows the plain background. Persists
    /// across slide changes, like ProPresenter's backgrounds.
    /// </summary>
    Media.MediaItem? Background { get; }

    /// <summary>Raised whenever the slide, the output state or the overlay changes.</summary>
    event EventHandler? Changed;

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

    /// <summary>Sets the background layer; null clears it.</summary>
    void SetBackground(Media.MediaItem? background);

    /// <summary>Hides the text so only the background shows (for image/video-only slides).</summary>
    void ShowBackgroundOnly();
}
