using CommunityToolkit.Mvvm.ComponentModel;
using EcclesiaCast.Core.Presentation;

namespace EcclesiaCast.App.ViewModels;

/// <summary>
/// Bindable mirror of <see cref="IPresentationService"/>. The output window
/// and the operator's Live preview both bind to this single instance, so
/// they can never disagree about what is on the projector.
/// </summary>
public sealed partial class ProjectionViewModel : ObservableObject
{
    [ObservableProperty]
    private SlideContent? _slide;

    [ObservableProperty]
    private OutputState _state;

    [ObservableProperty]
    private string? _overlay;

    [ObservableProperty]
    private string? _highlight;

    [ObservableProperty]
    private EcclesiaCast.Core.Media.MediaItem? _background;

    /// <summary>
    /// How blurred the background is right now, 0–100. It lives here rather
    /// than only on the media item so the operator can turn the dial mid-song
    /// and see both previews and the projector follow immediately.
    /// </summary>
    [ObservableProperty]
    private double _backgroundBlur;

    /// <summary>What fills the screen behind a framed background.</summary>
    [ObservableProperty]
    private EcclesiaCast.Core.Media.MediaItem? _backgroundFill;

    /// <summary>Notes for the platform, shown only on the stage display.</summary>
    [ObservableProperty]
    private string? _stageNotes;

    /// <summary>The countdown running before the service, if any.</summary>
    [ObservableProperty]
    private Countdown? _countdown;

    /// <summary>The logo the Logo state shows right now.</summary>
    [ObservableProperty]
    private EcclesiaCast.Core.Logos.Logo? _activeLogo;

    /// <summary>
    /// The slide after the live one. The stage display shows it so the singers
    /// know what is coming; the operator sets it as the grid moves.
    /// </summary>
    [ObservableProperty]
    private SlideContent? _nextSlide;

    /// <summary>Label of the live slide ("Coro", "3:16"), for the stage display.</summary>
    [ObservableProperty]
    private string? _slideLabel;

    public ProjectionViewModel(IPresentationService presentation)
    {
        presentation.Changed += (_, _) => Sync(presentation);
        Sync(presentation);
    }

    private void Sync(IPresentationService presentation)
    {
        Slide = presentation.CurrentSlide;
        State = presentation.State;
        Overlay = presentation.OverlayMessage;
        Highlight = presentation.HighlightTerm;
        Background = presentation.Background;
        BackgroundBlur = presentation.Background?.Blur ?? 0;
        BackgroundFill = presentation.BackgroundFill;
        ActiveLogo = presentation.ActiveLogo;
        Countdown = presentation.Countdown;
        StageNotes = presentation.StageNotes;
    }
}
