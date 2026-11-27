using System.Windows;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.App.Views;

/// <summary>The name, abbreviation and language of a Bible being imported (<see cref="BibleImportViewModel"/>).</summary>
public partial class BibleImportWindow : Window
{
    private readonly BibleImportViewModel _viewModel;

    public BibleImportWindow(string suggestedName)
    {
        InitializeComponent();
        _viewModel = new BibleImportViewModel(suggestedName);
        NameBox.Text = _viewModel.SuggestedName;
        NameBox.Focus();
        NameBox.SelectAll();
    }

    public BibleImportMetadata? Result => _viewModel.Result;

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        var missing = _viewModel.TryAccept(NameBox.Text, AbbreviationBox.Text, LanguageBox.Text);
        if (missing is not null)
        {
            ErrorText.Text = _viewModel.Error;
            (missing == BibleImportField.Name ? NameBox : AbbreviationBox).Focus();
            return;
        }

        DialogResult = true;
    }
}
