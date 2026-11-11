namespace EcclesiaCast.Core.Listening;

/// <summary>How the program moves the slides from what it hears.</summary>
public enum FollowMode
{
    /// <summary>Goes to the slide being sung, once it has heard enough of it. Always a little late.</summary>
    Follow,

    /// <summary>
    /// Moves on to the next slide when the singing reaches the end of the
    /// one on screen, so the words are up before they are sung; a jump
    /// elsewhere in the song is still followed.
    /// </summary>
    Anticipate,
}

/// <summary>The knobs of the pilot. The defaults are the cautious ones.</summary>
public sealed record FollowOptions
{
    public FollowMode Mode { get; init; } = FollowMode.Anticipate;

    /// <summary>Anticipate: move on when this many words (or fewer) of the slide are left.</summary>
    public int LeadWords { get; init; } = 2;

    /// <summary>How sure the follower must be to act at all.</summary>
    public double MinConfidence { get; init; } = 0.55;

    /// <summary>How sure to jump somewhere that is neither this slide nor the next.</summary>
    public double JumpConfidence { get; init; } = 0.75;

    /// <summary>Words that must have matched before anything moves.</summary>
    public int MinMatched { get; init; } = 3;

    /// <summary>The least time between two changes made by the pilot.</summary>
    public TimeSpan MinBetweenChanges { get; init; } = TimeSpan.FromSeconds(1.5);

    /// <summary>After the operator moves by hand, the pilot keeps its hands off this long.</summary>
    public TimeSpan PauseAfterOperator { get; init; } = TimeSpan.FromSeconds(3);
}

/// <summary>What the pilot decided for the words it was just given.</summary>
/// <param name="GoTo">The slide to put up, or null to leave things as they are.</param>
/// <param name="Position">Where the follower thinks the singing is, for the operator to see.</param>
public readonly record struct FollowDecision(int? GoTo, LyricPosition? Position);

/// <summary>
/// Decides when the slides of the chosen song change by themselves. It only
/// ever moves inside that song, never twice in a hurry, never against the
/// operator, and it does nothing while it is not sure. Time comes in as a
/// parameter, so every rule can be tested without waiting.
/// </summary>
public sealed class FollowPilot
{
    private readonly LyricFollower _follower;
    private readonly FollowOptions _options;
    private DateTimeOffset _quietUntil = DateTimeOffset.MinValue;

    public FollowPilot(IEnumerable<string> slideTexts, int liveSlide = 0, FollowOptions? options = null)
    {
        _follower = new LyricFollower(slideTexts);
        _options = options ?? new FollowOptions();
        LiveSlide = liveSlide;
    }

    /// <summary>The slide the pilot takes to be on screen.</summary>
    public int LiveSlide { get; private set; }

    /// <summary>
    /// The operator put a slide up by hand (arrow, click, the phone): that
    /// is where the song is now, and the pilot waits before touching anything.
    /// </summary>
    public void OperatorMoved(int slide, DateTimeOffset now)
    {
        LiveSlide = slide;
        _follower.Forget();
        _quietUntil = now + _options.PauseAfterOperator;
    }

    /// <summary>
    /// For a recogniser that listens again to the last seconds each time
    /// (Whisper): <paramref name="window"/> is everything it made out of them,
    /// and takes the place of what was heard before instead of adding to it.
    /// The caller starts its window after the last change of slide.
    /// </summary>
    public FollowDecision HearWindow(string? window, DateTimeOffset now)
    {
        _follower.Forget();
        return Hear(window, now);
    }

    /// <summary>Takes the words just recognised and says whether a slide should go up.</summary>
    public FollowDecision Hear(string? words, DateTimeOffset now)
    {
        _follower.Hear(words);
        var position = _follower.Locate(LiveSlide);

        if (position is not { } at || now < _quietUntil
            || at.Matched < _options.MinMatched || at.Confidence < _options.MinConfidence)
            return new FollowDecision(null, position);

        var target = Target(at);
        if (target is not { } slide || slide == LiveSlide)
            return new FollowDecision(null, position);

        LiveSlide = slide;
        _quietUntil = now + _options.MinBetweenChanges;

        // The end of the slide just left is still in the ear: it must not
        // count as «they went back».
        _follower.Forget();
        return new FollowDecision(slide, position);
    }

    private int? Target(LyricPosition at)
    {
        if (at.Slide == LiveSlide)
        {
            // Singing the slide on screen: in Anticipate, its end calls the next one.
            return _options.Mode == FollowMode.Anticipate
                   && at.WordsLeft <= _options.LeadWords
                   && LiveSlide + 1 < _follower.SlideCount
                ? LiveSlide + 1
                : null;
        }

        // Singing another slide. The next one is the expected move; anywhere
        // else (back to the chorus, a verse skipped) asks for more proof.
        var needed = at.Slide == LiveSlide + 1 ? _options.MinConfidence : _options.JumpConfidence;
        if (at.Confidence < needed)
            return null;

        // Already at the end of that slide too (the pilot was late): in
        // Anticipate, go straight to the one that follows it.
        return _options.Mode == FollowMode.Anticipate
               && at.WordsLeft <= _options.LeadWords
               && at.Slide + 1 < _follower.SlideCount
            ? at.Slide + 1
            : at.Slide;
    }
}
