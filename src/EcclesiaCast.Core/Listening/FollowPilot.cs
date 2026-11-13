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

    /// <summary>
    /// The evidence (matched words, short ones counting half) a jump far
    /// away needs, besides being heard twice in a row.
    /// </summary>
    public double JumpScore { get; init; } = 3.5;

    /// <summary>The least time between two changes made by the pilot.</summary>
    public TimeSpan MinBetweenChanges { get; init; } = TimeSpan.FromSeconds(1.5);

    /// <summary>After the operator moves by hand, the pilot keeps its hands off this long.</summary>
    public TimeSpan PauseAfterOperator { get; init; } = TimeSpan.FromSeconds(3);

    /// <summary>A jump far away must be heard again within this time to count.</summary>
    public TimeSpan JumpConfirmWithin { get; init; } = TimeSpan.FromSeconds(6);
}

/// <summary>What the pilot decided for the words it was just given.</summary>
/// <param name="GoTo">The slide to put up, or null to leave things as they are.</param>
/// <param name="Position">Where the follower thinks the singing is, for the operator to see.</param>
public readonly record struct FollowDecision(int? GoTo, LyricPosition? Position);

/// <summary>
/// Decides when the slides of the chosen song change by themselves. It only
/// ever moves inside that song, never twice in a hurry, never against the
/// operator, and it does nothing while it is not sure. It keeps count of
/// where in the song the singing is, and knows that singing goes forward at
/// the pace of a voice: a line that is in the song several times (a chorus,
/// two slides with the same words) is the copy the singing can have reached,
/// and going anywhere else has to be heard twice. Time comes in as a
/// parameter, so every rule can be tested without waiting.
/// </summary>
public sealed class FollowPilot
{
    /// <summary>Words a second of ordinary singing: what was sung while the recogniser worked.</summary>
    private const double TypicalWordsPerSecond = 1.5;

    /// <summary>Nobody sings faster than this for long: how far the singing can have got since it was last placed.</summary>
    private const double FastestWordsPerSecond = 3.5;

    /// <summary>Room for the recogniser hearing a word early or late.</summary>
    private const int Slack = 5;

    /// <summary>How far ahead the singing may be found without calling it a jump, however long nothing was heard.</summary>
    private const int FurthestAhead = 400;

    private readonly LyricFollower _follower;
    private readonly FollowOptions _options;
    private DateTimeOffset _quietUntil = DateTimeOffset.MinValue;

    // Where the singing was last placed (a word of the whole song) and when.
    private int _at;
    private DateTimeOffset _atTime = DateTimeOffset.MinValue;

    // Whether the slide on screen has been heard sung at all.
    private bool _heardLive;

    // A jump far away heard once, waiting to be heard again.
    private int? _pendingJump;
    private DateTimeOffset _pendingSince;

    public FollowPilot(IEnumerable<string> slideTexts, int liveSlide = 0, FollowOptions? options = null)
    {
        _follower = new LyricFollower(slideTexts);
        _options = options ?? new FollowOptions();
        LiveSlide = liveSlide;
        _at = _follower.FirstWordOf(liveSlide) - 1;
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
        _pendingJump = null;
        _heardLive = false;
        _at = _follower.FirstWordOf(slide) - 1;
        _atTime = now;
        _quietUntil = now + _options.PauseAfterOperator;
    }

    /// <summary>
    /// For a recogniser that listens again to the last seconds each time
    /// (Whisper): <paramref name="window"/> is everything it made out of them,
    /// and takes the place of what was heard before instead of adding to it.
    /// The window may reach back before the last change of slide: the song is
    /// followed as a whole, so the end of the old slide is just where the
    /// singing came from.
    /// </summary>
    /// <param name="lag">
    /// How long ago the window ended (what the recogniser took): in that time
    /// the singing went on, and «adelantarse» counts it.
    /// </param>
    public FollowDecision HearWindow(string? window, DateTimeOffset now, TimeSpan lag = default)
    {
        _follower.HearAll(window);
        return Decide(now, lag, wholeWindow: true);
    }

    /// <summary>Takes the words just recognised and says whether a slide should go up.</summary>
    public FollowDecision Hear(string? words, DateTimeOffset now)
    {
        _follower.Hear(words);
        return Decide(now, TimeSpan.Zero, wholeWindow: false);
    }

    private FollowDecision Decide(DateTimeOffset now, TimeSpan lag, bool wholeWindow)
    {
        var heardAt = now - lag;
        if (Choose(_follower.Candidates(), heardAt) is not { } at)
            return new FollowDecision(null, null);

        if (now < _quietUntil || at.Matched < _options.MinMatched - (wholeWindow ? 1 : 0)
            || at.Confidence < _options.MinConfidence)
            return new FollowDecision(null, at);

        var target = Target(at, now, heardAt, lag);
        if (target is not { } slide || slide == LiveSlide)
            return new FollowDecision(null, at);

        // Going to the slide being sung, it has been heard; moving on ahead of it, not yet.
        _heardLive = slide == at.Slide;
        LiveSlide = slide;
        _pendingJump = null;
        _quietUntil = now + _options.MinBetweenChanges;

        // For words fed one by one, the end of the slide just left is still
        // in the ear and must not count again. A whole window is heard afresh
        // every time, so there is nothing to forget.
        if (!wholeWindow)
            _follower.Forget();
        return new FollowDecision(slide, at);
    }

    /// <summary>How many words ahead of where it was the singing can be by now.</summary>
    private int Reach(DateTimeOffset heardAt)
    {
        var seconds = Math.Max(0, (heardAt - _atTime).TotalSeconds);
        return (int)Math.Min(FurthestAhead, seconds * FastestWordsPerSecond + Slack);
    }

    /// <summary>The singing going on from where it was: a little back (heard again) up to as far as a voice gets.</summary>
    private bool GoesOn(LyricPosition c, DateTimeOffset heardAt)
    {
        var moved = _follower.WordIndex(c) - _at;
        return moved >= -Slack && moved <= Reach(heardAt);
    }

    /// <summary>
    /// Which of the places the words fit is the one being sung. A place the
    /// singing can have reached wins over a clearer one elsewhere, unless
    /// that one is much clearer; among copies of the same words, the nearest.
    /// </summary>
    private LyricPosition? Choose(IReadOnlyList<LyricPosition> candidates, DateTimeOffset heardAt)
    {
        if (candidates.Count == 0)
            return null;

        var top = candidates.Max(c => c.Score);
        var going = candidates.Where(c => GoesOn(c, heardAt)).ToList();
        var pool = going.Count > 0 && going.Max(c => c.Score) >= top - 1.0 ? going : [.. candidates];
        var best = pool.Max(c => c.Score);

        LyricPosition? chosen = null;
        var nearest = int.MaxValue;
        foreach (var c in pool)
        {
            if (c.Score < best - 0.3)
                continue;

            // Ahead first; behind only after anything as far ahead.
            var moved = _follower.WordIndex(c) - _at;
            var far = moved >= 0 ? moved * 2 : -moved * 2 + 1;
            if (far < nearest)
            {
                nearest = far;
                chosen = c;
            }
        }

        return chosen;
    }

    private int? Target(LyricPosition at, DateTimeOffset now, DateTimeOffset heardAt, TimeSpan lag)
    {
        // Words sung since the window ended, while the recogniser worked.
        var gone = (int)Math.Round(lag.TotalSeconds * TypicalWordsPerSecond);
        var anticipate = _options.Mode == FollowMode.Anticipate;
        var endingSoon = at.WordsLeft - gone <= _options.LeadWords;
        var target = anticipate && endingSoon && at.Slide < _follower.SlideCount - 1 ? at.Slide + 1 : at.Slide;
        var goesOn = GoesOn(at, heardAt);

        // The slide on screen, or the next one: what a song does.
        if (goesOn && (at.Slide == LiveSlide || at.Slide == LiveSlide + 1
                       || (at.Slide == LiveSlide - 1 && target >= LiveSlide)))
        {
            _pendingJump = null;
            Place(at, heardAt);

            if (at.Slide == LiveSlide && at.InSlide >= 2)
                _heardLive = true;

            // One word into the next slide may still be the end of this one.
            if (at.Slide > LiveSlide && at.InSlide < 2)
                return null;

            // Leaving a slide nobody was heard singing (somebody talking
            // before the song, saying its words) takes clearer words.
            if (at.Slide > LiveSlide && !_heardLive
                && (at.Confidence < _options.JumpConfidence || at.Score < _options.JumpScore))
                return null;
            return target;
        }

        // The singing is still on the slide before the one on screen (the
        // pilot moved on ahead, or the ear slipped): never one step back.
        if (goesOn && at.Slide == LiveSlide - 1)
        {
            Place(at, heardAt);
            return null;
        }

        // Anywhere else (back to the chorus, a verse skipped) asks for more:
        // clearer words, and the same place heard twice in a row.
        if (at.Confidence < _options.JumpConfidence || at.Score < _options.JumpScore || at.InSlide < 2)
            return null;

        var confirmed = _pendingJump is { } pending
                        && now - _pendingSince <= _options.JumpConfirmWithin
                        && (pending == at.Slide || pending + 1 == at.Slide);
        if (!confirmed)
        {
            _pendingJump = at.Slide;
            _pendingSince = now;
            return null;
        }

        _at = _follower.WordIndex(at);
        _atTime = heardAt;
        return target;
    }

    /// <summary>The singing got this far: it only goes forward.</summary>
    private void Place(LyricPosition at, DateTimeOffset heardAt)
    {
        var index = _follower.WordIndex(at);
        if (index <= _at)
            return;
        _at = index;
        _atTime = heardAt;
    }
}
