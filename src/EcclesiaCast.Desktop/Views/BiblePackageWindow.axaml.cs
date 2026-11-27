using Avalonia.Controls;
using Avalonia.Interactivity;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Desktop.Views;

/// <summary>
/// The list of Bible versions with a tick box each. What it lists, the picker
/// of languages, the number on the button and «Todas / Ninguna» are in
/// <see cref="BiblePackageViewModel"/>.
/// </summary>
public sealed partial class BiblePackageWindow : Window
{
    private readonly BiblePackageViewModel _viewModel;

    public BiblePackageWindow(BiblePackageViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    /// <summary>True when the operator accepted the ticked ones.</summary>
    public bool Accepted { get; private set; }

    private void CheckAll_Click(object? sender, RoutedEventArgs e) => _viewModel.CheckAll();

    private void CheckNone_Click(object? sender, RoutedEventArgs e) => _viewModel.CheckNone();

    private void Accept_Click(object? sender, RoutedEventArgs e)
    {
        if (!_viewModel.TryAccept())
            return;

        Accepted = true;
        Close();
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close();
}
