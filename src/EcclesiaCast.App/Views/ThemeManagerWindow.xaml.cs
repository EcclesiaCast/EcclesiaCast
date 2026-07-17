using System.Windows;
using System.Windows.Media;
using EcclesiaCast.App.ViewModels;

namespace EcclesiaCast.App.Views;

public partial class ThemeManagerWindow : Window
{
    public ThemeManagerWindow(ThemeManagerViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        FontCombo.ItemsSource = Fonts.SystemFontFamilies
            .Select(f => f.Source)
            .OrderBy(name => name)
            .ToList();
    }
}
