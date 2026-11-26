using EcclesiaCast.Core.Remote;

namespace EcclesiaCast.Core.Tests.Remote;

public class RemotePinTests
{
    [Theory]
    [InlineData("1234", true)]
    [InlineData("0000", true)]
    [InlineData("123", false)]
    [InlineData("12345", false)]
    [InlineData("12a4", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Only_four_digits_are_a_valid_pin(string? pin, bool expected) =>
        Assert.Equal(expected, RemotePin.IsValid(pin));

    [Fact]
    public void A_generated_pin_is_four_digits()
    {
        for (var i = 0; i < 200; i++)
            Assert.True(RemotePin.IsValid(RemotePin.Generate()));
    }

    [Fact]
    public void A_new_pin_is_never_the_current_one()
    {
        // A generator that keeps answering 4821 first, then something else.
        var random = new SequenceRandom(1821, 5555);
        Assert.Equal("5555", RemotePin.Generate("1821", random));
    }

    private sealed class SequenceRandom(params int[] values) : Random
    {
        private int _next;
        public override int Next(int minValue, int maxValue) => values[_next++ % values.Length];
    }
}
