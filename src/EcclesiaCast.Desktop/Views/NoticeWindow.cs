using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace EcclesiaCast.Desktop.Views;

/// <summary>
/// The program's notices and questions, in Avalonia: the message, a title
/// and its buttons. Plain on purpose: the real look is for phase 3.
/// </summary>
public sealed class NoticeWindow : Window
{
    public NoticeWindow(string message, string? title, NoticeKind kind, IReadOnlyList<string> buttons,
        int defaultIndex, int cancelIndex)
    {
        Title = string.IsNullOrWhiteSpace(title) ? "EcclesiaCast" : title;
        Chosen = cancelIndex;
        Width = 460;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        ShowInTaskbar = false;

        var icon = kind switch
        {
            NoticeKind.Error => "⛔",
            NoticeKind.Warning => "⚠",
            NoticeKind.Question => "❓",
            _ => "ℹ",
        };

        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Margin = new Thickness(0, 16, 0, 0),
        };
        for (var i = 0; i < buttons.Count; i++)
        {
            var index = i;
            var button = new Button { Content = buttons[i], IsDefault = i == defaultIndex, IsCancel = i == cancelIndex };
            button.Click += (_, _) =>
            {
                Chosen = index;
                Close();
            };
            row.Children.Add(button);
        }

        var body = new StackPanel { Margin = new Thickness(20), Spacing = 8 };
        if (!string.IsNullOrWhiteSpace(title))
            body.Children.Add(new TextBlock { Text = $"{icon}  {title}", FontSize = 16, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap });
        body.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap });
        body.Children.Add(row);
        Content = body;

        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
                Close();
        };
    }

    /// <summary>The index of the button chosen; the cancel index when the window was closed.</summary>
    public int Chosen { get; private set; }
}

/// <summary>Notices and questions in Avalonia, waiting for the answer like the WPF ones.</summary>
public sealed class AvaloniaNotices : INotices
{
    public int Ask(string message, string? title, NoticeKind kind, IReadOnlyList<string> buttons,
        int defaultIndex, int cancelIndex, bool destructive)
    {
        if (Avalonia.Application.Current is null)
            return -1;
        if (!Dispatcher.UIThread.CheckAccess())
            return Dispatcher.UIThread.Invoke(() => Ask(message, title, kind, buttons, defaultIndex, cancelIndex, destructive));

        var window = new NoticeWindow(message, title, kind, buttons, defaultIndex, cancelIndex);
        Modal.Run(window);
        return window.Chosen;
    }
}
