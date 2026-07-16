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
    public void GoLive_while_black_returns_to_content()
    {
        _service.GoLive(new SlideContent("Uno"));
        _service.ToggleBlack();

        _service.GoLive(new SlideContent("Dos"));

        Assert.Equal(OutputState.Content, _service.State);
        Assert.Equal("Dos", _service.CurrentSlide!.MainText);
    }
}
