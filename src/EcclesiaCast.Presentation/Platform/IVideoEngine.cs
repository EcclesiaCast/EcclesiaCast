using LibVLCSharp.Shared;

namespace EcclesiaCast.Presentation.Platform;

/// <summary>The one video engine (VLC) every player of the program shares.</summary>
public interface IVideoEngine
{
    /// <summary>Null when VLC could not start: the videos stay off and the rest works.</summary>
    LibVLC? Instance { get; }
}

/// <summary>Holds the engine made at start-up.</summary>
public sealed class VideoEngine(LibVLC? instance) : IVideoEngine
{
    public LibVLC? Instance { get; } = instance;
}
