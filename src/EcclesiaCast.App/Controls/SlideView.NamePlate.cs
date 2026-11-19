using System.Windows;
using System.Windows.Media.Animation;
using EcclesiaCast.Core.Presentation;

namespace EcclesiaCast.App.Controls;

/// <summary>
/// The name plate («lower third»): a card at the bottom left with who is
/// speaking. It slides in from the left while it fades in, and fades out.
/// With the «aviso al pie» on, it sits above it instead of under it.
/// </summary>
public partial class SlideView
{
    public static readonly DependencyProperty NamePlateProperty =
        DependencyProperty.Register(nameof(NamePlate), typeof(NamePlate), typeof(SlideView),
            new PropertyMetadata(null, (d, _) => ((SlideView)d).OnNamePlateChanged()));

    public NamePlate? NamePlate
    {
        get => (NamePlate?)GetValue(NamePlateProperty);
        set => SetValue(NamePlateProperty, value);
    }

    private static readonly TimeSpan PlateIn = TimeSpan.FromMilliseconds(420);
    private static readonly TimeSpan PlateOut = TimeSpan.FromMilliseconds(320);
    private int _plateVersion;

    private void OnNamePlateChanged()
    {
        var plate = NamePlate;
        var version = ++_plateVersion;

        if (plate is { IsEmpty: false })
        {
            NamePlateName.Text = plate.Name.Trim();
            NamePlateName.Visibility = string.IsNullOrWhiteSpace(plate.Name) ? Visibility.Collapsed : Visibility.Visible;
            NamePlateRole.Text = plate.Role.Trim();
            NamePlateRole.Visibility = string.IsNullOrWhiteSpace(plate.Role) ? Visibility.Collapsed : Visibility.Visible;
            PlaceNamePlate();

            var wasHidden = NamePlateLayer.Visibility != Visibility.Visible;
            NamePlateLayer.Visibility = Visibility.Visible;
            if (AnimateTransitions && wasHidden)
            {
                var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
                NamePlateLayer.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, PlateIn) { EasingFunction = ease });
                NamePlateShift.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty,
                    new DoubleAnimation(-120, 0, PlateIn) { EasingFunction = ease });
            }
            else
            {
                NamePlateLayer.BeginAnimation(OpacityProperty, null);
                NamePlateShift.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, null);
                NamePlateLayer.Opacity = 1;
                NamePlateShift.X = 0;
            }
            return;
        }

        void Remove()
        {
            if (version != _plateVersion)
                return;

            NamePlateLayer.BeginAnimation(OpacityProperty, null);
            NamePlateLayer.Opacity = 1;
            NamePlateLayer.Visibility = Visibility.Collapsed;
        }

        if (AnimateTransitions && NamePlateLayer.Visibility == Visibility.Visible)
        {
            var fade = new DoubleAnimation(NamePlateLayer.Opacity, 0, PlateOut) { EasingFunction = new QuadraticEase() };
            fade.Completed += (_, _) => Remove();
            NamePlateLayer.BeginAnimation(OpacityProperty, fade);
        }
        else
        {
            Remove();
        }
    }

    /// <summary>Above the «aviso al pie» when it is on, so neither covers the other.</summary>
    private void PlaceNamePlate()
    {
        var bottom = 110.0;
        if (OverlayLayer.Visibility == Visibility.Visible)
        {
            OverlayLayer.UpdateLayout();
            bottom = Math.Max(bottom, OverlayLayer.ActualHeight + 40);
        }

        NamePlateLayer.Margin = new Thickness(90, 0, 0, bottom);
    }
}
