using System.Windows;
using EcclesiaCast.App.Views;

namespace EcclesiaCast.App.Platform;

/// <summary>
/// The program's own notice window in place of the grey system message
/// boxes. Safe to call from any thread.
/// </summary>
public sealed class WpfNotices : INotices
{
    public int Ask(string message, string? title, NoticeKind kind, IReadOnlyList<string> buttons,
        int defaultIndex, int cancelIndex, bool destructive)
    {
        var app = Application.Current;
        if (app is null)
            return -1;
        if (!app.Dispatcher.CheckAccess())
            return app.Dispatcher.Invoke(() => Ask(message, title, kind, buttons, defaultIndex, cancelIndex, destructive));

        var owner = app.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive && w.IsVisible)
            ?? (app.MainWindow is { IsVisible: true } main ? main : null);

        var window = new NoticeWindow(message, title, kind, buttons, defaultIndex, cancelIndex, destructive,
            hasOwner: owner is not null);
        if (owner is not null)
            window.Owner = owner;
        window.ShowDialog();
        return window.Chosen;
    }
}
