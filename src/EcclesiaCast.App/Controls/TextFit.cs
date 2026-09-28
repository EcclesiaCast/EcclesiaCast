using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EcclesiaCast.App.Controls;

/// <summary>
/// Shrinks a wrapping TextBlock's font until all of its text fits the space
/// its parent gives it. Used on the slide cards: every card keeps the same
/// size, and a long verse reads smaller instead of ending in "…".
/// </summary>
public static class TextFit
{
    private const double MinFontSize = 7;

    /// <summary>The size the text starts from; setting it turns fitting on.</summary>
    public static readonly DependencyProperty MaxFontSizeProperty =
        DependencyProperty.RegisterAttached("MaxFontSize", typeof(double), typeof(TextFit),
            new PropertyMetadata(0d, OnMaxFontSizeChanged));

    public static double GetMaxFontSize(DependencyObject element) => (double)element.GetValue(MaxFontSizeProperty);

    public static void SetMaxFontSize(DependencyObject element, double value) => element.SetValue(MaxFontSizeProperty, value);

    // The text itself isn't watched: a card's text never changes (editing a
    // slide rebuilds the cards), and watching it through a property
    // descriptor would keep every card ever shown alive.
    private static void OnMaxFontSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBlock text)
            return;

        text.Loaded -= OnLoaded;
        text.Loaded += OnLoaded;
        Fit(text);
    }

    private static void OnLoaded(object sender, RoutedEventArgs e)
    {
        var text = (TextBlock)sender;
        if (text.Parent is FrameworkElement parent)
        {
            parent.SizeChanged -= OnParentSizeChanged;
            parent.SizeChanged += OnParentSizeChanged;
        }
        Fit(text);
    }

    private static void OnParentSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is Panel panel)
        {
            foreach (var child in panel.Children.OfType<TextBlock>())
            {
                if (GetMaxFontSize(child) > 0)
                    Fit(child);
            }
        }
        else if (sender is Decorator { Child: TextBlock child })
        {
            Fit(child);
        }
    }

    /// <summary>
    /// The room is what the parent has left once the text's neighbours are
    /// laid out: in a DockPanel, the text is the last child and fills the rest.
    /// </summary>
    private static void Fit(TextBlock text)
    {
        var max = GetMaxFontSize(text);
        if (max <= 0 || text.Parent is not FrameworkElement parent)
            return;

        var width = parent.ActualWidth - text.Margin.Left - text.Margin.Right;
        var height = parent.ActualHeight - text.Margin.Top - text.Margin.Bottom;
        if (parent is DockPanel dock)
        {
            foreach (UIElement sibling in dock.Children)
            {
                if (ReferenceEquals(sibling, text) || sibling is not FrameworkElement other)
                    continue;
                var side = DockPanel.GetDock(other);
                if (side is Dock.Top or Dock.Bottom)
                    height -= other.ActualHeight + other.Margin.Top + other.Margin.Bottom;
                else
                    width -= other.ActualWidth + other.Margin.Left + other.Margin.Right;
            }
        }

        if (width <= 0 || height <= 0 || string.IsNullOrEmpty(text.Text))
        {
            text.FontSize = max;
            return;
        }

        var typeface = new Typeface(text.FontFamily, text.FontStyle, text.FontWeight, text.FontStretch);
        var dpi = VisualTreeHelper.GetDpi(text).PixelsPerDip;

        var size = max;
        for (; size > MinFontSize; size -= 0.5)
        {
            var formatted = new FormattedText(text.Text, CultureInfo.CurrentCulture, text.FlowDirection,
                typeface, size, Brushes.White, dpi)
            {
                MaxTextWidth = width,
                Trimming = TextTrimming.None,
            };
            if (formatted.Height <= height)
                break;
        }

        text.FontSize = size;
    }
}
