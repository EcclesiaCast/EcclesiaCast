using EcclesiaCast.Core.Abstractions;

namespace EcclesiaCast.Core.Tests.Persistence;

public class OneTimeNoticeTests
{
    private sealed class MemorySettings : ISettingsStore
    {
        public Dictionary<string, string> Values { get; } = [];

        public string? Get(string key) => Values.GetValueOrDefault(key);

        public void Set(string key, string value) => Values[key] = value;
    }

    [Fact]
    public void A_notice_shows_the_first_time_and_never_again()
    {
        var settings = new MemorySettings();

        Assert.True(OneTimeNotice.ShouldShow(settings, "app.about.seen"));
        Assert.False(OneTimeNotice.ShouldShow(settings, "app.about.seen"));
        Assert.False(OneTimeNotice.ShouldShow(settings, "app.about.seen"));
    }

    [Fact]
    public void Each_notice_is_remembered_on_its_own()
    {
        var settings = new MemorySettings();
        OneTimeNotice.ShouldShow(settings, "one");

        Assert.True(OneTimeNotice.ShouldShow(settings, "two"));
    }

    [Fact]
    public void A_notice_already_marked_in_the_settings_does_not_show()
    {
        var settings = new MemorySettings();
        settings.Set("app.about.seen", "1");

        Assert.False(OneTimeNotice.ShouldShow(settings, "app.about.seen"));
    }
}
