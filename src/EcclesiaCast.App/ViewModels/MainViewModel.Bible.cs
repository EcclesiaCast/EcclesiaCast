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
    // ── Biblia ───────────────────────────────────────────────────

    /// <summary>The version projected as main text (the first one checked).</summary>
    private BibleVersionInfo? PrimaryVersion =>
        _checkedVersionIds.Count > 0
            ? BibleVersionOptions.FirstOrDefault(o => o.Info.Id == _checkedVersionIds[0])?.Info
            : null;

    /// <summary>The second checked version, shown below the main text.</summary>
    private BibleVersionInfo? SecondaryVersion =>
        _checkedVersionIds.Count > 1
            ? BibleVersionOptions.FirstOrDefault(o => o.Info.Id == _checkedVersionIds[1])?.Info
            : null;

    private void OnVersionOptionChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_suppressVersionEvents
            || e.PropertyName != nameof(BibleVersionOption.IsSelected)
            || sender is not BibleVersionOption option)
            return;

        if (option.IsSelected)
        {
            _checkedVersionIds.Add(option.Info.Id);

            // Up to two versions at a time: a third takes the place of the
            // second. The first one stays — it is the version being read.
            while (_checkedVersionIds.Count > 2)
            {
                var replacedId = _checkedVersionIds[1];
                _checkedVersionIds.RemoveAt(1);
                var replaced = BibleVersionOptions.FirstOrDefault(o => o.Info.Id == replacedId);
                if (replaced is not null)
                {
                    _suppressVersionEvents = true;
                    replaced.IsSelected = false;
                    _suppressVersionEvents = false;
                }
            }
        }
        else
        {
            _checkedVersionIds.Remove(option.Info.Id);
        }

        RefreshAfterVersionChange();
    }

    /// <summary>
    /// A click on a version uses only that one; Ctrl+click adds it as the
    /// second version (in place of the second, if there were two already),
    /// or takes it out if it was already active.
    /// </summary>
    [RelayCommand]
    private void ChooseVersion(BibleVersionOption? option)
    {
        if (option is null)
            return;

        if ((System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == 0)
        {
            SelectSoloVersion(option);
            return;
        }

        SelectedBibleVersionOption = option;
        // The last active version cannot be taken out: something must project.
        if (option.IsSelected && _checkedVersionIds.Count <= 1)
            return;

        // The change handler keeps the list of active ids, the badges and the grid.
        option.IsSelected = !option.IsSelected;
    }

    /// <summary>Marks which active version is the first and which the second.</summary>
    private void UpdateVersionBadges()
    {
        foreach (var option in BibleVersionOptions)
        {
            var position = _checkedVersionIds.IndexOf(option.Info.Id);
            option.IsPrimary = position == 0;
            option.IsSecondary = position == 1;
        }
    }

    private void SelectSoloVersion(BibleVersionOption? option)
    {
        if (option is null)
            return;

        SelectedBibleVersionOption = option;

        _suppressVersionEvents = true;
        foreach (var other in BibleVersionOptions)
            other.IsSelected = other == option;
        _suppressVersionEvents = false;

        _checkedVersionIds.Clear();
        _checkedVersionIds.Add(option.Info.Id);

        RefreshAfterVersionChange();
    }

    /// <summary>
    /// Refreshes the grid and — if a verse is live — re-projects it right
    /// away, so version changes update the output in real time.
    /// </summary>
    private void RefreshAfterVersionChange()
    {
        UpdateVersionBadges();
        RebuildSlidesPreservingLive(() =>
        {
            LoadBibleBooksAvailable();
            if (_currentPassage is not null)
                LoadBiblePassage(_currentPassage);
        });
    }

    private void LoadBibleBooksAvailable()
    {
        var keepNumber = SelectedBibleBook?.Number;
        BibleBooksAvailable.Clear();
        SelectedBibleBook = null;

        if (PrimaryVersion is null)
        {
            UpdateBibleViewState();
            return;
        }

        foreach (var number in _bibles.GetAvailableBookNumbers(PrimaryVersion.Id))
        {
            var info = BibleBookCatalog.FindByNumber(number);
            if (info is not null)
                BibleBooksAvailable.Add(info);
        }

        if (keepNumber is int n)
            SelectedBibleBook = BibleBooksAvailable.FirstOrDefault(b => b.Number == n);

        UpdateBibleViewState();
    }

    partial void OnSelectedBibleBookChanged(BibleBookInfo? value)
    {
        BibleChapters.Clear();
        ChaptersTitle = value?.Name ?? string.Empty;

        if (value is not null && PrimaryVersion is not null)
        {
            foreach (var chapter in _bibles.GetChapterNumbers(PrimaryVersion.Id, value.Number))
                BibleChapters.Add(new ChapterOption(chapter));
        }

        UpdateBibleViewState();
    }

    [RelayCommand]
    private void BackToBooks() => SelectedBibleBook = null;

    [RelayCommand]
    private void SelectChapter(ChapterOption? option)
    {
        if (option is null || SelectedBibleBook is null)
            return;

        // Browsing and typing a reference are two paths to the same grid;
        // clear the search box so they don't fight visually.
        BibleQuery = string.Empty;
        LoadBiblePassage(new BibleReference(SelectedBibleBook.Number, option.Number, null, null));
    }

    private void MarkChapterSelected(int? chapter)
    {
        foreach (var option in BibleChapters)
            option.IsSelected = option.Number == chapter;
    }

    /// <summary>Which of the three views (books / chapters / results) the Bible tab shows.</summary>
    private void UpdateBibleViewState()
    {
        ShowBibleResults = BibleSearchResults.Count > 0;
        ShowChaptersView = !ShowBibleResults && SelectedBibleBook is not null;
        ShowBooksList = !ShowBibleResults && !ShowChaptersView;
    }

    partial void OnBibleQueryChanged(string value)
    {
        // The text changed, so the arrows are for editing it again.
        ArrowsDriveSlidesFromSearch = false;
        RunBibleQuery();
    }

    private void RunBibleQuery()
    {
        BibleSearchResults.Clear();

        if (PrimaryVersion is null)
        {
            BibleStatusText = "Importá una Biblia con 📥 y marcá su casilla.";
            UpdateBibleViewState();
            return;
        }

        if (string.IsNullOrWhiteSpace(BibleQuery))
        {
            FilterBooks(null);
            BibleStatusText = BibleHint;
            UpdateBibleViewState();
            return;
        }

        // "?" asks for the words inside the verses. Everything else is taken
        // as a book, chapter and verse: that is what gets typed nine times
        // out of ten, and searching the text for "Juan" was never the point.
        var query = BibleQuery.Trim();
        if (query.StartsWith('?'))
        {
            FilterBooks(null);
            SearchBibleText(query[1..].Trim());
            UpdateBibleViewState();
            return;
        }

        var reference = BibleReferenceParser.TryParse(BibleQuery);
        if (reference is not null)
        {
            FilterBooks(null);
            UpdateBibleViewState();

            // Park the left panel on the referenced book (chapter grid view).
            if (SelectedBibleBook?.Number != reference.BookNumber)
                SelectedBibleBook = BibleBooksAvailable.FirstOrDefault(b => b.Number == reference.BookNumber);

            // Always load the whole chapter; the referenced verse becomes
            // the "next up" selection, projected with Enter.
            LoadBiblePassage(new BibleReference(reference.BookNumber, reference.Chapter, null, null));

            if (reference.VerseStart is int verse)
            {
                var index = FindVerseSlide(reference.Chapter, verse);
                SetPreviewIndex(index);
                if (index >= 0)
                    BibleStatusText = $"Versículo {verse} seleccionado — Enter lo proyecta.";
            }
            else
            {
                SetPreviewIndex(Slides.FirstOrDefault(s => s.JumpTarget is null)?.Index ?? -1);
            }
            return;
        }

        // Halfway through a reference ("juan 3:" before the verse): leave the
        // chapter grid and the slides as they are, the next key finishes it.
        if (System.Text.RegularExpressions.Regex.IsMatch(query, @"\p{L}.*\d"))
        {
            var bookPart = System.Text.RegularExpressions.Regex.Match(query, @"^\s*(\d\s*)?\p{L}[\p{L}\s.]*").Value;
            BibleStatusText = BibleBookCatalog.FindByName(bookPart) is null
                ? $"No reconozco el libro «{bookPart.Trim()}»."
                : "Seguí escribiendo: «juan 3 16» o «juan 3:16».";
            UpdateBibleViewState();
            return;
        }

        // Not a full reference yet: narrow the book list to what fits. A
        // single fit opens its chapters straight away, so "juan" already
        // shows Juan's chapters before the number is typed.
        var fits = FilterBooks(query);
        if (fits.Count == 1)
        {
            if (SelectedBibleBook?.Number != fits[0].Number)
                SelectedBibleBook = fits[0];
            BibleStatusText = $"{fits[0].Name}: escribí el capítulo («{fits[0].Name} 3») o elegilo abajo.";
        }
        else
        {
            SelectedBibleBook = null;
            BibleStatusText = fits.Count == 0
                ? $"Ningún libro empieza con «{query}». Para buscar esa palabra en los versículos: ?{query} · Ctrl+K busca en todo."
                : $"{fits.Count} libros · Enter abre {fits[0].Name}.";
        }

        UpdateBibleViewState();
    }

    private const string BibleHint =
        "Libro, capítulo y versículo: «juan 3 16», «sal 23». Con ? adelante busca en el texto (?paz). Ctrl+K busca en todo.";

    private void SearchBibleText(string words)
    {
        if (PrimaryVersion is null)
            return;

        if (words.Length < 3)
        {
            BibleStatusText = "Escribí al menos 3 letras después del ? para buscar en el texto.";
            return;
        }

        foreach (var result in BibleTextCleaner.ForList(_bibles.SearchText(PrimaryVersion.Id, words), ShowSelahAndTitles))
            BibleSearchResults.Add(result);

        BibleStatusText = BibleSearchResults.Count == 0
            ? $"Ningún versículo dice «{words}»."
            : $"{BibleSearchResults.Count} versículo(s) con «{words}». Enter proyecta el primero, doble clic cualquiera.";
    }

    /// <summary>
    /// Shows only the books that the typed text could be the start of; null
    /// shows them all again. Returns the books that fit, in biblical order.
    /// </summary>
    private List<BibleBookInfo> FilterBooks(string? typed)
    {
        var view = System.Windows.Data.CollectionViewSource.GetDefaultView(BibleBooksAvailable);
        if (string.IsNullOrWhiteSpace(typed))
        {
            if (view.Filter is not null)
                view.Filter = null;
            return BibleBooksAvailable.ToList();
        }

        view.Filter = item => item is BibleBookInfo book && BibleBookCatalog.StartsWith(book, typed);
        return BibleBooksAvailable.Where(b => BibleBookCatalog.StartsWith(b, typed)).ToList();
    }

    private void LoadBiblePassage(BibleReference reference)
    {
        var primary = PrimaryVersion;
        if (primary is null)
            return;

        NoteAction($"Abrir el capítulo {BibleBookCatalog.FindByNumber(reference.BookNumber)?.Name} {reference.Chapter} ({primary.Abbreviation})");
        var showSelah = ShowSelahAndTitles;
        var verses = BibleTextCleaner.WithoutHiddenTitles(_bibles.GetPassage(primary.Id, reference), showSelah);
        if (verses.Count == 0)
        {
            BibleStatusText = "No se encontraron versículos para esa referencia.";
            return;
        }

        var secondary = SecondaryVersion;
        Dictionary<(int Chapter, int Verse), string>? secondaryTexts = null;
        if (secondary is not null)
        {
            secondaryTexts = BibleTextCleaner.WithoutHiddenTitles(_bibles.GetPassage(secondary.Id, reference), showSelah)
                .ToDictionary(v => (v.Chapter, v.Verse), v => v.Text);
        }

        // The grid stops showing the song, so the song list stops pointing at
        // it: otherwise clicking that same song again changes nothing and the
        // chapter stays on screen.
        if (SelectedSong is not null)
            SelectedSong = null;

        _currentPassage = reference;
        Slides.Clear();
        LiveSlideIndex = -1;
        PreviewSlideIndex = -1;

        // Loading a passage by hand steps out of the service order, the same
        // way choosing a song does. Otherwise the end of the playlist's verse
        // range kept jumping to the next item while the operator was just
        // reading through the chapter on their own.
        if (!_openingPlaylistItem)
            _currentPlaylistIndex = -1;

        var theme = DefaultBibleTheme;

        // Opening card: jump back to the previous chapter (crossing into the
        // previous book's last chapter when needed). Never projected as-is.
        if (GetPreviousChapter(reference) is var (prevBook, prevChapter, prevName))
        {
            Slides.Add(new SlideItemViewModel(
                Slides.Count,
                "ANTERIOR",
                new SlideContent($"◀  {prevName} {prevChapter}", "Volver al capítulo anterior", Theme: theme),
                new BibleReference(prevBook, prevChapter, null, null),
                jumpToEnd: true));
        }

        var bookName = verses[0].BookName;
        IsWebPanelOpen = false;
        PreviewMedia = null;
        LeavePresentation();
        GridTitle = secondary is null
            ? $"{bookName} {reference.Chapter} · {primary.Abbreviation}"
            : $"{bookName} {reference.Chapter} · {primary.Abbreviation} + {secondary.Abbreviation}";

        foreach (var stored in VerseGrouping.Group(verses, secondaryTexts))
        {
            // Grouping compares the stored texts; only what reaches the
            // screen is cleaned, so the database and the search stay as they were.
            var group = stored with
            {
                Text = BibleTextCleaner.Clean(stored.Text, showSelah),
                SecondaryText = stored.SecondaryText is null ? null : BibleTextCleaner.Clean(stored.SecondaryText, showSelah),
            };
            var mainText = theme.ShowVerseNumbers ? $"{group.Numbers}  {group.Text}" : group.Text;
            var verseReference = $"{bookName} {group.Chapter}:{group.Numbers}";

            // Two versions: label each text inline with its abbreviation (like
            // BibleShow) and drop the version(s) from the reference.
            Func<string, string> label = text => text;
            Func<string, string> secondaryLabel = text => text;
            string caption;
            if (secondary is not null)
            {
                label = text => $"[{primary.Abbreviation}] {text}";
                secondaryLabel = text => $"[{secondary.Abbreviation}] {text}";
                caption = verseReference;
            }
            else
            {
                caption = theme.ShowVersionName ? $"{verseReference} · {primary.Abbreviation}" : verseReference;
            }

            // A verse too long to read in one slide goes over several,
            // rather than shrinking past the theme's minimum.
            var parts = Controls.SlideTextFit.SplitToFit(
                new SlideContent(mainText, caption, group.SecondaryText, theme), label, secondaryLabel);

            for (var part = 0; part < parts.Count; part++)
            {
                var slideLabel = $"{group.Chapter}:{group.Numbers}";
                if (parts.Count > 1)
                    slideLabel += $" ({part + 1}/{parts.Count})";

                Slides.Add(new SlideItemViewModel(
                    Slides.Count,
                    slideLabel,
                    new SlideContent(parts[part].Main, caption, parts[part].Secondary, theme))
                {
                    Verses = group,
                });
            }
        }

        // Closing card: jump to the next chapter (crossing into the next
        // book when this was the last one). Never projected as-is.
        if (GetNextChapter(reference) is var (nextBook, nextChapter, nextName))
        {
            Slides.Add(new SlideItemViewModel(
                Slides.Count,
                "SIGUIENTE",
                new SlideContent($"▶  {nextName} {nextChapter}", "Pasar al siguiente capítulo", Theme: theme),
                new BibleReference(nextBook, nextChapter, null, null)));
        }

        if (SelectedBibleBook?.Number == reference.BookNumber)
            MarkChapterSelected(reference.Chapter);

        PreviewSlide = Slides.FirstOrDefault(s => s.JumpTarget is null)?.Slide;
        BibleStatusText = $"{verses.Count} versículo(s). Clic en una diapositiva para proyectar.";
    }

    /// <summary>Previous chapter before a reference: within the book, or the previous book's last chapter.</summary>
    private (int Book, int Chapter, string Name)? GetPreviousChapter(BibleReference current)
    {
        var primary = PrimaryVersion;
        if (primary is null)
            return null;

        var previousInBook = _bibles.GetChapterNumbers(primary.Id, current.BookNumber)
            .Where(c => c < current.Chapter)
            .Cast<int?>()
            .LastOrDefault();
        if (previousInBook is int chapter)
            return (current.BookNumber, chapter, BibleBookCatalog.FindByNumber(current.BookNumber)?.Name ?? string.Empty);

        var previousBook = _bibles.GetAvailableBookNumbers(primary.Id)
            .Where(n => n < current.BookNumber)
            .Cast<int?>()
            .LastOrDefault();
        if (previousBook is int book)
        {
            var lastChapter = _bibles.GetChapterNumbers(primary.Id, book).Cast<int?>().LastOrDefault();
            if (lastChapter is int last)
                return (book, last, BibleBookCatalog.FindByNumber(book)?.Name ?? string.Empty);
        }

        return null;
    }

    /// <summary>Next chapter after a reference: within the book, or the next book's first chapter.</summary>
    private (int Book, int Chapter, string Name)? GetNextChapter(BibleReference current)
    {
        var primary = PrimaryVersion;
        if (primary is null)
            return null;

        var nextInBook = _bibles.GetChapterNumbers(primary.Id, current.BookNumber)
            .Where(c => c > current.Chapter)
            .Cast<int?>()
            .FirstOrDefault();
        if (nextInBook is int chapter)
            return (current.BookNumber, chapter, BibleBookCatalog.FindByNumber(current.BookNumber)?.Name ?? string.Empty);

        var nextBook = _bibles.GetAvailableBookNumbers(primary.Id)
            .Where(n => n > current.BookNumber)
            .Cast<int?>()
            .FirstOrDefault();
        if (nextBook is int book)
        {
            var firstChapter = _bibles.GetChapterNumbers(primary.Id, book).Cast<int?>().FirstOrDefault();
            if (firstChapter is int first)
                return (book, first, BibleBookCatalog.FindByNumber(book)?.Name ?? string.Empty);
        }

        return null;
    }

    /// <summary>Marks a slide as "next up": accent border, preview box, and scroll-into-view.</summary>
    private void SetPreviewIndex(int index)
    {
        foreach (var slide in Slides)
            slide.IsPreviewed = slide.Index == index;

        PreviewSlideIndex = index;
        if (index >= 0 && index < Slides.Count)
            PreviewSlide = Slides[index].Slide;
    }

    /// <summary>Projects the verse a typed reference points to (Enter in the search box).</summary>
    [RelayCommand]
    private void ProjectReference()
    {
        var projected = ProjectReferenceCore();

        // "?words" remembers its verse in ProjectBibleResult.
        if (projected && !(BibleQuery ?? string.Empty).TrimStart().StartsWith('?'))
            RememberProjectedVerse();

        // Set after the whole thing: the book name written into the box above
        // fires OnBibleQueryChanged, which turns it off.
        ArrowsDriveSlidesFromSearch = projected && LiveSlideIndex >= 0;
    }

    /// <summary>
    /// True while the arrows belong to the slides even though the focus is
    /// still in the search box: right after Enter projected a verse, the next
    /// verse is one arrow away (as in BibleShow). Typing, or clicking the box,
    /// gives the arrows back to the text.
    /// </summary>
    public bool ArrowsDriveSlidesFromSearch { get; set; }

    /// <summary>True when something went live.</summary>
    private bool ProjectReferenceCore()
    {
        var query = (BibleQuery ?? string.Empty).Trim();

        // "?words": Enter projects the first verse that says them.
        if (query.StartsWith('?'))
        {
            if (BibleSearchResults.FirstOrDefault() is not { } firstResult)
                return false;

            ProjectBibleResult(firstResult);
            return true;
        }

        // Only part of a book typed ("ju"): Enter opens the first book that
        // fits and leaves its name in the box, ready for the chapter number.
        if (query.Length > 0 && BibleReferenceParser.TryParse(query) is null)
        {
            var book = BibleBooksAvailable.FirstOrDefault(b => BibleBookCatalog.StartsWith(b, query));
            if (book is not null)
                BibleQuery = book.Name + " ";
            return false;
        }

        // The box can still hold a reference while the grid shows a song the
        // operator went to since. Typing the same text again doesn't reload
        // it, so Enter would have projected a slide of the song.
        if (BibleReferenceParser.TryParse(BibleQuery ?? string.Empty) is { } typed
            && (_currentPassage is null
                || _currentPassage.BookNumber != typed.BookNumber
                || _currentPassage.Chapter != typed.Chapter))
            RunBibleQuery();

        if (PreviewSlideIndex >= 0)
        {
            GoLiveSlide(PreviewSlideIndex);
            return true;
        }

        // Going live consumed the preview, so a second Enter lands here.
        // Re-derive the verse from the typed reference: without this it fell
        // back to the chapter's first verse (Enter, Enter projected 3:1).
        var reference = BibleReferenceParser.TryParse(BibleQuery ?? string.Empty);
        if (reference?.VerseStart is int verse)
        {
            var index = FindVerseSlide(reference.Chapter, verse);
            if (index >= 0)
            {
                GoLiveSlide(index);
                return true;
            }
        }

        if (Slides.FirstOrDefault(s => s.JumpTarget is null) is not { } first)
            return false;

        GoLiveSlide(first.Index);
        return true;
    }

    // ── Resaltado en vivo ────────────────────────────────────────

    /// <summary>The box holds the terms, split by «;»; the appearances pointed at on the Live panel stay.</summary>
    partial void OnHighlightTextChanged(string value) =>
        _presentation.SetHighlight(_presentation.Highlight.WithTerms(HighlightSet.SplitTerms(value)));

    /// <summary>
    /// The operator pointed at words on the Live panel (positions in the main
    /// text of the live slide). Only that appearance is marked, not every one
    /// of the same word; Shift adds it to the earlier marks, and pointing at
    /// something already marked takes it off.
    /// </summary>
    public void MarkOnLive(TextSpan mark, bool addToOthers)
    {
        var text = _presentation.CurrentSlide?.MainText;
        if (string.IsNullOrEmpty(text) || mark.End > text.Length)
            return;

        var before = _presentation.Highlight;
        var after = before.Point(mark, text, addToOthers);
        _presentation.SetHighlight(after);

        // Pointing at a typed word took it off the Resaltar box as well.
        if (after.Terms.Count != before.Terms.Count)
            HighlightText = HighlightSet.JoinTerms(after.Terms);
    }

    [RelayCommand]
    private void ClearHighlight()
    {
        HighlightText = string.Empty;
        // The marks pointed at on the Live panel are not in the box.
        _presentation.SetHighlight(HighlightSet.None);
    }

    /// <summary>
    /// A new slide on screen drops the highlight (the presentation does it),
    /// so the box in the Resaltar tab empties too instead of showing a word
    /// that is no longer marked anywhere.
    /// </summary>
    private void SyncHighlightBox()
    {
        if (_presentation.Highlight.Terms.Count == 0 && HighlightText.Length > 0)
            HighlightText = string.Empty;
    }

    [RelayCommand]
    private void ProjectBibleResult(BibleVerseResult? result)
    {
        if (result is null)
            return;

        LoadBiblePassage(new BibleReference(result.BookNumber, result.Chapter, null, null));
        var index = FindVerseSlide(result.Chapter, result.Verse);
        if (index < 0)
            return;

        GoLiveSlide(index);
        if (LiveSlideIndex == index)
            RememberVerse(new BibleReference(result.BookNumber, result.Chapter, result.Verse, null));
    }

    // ── Historial de versículos buscados ─────────────────────────

    private void LoadBibleHistory()
    {
        BibleHistory.Clear();
        foreach (var entry in BibleHistoryList.Parse(_settings.Get(BibleHistoryKey)))
            BibleHistory.Add(entry);
    }

    /// <summary>
    /// The verse that just went live from the search box: what was typed when
    /// it points at the live slide, otherwise the verses the slide shows (a
    /// paragraph of the TLA, or the chapter's first verse after a bare Enter).
    /// </summary>
    private void RememberProjectedVerse()
    {
        if (_currentPassage is null
            || LiveSlideIndex < 0 || LiveSlideIndex >= Slides.Count
            || Slides[LiveSlideIndex].Verses is not { } group)
            return;

        var typed = BibleReferenceParser.TryParse(BibleQuery ?? string.Empty);
        if (typed is { VerseStart: int start }
            && typed.BookNumber == _currentPassage.BookNumber
            && typed.Chapter == group.Chapter
            && group.Contains(start))
            RememberVerse(typed);
        else
            RememberVerse(new BibleReference(_currentPassage.BookNumber, group.Chapter, group.FirstVerse, group.LastVerse));
    }

    /// <summary>Puts a projected verse on top of the history. Arrow steps and card clicks do not come through here.</summary>
    private void RememberVerse(BibleReference reference)
    {
        if (reference.VerseStart is not int start)
            return;

        var primary = PrimaryVersion;
        var entry = new BibleHistoryEntry(
            reference.BookNumber, reference.Chapter, start,
            reference.VerseEnd is int end && end != start ? end : null,
            primary?.Id, primary?.Abbreviation ?? string.Empty,
            SecondaryVersion?.Id, DateTimeOffset.Now);

        var updated = BibleHistoryList.Add(BibleHistory, entry);
        BibleHistory.Clear();
        foreach (var kept in updated)
            BibleHistory.Add(kept);

        _settings.Set(BibleHistoryKey, BibleHistoryList.Serialize(BibleHistory));
    }

    /// <summary>
    /// Brings a history entry back: its versions when they are installed, the
    /// chapter loaded and the verse marked. Projecting is a separate step.
    /// </summary>
    [RelayCommand]
    private void OpenHistoryEntry(BibleHistoryEntry? entry)
    {
        if (entry is null)
            return;

        RestoreHistoryVersions(entry);

        // Typing the same text again would not reload anything.
        var text = BibleReferenceFormatter.Format(entry.ToReference());
        if (BibleQuery == text)
            RunBibleQuery();
        else
            BibleQuery = text;
    }

    [RelayCommand]
    private void ProjectHistoryEntry(BibleHistoryEntry? entry)
    {
        if (entry is null)
            return;

        OpenHistoryEntry(entry);
        ProjectReference();
    }

    [RelayCommand]
    private void ClearBibleHistory()
    {
        BibleHistory.Clear();
        _settings.Set(BibleHistoryKey, BibleHistoryList.Serialize(BibleHistory));
        StatusText = "Historial de versículos borrado.";
    }

    /// <summary>
    /// The version the verse was read in becomes the main one, and the second
    /// version comes back too, as long as they are still installed. When the
    /// main one is already right and the entry had no second, nothing moves.
    /// </summary>
    private void RestoreHistoryVersions(BibleHistoryEntry entry)
    {
        var primary = entry.VersionId is int primaryId
            ? BibleVersionOptions.FirstOrDefault(o => o.Info.Id == primaryId)
            : null;
        var secondary = entry.SecondaryVersionId is int secondaryId
            ? BibleVersionOptions.FirstOrDefault(o => o.Info.Id == secondaryId)
            : null;

        if (primary is not null && PrimaryVersion?.Id != primary.Info.Id)
            SelectSoloVersion(primary);

        if (secondary is not null && secondary.Info.Id != PrimaryVersion?.Id && SecondaryVersion?.Id != secondary.Info.Id)
            secondary.IsSelected = true;
    }

    private void LoadBibleVersions()
    {
        _suppressVersionEvents = true;

        var keepSelectedId = SelectedBibleVersionOption?.Info.Id;
        foreach (var option in BibleVersionOptions)
            option.PropertyChanged -= OnVersionOptionChanged;
        BibleVersionOptions.Clear();

        foreach (var version in _bibles.GetVersions())
        {
            var option = new BibleVersionOption(version)
            {
                IsSelected = _checkedVersionIds.Contains(version.Id),
            };
            option.PropertyChanged += OnVersionOptionChanged;
            BibleVersionOptions.Add(option);
        }

        // Drop checked ids whose version no longer exists.
        _checkedVersionIds.RemoveAll(id => BibleVersionOptions.All(o => o.Info.Id != id));

        // Something must be projectable: default to the first version.
        if (_checkedVersionIds.Count == 0 && BibleVersionOptions.Count > 0)
        {
            BibleVersionOptions[0].IsSelected = true;
            _checkedVersionIds.Add(BibleVersionOptions[0].Info.Id);
        }

        SelectedBibleVersionOption =
            BibleVersionOptions.FirstOrDefault(o => o.Info.Id == keepSelectedId)
            ?? BibleVersionOptions.FirstOrDefault();

        _suppressVersionEvents = false;
        UpdateVersionBadges();
        LoadBibleBooksAvailable();
    }

    [RelayCommand]
    private void ImportBible()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Importar Biblia",
            Filter = "Biblias (*.json, *.xml)|*.json;*.xml|JSON|*.json|Zefania XML|*.xml",
        };
        if (dialog.ShowDialog() != true)
            return;

        ParsedBible parsed;
        try
        {
            var content = ReadTextFile(dialog.FileName);
            parsed = Path.GetExtension(dialog.FileName).ToLowerInvariant() == ".xml"
                ? ZefaniaBibleImporter.Parse(content)
                : JsonBibleImporter.Parse(content);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Falló el análisis de la Biblia {Path}", dialog.FileName);
            MessageBox.Show(
                $"No se pudo leer el archivo:\n\n{ex.Message}",
                "EcclesiaCast", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (parsed.VerseCount == 0)
        {
            MessageBox.Show(
                "El archivo no contiene versículos reconocibles.",
                "EcclesiaCast", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var suggestion = Path.GetFileNameWithoutExtension(dialog.FileName);
        var metadata = _bibleImportDialog.Prompt(suggestion);
        if (metadata is null)
            return;

        var saved = _bibles.Import(metadata.Name, metadata.Abbreviation, metadata.Language, parsed);
        LoadBibleVersions();

        // Leave the freshly imported version active for projection.
        var imported = BibleVersionOptions.FirstOrDefault(o => o.Info.Id == saved.Id);
        if (imported is not null)
        {
            SelectedBibleVersionOption = imported;
            if (!imported.IsSelected)
                imported.IsSelected = true;
        }

        var message = $"\"{saved.Name}\" importada: {parsed.VerseCount} versículos.";
        if (parsed.MissingBookNumbers.Count > 0)
            message += $" Faltan {parsed.MissingBookNumbers.Count} de los 66 libros.";
        BibleStatusText = message;
        Log.Information(
            "Biblia importada: {Name} ({Abbreviation}), {Verses} versículos, {Missing} libros faltantes",
            saved.Name, saved.Abbreviation, parsed.VerseCount, parsed.MissingBookNumbers.Count);
    }

    [RelayCommand]
    private void RenameBibleVersion(BibleVersionOption? option)
    {
        var target = (option ?? SelectedBibleVersionOption)?.Info;
        if (target is null)
            return;

        var answer = _textPrompt.AskTwo(
            "Editar versión",
            "Nombre:", target.Name,
            "Abreviatura (la que sale en la referencia, por ejemplo RVR1960):", target.Abbreviation);
        if (answer is not var (name, abbreviation))
            return;

        name = string.IsNullOrWhiteSpace(name) ? target.Name : name;
        abbreviation = string.IsNullOrWhiteSpace(abbreviation) ? target.Abbreviation : abbreviation;
        if (name == target.Name && abbreviation == target.Abbreviation)
            return;

        _bibles.UpdateVersion(target.Id, name, abbreviation);
        LoadBibleVersions();

        // The playlist captions carry the abbreviation, and the slides on the
        // grid show it in their reference: both are read again.
        if (abbreviation != target.Abbreviation)
        {
            LoadPlaylists(null);
            if (_currentPassage is not null)
                RebuildSlidesPreservingLive(() => LoadBiblePassage(_currentPassage));
        }

        BibleStatusText = "Versión actualizada.";
    }

    [RelayCommand]
    private void DeleteBibleVersion(BibleVersionOption? option)
    {
        var target = (option ?? SelectedBibleVersionOption)?.Info;
        if (target is null)
            return;

        var confirm = MessageBox.Show(
            $"¿Eliminar la versión \"{target.Name}\" y sus {target.VerseCount} versículos?",
            "EcclesiaCast", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes)
            return;

        var wasActive = _checkedVersionIds.Contains(target.Id);
        _bibles.DeleteVersion(target.Id);
        SelectedBibleVersionOption = null;
        LoadBibleVersions();
        // The text on the grid came from the version that is gone.
        if (wasActive)
            RefreshAfterVersionChange();
        BibleStatusText = "Versión eliminada.";
    }

    /// <summary>
    /// Saves a version to a file this same program can import again (the JSON
    /// of <see cref="JsonBibleImporter"/>), to move it to another computer.
    /// </summary>
    [RelayCommand]
    private void ExportBibleVersion(BibleVersionOption? option)
    {
        var target = (option ?? SelectedBibleVersionOption)?.Info;
        if (target is null)
            return;

        var invalid = Path.GetInvalidFileNameChars();
        var fileName = new string(target.Name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Exportar Biblia",
            Filter = "Biblia (*.json)|*.json",
            FileName = fileName + ".json",
            AddExtension = true,
            DefaultExt = ".json",
        };
        if (dialog.ShowDialog() != true)
            return;

        try
        {
            var json = JsonBibleExporter.ToJson(_bibles.ReadVersion(target.Id));
            File.WriteAllText(dialog.FileName, json, new UTF8Encoding(false));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Falló la exportación de la Biblia {Name}", target.Name);
            MessageBox.Show(
                $"No se pudo guardar el archivo:\n\n{ex.Message}",
                "EcclesiaCast", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        BibleStatusText = $"«{target.Name}» exportada. Se vuelve a traer con el botón 📥.";
        Log.Information("Biblia exportada: {Name} a {Path}", target.Name, dialog.FileName);
    }
}
