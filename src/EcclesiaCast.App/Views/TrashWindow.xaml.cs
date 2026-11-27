using System.Windows;
using System.Windows.Controls;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.App.Views;

/// <summary>The songs and themes that were deleted in the last 30 days, to bring back or erase.</summary>
public partial class TrashWindow : Window
{
    private readonly TrashViewModel _viewModel;

    public TrashWindow(TrashViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        ShowEntries();
    }

    private void ShowEntries()
    {
        if (!ReferenceEquals(TrashList.ItemsSource, _viewModel.Entries))
            TrashList.ItemsSource = _viewModel.Entries;
        EmptyText.Visibility = _viewModel.IsEmpty ? Visibility.Visible : Visibility.Collapsed;
        EmptyButton.IsEnabled = _viewModel.CanEmpty;
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        RestoreButton.IsEnabled = _viewModel.CanRestore;
        PurgeButton.IsEnabled = _viewModel.CanPurge;
    }

    private void TrashList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _viewModel.Selected = TrashList.SelectedItem as TrashEntry;
        UpdateButtons();
    }

    private void Restore_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.RestoreSelected();
        ShowEntries();
    }

    private void Purge_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.PurgeSelected();
        ShowEntries();
    }

    private void Empty_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.EmptyAll();
        ShowEntries();
    }
}
