using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace EcclesiaCast.App.Converters;

/// <summary>
/// Turns a colour written as text («#FFFFFF») into a brush, so a field can
/// show the colour it holds next to its code. Text that is not a colour yet
/// (half typed) shows nothing instead of failing.
/// </summary>
public sealed class HexToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string text && !string.IsNullOrWhiteSpace(text))
        {
            try
            {
                var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(text.Trim()));
                brush.Freeze();
                return brush;
            }
            catch (FormatException)
            {
                // Not a colour yet.
            }
        }

        return Brushes.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}
