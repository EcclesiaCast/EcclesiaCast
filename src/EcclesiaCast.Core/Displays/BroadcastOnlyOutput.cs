namespace EcclesiaCast.Core.Displays;

/// <summary>
/// The «only the stream» entry of the list of screens: going live with it opens
/// no output window, so the pages for OBS (and the phone and the stage) carry
/// the words while the projector, or the screen the program sits on, stays free.
///
/// It travels as a <see cref="DisplayInfo"/> like any screen so the rest of the
/// program (the proportion of LIVE, the remembered choice) needs no special case;
/// <see cref="IsBroadcastOnly"/> is what tells it apart.
/// </summary>
public static class BroadcastOnlyOutput
{
    /// <summary>Not a Windows device name, so it can never match a real screen.</summary>
    public const string DeviceName = "ecclesiacast:solo-transmision";

    public const string Label = "📡 Sólo la transmisión (sin ventana)";

    /// <summary>
    /// The entry for these screens. It borrows the size of the main one (or a
    /// 16:9 picture when there is none) so LIVE and PREVIEW are drawn in the
    /// proportion a stream normally has.
    /// </summary>
    public static DisplayInfo For(IReadOnlyList<DisplayInfo> displays)
    {
        var reference = displays.FirstOrDefault(d => d.IsPrimary) ?? displays.FirstOrDefault();
        return reference is null
            ? new DisplayInfo(DeviceName, 0, 0, 1920, 1080, IsPrimary: false)
            : reference with { DeviceName = DeviceName, IsPrimary = false };
    }

    public static bool IsBroadcastOnly(DisplayInfo? display) => display?.DeviceName == DeviceName;
}
