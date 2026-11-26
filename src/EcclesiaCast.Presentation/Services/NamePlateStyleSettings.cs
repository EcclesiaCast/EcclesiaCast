using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Presentation;

namespace EcclesiaCast.Presentation.Services;

/// <summary>
/// The church's name plate styles, shared by the «Placas» tab (which chooses
/// one) and the designer (which changes them). Kept in the settings.
/// </summary>
public static class NamePlateStyleSettings
{
    private static ISettingsStore? s_settings;

    public static IReadOnlyList<NamePlateStyle> Current { get; private set; } = NamePlateStyle.Presets;

    /// <summary>Raised on the UI thread when the styles change.</summary>
    public static event EventHandler? Changed;

    public static void Load(ISettingsStore settings)
    {
        s_settings = settings;
        Current = NamePlateStyleList.Parse(settings.Get(NamePlateStyleList.SettingKey));
        Changed?.Invoke(null, EventArgs.Empty);
    }

    public static void Set(IReadOnlyList<NamePlateStyle> styles)
    {
        Current = NamePlateStyleList.Parse(NamePlateStyleList.ToSetting(styles));
        s_settings?.Set(NamePlateStyleList.SettingKey, NamePlateStyleList.ToSetting(Current));
        Changed?.Invoke(null, EventArgs.Empty);
    }

    public static NamePlateStyle Find(string? name) => NamePlateStyleList.Find(Current, name);
}
