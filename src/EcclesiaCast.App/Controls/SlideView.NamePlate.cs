using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using EcclesiaCast.Core.Presentation;
using Serilog;

namespace EcclesiaCast.App.Controls;

/// <summary>
/// The name plate («lower third»): a card with who is speaking and, if it has
/// one, a picture or a short looping video beside it. How it looks, where it
/// sits and how it comes and goes all come from its style
/// (<see cref="NamePlateStyle"/>). With the «aviso al pie» on, it sits above it.
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

    private const double MediaHeight = 148;
    private int _plateVersion;
    private string? _plateMediaPath;

    /// <summary>The style the card on the screen was drawn with: it also leaves the way that style says.</summary>
    private NamePlateStyle _plateStyle = NamePlateStyle.Classic;

    private void OnNamePlateChanged()
    {
        var plate = NamePlate;
        var version = ++_plateVersion;

        if (plate is { IsEmpty: false })
        {
            var style = plate.Look.Clamped();
            var restyled = style != _plateStyle;
            _plateStyle = style;
            ApplyPlateStyle(plate, style);
            ShowPlateMedia(plate, style);
            PlaceNamePlate();

            var wasHidden = NamePlateLayer.Visibility != Visibility.Visible;
            NamePlateLayer.Visibility = Visibility.Visible;
            if (AnimateTransitions && wasHidden && style.EnterMs > 0)
                AnimatePlateIn(style);
            else if (wasHidden || restyled)
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

        if (AnimateTransitions && NamePlateLayer.Visibility == Visibility.Visible && _plateStyle.ExitMs > 0)
            AnimatePlateOut(_plateStyle, Remove);
        else
            Remove();
    }

    // ── El aspecto ───────────────────────────────────────────────

    private static Color ColorOf(string hex, Color fallback)
    {
        try
        {
            return (Color)ColorConverter.ConvertFromString(hex);
        }
        catch (FormatException)
        {
            return fallback;
        }
    }

    private void ApplyPlateStyle(NamePlate plate, NamePlateStyle style)
    {
        var box = ColorOf(style.BackgroundColor, Color.FromRgb(0x10, 0x14, 0x20));
        box.A = (byte)Math.Round(style.BackgroundOpacity * 2.55);
        var boxBrush = new SolidColorBrush(box);
        var accent = new SolidColorBrush(ColorOf(style.AccentColor, Color.FromRgb(0x2F, 0x63, 0xC9)));

        // The card.
        NamePlateCard.Background = boxBrush;
        NamePlateCard.CornerRadius = new CornerRadius(style.CornerRadius);
        NamePlateCard.Width = style.Width > 0 ? style.Width : double.NaN;
        NamePlateCard.Height = style.Height > 0 ? style.Height : double.NaN;
        NamePlateMediaBox.Background = boxBrush;
        NamePlateMediaBox.CornerRadius = new CornerRadius(style.CornerRadius);

        // The coloured band: a bar on the left, or a line along the top or the bottom.
        NamePlateBar.Fill = accent;
        NamePlateBar.Width = style.AccentThickness;
        NamePlateBar.Visibility = style.Accent == NamePlateAccent.Left && style.AccentThickness > 0
            ? Visibility.Visible : Visibility.Collapsed;
        NamePlateCard.BorderBrush = accent;
        NamePlateCard.BorderThickness = style.Accent switch
        {
            NamePlateAccent.Top => new Thickness(0, style.AccentThickness, 0, 0),
            NamePlateAccent.Bottom => new Thickness(0, 0, 0, style.AccentThickness),
            _ => new Thickness(0),
        };

        // The words.
        var font = new FontFamily(string.IsNullOrWhiteSpace(style.FontFamily) ? "Segoe UI" : style.FontFamily);
        var name = plate.Name.Trim();
        var role = plate.Role.Trim();
        NamePlateName.Text = style.Uppercase ? name.ToUpperInvariant() : name;
        NamePlateRole.Text = style.Uppercase ? role.ToUpperInvariant() : role;
        NamePlateNameBox.Visibility = name.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
        NamePlateRole.Visibility = role.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
        NamePlateName.FontFamily = NamePlateRole.FontFamily = font;
        NamePlateName.FontSize = style.NameSize;
        NamePlateRole.FontSize = style.RoleSize;
        NamePlateName.FontWeight = style.NameBold ? FontWeights.SemiBold : FontWeights.Normal;
        NamePlateName.Foreground = new SolidColorBrush(ColorOf(style.NameColor, Colors.White));
        NamePlateRole.Foreground = new SolidColorBrush(ColorOf(style.RoleColor, Color.FromRgb(0xC9, 0xD3, 0xE6)));
        NamePlateText.Effect = style.TextShadow
            ? new DropShadowEffect { BlurRadius = 22, ShadowDepth = 2, Opacity = 0.85, Color = Colors.Black }
            : null;

        // «Noticias»: the name on a block of the band's colour, the role on the box below.
        if (style.NameOnAccent)
        {
            NamePlateCard.Padding = new Thickness(0);
            NamePlateNameBox.Background = accent;
            NamePlateNameBox.Padding = new Thickness(38, 12, 46, 14);
            NamePlateRole.Margin = new Thickness(38, 12, 46, 18);
        }
        else
        {
            NamePlateCard.Padding = new Thickness(38, 22, 52, 24);
            NamePlateNameBox.Background = null;
            NamePlateNameBox.Padding = new Thickness(0);
            NamePlateRole.Margin = new Thickness(2, 4, 0, 0);
        }

        NamePlateLayer.HorizontalAlignment = style.Align switch
        {
            NamePlateAlign.Center => HorizontalAlignment.Center,
            NamePlateAlign.Right => HorizontalAlignment.Right,
            _ => HorizontalAlignment.Left,
        };
        NamePlateText.HorizontalAlignment = style.Align == NamePlateAlign.Center
            ? HorizontalAlignment.Center : HorizontalAlignment.Left;
    }

    // ── El movimiento ────────────────────────────────────────────

    /// <summary>Which way «the side» is: towards the edge the card sits on.</summary>
    private double SideSign => _plateStyle.Align == NamePlateAlign.Right ? 1 : -1;

    private void AnimatePlateIn(NamePlateStyle style)
    {
        ResetPlateMotion();
        var time = TimeSpan.FromMilliseconds(style.EnterMs);
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

        // Every way in fades in too; the blind opens faster than it fades.
        var fadeTime = style.Enter == NamePlateAnimation.Wipe ? TimeSpan.FromMilliseconds(Math.Min(160, style.EnterMs)) : time;
        NamePlateLayer.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, fadeTime) { EasingFunction = ease });

        switch (style.Enter)
        {
            case NamePlateAnimation.Rise:
                NamePlateShift.BeginAnimation(TranslateTransform.YProperty,
                    new DoubleAnimation(90, 0, time) { EasingFunction = ease });
                break;
            case NamePlateAnimation.Wipe:
                Blind(from: 0, to: 2000, time, removeWhenDone: true, done: null);
                break;
            case NamePlateAnimation.Fade:
                break;
            default:
                NamePlateShift.BeginAnimation(TranslateTransform.XProperty,
                    new DoubleAnimation(120 * SideSign, 0, time) { EasingFunction = ease });
                break;
        }
    }

    private void AnimatePlateOut(NamePlateStyle style, Action done)
    {
        var time = TimeSpan.FromMilliseconds(style.ExitMs);
        var ease = new QuadraticEase { EasingMode = EasingMode.EaseIn };

        if (style.Exit == NamePlateExit.Wipe)
        {
            // Closes from its right edge; it is gone when the window is.
            var width = Math.Max(NamePlateLayer.ActualWidth, 1);
            Blind(from: width, to: 0, time, removeWhenDone: false, done);
            return;
        }

        var fade = new DoubleAnimation(NamePlateLayer.Opacity, 0, time) { EasingFunction = ease };
        fade.Completed += (_, _) => done();
        NamePlateLayer.BeginAnimation(OpacityProperty, fade);

        if (style.Exit == NamePlateExit.Slide)
            NamePlateShift.BeginAnimation(TranslateTransform.XProperty,
                new DoubleAnimation(0, 120 * SideSign, time) { EasingFunction = ease });
        else if (style.Exit == NamePlateExit.Drop)
            NamePlateShift.BeginAnimation(TranslateTransform.YProperty,
                new DoubleAnimation(0, 90, time) { EasingFunction = ease });
    }

    /// <summary>
    /// A window over the card that widens (in) or narrows (out) from its left
    /// edge: the card is uncovered or covered like a blind, never squeezed.
    /// </summary>
    private void Blind(double from, double to, TimeSpan time, bool removeWhenDone, Action? done)
    {
        var window = new RectangleGeometry(new Rect(0, 0, from, 2000));
        NamePlateLayer.Clip = window;
        var move = new RectAnimation(new Rect(0, 0, from, 2000), new Rect(0, 0, to, 2000), time)
        {
            EasingFunction = new QuadraticEase { EasingMode = from < to ? EasingMode.EaseIn : EasingMode.EaseOut },
        };
        move.Completed += (_, _) =>
        {
            if (removeWhenDone && ReferenceEquals(NamePlateLayer.Clip, window))
                NamePlateLayer.Clip = null;
            done?.Invoke();
        };
        window.BeginAnimation(RectangleGeometry.RectProperty, move);
    }

    private void ResetPlateMotion()
    {
        NamePlateLayer.BeginAnimation(OpacityProperty, null);
        NamePlateShift.BeginAnimation(TranslateTransform.XProperty, null);
        NamePlateShift.BeginAnimation(TranslateTransform.YProperty, null);
        NamePlateScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        NamePlateLayer.Clip = null;
        NamePlateLayer.Opacity = 1;
        NamePlateShift.X = 0;
        NamePlateShift.Y = 0;
        NamePlateScale.ScaleX = 1;
    }

    // ── La imagen o el video de la placa ─────────────────────────

    private void ShowPlateMedia(NamePlate plate, NamePlateStyle style)
    {
        var path = plate.HasImage && File.Exists(plate.ImagePath) ? plate.ImagePath : null;

        // On the outer side of the band, or after the words on the right.
        Grid.SetColumn(NamePlateMediaBox, plate.ImageRight ? 3 : 0);
        NamePlateCard.MinHeight = path is null ? 0 : MediaHeight + 28; // the picture and its frame
        NamePlateMediaBox.Visibility = path is null ? Visibility.Collapsed : Visibility.Visible;
        NamePlateMediaBox.Margin = path is null || style.BackgroundOpacity > 0
            ? new Thickness(0)
            : plate.ImageRight ? new Thickness(18, 0, 0, 0) : new Thickness(0, 0, 18, 0);

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

    private bool _plateFollowsOverlay;

    /// <summary>
    /// Where the style puts it, and above the «aviso al pie» when that is on, so
    /// neither covers the other. The notice may not have its size yet when it
    /// appears, so the plate also moves whenever the notice changes size.
    /// </summary>
    private void PlaceNamePlate()
    {
        if (!_plateFollowsOverlay)
        {
            _plateFollowsOverlay = true;
            OverlayLayer.SizeChanged += (_, _) =>
            {
                if (NamePlateLayer.Visibility == Visibility.Visible)
                    PlaceNamePlate();
            };
        }

        var bottom = (double)_plateStyle.MarginBottom;
        if (OverlayLayer.Visibility == Visibility.Visible && OverlayLayer.ActualHeight > 0)
            bottom = Math.Max(bottom, OverlayLayer.ActualHeight + 40);

        var side = _plateStyle.MarginSide;
        NamePlateLayer.Margin = _plateStyle.Align switch
        {
            NamePlateAlign.Right => new Thickness(0, 0, side, bottom),
            NamePlateAlign.Center => new Thickness(0, 0, 0, bottom),
            _ => new Thickness(side, 0, 0, bottom),
        };
    }
}
