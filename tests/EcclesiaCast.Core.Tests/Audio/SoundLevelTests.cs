using EcclesiaCast.Core.Audio;

namespace EcclesiaCast.Core.Tests.Audio;

public class SoundLevelTests
{
    [Fact]
    public void Full_volume_needs_no_equalizer()
    {
        Assert.Null(SoundLevel.Split(100));
        Assert.Equal(0, SoundLevel.ToDecibels(100));
    }

    [Fact]
    public void Half_volume_falls_like_a_knob()
    {
        // The cube of 0.5 is 1/8: about 18 dB down, all of it in the pre-amplifier
        // until the pre-amplifier runs out.
        var db = SoundLevel.ToDecibels(50);
        Assert.InRange(db, -18.2, -17.9);
        var split = SoundLevel.Split(50)!.Value;
        Assert.Equal(db, split.Preamp, 3);
        Assert.Equal(0, split.Bands, 3);
    }

    [Fact]
    public void Low_volumes_use_the_bands_too()
    {
        var split = SoundLevel.Split(20)!.Value;
        Assert.Equal(SoundLevel.PreampFloor, split.Preamp);
        Assert.InRange(split.Bands, -20, -1);
        Assert.Equal(SoundLevel.ToDecibels(20), split.Preamp + split.Bands, 3);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-5)]
    public void The_quietest_is_the_floor(int volume)
    {
        Assert.Equal(SoundLevel.Floor, SoundLevel.ToDecibels(volume));
        var split = SoundLevel.Split(volume)!.Value;
        Assert.Equal(SoundLevel.PreampFloor, split.Preamp);
        Assert.Equal(SoundLevel.BandFloor, split.Bands);
    }

    [Fact]
    public void Louder_is_never_quieter()
    {
        var previous = double.NegativeInfinity;
        for (var v = 0; v <= 100; v++)
        {
            var db = SoundLevel.ToDecibels(v);
            Assert.True(db >= previous);
            previous = db;
        }
    }
}
