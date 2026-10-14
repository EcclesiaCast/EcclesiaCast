using System.Windows;

namespace EcclesiaCast.App.Controls;

/// <summary>
/// Whether a card of the media bar is picked. The media items are plain
/// library objects that know nothing of the screen, so the window flags the
/// card itself and the template draws the frame from this.
/// </summary>
public static class MediaCardState
{
    public static readonly DependencyProperty IsSelectedProperty =
        DependencyProperty.RegisterAttached("IsSelected", typeof(bool), typeof(MediaCardState),
            new PropertyMetadata(false));

    public static bool GetIsSelected(DependencyObject element) => (bool)element.GetValue(IsSelectedProperty);

    public static void SetIsSelected(DependencyObject element, bool value) => element.SetValue(IsSelectedProperty, value);

    /// <summary>The card is the media that is on the screen right now.</summary>
    public static readonly DependencyProperty IsLiveProperty =
        DependencyProperty.RegisterAttached("IsLive", typeof(bool), typeof(MediaCardState),
            new PropertyMetadata(false));

    public static bool GetIsLive(DependencyObject element) => (bool)element.GetValue(IsLiveProperty);

    public static void SetIsLive(DependencyObject element, bool value) => element.SetValue(IsLiveProperty, value);
}
