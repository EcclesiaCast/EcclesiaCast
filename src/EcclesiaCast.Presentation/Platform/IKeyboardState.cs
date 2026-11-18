namespace EcclesiaCast.Presentation.Platform;

/// <summary>What the operator's keyboard is doing right now, for the clicks that mean something else with a key held.</summary>
public interface IKeyboardState
{
    /// <summary>True while Ctrl is held down (Cmd on a Mac).</summary>
    bool IsControlDown { get; }
}
