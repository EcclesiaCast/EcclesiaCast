using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using EcclesiaCast.Presentation.Platform;
using EcclesiaCast.Presentation.Services;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Desktop.Views;

/// <summary>
/// One box that looks through songs, verses and media at once. What it
/// decides is in <see cref="GlobalSearchViewModel"/>.
/// </summary>
public sealed partial class GlobalSearchWindow : Window
{
    private readonly GlobalSearchViewModel _viewModel;

    public GlobalSearchWindow(
        Func<string, IReadOnlyList<SearchHit>> search, string initialQuery, IUiTimerFactory timers)
    {
        InitializeComponent();
        _viewModel = new GlobalSearchViewModel(search, initialQuery, timers);
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        Closed += (_, _) => _viewModel.Closed();

        QueryBox.Text = initialQuery;
        QueryBox.CaretIndex = initialQuery.Length;
        QueryBox.TextChanged += (_, _) =>
        {
            // The event arrives later than the change: a text the view model
            // already has is not a new search.
            _viewModel.Query = QueryBox.Text ?? string.Empty;
        };

        // The arrows are taken before the box moves its caret with them.
        QueryBox.AddHandler(KeyDownEvent, Query_KeyDown, RoutingStrategies.Tunnel);
        ResultsList.AddHandler(KeyDownEvent, Results_KeyDown, RoutingStrategies.Tunnel);
        ResultsList.SelectionChanged += (_, _) => _viewModel.SelectedIndex = ResultsList.SelectedIndex;
        ResultsList.DoubleTapped += (_, _) => Accept();

        Opened += (_, _) => QueryBox.Focus();
    }

    /// <summary>What the operator chose, or null if they closed the window.</summary>
    public SearchHit? Result => _viewModel.Result;

    /// <summary>Searches without waiting for the pause (what the screens bench does).</summary>
    internal void SearchNow() => _viewModel.SearchNow();

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

    /// <summary>
    /// The arrows walk the results while the caret stays in the box, so the
    /// operator can keep typing to narrow it down.
    /// </summary>
    private void Query_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Down or Key.Up) || _viewModel.Hits.Count == 0)
            return;

        _viewModel.Move(e.Key == Key.Down ? 1 : -1);
        ResultsList.ScrollIntoView(_viewModel.SelectedIndex);
        e.Handled = true;
    }

    private void Results_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Accept();
            e.Handled = true;
        }
    }

    private void Open_Click(object? sender, RoutedEventArgs e) => Accept();

    private void Close_Click(object? sender, RoutedEventArgs e) => Close();

    private void Accept()
    {
        if (_viewModel.Accept())
            Close();
    }
}
