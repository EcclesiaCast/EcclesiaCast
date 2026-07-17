using System.Windows;

namespace EcclesiaCast.App.Views;

public partial class TextPromptWindow : Window
{
    public TextPromptWindow(string title, string message, string initialValue)
    {
        InitializeComponent();
        Title = title;
        MessageText.Text = message;
        ValueBox.Text = initialValue;
        ValueBox.Focus();
        ValueBox.SelectAll();
    }

    public string Value { get; private set; } = string.Empty;

    private void Accept_Click(object sender, RoutedEventArgs e)
    {
        Value = ValueBox.Text.Trim();
        DialogResult = true;
    }
}
