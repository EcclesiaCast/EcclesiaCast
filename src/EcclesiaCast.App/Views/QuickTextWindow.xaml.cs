using System.Windows;

namespace EcclesiaCast.App.Views;

public partial class QuickTextWindow : Window
{
    public QuickTextWindow(string text, string? title = null, string? message = null)
    {
        InitializeComponent();
        if (title is not null)
            Title = title;
        if (message is not null)
            MessageText.Text = message;
        TextBox.Text = text;
        TextBox.Focus();
        TextBox.CaretIndex = text.Length;
    }

    public string Result { get; private set; } = string.Empty;

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        Result = TextBox.Text.Trim();
        DialogResult = true;
    }
}
