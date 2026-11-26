using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using EcclesiaCast.Desktop.Controls;
using EcclesiaCast.Desktop.Converters;
using EcclesiaCast.Desktop.Styles;

namespace EcclesiaCast.Desktop.Tests;

/// <summary>The program's looks (EcclesiaCastTheme) and the converters of the screens.</summary>
public sealed class ThemeTests
{
    [AvaloniaFact]
    public void The_shortcut_of_a_button_shows_only_in_the_learning_layout()
    {
        var app = Application.Current!;
        var button = new Button { Content = "Black", Theme = (Avalonia.Styling.ControlTheme)app.FindResource("DarkButton")! };
        ShortcutHint.SetText(button, "F1");
        var window = new Window { Content = button, SizeToContent = SizeToContent.WidthAndHeight };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var hint = button.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Name == "PART_Shortcut");
        try
        {
            Assert.True(hint.IsEffectivelyVisible);

            EcclesiaCastTheme.ShowHints(app, learning: false);
            Dispatcher.UIThread.RunJobs();
            Assert.False(hint.IsEffectivelyVisible);
        }
        finally
        {
            EcclesiaCastTheme.ShowHints(app, learning: true);
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Every_window_gets_the_dark_background_and_the_program_font()
    {
        var window = new Window();
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(Color.Parse("#14161C"), ((ISolidColorBrush)window.Background!).Color);
        Assert.Equal(12, window.FontSize);
        var expected = OperatingSystem.IsWindows() ? "Segoe UI" : "Selawik";
        Assert.Contains(expected, window.FontFamily.Name);
        window.Close();
    }

    [Fact]
    public void A_colour_written_as_text_becomes_a_brush_and_a_half_typed_one_shows_nothing()
    {
        var brush = (ISolidColorBrush)Converters.Converters.HexToBrush.Convert("#2F63C9", typeof(IBrush), null, CultureInfo.InvariantCulture)!;
        Assert.Equal(Color.Parse("#2F63C9"), brush.Color);

        // Five digits is no colour (three, four, six and eight are, as in WPF).
        var half = (ISolidColorBrush)Converters.Converters.HexToBrush.Convert("#2F63C", typeof(IBrush), null, CultureInfo.InvariantCulture)!;
        Assert.Equal(Colors.Transparent, half.Color);
    }

    [Fact]
    public void A_radio_button_stands_for_one_number_and_unchecking_writes_nothing()
    {
        var c = Converters.Converters.IndexIs;
        Assert.Equal(true, c.Convert(2, typeof(bool), "2", CultureInfo.InvariantCulture));
        Assert.Equal(false, c.Convert(1, typeof(bool), "2", CultureInfo.InvariantCulture));
        Assert.Equal(2, c.ConvertBack(true, typeof(int), "2", CultureInfo.InvariantCulture));
        Assert.Same(BindingOperations.DoNothing, c.ConvertBack(false, typeof(int), "2", CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Not_zero_is_true_only_for_a_number_other_than_zero()
    {
        var c = Converters.Converters.NonZero;
        Assert.Equal(true, c.Convert(2.5, typeof(bool), null, CultureInfo.CurrentCulture));
        Assert.Equal(false, c.Convert(0, typeof(bool), null, CultureInfo.CurrentCulture));
        Assert.Equal(false, c.Convert(null, typeof(bool), null, CultureInfo.CurrentCulture));
    }

    [Fact]
    public void The_first_value_that_is_not_null_wins()
    {
        var page = new object();
        var background = new object();
        Assert.Same(page, Converters.Converters.FirstNonNull.Convert([null, page, background], typeof(object), null, CultureInfo.InvariantCulture));
        Assert.Same(background, Converters.Converters.FirstNonNull.Convert([AvaloniaProperty.UnsetValue, null, background], typeof(object), null, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void A_tab_shows_its_mark_when_it_is_in_the_set_not_minding_case()
    {
        var c = Converters.Converters.InSet;
        Assert.Equal(true, c.Convert(["Fondos", new[] { "fondos", "Medios" }], typeof(bool), null, CultureInfo.InvariantCulture));
        Assert.Equal(false, c.Convert(["Videos", new[] { "fondos" }], typeof(bool), null, CultureInfo.InvariantCulture));
    }
}
