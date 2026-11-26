using System.Globalization;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace EcclesiaCast.Desktop.Converters;

/// <summary>
/// The converters of WPF's App.xaml that Avalonia has no ready-made match
/// for. The others are not needed: a bool goes straight to IsVisible, «not»
/// is {Binding !IsOn} and «not null» is ObjectConverters.IsNotNull
/// (docs/guia-wpf-a-avalonia.md). Used as {x:Static conv:Converters.HexToBrush}.
/// </summary>
public static class Converters
{
    public static readonly IValueConverter HexToBrush = new HexToBrushConverter();
    public static readonly IValueConverter IndexIs = new IndexMatchConverter();
    public static readonly IValueConverter NonZero = new NonZeroConverter();
    public static readonly IMultiValueConverter FirstNonNull = new FirstNonNullConverter();
    public static readonly IMultiValueConverter InSet = new InSetConverter();
}

/// <summary>
/// A colour written as text («#FFFFFF») as a brush, so a field can show the
/// colour it holds next to its code. Text that is not a colour yet (half
/// typed) shows nothing instead of failing.
/// </summary>
public sealed class HexToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string text && Color.TryParse(text.Trim(), out var color)
            ? new ImmutableSolidColorBrush(color)
            : Brushes.Transparent;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        BindingOperations.DoNothing;
}

/// <summary>
/// Lets a radio button stand for one number of an int property
/// (ConverterParameter=1): checked when the property is 1, and checking it
/// sets the property to 1. Unchecking writes nothing, so the radio button
/// that loses the check does not undo the one that gained it.
/// </summary>
public sealed class IndexMatchConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int current && TryIndex(parameter, out var index) && current == index;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true && TryIndex(parameter, out var index) ? index : BindingOperations.DoNothing;

    private static bool TryIndex(object? parameter, out int index)
    {
        index = 0;
        return parameter is not null
            && int.TryParse(parameter.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out index);
    }
}

/// <summary>True when the bound number is not zero: greys out the settings
/// that only matter once a value is on (an outline colour, a blur amount…).</summary>
public sealed class NonZeroConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is not null
        && double.TryParse(value.ToString(), NumberStyles.Float, CultureInfo.CurrentCulture, out var number)
        && Math.Abs(number) > 1e-9;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        BindingOperations.DoNothing;
}

/// <summary>
/// Of several bound values, the first one that is not null: the PREVIEW
/// draws the next page of a presentation when there is one, and otherwise the
/// background that will be left behind the next slide.
/// </summary>
public sealed class FirstNonNullConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
        values.FirstOrDefault(v => v is not null && v != AvaloniaProperty.UnsetValue && v is not BindingNotification);
}

/// <summary>
/// True when a name (the first value) is in a set of names (the second), not
/// minding case: the little icons of the media tabs (silenced, on top of
/// everything). It takes both so the icon follows the set the moment it
/// changes. In WPF it gave a Visibility; here it goes to IsVisible.
/// </summary>
public sealed class InSetConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
        values is [string name, IEnumerable<string> set] && set.Contains(name, StringComparer.OrdinalIgnoreCase);
}
