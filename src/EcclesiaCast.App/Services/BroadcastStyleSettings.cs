using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Remote;

namespace EcclesiaCast.App.Services;

/// <summary>
/// The designs of the three layers of the stream, as set in
/// «Transmisión → Estilos de la transmisión…». Like <see cref="TransitionSettings"/>
/// it lives in one place so the server (which builds what OBS draws) and the
/// editor both see the same values.
/// </summary>
public static class BroadcastStyleSettings
{
    public static BroadcastStyles Current { get; private set; } = BroadcastStyles.Default;

    /// <summary>Raised when a style is saved.</summary>
    public static event EventHandler? Changed;

    /// <summary>Reads the saved designs; the factory ones when nothing was saved.</summary>
    public static void Load(ISettingsStore settings) => Current = BroadcastStyles.Load(settings);

    /// <summary>Applies and saves the design of one layer; OBS picks it up on its next poll.</summary>
    public static void Set(OutputChannel channel, BroadcastStyle style, ISettingsStore settings)
    {
        style = style.Clamp();
        settings.Set(BroadcastStyles.SettingKey(channel), style.ToSetting());
        Current = Current.With(channel, style);
        Changed?.Invoke(null, EventArgs.Empty);
    }
}
