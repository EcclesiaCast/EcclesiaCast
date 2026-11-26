using Avalonia.Controls;
using Avalonia.Interactivity;

namespace EcclesiaCast.Desktop.Views;

/// <summary>Asks for a text, or for two.</summary>
public sealed partial class TextPromptWindow : Window
{
    public TextPromptWindow(string title, string message, string initialValue)
    {
        InitializeComponent();
        Title = title;
        MessageText.Text = message;
        ValueBox.Text = initialValue;
        Opened += (_, _) =>
        {
            ValueBox.Focus();
            ValueBox.SelectAll();
        };
    }

    /// <summary>True when the operator accepted (WPF's DialogResult).</summary>
    public bool Accepted { get; private set; }

    public string Value { get; private set; } = string.Empty;

    public string SecondValue { get; private set; } = string.Empty;

    /// <summary>Shows a second labelled field under the first one.</summary>
    public void AddSecondField(string message, string initialValue)
    {
        SecondMessageText.Text = message;
        SecondValueBox.Text = initialValue;
        SecondMessageText.IsVisible = true;
        SecondValueBox.IsVisible = true;
    }

    private void Accept_Click(object? sender, RoutedEventArgs e)
    {
        Value = (ValueBox.Text ?? string.Empty).Trim();
        SecondValue = (SecondValueBox.Text ?? string.Empty).Trim();
        Accepted = true;
        Close();
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close();
}
