using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using EcclesiaCast.Core.Presentation;
using Serilog;

namespace EcclesiaCast.App.Controls;

/// <summary>
/// The name plate («lower third»): a card at the bottom left with who is
/// speaking, and if it has one, a picture or a short looping video at its left
/// or right. It comes in the way the plate says (slide, rise, open like a
/// blind, or just fade) and fades out. With the «aviso al pie» on, it sits
/// above it instead of under it.
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
    private string? _plateMediaPath;

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
            ShowPlateMedia(plate);
            PlaceNamePlate();

            var wasHidden = NamePlateLayer.Visibility != Visibility.Visible;
            NamePlateLayer.Visibility = Visibility.Visible;
            if (AnimateTransitions && wasHidden)
                AnimatePlateIn(plate.Animation);
            else
                ResetPlateMotion();
            return;
        }

        void Remove()
        {
            if (version != _plateVersion)
                return;

            ResetPlateMotion();
            NamePlateLayer.Visibility = Visibility.Collapsed;
            StopPlateMedia();
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

    private void AnimatePlateIn(NamePlateAnimation animation)
    {
        ResetPlateMotion();
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

        // Every way in fades in too; the blind opens faster than it fades.
        var fadeTime = animation == NamePlateAnimation.Wipe ? TimeSpan.FromMilliseconds(160) : PlateIn;
        NamePlateLayer.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, fadeTime) { EasingFunction = ease });

        switch (animation)
        {
            case NamePlateAnimation.Rise:
                NamePlateShift.BeginAnimation(TranslateTransform.YProperty,
                    new DoubleAnimation(90, 0, PlateIn) { EasingFunction = ease });
                break;
            case NamePlateAnimation.Wipe:
                NamePlateScale.BeginAnimation(ScaleTransform.ScaleXProperty,
                    new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(520)) { EasingFunction = ease });
                break;
            case NamePlateAnimation.Fade:
                break;
            default:
                NamePlateShift.BeginAnimation(TranslateTransform.XProperty,
                    new DoubleAnimation(-120, 0, PlateIn) { EasingFunction = ease });
                break;
        }
    }

    private void ResetPlateMotion()
    {
        NamePlateLayer.BeginAnimation(OpacityProperty, null);
        NamePlateShift.BeginAnimation(TranslateTransform.XProperty, null);
        NamePlateShift.BeginAnimation(TranslateTransform.YProperty, null);
        NamePlateScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        NamePlateLayer.Opacity = 1;
        NamePlateShift.X = 0;
        NamePlateShift.Y = 0;
        NamePlateScale.ScaleX = 1;
    }

    // ── La imagen o el video de la placa ─────────────────────────

    private void ShowPlateMedia(NamePlate plate)
    {
        var path = plate.HasImage && File.Exists(plate.ImagePath) ? plate.ImagePath : null;

        // On the left of the band, or after the words on the right.
        Grid.SetColumn(NamePlateMediaBox, plate.ImageRight ? 3 : 0);
        NamePlateCard.MinHeight = path is null ? 0 : NamePlateMediaBox.Width;
        NamePlateMediaBox.Visibility = path is null ? Visibility.Collapsed : Visibility.Visible;

        if (path == _plateMediaPath)
            return;

        StopPlateMedia();
        _plateMediaPath = path;
        if (path is null)
            return;

        try
        {
            if (NamePlateMedia.IsVideo(path))
            {
                NamePlateVideo.Source = new Uri(path);
                NamePlateVideo.Visibility = Visibility.Visible;
                NamePlateVideo.MediaEnded -= LoopPlateVideo;
                NamePlateVideo.MediaEnded += LoopPlateVideo;
                NamePlateVideo.Play();
            }
            else
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad; // the file is not kept open
                bitmap.DecodePixelHeight = 300;
                bitmap.UriSource = new Uri(path);
                bitmap.EndInit();
                bitmap.Freeze();
                NamePlateImage.Source = bitmap;
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "No se pudo mostrar la imagen de la placa {Path}", path);
            NamePlateMediaBox.Visibility = Visibility.Collapsed;
            NamePlateCard.MinHeight = 0;
        }
    }

    private void LoopPlateVideo(object sender, RoutedEventArgs e)
    {
        NamePlateVideo.Position = TimeSpan.Zero;
        NamePlateVideo.Play();
    }

    private void StopPlateMedia()
    {
        NamePlateVideo.Stop();
        NamePlateVideo.Source = null;
        NamePlateVideo.Visibility = Visibility.Collapsed;
        NamePlateImage.Source = null;
        _plateMediaPath = null;
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
