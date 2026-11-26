using EcclesiaCast.Core.Audio;

namespace EcclesiaCast.Core.Tests.Audio;

/// <summary>The music list's bar must act while it is dragged, not when the mouse is let go.</summary>
public class LivePlayerVolumeTests
{
    [Fact]
    public void EveryStepOfADragReachesThePlayerBeforeTheSetterReturns()
    {
        var volume = new LivePlayerVolume(80);
        var player = new List<int>();
        volume.Attach(player.Add);

        // No wait, no timer, no «mouse up» between one step and the check.
        foreach (var step in new[] { 79, 60, 41, 20, 35 })
        {
            volume.Value = step;
            Assert.Equal(step, player[^1]);
        }

        Assert.Equal([80, 79, 60, 41, 20, 35], player);
    }

    [Fact]
    public void ANewPlayerGetsTheVolumeWhenItIsAttached()
    {
        var volume = new LivePlayerVolume(80);
        volume.Value = 30;

        var player = -1;
        volume.Attach(v => player = v);

        Assert.Equal(30, player);
    }

    [Fact]
    public void ADiscardedPlayerIsNotTouchedAgainAndTheNextOneIs()
    {
        var volume = new LivePlayerVolume(80);
        var first = new List<int>();
        var second = new List<int>();
        volume.Attach(first.Add);

        volume.Detach();
        volume.Value = 50;
        volume.Attach(second.Add);
        volume.Value = 55;

        Assert.Equal([80], first);
        Assert.Equal([50, 55], second);
    }

    [Theory]
    [InlineData(-5, 0)]
    [InlineData(140, 100)]
    public void TheVolumeStaysBetween0And100(int asked, int expected)
    {
        var volume = new LivePlayerVolume(80) { Value = asked };

        Assert.Equal(expected, volume.Value);
    }
}
