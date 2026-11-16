using EcclesiaCast.Core.Logos;
using EcclesiaCast.Core.Media;
using EcclesiaCast.Data.Persistence;

namespace EcclesiaCast.Core.Tests.Persistence;

/// <summary>A logo is framed like a background: zoom, shift, fixed size and what fills the borders.</summary>
public class LogoFramingTests
{
    [Fact]
    public void The_framing_is_kept_in_the_library()
    {
        using var temp = new TempDatabase();
        var logos = new LogoRepository(temp.Path);

        var saved = logos.Save(new Logo
        {
            Name = "Cuadrado",
            Kind = LogoKind.Image,
            Path = @"C:\logos\cuadrado.png",
            Scaling = MediaScaling.Fit,
            Zoom = 0.8,
            OffsetX = 0.1,
            OffsetY = -0.05,
            FrameWidth = 1080,
            FrameHeight = 1080,
            FillMediaId = MediaItem.FillWithSelfId,
        });

        var read = logos.Get(saved.Id)!;
        Assert.Equal(0.8, read.Zoom);
        Assert.Equal(0.1, read.OffsetX);
        Assert.Equal(-0.05, read.OffsetY);
        Assert.Equal(1080, read.FrameWidth);
        Assert.Equal(1080, read.FrameHeight);
        Assert.Equal(MediaItem.FillWithSelfId, read.FillMediaId);
    }

    [Fact]
    public void A_new_logo_is_not_zoomed()
    {
        Assert.Equal(1, new Logo().Zoom);
    }

    [Fact]
    public void As_media_it_carries_the_framing_and_the_background_colour_as_fill()
    {
        var logo = new Logo
        {
            Id = 3,
            Kind = LogoKind.Image,
            Path = @"C:\logos\cuadrado.png",
            BackgroundColor = "#FFFFFF",
            Scaling = MediaScaling.Fit,
            Zoom = 1.2,
            OffsetX = 0.25,
            FrameWidth = 1440,
            FrameHeight = 1080,
            FillMediaId = MediaItem.FillWithSelfId,
        };

        var media = logo.AsFramedMedia();

        Assert.True(media.Id < 0);
        Assert.Equal(MediaType.Image, media.Type);
        Assert.Equal(MediaScaling.Fit, media.Scaling);
        Assert.Equal(1.2, media.Zoom);
        Assert.Equal(0.25, media.OffsetX);
        Assert.True(media.HasFrame);
        Assert.Equal("#FFFFFF", media.FillColor);
        Assert.True(media.FillsWithSelf);
    }

    [Fact]
    public void A_video_logo_cannot_fill_with_a_blurred_copy_of_itself()
    {
        var logo = new Logo { Kind = LogoKind.Video, Path = @"C:\logos\loop.mp4", FillMediaId = MediaItem.FillWithSelfId };

        Assert.False(logo.AsFramedMedia().FillsWithSelf);
    }
}
