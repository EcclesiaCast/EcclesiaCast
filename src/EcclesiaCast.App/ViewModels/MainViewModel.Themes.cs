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
    // ── Pantallas ────────────────────────────────────────────────

    [RelayCommand]
    private void RefreshDisplays()
    {
        var saved = SelectedDisplay?.Info.DeviceName ?? _settings.Get(OutputDisplayKey);
        var savedStage = SelectedStageDisplay?.Info.DeviceName ?? _settings.Get(StageDisplayKey);

        Displays.Clear();
        StageDisplays.Clear();
        var all = _displayProvider.GetDisplays();
        for (var i = 0; i < all.Count; i++)
        {
            var d = all[i];
            var label = $"Pantalla {i + 1} · {d.Width}×{d.Height}{(d.IsPrimary ? " (principal)" : string.Empty)}";
            Displays.Add(new DisplayOption(d, label));
            StageDisplays.Add(new DisplayOption(d, label));
        }

        // The last entry opens no window at all: the words go only to the pages
        // for OBS. The stage never lands on it, so it stays out of that list.
        Displays.Add(new DisplayOption(BroadcastOnlyOutput.For(all), BroadcastOnlyOutput.Label));

        // Prefer the remembered display, then the first secondary one; the
        // stream-only entry is only ever picked by hand.
        SelectedDisplay =
            Displays.FirstOrDefault(o => o.Info.DeviceName == saved)
            ?? Displays.FirstOrDefault(o => !o.BroadcastOnly && !o.Info.IsPrimary)
            ?? Displays.FirstOrDefault();

        // The stage goes anywhere but the congregation's screen.
        SelectedStageDisplay =
            StageDisplays.FirstOrDefault(o => o.Info.DeviceName == savedStage)
            ?? StageDisplays.FirstOrDefault(o => o.Info.DeviceName != SelectedDisplay?.Info.DeviceName);
    }

    // ── Temas ────────────────────────────────────────────────────

    private SlideTheme DefaultSongTheme =>
        Resolve(ThemeSeeder.GetDefaultId(_settings, ThemeSeeder.DefaultSongThemeKey));

    private SlideTheme DefaultBibleTheme =>
        Resolve(ThemeSeeder.GetDefaultId(_settings, ThemeSeeder.DefaultBibleThemeKey));

    /// <summary>
    /// Whether «Selah» and the Psalm headings are shown, as the Bible theme
    /// says. Every place that shows a verse's text asks, so the search results
    /// read like the slides do.
    /// </summary>
    private bool ShowSelahAndTitles => DefaultBibleTheme.ShowSelahAndTitles;

    private SlideTheme Resolve(int? themeId) =>
        (themeId is int id ? _themes.Get(id) : null) ?? SlideTheme.Fallback;

    private SlideTheme ResolveSongTheme(Song song) =>
        song.ThemeId is int id ? _themes.Get(id) ?? DefaultSongTheme : DefaultSongTheme;

    [RelayCommand]
    private void OpenThemes()
    {
        if (!_themeManager.Show())
            return;

        // Re-render whatever is on the grid with the fresh themes, keeping
        // the live slide projected.
        RebuildSlidesPreservingLive(() =>
        {
            if (_currentPassage is not null)
                LoadBiblePassage(_currentPassage);
            else if (SelectedSong is not null)
                BuildSongSlides();
        });
        StatusText = "Temas actualizados.";
    }

    /// <summary>Rebuilds the slide grid and re-projects the slide that was live.</summary>
    private void RebuildSlidesPreservingLive(Action rebuild)
    {
        var live = LiveSlideIndex >= 0 && LiveSlideIndex < Slides.Count
            ? Slides[LiveSlideIndex]
            : null;

        // Which part of a split verse was live, to land on the same one.
        var part = live?.Verses is { } before
            ? live.Index - FindVerseSlide(before.Chapter, before.FirstVerse)
            : 0;

        // The verse marked as next stays marked too, instead of falling back
        // to the chapter's first one.
        var previewed = BibleSlideAt(PreviewSlideIndex);

        // The slide last projected is only put up again when it really is on
        // the air: otherwise (cleared, black, a page or another media took the
        // screen) the rebuild would bring a dismissed slide back by itself.
        var wasOnAir = IsGridOnAir;

        rebuild();

        if (previewed is not null && FindVerseSlide(previewed.Chapter, previewed.FirstVerse) is var marked and >= 0)
            SetPreviewIndex(marked);

        if (live is null || !wasOnAir)
            return;

        // A verse is found by its number, not its label: after a theme change
        // it may take more or fewer slides, and its label changes with it.
        int index;
        if (live.Verses is { } verses)
        {
            index = FindVerseSlide(verses.Chapter, verses.FirstVerse);
            if (index >= 0 && part > 0 && index + part < Slides.Count
                && Slides[index + part].Verses?.FirstVerse == verses.FirstVerse)
                index += part;
        }
        else
        {
            index = Slides.ToList().FindIndex(s => s.Label == live.Label);
        }

        if (index >= 0)
            GoLiveSlide(index);
    }
}
