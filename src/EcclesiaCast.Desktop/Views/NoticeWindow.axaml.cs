using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Styling;
using EcclesiaCast.Desktop.Controls;
using EcclesiaCast.Presentation.Services;

namespace EcclesiaCast.Desktop.Views;

/// <summary>
/// A notice or question in the program's own dark look. Every button says what
/// it does; <see cref="Notice"/> is the only thing the rest of the program uses.
/// </summary>
public sealed partial class NoticeWindow : Window
{
    private static readonly IBrush WarningBrush = new Avalonia.Media.Immutable.ImmutableSolidColorBrush(Color.FromRgb(0xE8, 0xC3, 0x4A));

    /// <param name="buttons">Button texts, in order from left to right.</param>
    /// <param name="cancelIndex">Which button closes with Escape (-1: none).</param>
    public NoticeWindow(string message, string? title, NoticeKind kind, IReadOnlyList<string> buttons,
        int defaultIndex, int cancelIndex, bool destructive = false)
    {
        InitializeComponent();
        Chosen = cancelIndex;

        WriteMessage(message);
        if (!string.IsNullOrWhiteSpace(title))
        {
            TitleText.Text = title;
            TitleText.IsVisible = true;
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
                SignCircle.Background = (IBrush)this.FindResource("LiveBrush")!;
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
                Theme = (ControlTheme)this.FindResource(isDefault ? "AccentButton" : "DarkButton")!,
                IsDefault = isDefault,
                IsCancel = i == cancelIndex,
            };
            if (isDefault && destructive)
                button.Background = (IBrush)this.FindResource("LiveBrush")!;
            button.Click += (_, _) =>
            {
                Chosen = index;
                Close();
            };
            ButtonsPanel.Children.Add(button);
        }
    }

    /// <summary>The index of the button chosen; the cancel index when the window was closed.</summary>
    public int Chosen { get; private set; }

    /// <summary>The message, with the «on top of everything» icon where its mark was written.</summary>
    private void WriteMessage(string message)
    {
        var parts = message.Split(Notice.OnTopIconMark);
        var inlines = MessageText.Inlines ??= new InlineCollection();
        inlines.Clear();
        for (var i = 0; i < parts.Length; i++)
        {
            if (i > 0)
                inlines.Add(OnTopMark.InText(13));
            inlines.Add(new Run(parts[i]));
        }
    }
}
