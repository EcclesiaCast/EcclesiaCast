using CommunityToolkit.Mvvm.ComponentModel;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Songs;

namespace EcclesiaCast.Presentation.ViewModels;

/// <summary>One line of the trash: a song or a theme that was deleted.</summary>
public sealed record TrashEntry(bool IsSong, int Id, string Title, string Detail);

/// <summary>
/// Backs the trash window: lists what was deleted in the last 30 days, brings
/// it back, or erases it for good (asking first). After every change the list
/// is read again.
/// </summary>
public sealed partial class TrashViewModel : ObservableObject
{
    private readonly ISongRepository _songs;
    private readonly IThemeRepository _themes;

    public TrashViewModel(ISongRepository songs, IThemeRepository themes)
    {
        _songs = songs;
        _themes = themes;
        Reload();
    }

    /// <summary>The newest deleted first.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty), nameof(CanEmpty))]
    private IReadOnlyList<TrashEntry> _entries = [];

    /// <summary>The line the operator chose, or null.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanRestore), nameof(CanPurge))]
    private TrashEntry? _selected;

    public bool IsEmpty => Entries.Count == 0;

    public bool CanRestore => Selected is not null;

    public bool CanPurge => Selected is not null;

    public bool CanEmpty => !IsEmpty;

    /// <summary>«Restaurar».</summary>
    public void RestoreSelected()
    {
        if (Selected is { } entry)
            Restore(entry);
    }

    /// <summary>«Borrar del todo», asking first.</summary>
    public void PurgeSelected()
    {
        if (Selected is { } entry)
            Purge(entry);
    }

    /// <summary>Reads the trash again.</summary>
    public void Reload() => Reload(DateTime.UtcNow);

    internal void Reload(DateTime nowUtc)
    {
        var entries = new List<(DateTime When, TrashEntry Entry)>();

        foreach (var song in _songs.GetDeleted())
            entries.Add((song.DeletedAt ?? nowUtc, new TrashEntry(true, song.Id, "Canción: " + song.Title, Describe(song.DeletedAt, nowUtc))));
        foreach (var theme in _themes.GetDeleted())
            entries.Add((theme.DeletedAt ?? nowUtc, new TrashEntry(false, theme.Id, "Tema: " + theme.Name, Describe(theme.DeletedAt, nowUtc))));

        // The list is a new one, so nothing in it is chosen yet.
        Selected = null;
        Entries = entries.OrderByDescending(e => e.When).Select(e => e.Entry).ToList();
    }

    internal static string Describe(DateTime? deletedAtUtc, DateTime nowUtc)
    {
        if (deletedAtUtc is not { } at)
            return string.Empty;

        var days = TrashPolicy.DaysLeft(at, nowUtc);
        return $"Borrada el {at.ToLocalTime():dd/MM/yyyy HH:mm} · "
            + (days == 0 ? "se borra del todo hoy" : days == 1 ? "queda 1 día" : $"quedan {days} días");
    }

    /// <summary>Brings the entry back to the library.</summary>
    public void Restore(TrashEntry entry)
    {
        if (entry.IsSong)
            _songs.Restore(entry.Id);
        else
            _themes.Restore(entry.Id);
        Reload();
    }

    /// <summary>Asks, and erases the entry for good if the operator says yes. True when it was erased.</summary>
    public bool Purge(TrashEntry entry)
    {
        var answer = Notice.Confirm(
            $"¿Borrar del todo «{entry.Title}»?\n\nEsto no se puede deshacer.",
            "Borrar del todo", destructive: true);
        if (!answer)
            return false;

        if (entry.IsSong)
            _songs.Purge(entry.Id);
        else
            _themes.Purge(entry.Id);
        Reload();
        return true;
    }

    /// <summary>Asks, and erases everything in the trash for good if the operator says yes. True when it was erased.</summary>
    public bool EmptyAll()
    {
        var answer = Notice.Confirm(
            $"¿Vaciar la papelera? Se borran del todo {Entries.Count} elemento(s).\n\nEsto no se puede deshacer.",
            "Vaciar la papelera", destructive: true);
        if (!answer)
            return false;

        var far = DateTime.UtcNow.AddYears(100);
        _songs.PurgeOlderThan(far);
        _themes.PurgeOlderThan(far);
        Reload();
        return true;
    }
}
