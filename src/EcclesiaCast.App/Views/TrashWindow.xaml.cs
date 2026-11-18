using System.Windows;
using System.Windows.Controls;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Songs;

namespace EcclesiaCast.App.Views;

/// <summary>The songs and themes that were deleted in the last 30 days, to bring back or erase.</summary>
public partial class TrashWindow : Window
{
    private sealed record Entry(bool IsSong, int Id, string Title, string Detail);

    private readonly ISongRepository _songs;
    private readonly IThemeRepository _themes;

    public TrashWindow(ISongRepository songs, IThemeRepository themes)
    {
        InitializeComponent();
        _songs = songs;
        _themes = themes;
        Reload();
    }

    private Entry? Selected => TrashList.SelectedItem as Entry;

    private void Reload()
    {
        var now = DateTime.UtcNow;
        var entries = new List<(DateTime When, Entry Entry)>();

        foreach (var song in _songs.GetDeleted())
            entries.Add((song.DeletedAt ?? now, new Entry(true, song.Id, "Canción: " + song.Title, Describe(song.DeletedAt, now))));
        foreach (var theme in _themes.GetDeleted())
            entries.Add((theme.DeletedAt ?? now, new Entry(false, theme.Id, "Tema: " + theme.Name, Describe(theme.DeletedAt, now))));

        TrashList.ItemsSource = entries.OrderByDescending(e => e.When).Select(e => e.Entry).ToList();
        EmptyText.Visibility = entries.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        EmptyButton.IsEnabled = entries.Count > 0;
        UpdateButtons();
    }

    private static string Describe(DateTime? deletedAtUtc, DateTime nowUtc)
    {
        if (deletedAtUtc is not { } at)
            return string.Empty;

        var days = TrashPolicy.DaysLeft(at, nowUtc);
        return $"Borrada el {at.ToLocalTime():dd/MM/yyyy HH:mm} · "
            + (days == 0 ? "se borra del todo hoy" : days == 1 ? "queda 1 día" : $"quedan {days} días");
    }

    private void UpdateButtons()
    {
        RestoreButton.IsEnabled = Selected is not null;
        PurgeButton.IsEnabled = Selected is not null;
    }

    private void TrashList_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateButtons();

    private void Restore_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is not { } entry)
            return;

        if (entry.IsSong)
            _songs.Restore(entry.Id);
        else
            _themes.Restore(entry.Id);
        Reload();
    }

    private void Purge_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is not { } entry)
            return;

        var answer = EcclesiaCast.Presentation.Services.Notice.Confirm(
            $"¿Borrar del todo «{entry.Title}»?\n\nEsto no se puede deshacer.",
            "Borrar del todo", destructive: true);
        if (!answer)
            return;

        if (entry.IsSong)
            _songs.Purge(entry.Id);
        else
            _themes.Purge(entry.Id);
        Reload();
    }

    private void Empty_Click(object sender, RoutedEventArgs e)
    {
        var count = TrashList.Items.Count;
        var answer = EcclesiaCast.Presentation.Services.Notice.Confirm(
            $"¿Vaciar la papelera? Se borran del todo {count} elemento(s).\n\nEsto no se puede deshacer.",
            "Vaciar la papelera", destructive: true);
        if (!answer)
            return;

        var far = DateTime.UtcNow.AddYears(100);
        _songs.PurgeOlderThan(far);
        _themes.PurgeOlderThan(far);
        Reload();
    }
}
