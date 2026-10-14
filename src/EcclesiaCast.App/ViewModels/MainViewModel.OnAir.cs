using CommunityToolkit.Mvvm.ComponentModel;
using EcclesiaCast.Core.Presentation;

namespace EcclesiaCast.App.ViewModels;

/// <summary>
/// What is on the air, derived from the real state of the output (see
/// <see cref="OnAirResolver"/>), and the red marks that follow it: the grid
/// when it holds what is on screen, the media bar when a media is, the
/// Canciones and Biblia tabs, and the card that is live.
/// </summary>
public sealed partial class MainViewModel
{
    /// <summary>Where the words that are live came from; read when the state is worked out.</summary>
    private SlideOrigin _liveSlideOrigin;

    /// <summary>
    /// The title of the words that were last put up. A media on the whole
    /// screen takes <see cref="LiveTitle"/> for itself; when it goes and the
    /// words are back, this is what the title goes back to.
    /// </summary>
    private string? _textLiveTitle;

    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(IsSongsOnAir), nameof(IsBibleOnAir), nameof(IsMediaOnAir), nameof(IsGridOnAir), nameof(ShowGridLiveBadge))]
    private OnAirSource _onAir;

    /// <summary>The media on screen (or the page of a presentation), 0 when none: its card in the bar is marked.</summary>
    [ObservableProperty]
    private int _liveMediaId;

    public bool IsSongsOnAir => OnAir == OnAirSource.Song;

    public bool IsBibleOnAir => OnAir == OnAirSource.Bible;

    public bool IsMediaOnAir => OnAir is OnAirSource.Media or OnAirSource.Web;

    /// <summary>
    /// The grid holds what is on screen: its verses, its song or its pages.
    /// Browsing another song while one is live does not mark the grid.
    /// </summary>
    public bool IsGridOnAir => OnAir is OnAirSource.Song or OnAirSource.Bible or OnAirSource.Presentation && IsGridLive;

    partial void OnIsProjectingChanged(bool value) => UpdateOnAir();

    partial void OnGridTitleChanged(string? value) => RefreshSlideOnAir();

    partial void OnLiveTitleChanged(string? value) => RefreshSlideOnAir();

    private void UpdateOnAir()
    {
        OnAir = OnAirResolver.Resolve(
            IsProjecting, _presentation.State, _liveSlideOrigin, _presentation.Background, IsPresentationPageOnAir);
        LiveMediaId = OnAir is OnAirSource.Media or OnAirSource.Presentation ? _presentation.Background?.Id ?? 0 : 0;

        // The words are back on the screen after a media took the title.
        if (OnAir is OnAirSource.Song or OnAirSource.Bible or OnAirSource.QuickText
            && _textLiveTitle is not null && LiveTitle != _textLiveTitle)
            LiveTitle = _textLiveTitle;

        RefreshSlideOnAir();
    }

    /// <summary>The card that was projected is red only while it is really on the air; otherwise it stays dim («último proyectado»).</summary>
    private void RefreshSlideOnAir()
    {
        var onAir = IsGridOnAir;
        foreach (var slide in Slides)
            slide.IsOnAir = slide.IsLive && onAir;
    }
}
