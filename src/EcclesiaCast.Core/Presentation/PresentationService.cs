namespace EcclesiaCast.Core.Presentation;

public sealed class PresentationService : IPresentationService
{
    public SlideContent? CurrentSlide { get; private set; }

    public OutputState State { get; private set; } = OutputState.Clear;

    public string? OverlayMessage { get; private set; }

    public event EventHandler? Changed;

    public void GoLive(SlideContent slide)
    {
        CurrentSlide = slide;
        State = OutputState.Content;
        OnChanged();
    }

    public void ToggleClear() =>
        SetState(State == OutputState.Clear ? OutputState.Content : OutputState.Clear);

    public void ToggleBlack() =>
        SetState(State == OutputState.Black ? OutputState.Content : OutputState.Black);

    public void ToggleLogo() =>
        SetState(State == OutputState.Logo ? OutputState.Content : OutputState.Logo);

    public void ShowOverlay(string message)
    {
        OverlayMessage = message;
        OnChanged();
    }

    public void HideOverlay()
    {
        if (OverlayMessage is null)
            return;

        OverlayMessage = null;
        OnChanged();
    }

    public string? HighlightTerm { get; private set; }

    public void SetHighlight(string? term)
    {
        term = string.IsNullOrWhiteSpace(term) ? null : term.Trim();
        if (term == HighlightTerm)
            return;

        HighlightTerm = term;
        OnChanged();
    }

    public Media.MediaItem? Background { get; private set; }

    public void SetBackground(Media.MediaItem? background)
    {
        if (background?.Id == Background?.Id)
            return;

        Background = background;
        OnChanged();
    }

    private void SetState(OutputState state)
    {
        // There is nothing to return to without a slide: fall back to clear.
        if (state == OutputState.Content && CurrentSlide is null)
            state = OutputState.Clear;

        if (state == State)
            return;

        State = state;
        OnChanged();
    }

    private void OnChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
