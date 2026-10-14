using CommunityToolkit.Mvvm.ComponentModel;
using EcclesiaCast.Core.Presentation;

namespace EcclesiaCast.App.ViewModels;

/// <summary>
/// How big the cards of the slide grid and of the media bar are. The slider
/// and Ctrl + mouse wheel change the scale; the sizes below are what the
/// templates bind to. The text and the labels scale with the card, so a small
/// card still reads as one piece.
/// </summary>
public sealed partial class MainViewModel
{
    private const string SlidesZoomKey = "view.slides.zoom";
    private const string MediaZoomKey = "view.media.zoom";

    [ObservableProperty]
    private double _slideCardScale = CardZoom.Default;

    [ObservableProperty]
    private double _mediaCardScale = CardZoom.Default;

    public double SlideCardWidth => 220 * SlideCardScale;

    public double SlideCardHeight => 118 * SlideCardScale;

    public double SlideCardFontSize => 12 * SlideCardScale;

    public double SlideLabelFontSize => Math.Max(9, 10 * SlideCardScale);

    public double MediaCardWidth => 98 * MediaCardScale;

    public double MediaCardHeight => 56 * MediaCardScale;

    public double MediaNameFontSize => Math.Max(8, 9 * MediaCardScale);

    partial void OnSlideCardScaleChanged(double value)
    {
        OnPropertyChanged(nameof(SlideCardWidth));
        OnPropertyChanged(nameof(SlideCardHeight));
        OnPropertyChanged(nameof(SlideCardFontSize));
        OnPropertyChanged(nameof(SlideLabelFontSize));
        QueueCardZoomSave();
    }

    partial void OnMediaCardScaleChanged(double value)
    {
        OnPropertyChanged(nameof(MediaCardWidth));
        OnPropertyChanged(nameof(MediaCardHeight));
        OnPropertyChanged(nameof(MediaNameFontSize));
        QueueCardZoomSave();
    }

    /// <summary>
    /// Saves a moment after the operator lets go of the slider. Every save
    /// opens the database, so one write per slider step would be too many.
    /// </summary>
    private readonly System.Windows.Threading.DispatcherTimer _cardZoomSaveTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(400),
    };

    /// <summary>Off while the saved sizes are read, so starting up writes nothing.</summary>
    private bool _cardZoomReady;

    private void InitializeCardZoom()
    {
        _cardZoomSaveTimer.Tick += (_, _) =>
        {
            _cardZoomSaveTimer.Stop();
            _settings.Set(SlidesZoomKey, CardZoom.Format(SlideCardScale));
            _settings.Set(MediaZoomKey, CardZoom.Format(MediaCardScale));
        };

        SlideCardScale = CardZoom.Parse(_settings.Get(SlidesZoomKey), CardZoom.SlidesMin, CardZoom.SlidesMax);
        MediaCardScale = CardZoom.Parse(_settings.Get(MediaZoomKey), CardZoom.MediaMin, CardZoom.MediaMax);
        _cardZoomReady = true;
    }

    private void QueueCardZoomSave()
    {
        if (!_cardZoomReady)
            return;

        _cardZoomSaveTimer.Stop();
        _cardZoomSaveTimer.Start();
    }
}
