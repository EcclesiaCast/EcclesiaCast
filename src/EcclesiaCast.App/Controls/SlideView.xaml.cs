using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using EcclesiaCast.Core.Presentation;

namespace EcclesiaCast.App.Controls;

/// <summary>
/// Renders a slide (text + optional caption and secondary version) with the
/// current output state. Used at full size by the output window and scaled
/// down by the previews.
/// </summary>
public partial class SlideView : UserControl
{
    public static readonly DependencyProperty SlideProperty =
        DependencyProperty.Register(nameof(Slide), typeof(SlideContent), typeof(SlideView),
            new PropertyMetadata(null, (d, _) => ((SlideView)d).OnSlideChanged()));

    public static readonly DependencyProperty StateProperty =
        DependencyProperty.Register(nameof(State), typeof(OutputState), typeof(SlideView),
            new PropertyMetadata(OutputState.Content, (d, _) => ((SlideView)d).OnStateChanged()));

    public static readonly DependencyProperty AnimateTransitionsProperty =
        DependencyProperty.Register(nameof(AnimateTransitions), typeof(bool), typeof(SlideView),
            new PropertyMetadata(false));

    public static readonly DependencyProperty OverlayProperty =
        DependencyProperty.Register(nameof(Overlay), typeof(string), typeof(SlideView),
            new PropertyMetadata(null, (d, _) => ((SlideView)d).OnOverlayChanged()));

    public static readonly DependencyProperty HighlightProperty =
        DependencyProperty.Register(nameof(Highlight), typeof(string), typeof(SlideView),
            new PropertyMetadata(null, (d, _) => ((SlideView)d).RenderText()));

    private static readonly Brush HighlightBrush = new SolidColorBrush(Color.FromRgb(0xE8, 0xC3, 0x4A));

    public SlideView()
    {
        InitializeComponent();
        OnStateChanged();
    }

    public SlideContent? Slide
    {
        get => (SlideContent?)GetValue(SlideProperty);
        set => SetValue(SlideProperty, value);
    }

    public OutputState State
    {
        get => (OutputState)GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

    public bool AnimateTransitions
    {
        get => (bool)GetValue(AnimateTransitionsProperty);
        set => SetValue(AnimateTransitionsProperty, value);
    }

    public string? Overlay
    {
        get => (string?)GetValue(OverlayProperty);
        set => SetValue(OverlayProperty, value);
    }

    /// <summary>Word or phrase to paint over the projected text, marker-pen style.</summary>
    public string? Highlight
    {
        get => (string?)GetValue(HighlightProperty);
        set => SetValue(HighlightProperty, value);
    }

    private void OnSlideChanged()
    {
        RenderText();

        if (AnimateTransitions && State == OutputState.Content)
            FadeIn(TextLayer);
    }

    private void RenderText()
    {
        RenderWithHighlight(MainText, Slide?.MainText);
        RenderWithHighlight(SecondaryText, Slide?.SecondaryText);
        SecondaryText.Visibility = string.IsNullOrEmpty(Slide?.SecondaryText)
            ? Visibility.Collapsed
            : Visibility.Visible;

        CaptionText.Text = Slide?.Caption ?? string.Empty;
        CaptionText.Visibility = string.IsNullOrEmpty(Slide?.Caption)
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private void RenderWithHighlight(TextBlock target, string? text)
    {
        target.Inlines.Clear();
        if (string.IsNullOrEmpty(text))
            return;

        var term = Highlight;
        if (string.IsNullOrWhiteSpace(term))
        {
            target.Inlines.Add(new Run(text));
            return;
        }

        var position = 0;
        while (position < text.Length)
        {
            var index = text.IndexOf(term, position, StringComparison.CurrentCultureIgnoreCase);
            if (index < 0)
            {
                target.Inlines.Add(new Run(text[position..]));
                break;
            }

            if (index > position)
                target.Inlines.Add(new Run(text[position..index]));

            target.Inlines.Add(new Run(text.Substring(index, term.Length))
            {
                Background = HighlightBrush,
                Foreground = Brushes.Black,
            });

            position = index + term.Length;
        }
    }

    private void OnStateChanged()
    {
        TextLayer.Visibility = State == OutputState.Content ? Visibility.Visible : Visibility.Hidden;
        LogoLayer.Visibility = State == OutputState.Logo ? Visibility.Visible : Visibility.Collapsed;

        var blackTarget = State == OutputState.Black ? 1d : 0d;
        if (AnimateTransitions)
        {
            BlackLayer.BeginAnimation(OpacityProperty,
                new DoubleAnimation(blackTarget, TimeSpan.FromMilliseconds(250)));
            if (State == OutputState.Content)
                FadeIn(TextLayer);
        }
        else
        {
            BlackLayer.Opacity = blackTarget;
        }
    }

    private void OnOverlayChanged()
    {
        var hasMessage = !string.IsNullOrWhiteSpace(Overlay);
        OverlayText.Text = Overlay ?? string.Empty;
        OverlayLayer.Visibility = hasMessage ? Visibility.Visible : Visibility.Collapsed;

        if (AnimateTransitions && hasMessage)
            FadeIn(OverlayLayer);
    }

    private static void FadeIn(UIElement element) =>
        element.BeginAnimation(OpacityProperty,
            new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(280))
            {
                EasingFunction = new QuadraticEase()
            });
}
