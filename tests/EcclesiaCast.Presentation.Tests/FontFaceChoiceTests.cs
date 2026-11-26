using EcclesiaCast.Presentation.Rendering;

namespace EcclesiaCast.Presentation.Tests;

public class FontFaceChoiceTests
{
    private static readonly int[] SegoeUi = [300, 350, 400, 600, 700, 900];

    [Fact]
    public void A_weight_the_font_has_is_drawn_as_it_is()
    {
        Assert.Equal(new FaceChoice(600, false), FontFaceChoice.Choose(SegoeUi, 600));
        Assert.Equal(new FaceChoice(400, false), FontFaceChoice.Choose(SegoeUi, 400));
    }

    [Fact]
    public void Medium_in_segoe_ui_is_semibold_as_in_wpf()
    {
        // The notice at the foot asks for Medium; WPF draws Segoe UI Semibold.
        Assert.Equal(new FaceChoice(600, false), FontFaceChoice.Choose(SegoeUi, 500));
    }

    [Fact]
    public void Below_medium_a_tie_goes_to_the_lighter_face()
    {
        Assert.Equal(new FaceChoice(300, false), FontFaceChoice.Choose([300, 500], 400));
    }

    [Fact]
    public void Semibold_in_a_font_with_regular_and_bold_is_the_real_bold()
    {
        Assert.Equal(new FaceChoice(700, false), FontFaceChoice.Choose([400, 700], 600));
    }

    [Fact]
    public void Semibold_in_a_font_with_only_regular_is_thickened()
    {
        // Impact: one face; WPF thickens it.
        Assert.Equal(new FaceChoice(400, true), FontFaceChoice.Choose([400], 600));
        Assert.Equal(new FaceChoice(400, false), FontFaceChoice.Choose([400], 400));
    }

    [Fact]
    public void Without_faces_the_asked_weight_is_kept()
    {
        Assert.Equal(new FaceChoice(500, false), FontFaceChoice.Choose([], 500));
    }
}
