using EcclesiaCast.Core.Remote;

namespace EcclesiaCast.Core.Tests.Remote;

public class ByteRangeTests
{
    [Theory]
    [InlineData("bytes=0-99", 1000, 0, 99)]
    [InlineData("bytes=500-", 1000, 500, 999)]
    [InlineData("bytes=-200", 1000, 800, 999)]
    [InlineData("bytes=900-5000", 1000, 900, 999)]
    [InlineData("bytes=0-0", 1000, 0, 0)]
    [InlineData("BYTES=10-20", 1000, 10, 20)]
    [InlineData(" bytes=10-20 ", 1000, 10, 20)]
    [InlineData("bytes=-5000", 1000, 0, 999)]
    public void A_range_is_the_first_and_last_byte_inside_the_file(string header, long length, long start, long end)
    {
        Assert.Equal((start, end), ByteRange.Parse(header, length));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("items=0-5")]
    [InlineData("bytes=0-5,10-20")]
    [InlineData("bytes=abc-def")]
    [InlineData("bytes=20-10")]
    [InlineData("bytes=-0")]
    [InlineData("bytes=1000-")]
    [InlineData("bytes=5000-6000")]
    [InlineData("bytes=5")]
    public void Anything_that_is_not_a_usable_range_sends_the_whole_file(string? header)
    {
        Assert.Null(ByteRange.Parse(header, 1000));
    }

    [Fact]
    public void A_file_with_no_length_has_no_ranges()
    {
        Assert.Null(ByteRange.Parse("bytes=0-5", 0));
    }

    [Theory]
    [InlineData("bytes=1000-", 1000, true)]
    [InlineData("bytes=5000-6000", 1000, true)]
    [InlineData("bytes=999-", 1000, false)]
    [InlineData("bytes=-5", 1000, false)]
    [InlineData(null, 1000, false)]
    [InlineData("items=5000-", 1000, false)]
    public void A_range_that_starts_past_the_end_is_told_apart_for_the_416(string? header, long length, bool expected)
    {
        Assert.Equal(expected, ByteRange.IsPastTheEnd(header, length));
    }
}
