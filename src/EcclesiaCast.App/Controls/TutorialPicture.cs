using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace EcclesiaCast.App.Controls;

/// <summary>
/// The small drawings a tutorial card can carry (<c>dibujo:</c>): how the
/// phone looks when it asks for the PIN and when it drives the projection.
/// Drawn with the same colors as the real page (<c>RemotePage</c>), not a photo,
/// so they never go out of date with the window's look.
/// </summary>
public static class TutorialPicture
{
    private static readonly Brush Screen = Frozen("#14161C");
    private static readonly Brush Panel = Frozen("#232630");
    private static readonly Brush Line = Frozen("#2A2E3A");
    private static readonly Brush Accent = Frozen("#2F63C9");
    private static readonly Brush Live = Frozen("#C43B3B");
    private static readonly Brush Muted = Frozen("#9AA1B2");

    /// <summary>The drawing named in the tutorial file, or null if there is none by that name.</summary>
    public static UIElement? Create(string name) => name switch
    {
        "celular-pin" => Phone(PinScreen()),
        "celular-control" => Phone(ControlScreen()),
        _ => null,
    };

    private static UIElement Phone(UIElement screen) => new Border
    {
        Width = 150,
        Height = 232,
        CornerRadius = new CornerRadius(18),
        Background = Frozen("#0B0C10"),
        BorderBrush = Frozen("#5A6072"),
        BorderThickness = new Thickness(3),
        Padding = new Thickness(5, 12, 5, 10),
        Child = new Border { CornerRadius = new CornerRadius(8), Background = Screen, ClipToBounds = true, Child = screen },
    };

    private static UIElement PinScreen()
    {
        var stack = new StackPanel { Margin = new Thickness(12, 30, 12, 0) };
        stack.Children.Add(Text("EcclesiaCast", 12, Brushes.White, bold: true, center: true));
        stack.Children.Add(Text("Escribí el PIN", 9, Muted, center: true, top: 6));

        var dots = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 14, 0, 0) };
        for (var i = 0; i < 4; i++)
            dots.Children.Add(Button("●", 22, 28, Panel, margin: new Thickness(2, 0, 2, 0)));
        stack.Children.Add(dots);
        stack.Children.Add(Button("Entrar", double.NaN, 24, Accent, margin: new Thickness(0, 16, 0, 0)));
        return stack;
    }

    private static UIElement ControlScreen()
    {
        var stack = new StackPanel { Margin = new Thickness(6, 6, 6, 0) };

        var header = new DockPanel { Margin = new Thickness(0, 0, 0, 5) };
        header.Children.Add(new Border { Width = 7, Height = 7, CornerRadius = new CornerRadius(4), Background = Live, Margin = new Thickness(0, 0, 4, 0), VerticalAlignment = VerticalAlignment.Center });
        header.Children.Add(Text("Santo, santo", 9, Brushes.White, bold: true));
        stack.Children.Add(header);

        stack.Children.Add(Card("EN VIVO", "Santo, santo, santo es el Señor"));

        stack.Children.Add(Row(top: 4, Button("◀", double.NaN, 24, Accent), Button("▶", double.NaN, 24, Accent)));
        stack.Children.Add(Row(top: 3, Button("Clear", double.NaN, 18, Panel, 8), Button("Black", double.NaN, 18, Panel, 8), Button("Logo", double.NaN, 18, Panel, 8)));
        stack.Children.Add(Row(top: 3, Button("Sin fondo", double.NaN, 18, Panel, 8), Button("Salida", double.NaN, 18, Panel, 8)));

        var slides = new UniformGrid { Columns = 3, Margin = new Thickness(0, 5, 0, 0) };
        for (var i = 0; i < 3; i++)
            slides.Children.Add(new Border
            {
                Height = 22,
                Margin = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Background = Panel,
                BorderBrush = i == 1 ? Accent : Line,
                BorderThickness = new Thickness(i == 1 ? 2 : 1),
            });
        stack.Children.Add(slides);
        return stack;
    }

    private static Border Card(string label, string body)
    {
        var inner = new StackPanel();
        inner.Children.Add(Text(label, 7, Muted));
        inner.Children.Add(Text(body, 9, Brushes.White, top: 1, wrap: true));
        return new Border { Background = Panel, CornerRadius = new CornerRadius(4), Padding = new Thickness(6, 4, 6, 5), Child = inner };
    }

    private static UIElement Row(double top, params UIElement[] buttons)
    {
        var grid = new Grid { Margin = new Thickness(0, top, 0, 0) };
        for (var i = 0; i < buttons.Length; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            if (buttons[i] is FrameworkElement element)
                element.Margin = new Thickness(i == 0 ? 0 : 2, 0, i == buttons.Length - 1 ? 0 : 2, 0);
            Grid.SetColumn(buttons[i], i);
            grid.Children.Add(buttons[i]);
        }

        return grid;
    }

    private static Border Button(string text, double width, double height, Brush fill, double size = 12, Thickness? margin = null) => new()
    {
        Width = width,
        Height = height,
        CornerRadius = new CornerRadius(5),
        Background = fill,
        Margin = margin ?? default,
        Child = Text(text, size, Brushes.White, center: true, vcenter: true),
    };

    private static TextBlock Text(string text, double size, Brush color, bool bold = false, bool center = false,
        double top = 0, bool wrap = false, bool vcenter = false) => new()
    {
        Text = text,
        FontSize = size,
        Foreground = color,
        FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
        HorizontalAlignment = center ? HorizontalAlignment.Center : HorizontalAlignment.Left,
        VerticalAlignment = vcenter ? VerticalAlignment.Center : VerticalAlignment.Top,
        TextWrapping = wrap ? TextWrapping.Wrap : TextWrapping.NoWrap,
        Margin = new Thickness(0, top, 0, 0),
    };

    private static Brush Frozen(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }
}
