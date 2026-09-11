using EcclesiaCast.Core.Remote;

namespace EcclesiaCast.Core.Tests.Remote;

public class PinAttemptLimiterTests
{
    private static readonly DateTimeOffset Sunday =
        new(2026, 9, 13, 10, 0, 0, TimeSpan.FromHours(-3));

    /// <summary>
    /// Someone typing a PIN takes a few seconds between tries; the limiter
    /// only counts one miss per interval, so the tests type like a person.
    /// </summary>
    private static void Miss(PinAttemptLimiter limiter, string client, int times)
    {
        for (var i = 0; i < times; i++)
            limiter.RecordFailure(client, Sunday.AddSeconds(i * 3));
    }

    [Fact]
    public void A_client_that_never_missed_can_try()
    {
        var limiter = new PinAttemptLimiter();

        Assert.Null(limiter.RetryAfter("192.168.1.50", Sunday));
    }

    [Fact]
    public void A_couple_of_fat_fingered_tries_do_not_lock_anyone_out()
    {
        var limiter = new PinAttemptLimiter(maxAttempts: 5);

        for (var i = 0; i < 4; i++)
            limiter.RecordFailure("192.168.1.50", Sunday);

        Assert.Null(limiter.RetryAfter("192.168.1.50", Sunday));
    }

    [Fact]
    public void The_lockout_starts_at_the_last_allowed_miss()
    {
        var limiter = new PinAttemptLimiter(maxAttempts: 5, lockout: TimeSpan.FromMinutes(5));

        Miss(limiter, "192.168.1.50", 5);

        // The fifth miss lands twelve seconds in, and the wait runs from there.
        Assert.Equal(TimeSpan.FromMinutes(5), limiter.RetryAfter("192.168.1.50", Sunday.AddSeconds(12)));
    }

    [Fact]
    public void Waiting_it_out_clears_the_lockout()
    {
        var limiter = new PinAttemptLimiter(maxAttempts: 5, lockout: TimeSpan.FromMinutes(5));

        Miss(limiter, "192.168.1.50", 5);

        Assert.NotNull(limiter.RetryAfter("192.168.1.50", Sunday.AddMinutes(4)));
        Assert.Null(limiter.RetryAfter("192.168.1.50", Sunday.AddMinutes(6)));
    }

    [Fact]
    public void After_the_wait_the_client_starts_from_zero()
    {
        var limiter = new PinAttemptLimiter(maxAttempts: 5, lockout: TimeSpan.FromMinutes(5));

        Miss(limiter, "192.168.1.50", 5);

        var later = Sunday.AddMinutes(6);
        Assert.Null(limiter.RetryAfter("192.168.1.50", later));

        // One more miss must not put the client straight back in the corner.
        limiter.RecordFailure("192.168.1.50", later);
        Assert.Null(limiter.RetryAfter("192.168.1.50", later));
    }

    [Fact]
    public void Misses_far_apart_never_add_up()
    {
        var limiter = new PinAttemptLimiter(maxAttempts: 5, window: TimeSpan.FromMinutes(5));

        // One wrong PIN a week, forever, is a volunteer with a bad memory.
        for (var week = 0; week < 10; week++)
            limiter.RecordFailure("192.168.1.50", Sunday.AddDays(7 * week));

        Assert.Null(limiter.RetryAfter("192.168.1.50", Sunday.AddDays(70)));
    }

    [Fact]
    public void Getting_the_pin_right_wipes_the_slate()
    {
        var limiter = new PinAttemptLimiter(maxAttempts: 5);

        for (var i = 0; i < 4; i++)
            limiter.RecordFailure("192.168.1.50", Sunday);
        limiter.RecordSuccess("192.168.1.50");

        for (var i = 0; i < 4; i++)
            limiter.RecordFailure("192.168.1.50", Sunday);

        Assert.Null(limiter.RetryAfter("192.168.1.50", Sunday));
    }

    [Fact]
    public void A_page_hammering_the_door_counts_as_one_miss_at_a_time()
    {
        // The streaming page asks three times a second. A stale PIN in OBS
        // used to lock the machine out in a second and a half.
        var limiter = new PinAttemptLimiter(maxAttempts: 5, betweenFailures: TimeSpan.FromSeconds(2));

        for (var i = 0; i < 20; i++)
            limiter.RecordFailure("192.168.1.50", Sunday.AddMilliseconds(i * 300));

        // Six seconds of asking is three counted misses, not twenty.
        Assert.Null(limiter.RetryAfter("192.168.1.50", Sunday.AddSeconds(6)));
    }

    [Fact]
    public void Someone_guessing_patiently_still_gets_locked_out()
    {
        var limiter = new PinAttemptLimiter(maxAttempts: 5, betweenFailures: TimeSpan.FromSeconds(2));

        for (var i = 0; i < 5; i++)
            limiter.RecordFailure("192.168.1.50", Sunday.AddSeconds(i * 3));

        Assert.NotNull(limiter.RetryAfter("192.168.1.50", Sunday.AddSeconds(12)));
    }

    [Fact]
    public void One_phone_locking_itself_out_leaves_the_rest_alone()
    {
        var limiter = new PinAttemptLimiter(maxAttempts: 5);

        Miss(limiter, "192.168.1.50", 5);

        Assert.NotNull(limiter.RetryAfter("192.168.1.50", Sunday.AddSeconds(12)));
        Assert.Null(limiter.RetryAfter("192.168.1.51", Sunday.AddSeconds(12)));
    }

    [Fact]
    public void Restarting_the_remote_lets_a_locked_out_volunteer_back_in()
    {
        var limiter = new PinAttemptLimiter(maxAttempts: 5);

        Miss(limiter, "192.168.1.50", 5);
        Assert.NotNull(limiter.RetryAfter("192.168.1.50", Sunday.AddSeconds(12)));

        limiter.Clear();

        Assert.Null(limiter.RetryAfter("192.168.1.50", Sunday.AddSeconds(12)));
    }

    [Fact]
    public void Forged_addresses_cannot_grow_the_table_without_bound()
    {
        var limiter = new PinAttemptLimiter(maxAttempts: 5, window: TimeSpan.FromMinutes(5));

        // A thousand addresses, each with a single miss, an hour ago.
        for (var i = 0; i < 1000; i++)
            limiter.RecordFailure($"10.0.{i / 256}.{i % 256}", Sunday);

        limiter.RecordFailure("192.168.1.50", Sunday.AddHours(1));

        Assert.True(limiter.TrackedClients < 1000, $"quedaron {limiter.TrackedClients} clientes en la tabla");
        Assert.Null(limiter.RetryAfter("10.0.0.1", Sunday.AddHours(1)));
    }
}
