using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using EcclesiaCast.Core.Presentation;

namespace EcclesiaCast.App.Controls;

/// <summary>
/// Renders a slide (text + optional caption) with the current output state.
/// Used at full size by the output window and scaled down by the previews.
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

    private void OnSlideChanged()
    {
        MainText.Text = Slide?.MainText ?? string.Empty;
        CaptionText.Text = Slide?.Caption ?? string.Empty;
        CaptionText.Visibility = string.IsNullOrEmpty(Slide?.Caption)
            ? Visibility.Collapsed
            : Visibility.Visible;

        if (AnimateTransitions && State == OutputState.Content)
            FadeIn(TextLayer);
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

    private static void FadeIn(UIElement element) =>
        element.BeginAnimation(OpacityProperty,
            new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(280))
            {
                EasingFunction = new QuadraticEase()
            });
}
