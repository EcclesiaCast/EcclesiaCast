using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EcclesiaCast.App.Remote;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Bible;
using EcclesiaCast.Core.Diagnostics;
using EcclesiaCast.Core.Displays;
using EcclesiaCast.Core.Logos;
using EcclesiaCast.Core.Media;
using EcclesiaCast.Core.Performance;
using EcclesiaCast.Core.Playlists;
using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Core.Songs;
using EcclesiaCast.Core.Stage;
using EcclesiaCast.Core.Timing;
using EcclesiaCast.Core.Themes;
using EcclesiaCast.Data.Persistence;
using Serilog;

namespace EcclesiaCast.App.ViewModels;

public sealed partial class MainViewModel
{
    // ── Búsqueda global ──────────────────────────────────────────

    /// <summary>
    /// One box over the whole library. Each panel had its own search, so
    /// finding a word meant first guessing whether it was a song, a verse or
    /// a video — and mid-service there is no time to guess.
    /// </summary>
    [RelayCommand]
    private void SearchEverything()
    {
        var hit = _globalSearch.Show(SearchEverywhere, SearchText);
        if (hit is not null)
            OpenSearchHit(hit);
    }

    /// <summary>Loads what a search result points at (song, verse or media), as the operator's box and the phone's both do.</summary>
    private void OpenSearchHit(SearchHit hit)
    {
        switch (hit.Kind)
        {
            case SearchHitKind.Song when Songs.FirstOrDefault(s => s.Id == hit.Id) is { } song:
                IsBibleTabActive = false;
                SelectedSong = song;
                StatusText = $"«{song.Title}» cargada. Doble clic o Enter para proyectar.";
                break;

            case SearchHitKind.Verse:
                IsBibleTabActive = true;
                // The verse opens in the version it was found in, not in
                // whichever one happened to be selected.
                if (BibleVersionOptions.FirstOrDefault(o => o.Info.Id == hit.Id) is { } found)
                    SelectSoloVersion(found);
                BibleQuery = hit.Reference ?? hit.Title;
                StatusText = $"{hit.Title} cargado.";
                break;

            case SearchHitKind.Media when MediaItems.FirstOrDefault(m => m.Id == hit.Id) is { } media:
                SelectedMediaTab = media.Category;
                ApplyBackground(media);
                break;
        }
    }

    /// <summary>
    /// Looks through songs, every Bible version and the media library at
    /// once. Capped per kind so one enormous group cannot bury the others.
    /// </summary>
    private IReadOnlyList<SearchHit> SearchEverywhere(string query)
    {
        var hits = new List<SearchHit>();

        foreach (var song in _songs.Search(query).Take(12))
        {
            hits.Add(new SearchHit(
                SearchHitKind.Song, song.Id, "♪", song.Title,
                string.IsNullOrWhiteSpace(song.Artist) ? "Canción" : song.Artist));
        }

        // Every installed version is searched, and each result says which one
        // it came from: the same phrase reads differently in each translation.
        // The cap is per version so one of them cannot bury the others.
        var perVersion = Math.Clamp(24 / Math.Max(1, BibleVersionOptions.Count), 4, 12);
        foreach (var option in BibleVersionOptions)
        {
            foreach (var verse in BibleTextCleaner.ForList(_bibles.SearchText(option.Info.Id, query, perVersion), ShowSelahAndTitles))
            {
                hits.Add(new SearchHit(
                    SearchHitKind.Verse, option.Info.Id, "†",
                    $"{verse.Reference} · {option.Info.Abbreviation}", Shorten(verse.Text),
                    verse.BookNumber, verse.Chapter, verse.Verse, verse.Reference));
            }
        }

        foreach (var media in MediaItems.Where(m => m.Name.Contains(query, StringComparison.OrdinalIgnoreCase)).Take(8))
        {
            hits.Add(new SearchHit(
                SearchHitKind.Media, media.Id,
                media.Type switch { MediaType.Image => "▣", MediaType.Web => "🌐", _ => "▶" }, media.Name, media.Category));
        }

        return hits;
    }

    /// <summary>
    /// The screen that counts down to the start of the service. What the
    /// operator typed is remembered, because a church starts at the same time
    /// every Sunday and nobody should retype "Empezamos en" every week.
    /// </summary>
    [RelayCommand]
    private void OpenCountdown()
    {
        var saved = new CountdownSettings(
            int.TryParse(_settings.Get(CountdownMinutesKey), out var minutes) && minutes > 0
                ? minutes
                : CountdownSettings.Default.Minutes,
            _settings.Get(CountdownClockKey) ?? CountdownSettings.Default.Clock,
            _settings.Get(CountdownHeadingKey) ?? CountdownSettings.Default.Heading,
            _settings.Get(CountdownFinishedKey) ?? CountdownSettings.Default.FinishedMessage);

        var choice = _countdownDialog.Show(saved, _presentation.Countdown is not null);
        if (choice is null)
            return;

        if (choice.Stop)
        {
            _presentation.StopCountdown();
            StatusText = "Cuenta regresiva detenida.";
            return;
        }

        _settings.Set(CountdownMinutesKey, choice.Settings.Minutes.ToString());
        _settings.Set(CountdownClockKey, choice.Settings.Clock);
        _settings.Set(CountdownHeadingKey, choice.Settings.Heading);
        _settings.Set(CountdownFinishedKey, choice.Settings.FinishedMessage);

        if (choice.Countdown is not { } countdown)
            return;

        _presentation.StartCountdown(countdown);
        StatusText = $"Cuenta regresiva: {countdown.Format(DateTimeOffset.Now)}.";
    }

    /// <summary>
    /// The whole library is one file, so a backup is one file too. Volunteers
    /// were being told to go find it in AppData; now it is a menu item.
    /// </summary>
    [RelayCommand]
    private void SaveBackup() => _backup.SaveBackup();

    [RelayCommand]
    private void RestoreBackup() => _backup.RestoreBackup();

    [RelayCommand]
    private void OpenLogos()
    {
        if (!_logoManager.Show())
            return;

        LoadLogos(null);
        RefreshLightCopies();
        // Re-apply so the output picks up an edited logo without a toggle.
        _presentation.SetActiveLogo(SelectedLogo);
        StatusText = "Logos actualizados.";
    }

    /// <summary>
    /// Puts the active logo behind the lyrics as a background, blurred by the
    /// amount set for it — the usual "our logo, softened, under the words".
    /// </summary>
    [RelayCommand]
    private void UseLogoAsBackground()
    {
        if (SelectedLogo is not { CanBeBackground: true } logo)
        {
            StatusText = "Para usarlo de fondo, el logo tiene que ser una imagen o un video.";
            return;
        }

        _presentation.SetBackground(LogoBackground(logo));
        BackgroundBlur = logo.BackgroundBlur;

        // The logo is a background now, not the Logo state.
        if (IsLogoActive)
            _presentation.ToggleLogo();

        EnsureOutputOn();
        StatusText = $"«{logo.Name}» de fondo, con {logo.BackgroundBlur:0} % de desenfoque.";
    }

    /// <summary>
    /// Wraps a logo as a background media item. Its id is negative so the rest
    /// of the app can tell it apart from a real library item and never tries
    /// to write it back to the media table.
    /// </summary>
    private static MediaItem LogoBackground(Logo logo) => new()
    {
        Id = -1000 - logo.Id,
        Name = logo.Name,
        Path = logo.Path!,
        Type = logo.Kind == LogoKind.Video ? MediaType.Video : MediaType.Image,
        ThumbnailPath = logo.Kind == LogoKind.Video ? logo.PosterPath : logo.Path,
        Scaling = logo.Scaling,
        Behavior = MediaBehavior.Background,
        EndBehavior = VideoEndBehavior.Loop,
        Muted = true,
        Blur = logo.BackgroundBlur,
    };
}
