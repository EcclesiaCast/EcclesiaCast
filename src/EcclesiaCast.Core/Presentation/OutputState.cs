namespace EcclesiaCast.Core.Presentation;

/// <summary>What the projection output is currently showing.</summary>
public enum OutputState
{
    /// <summary>The current slide's text over the background.</summary>
    Content,

    /// <summary>Background only, no text.</summary>
    Clear,

    /// <summary>Full black screen.</summary>
    Black,

    /// <summary>The church logo.</summary>
    Logo,
}
