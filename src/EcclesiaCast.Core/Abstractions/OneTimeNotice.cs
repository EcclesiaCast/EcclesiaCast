namespace EcclesiaCast.Core.Abstractions;

/// <summary>
/// Something that is shown once and never again (the "About" window the very
/// first time the program opens). The memory of having shown it lives in the
/// settings, so it survives restarts.
/// </summary>
public static class OneTimeNotice
{
    /// <summary>
    /// True the first time it is asked for a key and false from then on. It
    /// marks the notice as shown right away: a program that is closed before
    /// the window is dismissed must not nag again on the next start.
    /// </summary>
    public static bool ShouldShow(ISettingsStore settings, string key)
    {
        if (settings.Get(key) == "1")
            return false;

        settings.Set(key, "1");
        return true;
    }
}
