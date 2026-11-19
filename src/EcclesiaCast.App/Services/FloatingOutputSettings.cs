using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Displays;

namespace EcclesiaCast.App.Services;

/// <summary>
/// How the floating output window was left: its shape, size, place and
/// «siempre visible». Shared by the window (which changes it) and the operator
/// panel (whose LIVE and PREVIEW follow the shape).
/// </summary>
public static class FloatingOutputSettings
{
    private static ISettingsStore? s_settings;

    public static FloatingOutput.Layout Current { get; private set; } = FloatingOutput.Layout.Default;

    /// <summary>Raised on the UI thread when the shape changes (not when the window only moves).</summary>
    public static event EventHandler? ShapeChanged;

    public static void Load(ISettingsStore settings)
    {
        s_settings = settings;
        var before = Current.Shape;
        Current = FloatingOutput.Layout.Parse(settings.Get(FloatingOutput.SettingKey));
        if (Current.Shape != before)
            ShapeChanged?.Invoke(null, EventArgs.Empty);
    }

    public static void Set(FloatingOutput.Layout layout)
    {
        var shapeChanged = layout.Shape != Current.Shape;
        Current = layout;
        s_settings?.Set(FloatingOutput.SettingKey, layout.ToSetting());
        if (shapeChanged)
            ShapeChanged?.Invoke(null, EventArgs.Empty);
    }
}
