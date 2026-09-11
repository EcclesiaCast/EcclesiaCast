using EcclesiaCast.Core.Playlists;

namespace EcclesiaCast.App.Services;

/// <summary>What the operator set up for a self-filling list.</summary>
public sealed record SmartPlaylistChoice(string Name, PlaylistRule Rule, string Value);

/// <summary>Creating or editing a list that fills itself from the library.</summary>
public interface ISmartPlaylistDialog
{
    /// <summary>Returns null when the operator cancelled.</summary>
    SmartPlaylistChoice? Show(string name, PlaylistRule rule, string? value);
}
