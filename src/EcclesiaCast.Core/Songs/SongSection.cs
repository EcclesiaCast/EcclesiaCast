using System.Text.Json;
using EcclesiaCast.Core.Presentation;

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

    /// <summary>Serialized <see cref="SlideOverride"/>; null when the slide uses the theme as-is.</summary>
    public string? StyleJson { get; set; }

    public SlideOverride? GetOverride()
    {
        if (string.IsNullOrEmpty(StyleJson))
            return null;
        try
        {
            return JsonSerializer.Deserialize<SlideOverride>(StyleJson);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public void SetOverride(SlideOverride? value) =>
        StyleJson = value is null || value.IsEmpty ? null : JsonSerializer.Serialize(value);
}
