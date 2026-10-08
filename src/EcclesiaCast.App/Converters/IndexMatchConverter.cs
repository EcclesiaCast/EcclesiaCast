using System.Globalization;
using System.Windows.Data;

namespace EcclesiaCast.App.Converters;

/// <summary>
/// Lets a radio button stand for one number of an int property
/// (<c>ConverterParameter=1</c>): checked when the property is 1, and
/// checking it sets the property to 1. Unchecking writes nothing, so the radio
/// button that loses the check does not undo the one that gained it.
/// </summary>
public sealed class IndexMatchConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int current && TryIndex(parameter, out var index) && current == index;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true && TryIndex(parameter, out var index) ? index : Binding.DoNothing;

    private static bool TryIndex(object? parameter, out int index)
    {
        index = 0;
        return parameter is not null
            && int.TryParse(parameter.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out index);
    }
}
