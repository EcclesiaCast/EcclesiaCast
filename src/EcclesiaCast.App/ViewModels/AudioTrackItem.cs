using CommunityToolkit.Mvvm.ComponentModel;
using EcclesiaCast.Core.Audio;

namespace EcclesiaCast.App.ViewModels;

/// <summary>A row of the audio list: the track and whether it is the one loaded in the player.</summary>
public sealed partial class AudioTrackItem(AudioTrack track) : ObservableObject
{
    public AudioTrack Track { get; } = track;

    public int Id => Track.Id;

    public string Name => Track.Name;

    public string Path => Track.Path;

    /// <summary>A YouTube song played over the internet (not yet saved to this computer).</summary>
    public bool IsYouTube => EcclesiaCast.Core.Media.YouTubeAudio.TrackVideoId(Track.Path) is not null;

    /// <summary>A song heard from the YouTube search without adding it to the list.</summary>
    public bool IsPreview => Track.Id <= 0;

    /// <summary>The track that is playing, paused, or has just finished.</summary>
    [ObservableProperty]
    private bool _isCurrent;
}
