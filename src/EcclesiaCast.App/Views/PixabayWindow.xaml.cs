using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using EcclesiaCast.Presentation.ViewModels;
using EcclesiaCast.Core.Media;

namespace EcclesiaCast.App.Views;

/// <summary>
/// «Buscar fondos en Pixabay»: searches, shows what it found, and downloads
/// the chosen ones into a tab of the media bar. It stays open beside the
/// program (it is not modal) while it downloads.
/// </summary>
public partial class PixabayWindow : Window
{
    private readonly PixabayViewModel _viewModel;

    public PixabayWindow(MainViewModel vm)
    {
        InitializeComponent();
        // The tab list and the chosen tab are the main program's.
        DataContext = vm;
        _viewModel = new PixabayViewModel(vm, EmptyText.Text);
        Results.ItemsSource = _viewModel.Cards;
        CreditText.Text = PixabayBackgrounds.Credit;
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        _viewModel.ResultsCleared += (_, _) => Scroller.ScrollToTop();
        ShowState();
        Loaded += (_, _) => QueryBox.Focus();
        Closed += (_, _) => _viewModel.Cancel();
    }

    private bool Photos => PhotosRadio.IsChecked == true;

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e) => ShowState();

    private void ShowState()
    {
        StatusLine.Text = _viewModel.StatusText;
        var message = _viewModel.CenterMessage;
        EmptyText.Text = message;
        EmptyText.Visibility = message.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
        MoreButton.Visibility = _viewModel.HasMore ? Visibility.Visible : Visibility.Collapsed;
        MoreButton.IsEnabled = _viewModel.CanLoadMore;
        AddButton.IsEnabled = _viewModel.CanAdd;
        AddButton.Content = _viewModel.AddText;
    }

    private void Search_Click(object sender, RoutedEventArgs e) =>
        _ = _viewModel.SearchAsync(QueryBox.Text, Photos, newSearch: true);

    private void Kind_Click(object sender, RoutedEventArgs e) =>
        _ = _viewModel.KindChangedAsync(QueryBox.Text, Photos);

    private void More_Click(object sender, RoutedEventArgs e) =>
        _ = _viewModel.SearchAsync(QueryBox.Text, Photos, newSearch: false);

    private async void Add_Click(object sender, RoutedEventArgs e) => await _viewModel.AddSelectedAsync();

    private void Credit_Click(object sender, RoutedEventArgs e) =>
        Process.Start(new ProcessStartInfo(PixabayBackgrounds.Site) { UseShellExecute = true });

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
