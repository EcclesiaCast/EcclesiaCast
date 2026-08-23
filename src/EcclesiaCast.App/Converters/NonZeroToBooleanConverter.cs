using System.Globalization;
using System.Windows.Data;

namespace EcclesiaCast.App.Converters;

/// <summary>True when the bound number isn't zero — greys out the settings that
/// only matter once a value is on (an outline colour, a blur amount…).</summary>
public sealed class NonZeroToBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is not null
        && double.TryParse(value.ToString(), NumberStyles.Float, CultureInfo.CurrentCulture, out var number)
        && Math.Abs(number) > 1e-9;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
