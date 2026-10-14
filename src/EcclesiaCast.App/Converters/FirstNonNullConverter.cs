using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace EcclesiaCast.App.Converters;

/// <summary>
/// Of several bound values, the first one that is not null: the PREVIEW draws
/// the next page of a presentation when there is one, and otherwise the
/// background that will be left behind the next slide.
/// </summary>
public sealed class FirstNonNullConverter : IMultiValueConverter
{
    public object? Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture) =>
        values.FirstOrDefault(v => v is not null && v != DependencyProperty.UnsetValue);

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
