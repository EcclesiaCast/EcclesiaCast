using System.Windows;
using System.Windows.Controls;

namespace EcclesiaCast.App.Views;

/// <summary>
/// Asks the operator to pick one of a few named options. Plain Yes/No boxes
/// say nothing about what each answer does, so every option is a button
/// that says it.
/// </summary>
public partial class ChoiceWindow : Window
{
    public ChoiceWindow(string title, string message, IReadOnlyList<string> options, string? rememberLabel = null)
    {
        InitializeComponent();
        Title = title;
        MessageText.Text = message;

        if (!string.IsNullOrEmpty(rememberLabel))
        {
            RememberBox.Content = rememberLabel;
            RememberBox.Visibility = Visibility.Visible;
        }

        for (var i = 0; i < options.Count; i++)
        {
            var index = i;
            var button = new Button
            {
                Content = options[i],
                Padding = new Thickness(16, 10, 16, 10),
                Margin = new Thickness(0, i == 0 ? 0 : 8, 0, 0),
                HorizontalContentAlignment = HorizontalAlignment.Left,
                // The first option is the suggested one.
                Style = (Style)FindResource(i == 0 ? "AccentButton" : "DarkButton"),
                IsDefault = i == 0,
            };
            button.Click += (_, _) =>
            {
                Chosen = index;
                Remember = RememberBox.IsChecked == true;
                DialogResult = true;
            };
            OptionsPanel.Children.Add(button);
        }
    }

    /// <summary>Index of the chosen option, or -1 when the window was closed.</summary>
    public int Chosen { get; private set; } = -1;

    /// <summary>The "do the same with the rest" box was ticked when the option was picked.</summary>
    public bool Remember { get; private set; }
}
