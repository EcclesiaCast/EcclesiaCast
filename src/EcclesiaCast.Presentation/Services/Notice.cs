using EcclesiaCast.Presentation.Platform;

namespace EcclesiaCast.Presentation.Services;

/// <summary>
/// Notices and questions in the program's own look, in place of the grey
/// system message boxes. Safe to call from any thread. Shown by the
/// <see cref="INotices"/> each interface sets at start-up.
/// </summary>
public static class Notice
{
    /// <summary>Written inside a message, the window draws there the «goes on top of everything» icon.</summary>
    public const string OnTopIconMark = "{icono-encima}";

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

    /// <summary>-1 when there is no screen to ask on (the tests).</summary>
    private static int Run(string message, string? title, NoticeKind kind, IReadOnlyList<string> buttons,
        int defaultIndex, int cancelIndex, bool destructive) =>
        PlatformServices.Notices?.Ask(message, title, kind, buttons, defaultIndex, cancelIndex, destructive) ?? -1;
}
