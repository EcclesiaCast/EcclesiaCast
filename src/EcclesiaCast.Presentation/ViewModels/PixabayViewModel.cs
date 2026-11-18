using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using EcclesiaCast.Core.Media;

namespace EcclesiaCast.Presentation.ViewModels;

/// <summary>One result of the search, as a card that can be chosen.</summary>
public sealed partial class PixabayCardViewModel(PixabayHit hit, bool alreadyHave) : ObservableObject
{
    public PixabayHit Hit { get; } = hit;

    /// <summary>The small picture, read from Pixabay in the background.</summary>
    public string ThumbnailUrl => Hit.Thumbnail;

    public bool AlreadyHave { get; } = alreadyHave;

    public bool HasDuration => Hit.DurationText.Length > 0;

    public string AuthorLine => string.IsNullOrWhiteSpace(Hit.Author) ? "Pixabay" : $"de {Hit.Author}";

    public string Tip => Hit.IsVideo
        ? $"Video de {Hit.Width}×{Hit.Height}, {Hit.DurationText}. Clic para elegirlo."
        : $"Foto de {Hit.Width}×{Hit.Height}. Clic para elegirla.";

    [ObservableProperty]
    private bool _isSelected;
}

/// <summary>
/// Backs «Buscar fondos en Pixabay»: searches, pages, remembers the chosen
/// ones and downloads them into the open media tab. The window only shows
/// <see cref="Cards"/>, <see cref="StatusText"/>, <see cref="CenterMessage"/>,
/// <see cref="HasMore"/> and the state of the buttons.
/// </summary>
public sealed partial class PixabayViewModel : ObservableObject
{
    private readonly MainViewModel _main;
    private readonly string _welcomeText;
    private CancellationTokenSource? _work;
    private string _query = string.Empty;
    private bool _photos;
    private int _page;

    // What is said in the middle when there are no results (none found, or
    // the search is not available yet): the welcome text when nothing was searched.
    private string? _centerMessage;

    public PixabayViewModel(MainViewModel main, string welcomeText)
    {
        _main = main;
        _welcomeText = welcomeText;
        Cards.CollectionChanged += (_, e) =>
        {
            // A card chosen or let go changes the button to download.
            foreach (PixabayCardViewModel card in e.NewItems ?? Array.Empty<object>())
                card.PropertyChanged += (_, _) => RefreshDerived();
            RefreshDerived();
        };
    }

    public ObservableCollection<PixabayCardViewModel> Cards { get; } = [];

    /// <summary>The line at the bottom.</summary>
    [ObservableProperty]
    private string _statusText = string.Empty;

    /// <summary>True when there are more pages to ask for.</summary>
    [ObservableProperty]
    private bool _hasMore;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAdd), nameof(CanLoadMore), nameof(CenterMessage), nameof(AddText))]
    private bool _isBusy;

    /// <summary>The text in the middle, or empty when it is not shown.</summary>
    public string CenterMessage
    {
        get
        {
            var message = Cards.Count > 0 || IsBusy ? null
                : _centerMessage ?? (string.IsNullOrEmpty(_query) ? _welcomeText : null);
            return message ?? string.Empty;
        }
    }

    public bool CanLoadMore => !IsBusy;

    public int SelectedCount => Cards.Count(c => c.IsSelected);

    public bool CanAdd => SelectedCount > 0 && !IsBusy;

    public string AddText => SelectedCount switch
    {
        0 => "Bajar los elegidos",
        1 => "Bajar 1",
        _ => $"Bajar {SelectedCount}",
    };

    /// <summary>Raised when a new search empties the list, so the window goes back to the top.</summary>
    public event EventHandler? ResultsCleared;

    /// <summary>The kind (videos or photos) was changed: it searches again if there is something to search for.</summary>
    public Task KindChangedAsync(string queryText, bool photos) =>
        queryText.Trim().Length >= 2 ? SearchAsync(queryText, photos, newSearch: true) : Task.CompletedTask;

    /// <summary>A search with what the operator typed, or the next page of the last one.</summary>
    public async Task SearchAsync(string queryText, bool photos, bool newSearch)
    {
        if (IsBusy)
            return;

        if (newSearch)
        {
            var query = queryText.Trim();
            if (query.Length < 2)
            {
                StatusText = "Escribí qué fondo buscás.";
                return;
            }

            _query = query;
            _photos = photos;
            _page = 0;
            Cards.Clear();
            ResultsCleared?.Invoke(this, EventArgs.Empty);
        }

        _centerMessage = null;
        Busy("Buscando…");
        _work = new CancellationTokenSource();
        try
        {
            var page = await _main.SearchPixabayAsync(_query, _photos, _page + 1, _work.Token);
            _page++;
            foreach (var hit in page.Hits)
            {
                Cards.Add(new PixabayCardViewModel(hit, _main.HasMediaFile(_main.PixabayFilePath(hit, _query))));
            }

            HasMore = page.HasMore;
            StatusText = string.Empty;
            if (Cards.Count == 0)
                _centerMessage = $"No encontré {(_photos ? "fotos" : "videos")} con «{_query}». Probá con otra palabra.";
        }
        catch (PixabaySearchException ex)
        {
            if (Cards.Count == 0)
            {
                StatusText = string.Empty;
                _centerMessage = ex.Message;
            }
            else
            {
                StatusText = ex.Message;
            }
        }
        catch (OperationCanceledException)
        {
            // The window was closed.
        }
        finally
        {
            Idle();
        }
    }

    /// <summary>Downloads the chosen cards into the open media tab.</summary>
    public async Task AddSelectedAsync()
    {
        var chosen = Cards.Where(c => c.IsSelected).Select(c => c.Hit).ToList();
        if (chosen.Count == 0 || IsBusy)
            return;

        Busy("Bajando…");
        _work = new CancellationTokenSource();
        try
        {
            var progress = new Progress<string>(text => StatusText = text);
            var (added, failed) = await _main.AddPixabayAsync(chosen, _query, progress, _work.Token);
            foreach (var card in Cards)
                card.IsSelected = false;

            StatusText = (added, failed) switch
            {
                (0, 0) => "Ya los tenías todos.",
                (_, 0) => $"Listo: {added} en la pestaña «{_main.SelectedMediaTab}».",
                (0, _) => "No se pudo bajar. Revisá la conexión y probá de nuevo.",
                _ => $"Se agregaron {added}; {failed} no se pudieron bajar.",
            };
        }
        catch (OperationCanceledException)
        {
            // The window was closed in the middle: what was downloaded stays.
        }
        finally
        {
            Idle();
        }
    }

    /// <summary>The window was closed: a search or download in the middle stops.</summary>
    public void Cancel() => _work?.Cancel();

    private void Busy(string status)
    {
        StatusText = status;
        IsBusy = true;
    }

    private void Idle() => IsBusy = false;

    private void RefreshDerived()
    {
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(CanAdd));
        OnPropertyChanged(nameof(AddText));
        OnPropertyChanged(nameof(CenterMessage));
    }
}
