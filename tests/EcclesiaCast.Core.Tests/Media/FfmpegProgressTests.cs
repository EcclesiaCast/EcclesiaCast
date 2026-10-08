using EcclesiaCast.Core.Media;

namespace EcclesiaCast.Core.Tests.Media;

public class FfmpegProgressTests
{
    [Fact]
    public void The_time_done_is_read_from_the_microseconds_line()
    {
        Assert.True(FfmpegProgress.TryParseTime("out_time_us=12500000", out var time));
        Assert.Equal(TimeSpan.FromSeconds(12.5), time);
    }

    [Fact]
    public void The_older_misnamed_milliseconds_line_is_microseconds_too()
    {
        Assert.True(FfmpegProgress.TryParseTime("out_time_ms=3000000", out var time));
        Assert.Equal(TimeSpan.FromSeconds(3), time);
    }

    [Theory]
    [InlineData("out_time_us=N/A")]
    [InlineData("out_time_us=-577014")]
    [InlineData("frame=120")]
    [InlineData("progress=continue")]
    [InlineData("")]
    public void Lines_that_are_not_a_time_are_ignored(string line)
    {
        Assert.False(FfmpegProgress.TryParseTime(line, out _));
    }

    [Fact]
    public void The_length_comes_from_the_ffprobe_number()
    {
        Assert.True(FfmpegProgress.TryParseDuration("93.250000\r\n", out var duration));
        Assert.Equal(TimeSpan.FromSeconds(93.25), duration);
    }

    [Theory]
    [InlineData("N/A")]
    [InlineData("0")]
    [InlineData("")]
    public void An_unreadable_length_is_not_a_length(string output)
    {
        Assert.False(FfmpegProgress.TryParseDuration(output, out _));
    }

    [Fact]
    public void The_fraction_goes_from_zero_to_one_and_never_past_it()
    {
        var total = TimeSpan.FromSeconds(60);

        Assert.Equal(0.5, FfmpegProgress.Fraction(TimeSpan.FromSeconds(30), total), precision: 6);
        Assert.Equal(1, FfmpegProgress.Fraction(TimeSpan.FromSeconds(75), total));
    }

    [Fact]
    public void The_fraction_is_zero_while_the_length_is_unknown()
    {
        Assert.Equal(0, FfmpegProgress.Fraction(TimeSpan.FromSeconds(10), TimeSpan.Zero));
    }
}
