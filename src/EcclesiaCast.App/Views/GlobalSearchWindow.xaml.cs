using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EcclesiaCast.App.Platform;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.App.Views;

/// <summary>
/// One box that looks through songs, verses and media at once.
///
/// Until now each panel had its own search, so finding "santo" meant
/// guessing where it lived before looking for it. Mid-service there is no
/// time for that: the preacher says a reference and the operator has one
/// place to type it. What it decides is in <see cref="GlobalSearchViewModel"/>.
/// </summary>
public partial class GlobalSearchWindow : Window
{
    private readonly GlobalSearchViewModel _viewModel;

    public GlobalSearchWindow(Func<string, IReadOnlyList<SearchHit>> search, string initialQuery)
    {
        InitializeComponent();
        _viewModel = new GlobalSearchViewModel(search, initialQuery, new WpfUiTimerFactory());
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        Closed += (_, _) => _viewModel.Closed();

        QueryBox.Text = initialQuery;
        QueryBox.CaretIndex = QueryBox.Text.Length;
        Loaded += (_, _) => QueryBox.Focus();
    }

    /// <summary>What the operator chose, or null if they closed the window.</summary>
    public SearchHit? Result => _viewModel.Result;

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(GlobalSearchViewModel.Hits):
                ResultsList.ItemsSource = _viewModel.Hits;
                break;
            case nameof(GlobalSearchViewModel.SelectedIndex):
                if (ResultsList.SelectedIndex != _viewModel.SelectedIndex)
                    ResultsList.SelectedIndex = _viewModel.SelectedIndex;
                break;
            case nameof(GlobalSearchViewModel.Summary):
                SummaryText.Text = _viewModel.Summary;
                break;
        }
    }

    private void Query_Changed(object sender, TextChangedEventArgs e) => _viewModel.Query = QueryBox.Text;

    private void Results_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        _viewModel.SelectedIndex = ResultsList.SelectedIndex;

    /// <summary>
    /// The arrows walk the results while the caret stays in the box, so the
    /// operator can keep typing to narrow it down without reaching for the
    /// mouse.
    /// </summary>
    private void Query_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Down or Key.Up) || ResultsList.Items.Count == 0)
            return;

        _viewModel.Move(e.Key == Key.Down ? 1 : -1);
        ResultsList.ScrollIntoView(ResultsList.SelectedItem);
        e.Handled = true;
    }

    private void Results_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Accept();
            e.Handled = true;
        }
    }

    private void Results_DoubleClick(object sender, MouseButtonEventArgs e) => Accept();

    private void Open_Click(object sender, RoutedEventArgs e) => Accept();

    private void Accept()
    {
        if (_viewModel.Accept())
            DialogResult = true;
    }
}
