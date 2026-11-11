using EcclesiaCast.Core.Listening;

namespace EcclesiaCast.Core.Tests.Listening;

public class FollowPilotTests
{
    // «Santo, santo, santo» (Reginald Heber, trad. Juan B. Cabrera): dominio público.
    private static readonly string[] Hymn =
    [
        "Santo, santo, santo, Señor omnipotente,\nsiempre el labio mío loores te dará.",
        "Santo, santo, santo, te adoro reverente,\nDios en tres personas, bendita Trinidad.",
        "Santo, santo, santo, en numeroso coro\nsantos escogidos te adoran sin cesar,",
        "de alegría llenos, y sus coronas de oro\nrinden ante el trono y el cristalino mar.",
    ];

    private static readonly DateTimeOffset Start = new(2026, 11, 10, 10, 0, 0, TimeSpan.Zero);

    private static DateTimeOffset At(double seconds) => Start.AddSeconds(seconds);

    // ── Cómo suenan las palabras ─────────────────────────────────

    [Theory]
    [InlineData("bendita", "vendita")]
    [InlineData("hacia", "asia")]
    [InlineData("lleno", "yeno")]
    [InlineData("Señor", "senor")]
    [InlineData("quiero", "kiero")]
    [InlineData("guerra", "guera")]
    [InlineData("cielo", "sielo")]
    [InlineData("corazón", "korason")]
    public void Words_that_sound_the_same_get_the_same_key(string written, string heard)
    {
        Assert.Equal(LyricSounds.Key(written), LyricSounds.Key(heard));
    }

    [Fact]
    public void Different_words_keep_different_keys()
    {
        Assert.NotEqual(LyricSounds.Key("santo"), LyricSounds.Key("canto"));
        Assert.NotEqual(LyricSounds.Key("gente"), LyricSounds.Key("gato"));
    }

    [Fact]
    public void A_long_word_one_letter_off_still_counts_but_a_short_one_does_not()
    {
        Assert.True(LyricSounds.Similarity(LyricSounds.Key("reverente"), LyricSounds.Key("reberent")) > 0);
        Assert.Equal(0, LyricSounds.Similarity(LyricSounds.Key("mar"), LyricSounds.Key("dar")));
    }

    [Fact]
    public void Punctuation_and_line_breaks_do_not_make_words()
    {
        Assert.Equal(5, LyricSounds.Words("¡Santo, santo!\nSeñor — omnipotente; (sí) ...").Count);
    }

    // ── Dónde está el canto ──────────────────────────────────────

    [Fact]
    public void Too_few_words_say_nothing()
    {
        var follower = new LyricFollower(Hymn);
        follower.Hear("santo santo");

        Assert.Null(follower.Locate(0));
    }

    [Fact]
    public void A_line_heard_with_mistakes_is_found_in_its_slide()
    {
        var follower = new LyricFollower(Hymn);
        follower.Hear("te adoro reberente dios en tres");

        var at = follower.Locate(0);

        Assert.NotNull(at);
        Assert.Equal(1, at.Value.Slide);
        Assert.True(at.Value.Confidence > 0.6);
    }

    [Fact]
    public void Words_of_no_slide_give_no_confident_place()
    {
        var follower = new LyricFollower(Hymn);
        follower.Hear("buenos días hermanos vamos a orar juntos");

        var at = follower.Locate(0);

        Assert.True(at is null || at.Value.Confidence < 0.3);
    }

    [Fact]
    public void The_line_every_slide_starts_with_stays_on_the_slide_on_screen()
    {
        var follower = new LyricFollower(Hymn);
        follower.Hear("santo santo santo");

        Assert.Equal(1, follower.Locate(liveSlide: 1)!.Value.Slide);
        Assert.Equal(2, follower.Locate(liveSlide: 2)!.Value.Slide);
    }

    [Fact]
    public void The_end_of_a_slide_leaves_no_words_to_sing()
    {
        var follower = new LyricFollower(Hymn);
        follower.Hear("el labio mío loores te dará");

        var at = follower.Locate(0)!.Value;

        Assert.Equal(0, at.Slide);
        Assert.Equal(0, at.WordsLeft);
    }

    // ── El piloto ────────────────────────────────────────────────

    [Fact]
    public void Anticipate_moves_on_when_the_slide_is_ending()
    {
        var pilot = new FollowPilot(Hymn);

        Assert.Null(pilot.Hear("santo santo santo señor omnipotente", At(0)).GoTo);
        Assert.Null(pilot.Hear("siempre el labio", At(3)).GoTo);
        Assert.Equal(1, pilot.Hear("mío loores te", At(5)).GoTo);
        Assert.Equal(1, pilot.LiveSlide);
    }

    [Fact]
    public void After_moving_on_the_tail_of_the_old_slide_does_not_pull_it_back()
    {
        var pilot = new FollowPilot(Hymn);
        pilot.Hear("siempre el labio mío loores te", At(0));
        Assert.Equal(1, pilot.LiveSlide);

        // The last word of slide 0 arrives after the change, then slide 1 starts.
        Assert.Null(pilot.Hear("dará", At(2)).GoTo);
        Assert.Null(pilot.Hear("santo santo santo te adoro", At(4)).GoTo);
        Assert.Equal(1, pilot.LiveSlide);
    }

    [Fact]
    public void A_whole_song_sung_with_mistakes_is_walked_slide_by_slide()
    {
        var pilot = new FollowPilot(Hymn);
        string[] heard =
        [
            "santo santo señor potente", "siempre labio mio", "lores te dara",
            "santo santo te adoro", "reberente dios en tres", "personas vendita trinidad",
            "santo santo en numeroso coro", "santos escojidos te", "adoran sin sesar",
            "de alegria yenos", "sus coronas de oro", "rinden ante el trono",
        ];

        var visited = new List<int> { pilot.LiveSlide };
        for (var i = 0; i < heard.Length; i++)
        {
            if (pilot.Hear(heard[i], At(i * 3)).GoTo is { } slide)
                visited.Add(slide);
        }

        Assert.Equal([0, 1, 2, 3], visited);
    }

    [Fact]
    public void Follow_mode_waits_to_hear_the_next_slide()
    {
        var pilot = new FollowPilot(Hymn, options: new FollowOptions { Mode = FollowMode.Follow });

        Assert.Null(pilot.Hear("siempre el labio mío loores te dará", At(0)).GoTo);
        Assert.Equal(1, pilot.Hear("santo santo te adoro reverente", At(4)).GoTo);
    }

    [Fact]
    public void A_jump_back_in_the_song_is_followed_when_it_is_clear()
    {
        var pilot = new FollowPilot(Hymn, liveSlide: 3);

        var decision = pilot.Hear("santo santo santo te adoro reverente", At(0));

        // Slide 1 is being sung, and it is not near its end: go there.
        Assert.Equal(1, decision.GoTo);
    }

    [Fact]
    public void Nothing_moves_twice_in_a_hurry()
    {
        var pilot = new FollowPilot(Hymn);
        Assert.Equal(1, pilot.Hear("siempre el labio mío loores te", At(0)).GoTo);

        // Half a second later the end of slide 1 seems to be heard: too soon.
        Assert.Null(pilot.Hear("dios en tres personas bendita trinidad", At(0.5)).GoTo);
        Assert.Equal(1, pilot.LiveSlide);
    }

    [Fact]
    public void The_operator_wins_and_the_pilot_waits()
    {
        var pilot = new FollowPilot(Hymn);
        pilot.OperatorMoved(2, At(0));

        // Right after, even a clear ending does nothing.
        Assert.Null(pilot.Hear("santos escogidos te adoran sin cesar", At(1)).GoTo);

        // Later it picks up from where the operator left it.
        Assert.Equal(3, pilot.Hear("santos escogidos te adoran sin cesar", At(5)).GoTo);
    }

    [Fact]
    public void Talking_between_songs_moves_nothing()
    {
        var pilot = new FollowPilot(Hymn, liveSlide: 1);

        Assert.Null(pilot.Hear("vamos a ponernos de pie para recibir la palabra del señor", At(0)).GoTo);
        Assert.Equal(1, pilot.LiveSlide);
    }

    [Fact]
    public void The_last_slide_has_nowhere_to_go()
    {
        var pilot = new FollowPilot(Hymn, liveSlide: 3);

        Assert.Null(pilot.Hear("ante el trono y el cristalino mar", At(0)).GoTo);
    }
}
