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
        // (on top of the 12 dB it gives back for the equalizer itself).
        var db = SoundLevel.ToDecibels(50);
        Assert.InRange(db, -18.2, -17.9);
        var split = SoundLevel.Split(50)!.Value;
        Assert.Equal(SoundLevel.PreampUnity + db, split.Preamp, 3);
        Assert.Equal(0, split.Bands, 3);
    }

    [Fact]
    public void Low_volumes_use_the_bands_too()
    {
        var split = SoundLevel.Split(20)!.Value;
        Assert.Equal(SoundLevel.PreampFloor, split.Preamp);
        Assert.InRange(split.Bands, -20, -1);
        Assert.Equal(SoundLevel.PreampUnity + SoundLevel.ToDecibels(20), split.Preamp + split.Bands, 3);
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
    public void Just_under_full_is_almost_full()
    {
        // The equalizer's own 12 dB are given back: 99 % is not 12 dB down.
        var split = SoundLevel.Split(99)!.Value;
        Assert.InRange(split.Preamp, 11.5, 12);
        Assert.Equal(0, split.Bands);
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

    [Fact]
    public void Session_volume_only_reaches_zero_at_the_bottom_of_the_bar()
    {
        Assert.Equal(0, SoundLevel.SessionVolume(0));
        Assert.Equal(100, SoundLevel.SessionVolume(100));
        for (var bar = 1; bar < 100; bar++)
            Assert.InRange(SoundLevel.SessionVolume(bar), 1, 99);
    }

    [Fact]
    public void Session_volume_never_falls_when_the_bar_goes_up()
    {
        var previous = 0;
        for (var bar = 0; bar <= 100; bar++)
        {
            var volume = SoundLevel.SessionVolume(bar);
            Assert.True(volume >= previous, $"barra {bar}: {volume} < {previous}");
            previous = volume;
        }
    }

    [Fact]
    public void Each_step_of_the_bar_changes_the_loudness_by_a_similar_amount()
    {
        // LibVLC's law is the cube of the number: the loudness in dB of a session volume.
        static double Db(int volume) => 60 * Math.Log10(volume / 100.0);

        // From 10 % up, no step of the bar is a jump of more than 2 dB.
        for (var bar = 10; bar < 100; bar++)
            Assert.True(Db(SoundLevel.SessionVolume(bar + 1)) - Db(SoundLevel.SessionVolume(bar)) <= 2.0,
                $"salto grande entre {bar} y {bar + 1}");
    }
}
