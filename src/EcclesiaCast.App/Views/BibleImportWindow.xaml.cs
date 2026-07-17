using System.Windows;

namespace EcclesiaCast.App.Views;

public partial class BibleImportWindow : Window
{
    public BibleImportWindow(string suggestedName)
    {
        InitializeComponent();
        NameBox.Text = suggestedName;
        NameBox.Focus();
        NameBox.SelectAll();
    }

    public string ResultName { get; private set; } = string.Empty;
    public string ResultAbbreviation { get; private set; } = string.Empty;
    public string ResultLanguage { get; private set; } = "es";

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text.Trim();
        var abbreviation = AbbreviationBox.Text.Trim();

        if (name.Length == 0)
        {
            ErrorText.Text = "El nombre es obligatorio.";
            NameBox.Focus();
            return;
        }
        if (abbreviation.Length == 0)
        {
            ErrorText.Text = "La abreviatura es obligatoria.";
            AbbreviationBox.Focus();
            return;
        }

        ResultName = name;
        ResultAbbreviation = abbreviation;
        ResultLanguage = string.IsNullOrWhiteSpace(LanguageBox.Text) ? "es" : LanguageBox.Text.Trim();
        DialogResult = true;
    }
}
