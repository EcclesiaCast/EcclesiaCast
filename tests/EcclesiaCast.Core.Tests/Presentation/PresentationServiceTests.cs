using EcclesiaCast.Core.Presentation;

namespace EcclesiaCast.Core.Tests.Presentation;

public class PresentationServiceTests
{
    private readonly PresentationService _service = new();

    [Fact]
    public void Starts_cleared_with_no_slide()
    {
        Assert.Null(_service.CurrentSlide);
        Assert.Equal(OutputState.Clear, _service.State);
    }

    [Fact]
    public void GoLive_shows_the_slide_as_content()
    {
        var slide = new SlideContent("Bienvenidos");

        _service.GoLive(slide);

        Assert.Equal(slide, _service.CurrentSlide);
        Assert.Equal(OutputState.Content, _service.State);
    }

    [Fact]
    public void GoLive_raises_Changed()
    {
        var raised = 0;
        _service.Changed += (_, _) => raised++;

        _service.GoLive(new SlideContent("Hola"));

        Assert.Equal(1, raised);
    }

    [Fact]
    public void ToggleBlack_twice_returns_to_content()
    {
        _service.GoLive(new SlideContent("Texto"));

        _service.ToggleBlack();
        Assert.Equal(OutputState.Black, _service.State);

        _service.ToggleBlack();
        Assert.Equal(OutputState.Content, _service.State);
    }

    [Fact]
    public void ToggleClear_keeps_the_current_slide_for_when_content_returns()
    {
        var slide = new SlideContent("Texto");
        _service.GoLive(slide);

        _service.ToggleClear();

        Assert.Equal(OutputState.Clear, _service.State);
        Assert.Equal(slide, _service.CurrentSlide);
    }

    [Fact]
    public void Toggling_back_to_content_without_a_slide_stays_clear()
    {
        _service.ToggleBlack();
        Assert.Equal(OutputState.Black, _service.State);

        _service.ToggleBlack();

        Assert.Equal(OutputState.Clear, _service.State);
    }

    [Fact]
    public void Switching_from_black_to_logo_is_direct()
    {
        _service.GoLive(new SlideContent("Texto"));
        _service.ToggleBlack();

        _service.ToggleLogo();

        Assert.Equal(OutputState.Logo, _service.State);
    }

    [Fact]
    public void ShowOverlay_sets_the_message_and_raises_Changed()
    {
        var raised = 0;
        _service.Changed += (_, _) => raised++;

        _service.ShowOverlay("El auto ABC 123 está mal estacionado");

        Assert.Equal("El auto ABC 123 está mal estacionado", _service.OverlayMessage);
        Assert.Equal(1, raised);
    }

    [Fact]
    public void HideOverlay_clears_the_message()
    {
        _service.ShowOverlay("Aviso");

        _service.HideOverlay();

        Assert.Null(_service.OverlayMessage);
    }

    [Fact]
    public void Overlay_survives_state_changes()
    {
        _service.GoLive(new SlideContent("Letra"));
        _service.ShowOverlay("Aviso");

        _service.ToggleBlack();
        Assert.Equal("Aviso", _service.OverlayMessage);

        _service.ToggleLogo();
        Assert.Equal("Aviso", _service.OverlayMessage);
    }

    [Fact]
    public void HideOverlay_when_already_hidden_does_not_raise_Changed()
    {
        var raised = 0;
        _service.Changed += (_, _) => raised++;

        _service.HideOverlay();

        Assert.Equal(0, raised);
    }

    [Fact]
    public void SetHighlight_trims_and_raises_Changed()
    {
        var raised = 0;
        _service.Changed += (_, _) => raised++;

        _service.SetHighlight("  gracia  ");

        Assert.Equal("gracia", _service.HighlightTerm);
        Assert.Equal(1, raised);
    }

    [Fact]
    public void SetHighlight_with_blank_clears_the_term()
    {
        _service.SetHighlight("gracia");

        _service.SetHighlight("   ");

        Assert.Null(_service.HighlightTerm);
    }

    [Fact]
    public void SetHighlight_with_the_same_value_does_not_raise_Changed()
    {
        _service.SetHighlight("gracia");
        var raised = 0;
        _service.Changed += (_, _) => raised++;

        _service.SetHighlight("gracia");

        Assert.Equal(0, raised);
    }

    [Fact]
    public void GoLive_while_black_returns_to_content()
    {
        _service.GoLive(new SlideContent("Uno"));
        _service.ToggleBlack();

        _service.GoLive(new SlideContent("Dos"));

        Assert.Equal(OutputState.Content, _service.State);
        Assert.Equal("Dos", _service.CurrentSlide!.MainText);
    }
}
