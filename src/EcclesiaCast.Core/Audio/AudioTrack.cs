namespace EcclesiaCast.Core.Audio;

/// <summary>
/// One sound file in the list of the Música tab (a song for the offering, the
/// music before the service). Only the path is kept: the file stays where
/// the operator has it, like every other media of the program.
/// </summary>
public sealed class AudioTrack
{
    public int Id { get; set; }

    /// <summary>What the list shows: the file name without its extension.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Absolute path to the file.</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>Place in the list, from 0. The operator moves tracks around.</summary>
    public int Order { get; set; }
}
