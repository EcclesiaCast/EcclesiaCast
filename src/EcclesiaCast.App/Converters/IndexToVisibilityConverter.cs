using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace EcclesiaCast.App.Converters;

/// <summary>
/// Shows an element only while an int property holds one number
/// (<c>ConverterParameter=2</c>): the panel of the section that is open.
/// The hidden panels stay in the window, so their fields are already bound
/// when the operator opens them.
/// </summary>
public sealed class IndexToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int current
        && parameter is not null
        && int.TryParse(parameter.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var index)
        && current == index
            ? Visibility.Visible
            : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}
