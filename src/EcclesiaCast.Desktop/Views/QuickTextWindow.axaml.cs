using Avalonia.Controls;
using Avalonia.Interactivity;

namespace EcclesiaCast.Desktop.Views;

/// <summary>Quick text-only edit: a single slide, or the stored text of a verse.</summary>
public sealed partial class QuickTextWindow : Window
{
    public QuickTextWindow(string text, string? title = null, string? message = null)
    {
        InitializeComponent();
        if (title is not null)
            Title = title;
        if (message is not null)
            MessageText.Text = message;
        Editor.Text = text;
        Opened += (_, _) =>
        {
            Editor.Focus();
            Editor.CaretIndex = Editor.Text?.Length ?? 0;
        };
    }

    /// <summary>The edited text, trimmed; null while «Guardar» was not pressed.</summary>
    public string? Result { get; private set; }

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        Result = (Editor.Text ?? string.Empty).Trim();
        Close();
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close();
}
