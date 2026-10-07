namespace EcclesiaCast.App.Services;

/// <summary>
/// Whether «Modo PC de bajo rendimiento» is on. It is read by controls all
/// over the program (the previews, the output window), so it lives in one
/// place they can all see and listen to instead of being passed to each one.
///
/// It only ever lightens what the operator looks at, or drops animation: what
/// the congregation sees on the projector keeps its shadows and outlines.
/// </summary>
public static class PerformanceMode
{
    public static bool IsLow { get; private set; }

    /// <summary>Raised on the UI thread when the mode is switched.</summary>
    public static event EventHandler? Changed;

    public static void Set(bool low)
    {
        if (IsLow == low)
            return;

        IsLow = low;
        Changed?.Invoke(null, EventArgs.Empty);
    }
}
