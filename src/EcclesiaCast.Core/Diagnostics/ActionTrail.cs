using System.Globalization;
using System.Text;

namespace EcclesiaCast.Core.Diagnostics;

/// <summary>
/// The last few things the operator (or the program on their behalf) did, so
/// that when the window freezes the log can say what it was busy with. It is
/// written from the window's thread and read from the watchdog's, hence the lock.
/// </summary>
public sealed class ActionTrail(int capacity = 12)
{
    /// <summary>The trail the whole program writes to.</summary>
    public static ActionTrail Shared { get; } = new();

    private readonly object _gate = new();
    private readonly Queue<(DateTimeOffset At, string Text)> _entries = new();

    public void Note(string text) => Note(text, DateTimeOffset.Now);

    public void Note(string text, DateTimeOffset at)
    {
        lock (_gate)
        {
            _entries.Enqueue((at, text));
            while (_entries.Count > capacity)
                _entries.Dequeue();
        }
    }

    /// <summary>The entries, oldest first.</summary>
    public IReadOnlyList<(DateTimeOffset At, string Text)> Snapshot()
    {
        lock (_gate)
            return _entries.ToArray();
    }

    /// <summary>
    /// One line, newest first, each with how long before <paramref name="now"/>
    /// it happened: «hace 3.2 s: Proyectar «Juan 8:14» | hace 9.0 s: …».
    /// </summary>
    public string Describe(DateTimeOffset now)
    {
        var entries = Snapshot();
        if (entries.Count == 0)
            return "(sin acciones anotadas)";

        var text = new StringBuilder();
        for (var i = entries.Count - 1; i >= 0; i--)
        {
            if (text.Length > 0)
                text.Append(" | ");
            var ago = Math.Max(0, (now - entries[i].At).TotalSeconds);
            text.Append(CultureInfo.InvariantCulture, $"hace {ago:0.0} s: {entries[i].Text}");
        }

        return text.ToString();
    }
}
