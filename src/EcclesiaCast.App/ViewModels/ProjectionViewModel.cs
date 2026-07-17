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

    public ProjectionViewModel(IPresentationService presentation)
    {
        presentation.Changed += (_, _) =>
        {
            Slide = presentation.CurrentSlide;
            State = presentation.State;
            Overlay = presentation.OverlayMessage;
            Highlight = presentation.HighlightTerm;
            Background = presentation.Background;
        };
        Slide = presentation.CurrentSlide;
        State = presentation.State;
        Overlay = presentation.OverlayMessage;
        Highlight = presentation.HighlightTerm;
        Background = presentation.Background;
    }
}
