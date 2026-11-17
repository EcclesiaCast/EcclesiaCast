using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using EcclesiaCast.Core.Media;

namespace EcclesiaCast.App.Services;

/// <summary>
/// The list of pictures that can fill the borders of a logo or a media, grouped
/// by the tab they live in: with only the names, two pictures from different
/// tabs (or the same name in two) could not be told apart.
/// </summary>
internal static class FillOptionsBox
{
    private const string SpecialGroup = "Sin imagen de la biblioteca";

    /// <summary>
    /// Fills <paramref name="box"/> with <paramref name="options"/>, which must
    /// already come with the special choices first and the library after them.
    /// The library is ordered by tab and then by name.
    /// </summary>
    public static void Fill(ComboBox box, IEnumerable<MediaItem> options)
    {
        var special = options.Where(m => m.Id <= 0).ToList();
        var library = options.Where(m => m.Id > 0)
            .OrderBy(m => TabOf(m), StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(m => m.Name, StringComparer.CurrentCultureIgnoreCase);

        var ordered = special.Concat(library).ToList();
        var view = new ListCollectionView(ordered);
        view.GroupDescriptions.Add(new PropertyGroupDescription(null, new GroupNameConverter()));

        box.GroupStyle.Clear();
        box.GroupStyle.Add(new GroupStyle
        {
            HeaderTemplate = (System.Windows.DataTemplate)XamlReader.Parse(
                "<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>" +
                "<TextBlock Text='{Binding Name}' FontSize='10' FontWeight='SemiBold' Margin='4,6,0,2' Foreground='#8A93A6'/>" +
                "</DataTemplate>"),
        });
        box.ItemsSource = view;
    }

    private static string TabOf(MediaItem item) =>
        string.IsNullOrWhiteSpace(item.Category) ? "Sin pestaña" : item.Category!;

    private sealed class GroupNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is MediaItem { Id: > 0 } item ? "Pestaña «" + TabOf(item) + "»" : SpecialGroup;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
