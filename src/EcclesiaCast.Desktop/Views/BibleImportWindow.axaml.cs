using Avalonia.Controls;
using Avalonia.Interactivity;
using EcclesiaCast.Presentation.Services;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Desktop.Views;

/// <summary>
/// Asks for the name of a Bible that is being imported. What the boxes need
/// is in <see cref="BibleImportViewModel"/>.
/// </summary>
public sealed partial class BibleImportWindow : Window
{
    private readonly BibleImportViewModel _viewModel;

    public BibleImportWindow(string suggestedName)
    {
        InitializeComponent();
        _viewModel = new BibleImportViewModel(suggestedName);
        NameBox.Text = _viewModel.SuggestedName;
        Opened += (_, _) =>
        {
            NameBox.Focus();
            NameBox.SelectAll();
        };
    }

    /// <summary>What was accepted; null when the operator cancelled.</summary>
    public BibleImportMetadata? Result => _viewModel.Result;

    private void Import_Click(object? sender, RoutedEventArgs e)
    {
        var missing = _viewModel.TryAccept(NameBox.Text, AbbreviationBox.Text, LanguageBox.Text);
        ErrorText.Text = _viewModel.Error;
        if (missing is { } field)
            (field == BibleImportField.Name ? NameBox : AbbreviationBox).Focus();
        else
            Close();
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close();
}
