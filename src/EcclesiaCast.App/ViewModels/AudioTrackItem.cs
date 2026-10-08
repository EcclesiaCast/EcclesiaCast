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

    /// <summary>The track that is playing, paused, or has just finished.</summary>
    [ObservableProperty]
    private bool _isCurrent;
}
