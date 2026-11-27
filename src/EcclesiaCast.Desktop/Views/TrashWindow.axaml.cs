using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Desktop.Views;

/// <summary>The songs and themes that were deleted in the last 30 days, to bring back or erase.</summary>
public sealed partial class TrashWindow : Window
{
    private readonly TrashViewModel _viewModel;

    public TrashWindow(TrashViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        // The list keeps Enter for itself here; in WPF it reaches «Cerrar».
        TrashList.AddHandler(KeyDownEvent, List_KeyDown, RoutingStrategies.Tunnel);
        ShowEntries();
    }

    private void List_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            Close();
        }
    }

    private void ShowEntries()
    {
        if (!ReferenceEquals(TrashList.ItemsSource, _viewModel.Entries))
            TrashList.ItemsSource = _viewModel.Entries;
        EmptyText.IsVisible = _viewModel.IsEmpty;
        EmptyButton.IsEnabled = _viewModel.CanEmpty;
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        RestoreButton.IsEnabled = _viewModel.CanRestore;
        PurgeButton.IsEnabled = _viewModel.CanPurge;
    }

    private void TrashList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        _viewModel.Selected = TrashList.SelectedItem as TrashEntry;
        UpdateButtons();
    }

    private void Restore_Click(object? sender, RoutedEventArgs e)
    {
        _viewModel.RestoreSelected();
        ShowEntries();
    }

    private void Purge_Click(object? sender, RoutedEventArgs e)
    {
        _viewModel.PurgeSelected();
        ShowEntries();
    }

    private void Empty_Click(object? sender, RoutedEventArgs e)
    {
        _viewModel.EmptyAll();
        ShowEntries();
    }

    private void Close_Click(object? sender, RoutedEventArgs e) => Close();
}
