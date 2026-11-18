using EcclesiaCast.Core.Media;

namespace EcclesiaCast.Core.Tests.Media;

public class NewVideoFitTests
{
    [Fact]
    public void Nothing_saved_means_whole_with_blurred_edges()
    {
        Assert.Equal(NewVideoFitMode.BlurredEdges, NewVideoFit.Parse(null).Mode);
        Assert.Equal(NewVideoFit.Default, NewVideoFit.Parse(""));
        Assert.Equal(NewVideoFit.Default, NewVideoFit.Parse("Cualquiera|#123456"));
        Assert.Equal(NewVideoFit.Default, NewVideoFit.Parse("99"));
    }

    [Theory]
    [InlineData(NewVideoFitMode.BlurredEdges, "#000000")]
    [InlineData(NewVideoFitMode.Color, "#1E2A44")]
    [InlineData(NewVideoFitMode.Fill, "#000000")]
    [InlineData(NewVideoFitMode.Stretch, "#FFFFFF")]
    public void A_saved_choice_reads_back(NewVideoFitMode mode, string color)
    {
        var fit = new NewVideoFit(mode, color);
        Assert.Equal(fit, NewVideoFit.Parse(fit.ToSetting()));
    }

    [Fact]
    public void A_bad_colour_falls_back_to_black() =>
        Assert.Equal("#000000", NewVideoFit.Parse("Color|rojo").Color);

    [Fact]
    public void Blurred_edges_fit_the_video_and_fill_with_itself()
    {
        var video = new MediaItem { Type = MediaType.Video };
        NewVideoFit.Default.ApplyTo(video);

        Assert.Equal(MediaScaling.Fit, video.Scaling);
        Assert.True(video.FillsWithOwnVideo);
        Assert.False(video.FillsWithSelf);
    }

    [Fact]
    public void Colour_fits_the_video_on_that_colour()
    {
        var video = new MediaItem { Type = MediaType.Video };
        new NewVideoFit(NewVideoFitMode.Color, "#203040").ApplyTo(video);

        Assert.Equal(MediaScaling.Fit, video.Scaling);
        Assert.Null(video.FillMediaId);
        Assert.Equal("#203040", video.FillColor);
    }

    [Theory]
    [InlineData(NewVideoFitMode.Fill, MediaScaling.Fill)]
    [InlineData(NewVideoFitMode.Stretch, MediaScaling.Stretch)]
    public void Fill_and_stretch_are_what_they_say(NewVideoFitMode mode, MediaScaling scaling)
    {
        var video = new MediaItem { Type = MediaType.Video };
        new NewVideoFit(mode, "#000000").ApplyTo(video);

        Assert.Equal(scaling, video.Scaling);
        Assert.Null(video.FillMediaId);
    }

    [Fact]
    public void Only_videos_are_touched()
    {
        var image = new MediaItem { Type = MediaType.Image, Scaling = MediaScaling.Fit, FillMediaId = MediaItem.FillWithSelfId };
        new NewVideoFit(NewVideoFitMode.Fill, "#000000").ApplyTo(image);

        Assert.Equal(MediaScaling.Fit, image.Scaling);
        Assert.True(image.FillsWithSelf);
    }

    [Fact]
    public void A_video_saved_before_keeps_filling_the_screen() =>
        // What a library from before has: nothing in the column, so «Rellenar».
        Assert.Equal(MediaScaling.Fill, new MediaItem { Type = MediaType.Video }.Scaling);
}
