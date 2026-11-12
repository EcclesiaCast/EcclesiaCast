using System.Windows;
using EcclesiaCast.App.Views;

namespace EcclesiaCast.App.Services;

public enum NoticeKind { Info, Warning, Error, Question }

/// <summary>
/// Notices and questions in the program's own look, in place of the grey
/// system message boxes. Safe to call from any thread.
/// </summary>
public static class Notice
{
    /// <summary>A notice with a single button.</summary>
    public static void Show(string message, string? title = null,
        NoticeKind kind = NoticeKind.Info, string ok = "Entendido")
    {
        Run(message, title, kind, [ok], defaultIndex: 0, cancelIndex: 0, destructive: false);
    }

    /// <summary>
    /// A two-button question: true when <paramref name="yes"/> was chosen. The texts
    /// are verbs of what happens ("Eliminar" / "Cancelar"), never "Sí" / "No".
    /// </summary>
    public static bool Confirm(string message, string yes, string no = "Cancelar",
        string? title = null, bool destructive = false)
    {
        var chosen = Run(message, title, destructive ? NoticeKind.Warning : NoticeKind.Question,
            [no, yes], defaultIndex: 1, cancelIndex: 0, destructive);
        return chosen == 1;
    }

    /// <summary>Three ways out: true = yes, false = no, null = cancel or the window was closed.</summary>
    public static bool? Choose(string message, string yes, string no,
        string cancel = "Cancelar", string? title = null)
    {
        var chosen = Run(message, title, NoticeKind.Question,
            [cancel, no, yes], defaultIndex: 2, cancelIndex: 0, destructive: false);
        return chosen switch { 2 => true, 1 => false, _ => null };
    }

    private static int Run(string message, string? title, NoticeKind kind, IReadOnlyList<string> buttons,
        int defaultIndex, int cancelIndex, bool destructive)
    {
        var app = Application.Current;
        if (app is null)
            return -1;
        if (!app.Dispatcher.CheckAccess())
            return app.Dispatcher.Invoke(() => Run(message, title, kind, buttons, defaultIndex, cancelIndex, destructive));

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
