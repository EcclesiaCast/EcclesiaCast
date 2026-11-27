using CommunityToolkit.Mvvm.ComponentModel;

namespace EcclesiaCast.Presentation.ViewModels;

/// <summary>
/// One box that looks through songs, verses and media at once: it waits for a
/// pause in the typing, says how many it found, keeps one of them chosen, lets
/// the arrows walk them and gives back the one that is accepted.
/// </summary>
public sealed partial class GlobalSearchViewModel : ObservableObject
{
    /// <summary>
    /// Searching runs over the whole library, so it waits for a pause in the
    /// typing rather than running on every keystroke.
    /// </summary>
    internal static readonly TimeSpan Pause = TimeSpan.FromMilliseconds(180);

    private readonly Func<string, IReadOnlyList<SearchHit>> _search;
    private readonly IUiTimer _debounce;

    public GlobalSearchViewModel(
        Func<string, IReadOnlyList<SearchHit>> search, string initialQuery, IUiTimerFactory timers)
    {
        _search = search;
        _debounce = timers.Create(Pause);
        _debounce.Tick += (_, _) => SearchNow();
        _query = initialQuery;
        if (initialQuery.Length > 0)
            _debounce.Start();
    }

    /// <summary>What is written in the box.</summary>
    [ObservableProperty]
    private string _query;

    /// <summary>What was found for the last search.</summary>
    [ObservableProperty]
    private IReadOnlyList<SearchHit> _hits = [];

    /// <summary>The chosen one, by its place in <see cref="Hits"/>; -1 when none is.</summary>
    [ObservableProperty]
    private int _selectedIndex = -1;

    /// <summary>«3 resultados.»; empty until the first search.</summary>
    [ObservableProperty]
    private string _summary = string.Empty;

    /// <summary>What the operator chose, or null.</summary>
    public SearchHit? Result { get; private set; }

    partial void OnQueryChanged(string value)
    {
        _debounce.Stop();
        _debounce.Start();
    }

    /// <summary>Searches right away (what the pause in the typing ends in).</summary>
    public void SearchNow()
    {
        _debounce.Stop();
        var query = (Query ?? string.Empty).Trim();
        var hits = query.Length == 0 ? [] : _search(query);

        // The list is emptied of its choice first, so the first one of the
        // new results is chosen again even if it stood at the same place.
        SelectedIndex = -1;
        Hits = hits;
        if (hits.Count > 0)
            SelectedIndex = 0;

        Summary = query.Length == 0
            ? "Escribí para buscar."
            : hits.Count switch
            {
                0 => "Nada con eso.",
                1 => "1 resultado.",
                _ => $"{hits.Count} resultados.",
            };
    }

    /// <summary>↓ (1) or ↑ (-1), without going past either end.</summary>
    public void Move(int step)
    {
        if (Hits.Count > 0)
            SelectedIndex = Math.Clamp(SelectedIndex + step, 0, Hits.Count - 1);
    }

    /// <summary>Takes the chosen one. False when there is none to take.</summary>
    public bool Accept()
    {
        if (SelectedIndex < 0 || SelectedIndex >= Hits.Count)
            return false;

        Result = Hits[SelectedIndex];
        return true;
    }

    /// <summary>The window closed: no search is left waiting.</summary>
    public void Closed() => _debounce.Stop();
}
