using EcclesiaCast.Core.Abstractions;

namespace EcclesiaCast.App.Services;

/// <summary>What the stage display shows, remembered between services.</summary>
public sealed class StageOptions
{
    public bool ShowClock { get; set; } = true;

    /// <summary>Time since the service timer was last reset.</summary>
    public bool ShowTimer { get; set; } = true;

    /// <summary>The slide after the live one, so the singers can prepare.</summary>
    public bool ShowNext { get; set; } = true;

    /// <summary>Size of the live text, in points over the stage screen.</summary>
    public double TextScale { get; set; } = 96;

    private const string ClockKey = "stage.clock";
    private const string TimerKey = "stage.timer";
    private const string NextKey = "stage.next";
    private const string ScaleKey = "stage.scale";

    public static StageOptions Load(ISettingsStore settings) => new()
    {
        ShowClock = settings.Get(ClockKey) != "0",
        ShowTimer = settings.Get(TimerKey) != "0",
        ShowNext = settings.Get(NextKey) != "0",
        TextScale = double.TryParse(settings.Get(ScaleKey), out var scale) && scale > 0 ? scale : 96,
    };

    public void Save(ISettingsStore settings)
    {
        settings.Set(ClockKey, ShowClock ? "1" : "0");
        settings.Set(TimerKey, ShowTimer ? "1" : "0");
        settings.Set(NextKey, ShowNext ? "1" : "0");
        settings.Set(ScaleKey, TextScale.ToString("0"));
    }
}
