using CommunityToolkit.Mvvm.ComponentModel;
using EcclesiaCast.Core.Themes;
using EcclesiaCast.Data.Persistence;

namespace EcclesiaCast.Presentation.ViewModels;

/// <summary>
/// «Dividir» beside the Bible's background button, and the same on the phone:
/// the «Dividir versículos largos» of the Bible theme in use, changed without
/// opening the theme editor. Every verse, those of the playlist too, is drawn
/// with the default Bible theme, so that is the one read and saved.
/// </summary>
public sealed partial class MainViewModel
{
    // A number being typed passes through others on its way («2», «20»,
    // «200»): the slides are only cut again once it stops changing.
    private static readonly TimeSpan VerseSplitTypingPause = TimeSpan.FromMilliseconds(700);

    private IUiTimer? _verseSplitTimer;
    private bool _syncingVerseSplit;
    private int _lastVerseSplitChars;

    [ObservableProperty]
    private bool _bibleSplitOn;

    [ObservableProperty]
    private double _bibleSplitChars = Core.Bible.VerseSplitter.DefaultMaxChars;

    /// <summary>False only with no Bible theme in the library to save the choice in.</summary>
    [ObservableProperty]
    private bool _canSplitBibleVerses;

    /// <summary>Reads the theme again: at the start, with each passage and after the theme editor.</summary>
    private void SyncBibleSplit()
    {
        // A number still waiting to be saved goes first, so it isn't read over.
        if (_verseSplitTimer is { IsEnabled: true })
            return;

        var theme = DefaultBibleTheme;
        if (theme.VerseSplitMaxChars > 0)
            _lastVerseSplitChars = theme.VerseSplitMaxChars;

        _syncingVerseSplit = true;
        CanSplitBibleVerses = theme.Id != 0;
        BibleSplitOn = theme.VerseSplitMaxChars > 0;
        BibleSplitChars = BibleVerseSplit.ShownChars(theme, _lastVerseSplitChars);
        _syncingVerseSplit = false;
    }

    partial void OnBibleSplitOnChanged(bool value)
    {
        if (!_syncingVerseSplit)
            SaveBibleSplit();
    }

    partial void OnBibleSplitCharsChanged(double value)
    {
        if (_syncingVerseSplit)
            return;

        if (_verseSplitTimer is null)
        {
            _verseSplitTimer = _timers.Create(VerseSplitTypingPause);
            _verseSplitTimer.Tick += (_, _) => SaveBibleSplit();
        }

        _verseSplitTimer.Stop();
        _verseSplitTimer.Start();
    }

    /// <summary>Saves what the bar says in the theme and cuts the open passage again, keeping the live verse.</summary>
    private void SaveBibleSplit()
    {
        _verseSplitTimer?.Stop();
        var chars = (int)BibleSplitChars;
        if (BibleSplitOn)
            _lastVerseSplitChars = chars;

        var changed = BibleVerseSplit.Save(
            _themes, ThemeSeeder.GetDefaultId(_settings, ThemeSeeder.DefaultBibleThemeKey), BibleSplitOn, chars);
        SyncBibleSplit();
        if (!changed)
            return;

        if (IsBibleOpen && _currentPassage is { } passage)
        {
            // Cutting the verses again is not leaving the order of the service.
            var playlistPlace = _currentPlaylistIndex;
            RebuildSlidesPreservingLive(() => LoadBiblePassage(passage));
            _currentPlaylistIndex = playlistPlace;
        }

        StatusText = BibleSplitOn
            ? $"Versículos largos divididos en diapositivas de hasta {(int)BibleSplitChars} caracteres (queda en el tema de la Biblia)."
            : "Los versículos ya no se dividen por cantidad de caracteres (queda en el tema de la Biblia).";
    }

    /// <summary>The phone's «Dividir»: 0 turns it off, −1 on with the number there is, any other is the number.</summary>
    private void SetBibleSplitFromRemote(int value)
    {
        if (!CanSplitBibleVerses)
            return;

        _verseSplitTimer?.Stop();
        _syncingVerseSplit = true;
        BibleSplitOn = value != 0;
        if (value > 0)
            BibleSplitChars = BibleVerseSplit.MaxCharsFor(true, value);
        _syncingVerseSplit = false;
        SaveBibleSplit();
    }
}
