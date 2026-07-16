namespace EcclesiaCast.Core.Songs;

/// <summary>One projectable block of a song: a verse, chorus, bridge, etc.</summary>
public sealed class SongSection
{
    public int Id { get; set; }
    public int SongId { get; set; }
    public int Order { get; set; }

    /// <summary>Free label shown to the operator: "Verso 1", "Coro", "Puente"…</summary>
    public string Label { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;
}
