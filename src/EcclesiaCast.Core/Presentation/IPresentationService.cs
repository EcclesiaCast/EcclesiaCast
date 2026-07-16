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

    /// <summary>Raised whenever the slide or the output state changes.</summary>
    event EventHandler? Changed;

    /// <summary>Puts a slide live and switches the output to content.</summary>
    void GoLive(SlideContent slide);

    /// <summary>Toggles between background-only and the current content.</summary>
    void ToggleClear();

    /// <summary>Toggles between full black and the current content.</summary>
    void ToggleBlack();

    /// <summary>Toggles between the logo and the current content.</summary>
    void ToggleLogo();
}
