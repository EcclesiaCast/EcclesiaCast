using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Presentation.Services;

namespace EcclesiaCast.Presentation.Tests;

public sealed class UiModeChoiceTests
{
    private sealed class MemorySettings : ISettingsStore
    {
        private readonly Dictionary<string, string> _values = [];

        public string? Get(string key) => _values.GetValueOrDefault(key);

        public void Set(string key, string value) => _values[key] = value;
    }

    [Fact]
    public void The_choice_is_applied_remembered_and_put_back()
    {
        var settings = new MainWindowSettings(new MemorySettings());
        var applied = new List<bool>();
        var first = new UiModeChoice(() => settings, applied.Add);
        first.Restore();
        Assert.False(first.IsAdvanced);

        first.Set(true);

        var next = new UiModeChoice(() => settings, applied.Add);
        next.Restore();
        Assert.True(next.IsAdvanced);
        Assert.Equal([false, true, true], applied);
    }

    [Fact]
    public void Without_saved_settings_it_still_applies()
    {
        var choice = new UiModeChoice(() => null, _ => { });

        choice.Set(true);

        Assert.True(choice.IsAdvanced);
    }
}
