using System.Globalization;
using EcclesiaCast.Presentation.Services;

namespace EcclesiaCast.Presentation.Tests;

public sealed class NumberFieldTextTests
{
    [Theory]
    [InlineData(0.55, 0, 10, 1, 0.6)]
    [InlineData(-1, 0, 10, 1, 0)]
    [InlineData(99, 0, 10, 1, 10)]
    [InlineData(92.4, 10, 400, 0, 92)]
    public void A_value_is_rounded_and_held_in_its_range(
        double value, double minimum, double maximum, int decimals, double expected) =>
        Assert.Equal(expected, NumberFieldText.Clamp(value, minimum, maximum, decimals), 6);

    [Theory]
    [InlineData("1.15", 1.15)]
    [InlineData("1,15", 1.15)]
    [InlineData("  3 ", 3)]
    public void Both_decimal_separators_are_read(string text, double expected)
    {
        var before = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        try
        {
            Assert.True(NumberFieldText.TryParse(text, out var value));
            Assert.Equal(expected, value, 6);
        }
        finally
        {
            CultureInfo.CurrentCulture = before;
        }
    }

    [Theory]
    [InlineData("-")]
    [InlineData("")]
    [InlineData("a")]
    public void Half_typed_text_is_not_a_number(string text) =>
        Assert.False(NumberFieldText.TryParse(text, out _));

    [Fact]
    public void The_value_is_written_with_its_decimals() =>
        Assert.Equal(0.5.ToString("F1", CultureInfo.CurrentCulture), NumberFieldText.Format(0.5, 1));
}
