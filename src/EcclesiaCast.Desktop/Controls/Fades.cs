using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Styling;

namespace EcclesiaCast.Desktop.Controls;

/// <summary>
/// The one helper the Avalonia interface fades and moves things with («fade
/// this control in so much time»), as the plan asks (section 5.1, point 4):
/// the WPF screens do it with a <c>DoubleAnimation</c> each.
///
/// The property is set to where it ends at once, and the animation draws the
/// way there over it; so a fade that is cut halfway, or a control that is
/// drawn before the animation starts, is left where it should end. A new
/// animation of the same property of the same thing stops the one before.
/// </summary>
public static class Fades
{
    /// <summary>WPF's <c>QuadraticEase</c> with its default mode (ease out).</summary>
    public static Easing EaseOut { get; } = new QuadraticEaseOut();

    /// <summary>WPF's <c>QuadraticEase</c> in ease-in mode.</summary>
    public static Easing EaseIn { get; } = new QuadraticEaseIn();

    private static readonly ConditionalWeakTable<Animatable, Dictionary<AvaloniaProperty, CancellationTokenSource>> Running = new();

    /// <summary>
    /// Fades <paramref name="target"/> from its opacity now to <paramref name="to"/>.
    /// True when it got there; false when another fade of it took over first.
    /// </summary>
    public static Task<bool> Fade(Visual target, double to, TimeSpan duration, Easing? easing = null) =>
        Animate(target, Visual.OpacityProperty, target.Opacity, to, duration, easing);

    /// <summary>Fades <paramref name="target"/> from <paramref name="from"/> to <paramref name="to"/>.</summary>
    public static Task<bool> Fade(Visual target, double from, double to, TimeSpan duration, Easing? easing = null) =>
        Animate(target, Visual.OpacityProperty, from, to, duration, easing);

    /// <summary>
    /// Takes a number property (an opacity, a move, a scale) from
    /// <paramref name="from"/> to <paramref name="to"/>. With no time it just
    /// jumps there. True when it got there; false when it was stopped.
    /// </summary>
    public static async Task<bool> Animate(Animatable target, AvaloniaProperty<double> property,
        double from, double to, TimeSpan duration, Easing? easing = null)
    {
        Stop(target, property);
        target.SetValue(property, to);
        if (duration <= TimeSpan.Zero)
            return true;

        var stop = new CancellationTokenSource();
        lock (Running)
            Running.GetOrCreateValue(target)[property] = stop;

        var animation = new Animation
        {
            Duration = duration,
            Easing = easing ?? new LinearEasing(),
            FillMode = FillMode.None,
            Children =
            {
                new KeyFrame { Cue = new Cue(0), Setters = { new Setter(property, from) } },
                new KeyFrame { Cue = new Cue(1), Setters = { new Setter(property, to) } },
            },
        };

        await animation.RunAsync(target, stop.Token);

        lock (Running)
        {
            if (Running.TryGetValue(target, out var map) && map.TryGetValue(property, out var current) && current == stop)
                map.Remove(property);
        }
        return !stop.IsCancellationRequested;
    }

    /// <summary>Stops a running animation of that property, leaving it where it was going.</summary>
    public static void Stop(Animatable target, AvaloniaProperty property)
    {
        lock (Running)
        {
            if (Running.TryGetValue(target, out var map) && map.Remove(property, out var stop))
                stop.Cancel();
        }
    }
}
