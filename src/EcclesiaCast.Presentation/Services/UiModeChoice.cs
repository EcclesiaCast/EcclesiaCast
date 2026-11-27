namespace EcclesiaCast.Presentation.Services;

/// <summary>
/// Which layout the operator has: the learning one (the default: the keys are
/// written on the buttons and the help texts are in view) or the advanced one,
/// with less to read. Only what is written changes; the keys work in both.
/// How each interface hides those texts is its own <paramref name="apply"/>.
/// </summary>
/// <param name="settings">Asked each time, because the window gets them after it is made.</param>
/// <param name="apply">Shows or hides the hints; receives true for the advanced layout.</param>
public sealed class UiModeChoice(Func<MainWindowSettings?> settings, Action<bool> apply)
{
    /// <summary>True while the «avanzado» layout (no shortcut hints) is on.</summary>
    public bool IsAdvanced { get; private set; }

    /// <summary>The choice of the last time, put back as the program opens.</summary>
    public void Restore() => Apply(settings()?.ReadAdvancedUi() == true);

    /// <summary>Changes the layout and remembers the choice for the next service.</summary>
    public void Set(bool advanced)
    {
        Apply(advanced);
        settings()?.WriteAdvancedUi(advanced);
    }

    private void Apply(bool advanced)
    {
        IsAdvanced = advanced;
        apply(advanced);
    }
}
