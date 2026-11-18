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

    private TrashEntry? Selected => TrashList.SelectedItem as TrashEntry;

    private void ShowEntries()
    {
        TrashList.ItemsSource = _viewModel.Entries;
        EmptyText.Visibility = _viewModel.IsEmpty ? Visibility.Visible : Visibility.Collapsed;
        EmptyButton.IsEnabled = !_viewModel.IsEmpty;
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        RestoreButton.IsEnabled = Selected is not null;
        PurgeButton.IsEnabled = Selected is not null;
    }

    private void TrashList_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateButtons();

    private void Restore_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is not { } entry)
            return;

        _viewModel.Restore(entry);
        ShowEntries();
    }

    private void Purge_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is not { } entry)
            return;

        if (_viewModel.Purge(entry))
            ShowEntries();
    }

    private void Empty_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.EmptyAll())
            ShowEntries();
    }
}
