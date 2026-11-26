namespace EcclesiaCast.Presentation.Platform;

/// <summary>
/// The volume the system keeps for the whole program, where it has one (on
/// Windows, the program's audio session). It carries the general «Volumen»
/// bar. Where there is none, each player carries that bar in its own volume.
/// </summary>
public interface IProgramVolume
{
    /// <summary>Gives the program that volume (0 to 1) and lifts a mute left on it.</summary>
    void Apply(float volume);
}
