using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using EcclesiaCast.App.Services;

namespace EcclesiaCast.App.Views;

/// <summary>
/// A notice or question in the program's own dark look. Every button says what
/// it does; <see cref="Notice"/> is the only thing the rest of the program uses.
/// </summary>
public partial class NoticeWindow : Window
{
    private static readonly SolidColorBrush WarningBrush = Freeze(new SolidColorBrush(Color.FromRgb(0xE8, 0xC3, 0x4A)));

    /// <summary>Index of the button that was pressed, or -1 when the window was closed.</summary>
    public int Chosen { get; private set; } = -1;

    /// <param name="buttons">Button texts, in order from left to right; the last is the principal one.</param>
    /// <param name="cancelIndex">Which button closes with Escape (-1: none).</param>
    public NoticeWindow(string message, string? title, NoticeKind kind, IReadOnlyList<string> buttons,
        int defaultIndex, int cancelIndex, bool destructive, bool hasOwner)
    {
        InitializeComponent();

        MessageText.Text = message;
        if (!string.IsNullOrWhiteSpace(title))
        {
            TitleText.Text = title;
            TitleText.Visibility = Visibility.Visible;
            Title = title;
        }

        switch (kind)
        {
            case NoticeKind.Question:
                SignText.Text = "?";
                break;
            case NoticeKind.Warning:
                SignText.Text = "!";
                SignCircle.Background = WarningBrush;
                break;
            case NoticeKind.Error:
                SignText.Text = "×";
                SignCircle.Background = (Brush)FindResource("LiveBrush");
                break;
            default:
                SignText.Text = "i";
                break;
        }

        for (var i = 0; i < buttons.Count; i++)
        {
            var index = i;
            var isDefault = i == defaultIndex;
            var button = new Button
            {
                Content = buttons[i],
                Padding = new Thickness(18, 8, 18, 8),
                Margin = new Thickness(i == 0 ? 0 : 8, 0, 0, 0),
                Style = (Style)FindResource(isDefault ? "AccentButton" : "DarkButton"),
                IsDefault = isDefault,
                IsCancel = i == cancelIndex,
            };
            if (isDefault && destructive)
                button.Background = (Brush)FindResource("LiveBrush");
            button.Click += (_, _) =>
            {
                Chosen = index;
                DialogResult = true;
            };
            ButtonsPanel.Children.Add(button);
        }

        if (!hasOwner)
        {
            // Start-up notices: no window to sit on yet.
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Topmost = true;
        }

        Loaded += (_, _) => PlaySound(kind);
    }

    private static void PlaySound(NoticeKind kind)
    {
        try
        {
            switch (kind)
            {
                case NoticeKind.Error: SystemSounds.Hand.Play(); break;
                case NoticeKind.Warning: SystemSounds.Exclamation.Play(); break;
                default: SystemSounds.Asterisk.Play(); break;
            }
        }
        catch
        {
            // No sound device: the notice still shows.
        }
    }

    private static SolidColorBrush Freeze(SolidColorBrush brush)
    {
        brush.Freeze();
        return brush;
    }
}
