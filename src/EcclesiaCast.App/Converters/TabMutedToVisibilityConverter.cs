using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace EcclesiaCast.App.Converters;

/// <summary>
/// Shows the little speaker on a media tab that is silenced. Takes the tab's
/// name and the set of silenced tabs; both are needed so the icon appears and
/// disappears the moment the set changes, without rebuilding the list.
/// </summary>
public sealed class TabMutedToVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture) =>
        values is [string tab, IEnumerable<string> muted] && muted.Contains(tab, StringComparer.OrdinalIgnoreCase)
            ? Visibility.Visible
            : Visibility.Collapsed;

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
