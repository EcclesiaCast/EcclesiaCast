using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using EcclesiaCast.Core.Themes;

namespace EcclesiaCast.Proto;

/// <summary>
/// The slide, drawn at a fixed virtual 1920×1080 and scaled by a Viewbox, so
/// the projector and the small preview look the same (as SlideView.xaml in
/// the program). The text sits in a Grid because, as in WPF, an element takes
/// one effect: the Grid carries the shadow and the TextBlock the outline.
/// </summary>
public sealed class SlideView : UserControl
{
    /// <summary>Selawik, bundled: where a theme says «Segoe UI».</summary>
    public static readonly FontFamily Selawik = new("avares://EcclesiaCast.Proto/Assets/Fonts#Selawik");

    private static readonly IBrush EmptyBackground = new SolidColorBrush(Color.Parse("#10141E"));

    private readonly Grid _canvas;
    private readonly Grid _textLayer;
    private readonly Grid _textShadow;
    private readonly TextBlock _mainText;
    private readonly TextBlock _captionText;

    public SlideView()
    {
        _mainText = new TextBlock
        {
            FontFamily = Selawik,
            FontSize = 92,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brushes.White,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
        };

        _textShadow = new Grid { HorizontalAlignment = HorizontalAlignment.Center };
        _textShadow.Children.Add(_mainText);

        var stack = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        stack.Children.Add(_textShadow);

        _textLayer = new Grid { Margin = new Thickness(110, 80) };
        _textLayer.Children.Add(stack);

        _captionText = new TextBlock
        {
            FontFamily = Selawik,
            FontSize = 40,
            Foreground = new SolidColorBrush(Color.Parse("#B9C6DE")),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            IsVisible = false,
            Effect = new DropShadowDirectionEffect { BlurRadius = 12, ShadowDepth = 2, Direction = 315, Opacity = 0.75 },
        };
        var captionLayer = new Grid { Margin = new Thickness(110, 40), IsHitTestVisible = false };
        captionLayer.Children.Add(_captionText);

        _canvas = new Grid { Width = 1920, Height = 1080, Background = EmptyBackground, ClipToBounds = true };
        _canvas.Children.Add(_textLayer);
        _canvas.Children.Add(captionLayer);

        Content = new Viewbox { Stretch = Stretch.Uniform, Child = _canvas };
    }

    public string Text => _mainText.Text ?? string.Empty;

    /// <summary>
    /// Shows a text with a theme. <paramref name="overVideo"/> leaves the
    /// slide's own background transparent so the video behind it shows.
    /// </summary>
    public void Show(string text, string? caption, SlideTheme theme, bool overVideo)
    {
        _canvas.Background = overVideo ? Brushes.Transparent : EmptyBackground;

        _mainText.Text = text;
        _mainText.FontFamily = FontFor(theme.FontFamily);
        _mainText.FontWeight = theme.Bold ? FontWeight.SemiBold : FontWeight.Normal;
        _mainText.FontStyle = theme.Italic ? FontStyle.Italic : FontStyle.Normal;
        _mainText.Foreground = new SolidColorBrush(ParseColor(theme.TextColor, Colors.White));
        var margin = new Thickness(
            Math.Clamp(theme.MarginHorizontal, 0, 600), Math.Clamp(theme.MarginVertical, 0, 400));
        _textLayer.Margin = margin;
        FitText(theme, 1920 - margin.Left - margin.Right, 1080 - margin.Top - margin.Bottom);

        _textShadow.Effect = BuildShadow(theme);
        _mainText.Effect = BuildOutline(theme);

        _captionText.Text = caption;
        _captionText.IsVisible = !string.IsNullOrEmpty(caption);
        _captionText.FontFamily = FontFor(theme.FontFamily);
    }

    /// <summary>
    /// A crude stand-in for the program's automatic size (not part of the
    /// test): from the theme's largest size down in steps of 4 until the text
    /// fits, never below the theme's smallest. Without it a verse at the Bible
    /// theme's 120 runs off the screen.
    /// </summary>
    private void FitText(SlideTheme theme, double width, double height)
    {
        var max = Math.Clamp(theme.MaxFontSize, 10, 300);
        var min = Math.Clamp(Math.Min(theme.MinFontSize, max), 10, max);
        var typeface = new Typeface(_mainText.FontFamily, _mainText.FontStyle, _mainText.FontWeight);
        var size = max;
        while (size - 4 >= min && MeasureHeight(typeface, size, width) > height)
            size -= 4;
        _mainText.FontSize = size;
    }

    /// <summary>
    /// The height of the main text at a size. A TextLayout, because a
    /// TextBlock not yet on screen is not measured.
    /// </summary>
    private double MeasureHeight(Typeface typeface, double size, double width)
    {
        using var layout = new TextLayout(
            _mainText.Text, typeface, size, Brushes.White, TextAlignment.Center, TextWrapping.Wrap, maxWidth: width);
        return layout.Height;
    }

    /// <summary>The main text's size after fitting, for the tests.</summary>
    public double FittedFontSize => _mainText.FontSize;

    public void Clear()
    {
        _mainText.Text = string.Empty;
        _captionText.IsVisible = false;
    }

    /// <summary>
    /// The drop shadow, as BuildShadow in the program: blur from the theme,
    /// depth 3 down and to the right (WPF's default direction, 315°).
    /// </summary>
    private static IEffect? BuildShadow(SlideTheme theme)
    {
        var opacity = Math.Clamp(theme.ShadowOpacity, 0, 1);
        if (!theme.Shadow || opacity <= 0)
            return null;

        return new DropShadowDirectionEffect
        {
            BlurRadius = Math.Clamp(theme.ShadowBlur, 0, 80),
            ShadowDepth = 3,
            Direction = 315,
            Color = Colors.Black,
            Opacity = opacity,
        };
    }

    /// <summary>
    /// The outline, as BuildOutline in the program: a zero-offset, fully
    /// opaque shadow of the outline colour hugging every letter.
    /// </summary>
    private static IEffect? BuildOutline(SlideTheme theme)
    {
        var width = Math.Clamp(theme.OutlineWidth, 0, 40);
        if (width <= 0)
            return null;

        return new DropShadowEffect
        {
            OffsetX = 0,
            OffsetY = 0,
            BlurRadius = width,
            Color = ParseColor(theme.OutlineColor, Colors.Black),
            Opacity = 1,
        };
    }

    private static FontFamily FontFor(string? name) =>
        string.IsNullOrWhiteSpace(name) || name.Equals("Segoe UI", StringComparison.OrdinalIgnoreCase)
            ? Selawik
            : new FontFamily(name);

    private static Color ParseColor(string? hex, Color fallback) =>
        !string.IsNullOrWhiteSpace(hex) && Color.TryParse(hex, out var color) ? color : fallback;
}
