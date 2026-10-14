namespace EcclesiaCast.Core.Presentation;

public sealed class PresentationService : IPresentationService
{
    public SlideContent? CurrentSlide { get; private set; }

    public OutputState State { get; private set; } = OutputState.Clear;

    public string? OverlayMessage { get; private set; }

    public event EventHandler? Changed;

    public void GoLive(SlideContent slide)
    {
        // A mark belongs to the words it was put on: the next slide starts
        // clean. Sending the same slide again (a theme change) keeps them.
        if (!Equals(slide, CurrentSlide))
            Highlight = HighlightSet.None;

        CurrentSlide = slide;
        State = OutputState.Content;
        OnChanged();
    }

    public void ClearText()
    {
        if (CurrentSlide is null && State == OutputState.Clear && Highlight.Equals(HighlightSet.None))
            return;

        CurrentSlide = null;
        Highlight = HighlightSet.None;
        State = OutputState.Clear;
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

    public HighlightSet Highlight { get; private set; } = HighlightSet.None;

    public void SetHighlight(HighlightSet highlight)
    {
        highlight ??= HighlightSet.None;
        if (highlight.Equals(Highlight))
            return;

        Highlight = highlight;
        OnChanged();
    }

    public Logos.Logo? ActiveLogo { get; private set; }

    public void SetActiveLogo(Logos.Logo? logo)
    {
        ActiveLogo = logo;
        OnChanged();
    }

    public Media.MediaItem? Background { get; private set; }

    public Media.MediaItem? BackgroundFill { get; private set; }

    public void SetBackground(Media.MediaItem? background, Media.MediaItem? fill = null)
    {
        // Re-applying the same background must not restart it, but the fill
        // can have changed under it (the operator just edited the framing).
        if (background?.Id == Background?.Id && fill?.Id == BackgroundFill?.Id)
            return;

        Background = background;
        BackgroundFill = fill;
        OnChanged();
    }

    public void RefreshBackground(Media.MediaItem background, Media.MediaItem? fill = null)
    {
        // SetBackground ignores the same item on purpose (changing slides
        // re-applies it); edited settings are the one case that must get
        // through, so the screen gets a fresh object with the same id and
        // each layer redraws it in place.
        if (Background?.Id != background.Id)
            return;

        Background = background;
        BackgroundFill = fill;
        OnChanged();
    }

    public string? StageNotes { get; private set; }

    public void SetStageNotes(string? notes)
    {
        notes = string.IsNullOrWhiteSpace(notes) ? null : notes;
        if (notes == StageNotes)
            return;

        StageNotes = notes;
        OnChanged();
    }

    public Countdown? Countdown { get; private set; }

    public void StartCountdown(Countdown countdown)
    {
        Countdown = countdown;
        OnChanged();
    }

    public void StopCountdown()
    {
        if (Countdown is null)
            return;

        Countdown = null;
        OnChanged();
    }

    public void ShowBackgroundOnly() => SetState(OutputState.Clear);

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
