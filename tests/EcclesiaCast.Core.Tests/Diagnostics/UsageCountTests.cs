using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Diagnostics;

namespace EcclesiaCast.Core.Tests.Diagnostics;

public class UsageCountTests
{
    private sealed class Settings : ISettingsStore
    {
        public Dictionary<string, string> Values { get; } = [];
        public string? Get(string key) => Values.GetValueOrDefault(key);
        public void Set(string key, string value) => Values[key] = value;
    }

    private static readonly DateTimeOffset Sunday = new(2026, 11, 15, 10, 30, 0, TimeSpan.FromHours(-3));

    [Fact]
    public void It_is_on_unless_turned_off()
    {
        var settings = new Settings();
        Assert.True(UsageCount.IsEnabled(settings));

        UsageCount.SetEnabled(settings, false);
        Assert.False(UsageCount.IsEnabled(settings));
        Assert.False(UsageCount.ShouldSend(settings, Sunday, isDevelopmentBuild: false));
    }

    [Fact]
    public void The_church_name_is_optional_cleaned_and_told_again_when_it_changes()
    {
        var settings = new Settings();
        Assert.Equal("", UsageCount.Ping(settings, Sunday, "1.4.0").Church);

        UsageCount.MarkSent(settings, Sunday);
        UsageCount.SetChurchName(settings, "  Iglesia   del Centro ");
        Assert.Equal("Iglesia del Centro", UsageCount.Ping(settings, Sunday, "1.4.0").Church);
        // A new name asks for today to be told again.
        Assert.True(UsageCount.ShouldSend(settings, Sunday, isDevelopmentBuild: false));

        UsageCount.SetChurchName(settings, new string('a', 200));
        Assert.Equal(UsageCount.MaxChurchLength, UsageCount.ChurchName(settings).Length);
    }

    [Fact]
    public void Turning_the_count_off_forgets_the_church_name()
    {
        var settings = new Settings();
        UsageCount.SetChurchName(settings, "Iglesia");
        UsageCount.Forget(settings);
        Assert.Equal("", UsageCount.ChurchName(settings));
    }

    [Fact]
    public void A_development_build_never_counts()
    {
        Assert.False(UsageCount.ShouldSend(new Settings(), Sunday, isDevelopmentBuild: true));
    }

    [Fact]
    public void Once_a_day_and_again_the_next_day()
    {
        var settings = new Settings();
        Assert.True(UsageCount.ShouldSend(settings, Sunday, false));

        UsageCount.MarkSent(settings, Sunday);
        Assert.False(UsageCount.ShouldSend(settings, Sunday.AddHours(8), false));
        Assert.True(UsageCount.ShouldSend(settings, Sunday.AddDays(1), false));
    }

    [Fact]
    public void The_day_is_the_one_this_PC_sees()
    {
        // 23:30 in Montevideo is already Monday in UTC: it still counts as Sunday.
        var lateSunday = new DateTimeOffset(2026, 11, 15, 23, 30, 0, TimeSpan.FromHours(-3));

        Assert.Equal("2026-11-15", UsageCount.Ping(new Settings(), lateSunday, "1.4.0").Day);
    }

    [Fact]
    public void The_installation_number_is_drawn_once_and_kept()
    {
        var settings = new Settings();
        var first = UsageCount.Ping(settings, Sunday, "1.4.0");
        var second = UsageCount.Ping(settings, Sunday.AddDays(7), "1.4.1");

        Assert.True(UsageCount.IsInstallNumber(first.Install));
        Assert.Equal(first.Install, second.Install);
        Assert.Equal("1.4.1", second.Version);
    }

    [Fact]
    public void Forgetting_draws_a_new_number_that_ties_to_nothing()
    {
        var settings = new Settings();
        var before = UsageCount.Ping(settings, Sunday, "1.4.0").Install;
        UsageCount.MarkSent(settings, Sunday);

        UsageCount.Forget(settings);

        Assert.True(UsageCount.ShouldSend(settings, Sunday, false));
        Assert.NotEqual(before, UsageCount.Ping(settings, Sunday, "1.4.0").Install);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("hola", false)]
    [InlineData("0123456789abcdef0123456789abcdeZ", false)]
    [InlineData("0123456789abcdef0123456789abcdef", true)]
    public void Only_32_hexadecimal_characters_are_a_number(string? value, bool expected)
    {
        Assert.Equal(expected, UsageCount.IsInstallNumber(value));
    }
}
