using EcclesiaCast.Core.Media;

namespace EcclesiaCast.Core.Tests.Media;

public class LightCopyPolicyTests
{
    [Fact]
    public void A_1080p_30_frame_video_plays_from_the_original()
    {
        Assert.Null(LightCopyPolicy.Plan(new VideoStreamInfo(1920, 1080, 30, 1)));
    }

    [Fact]
    public void The_churchs_1920_by_1280_loops_need_no_copy()
    {
        // Wider than 16:9 is not "bigger": the width is exactly the screen's.
        Assert.Null(LightCopyPolicy.Plan(new VideoStreamInfo(1920, 1280, 30, 1)));
    }

    [Fact]
    public void A_4K_video_is_shrunk()
    {
        var plan = LightCopyPolicy.Plan(new VideoStreamInfo(3840, 2160, 30, 1));

        Assert.NotNull(plan);
        Assert.True(plan.Shrink);
        Assert.False(plan.ChangesFrameRate);
    }

    [Fact]
    public void A_cinema_2K_file_is_close_enough_to_leave_alone()
    {
        Assert.Null(LightCopyPolicy.Plan(new VideoStreamInfo(2048, 1080, 24, 1)));
    }

    [Fact]
    public void A_video_smaller_than_the_screen_is_never_enlarged()
    {
        Assert.Null(LightCopyPolicy.Plan(new VideoStreamInfo(1280, 720, 30, 1)));
    }

    [Theory]
    [InlineData(60, 1, 30, 1)]
    [InlineData(50, 1, 25, 1)]
    [InlineData(60000, 1001, 60000, 2002)]  // 59.94 → 29.97
    [InlineData(120, 1, 30, 1)]
    public void Sixty_frame_videos_are_halved(int num, int den, int expectedNum, int expectedDen)
    {
        var plan = LightCopyPolicy.Plan(new VideoStreamInfo(1920, 1080, num, den));

        Assert.NotNull(plan);
        Assert.False(plan.Shrink);
        Assert.Equal(expectedNum / (double)expectedDen, plan.FrameRateNumerator / (double)plan.FrameRateDenominator, 3);
    }

    [Theory]
    [InlineData(30000, 1001)]  // 29.97
    [InlineData(25, 1)]
    [InlineData(24, 1)]
    public void Ordinary_frame_rates_are_kept(int num, int den)
    {
        Assert.Null(LightCopyPolicy.Plan(new VideoStreamInfo(1920, 1080, num, den)));
    }

    [Fact]
    public void A_4K_60_video_gets_both()
    {
        var plan = LightCopyPolicy.Plan(new VideoStreamInfo(3840, 2160, 60, 1));

        Assert.NotNull(plan);
        Assert.True(plan.Shrink);
        Assert.True(plan.ChangesFrameRate);
    }

    [Fact]
    public void An_unreadable_size_is_not_treated_as_huge()
    {
        Assert.Null(LightCopyPolicy.Plan(new VideoStreamInfo(0, 0, 30, 1)));
    }
}
