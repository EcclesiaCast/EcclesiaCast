using Avalonia.Controls;
using Avalonia.Threading;
using EcclesiaCast.Desktop.Views;

namespace EcclesiaCast.Desktop.Services;

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

        var window = new NoticeWindow(message, title, kind, buttons, defaultIndex, cancelIndex, destructive);
        // Start-up notices have no window to sit on yet: nothing may cover them.
        if (Modal.Owner is null)
            window.Topmost = true;
        Modal.Run(window);
        return window.Chosen;
    }
}
