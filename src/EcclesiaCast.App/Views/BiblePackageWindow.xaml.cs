using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.App.Views;

/// <summary>
/// The list of Bible versions with a tick box each. What it lists, the picker
/// of languages, the number on the button and «Todas / Ninguna» are in
/// <see cref="BiblePackageViewModel"/>.
/// </summary>
public partial class BiblePackageWindow : Window
{
    private readonly BiblePackageViewModel _viewModel;

    public BiblePackageWindow(BiblePackageViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        Title = viewModel.Title;
        IntroText.Text = viewModel.Intro;
        AcceptButton.Content = viewModel.AcceptButtonText;
        if (viewModel.HasWarning)
        {
            WarningText.Text = viewModel.Warning;
            WarningBox.Visibility = Visibility.Visible;
        }

        VersionsList.ItemsSource = viewModel.VisibleRows;

        if (viewModel.HasGroups)
        {
            GroupPicker.ItemsSource = viewModel.Groups;
            GroupPicker.SelectedItem = viewModel.SelectedGroup;
            GroupPanel.Visibility = Visibility.Visible;
        }

        viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BiblePackageViewModel.VisibleRows))
            VersionsList.ItemsSource = _viewModel.VisibleRows;
        else if (e.PropertyName == nameof(BiblePackageViewModel.AcceptButtonText))
            AcceptButton.Content = _viewModel.AcceptButtonText;
    }

    private void GroupPicker_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        _viewModel.SelectedGroup = GroupPicker.SelectedItem as string;

    private void CheckAll_Click(object sender, RoutedEventArgs e) => _viewModel.CheckAll();

    private void CheckNone_Click(object sender, RoutedEventArgs e) => _viewModel.CheckNone();

    private void Accept_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.TryAccept())
            DialogResult = true;
    }
}
