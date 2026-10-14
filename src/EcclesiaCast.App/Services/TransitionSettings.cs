using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Presentation;

namespace EcclesiaCast.App.Services;

/// <summary>
/// How long the logo, the medium changes, the black screen and switching the
/// output off take, as set in «Proyección → Transiciones…». Like
/// <see cref="PerformanceMode"/> it is read by controls all over the program
/// (the output window, the previews), so it lives in one place they can all
/// see and listen to.
/// </summary>
public static class TransitionSettings
{
    public static OutputTransitions Current { get; private set; } = OutputTransitions.Default;

    /// <summary>Raised on the UI thread when the durations are changed.</summary>
    public static event EventHandler? Changed;

    /// <summary>Reads the saved durations; the factory ones when nothing was saved.</summary>
    public static void Load(ISettingsStore settings) =>
        Current = OutputTransitions.Parse(settings.Get(OutputTransitions.SettingKey));

    /// <summary>Applies and saves new durations.</summary>
    public static void Set(OutputTransitions value, ISettingsStore settings)
    {
        value = value.Clamp();
        settings.Set(OutputTransitions.SettingKey, value.ToSetting());
        if (value == Current)
            return;

        Current = value;
        Changed?.Invoke(null, EventArgs.Empty);
    }
}
