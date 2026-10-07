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

    public string SecondValue { get; private set; } = string.Empty;

    /// <summary>Shows a second labelled field under the first one.</summary>
    public void AddSecondField(string message, string initialValue)
    {
        SecondMessageText.Text = message;
        SecondValueBox.Text = initialValue;
        SecondMessageText.Visibility = Visibility.Visible;
        SecondValueBox.Visibility = Visibility.Visible;
    }

    private void Accept_Click(object sender, RoutedEventArgs e)
    {
        Value = ValueBox.Text.Trim();
        SecondValue = SecondValueBox.Text.Trim();
        DialogResult = true;
    }
}
