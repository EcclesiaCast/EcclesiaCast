using System.Windows;
using EcclesiaCast.App.ViewModels;

namespace EcclesiaCast.App.Views;

/// <summary>
/// «Seguir la canción»: where the operator picks the input, the way the
/// program acts and turns listening on. It can stay open beside the main
/// window during the service, or be closed: listening goes on either way.
/// </summary>
public partial class ListeningWindow : Window
{
    public ListeningWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.RefreshListenDevicesCommand.Execute(null);
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
