using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace EcclesiaCast.Presentation.ViewModels;

/// <summary>One version in the list of a Bible package.</summary>
public sealed partial class BiblePackageRow(string title, string detail, bool isChecked, string? group = null)
    : ObservableObject
{
    public string Title { get; } = title;

    public string Detail { get; } = detail;

    /// <summary>The group the picker shows it under (its language).</summary>
    public string? Group { get; } = group;

    [ObservableProperty]
    private bool _isChecked = isChecked;
}

/// <summary>
/// The list of Bible versions with a tick box each, used both to choose which
/// versions go into a package and which ones of a package come in. Given
/// groups (the languages of the Bibles to download), a picker on top shows one
/// group at a time; what is ticked stays ticked when the group changes.
/// </summary>
public sealed partial class BiblePackageViewModel : ObservableObject
{
    /// <summary>The choice of the picker that shows every row.</summary>
    public const string AllGroups = "Todos los idiomas";

    private readonly IReadOnlyList<BiblePackageRow> _rows;
    private readonly string _acceptText;

    /// <param name="groups">The groups for the picker, in order; with fewer than two there is no picker.</param>
    /// <param name="group">The group shown first; <see cref="AllGroups"/> when it is not among them.</param>
    public BiblePackageViewModel(string title, string intro, string? warning, string acceptText,
        IReadOnlyList<BiblePackageRow> rows, IReadOnlyList<string>? groups = null, string? group = null)
    {
        Title = title;
        Intro = intro;
        Warning = warning ?? string.Empty;
        _acceptText = acceptText;
        _acceptButtonText = acceptText;
        _rows = rows;
        _visibleRows = rows;

        if (groups is { Count: > 1 })
        {
            // Rows ticked under another language are still downloaded: the
            // button counts them all so none is a surprise.
            HasGroups = true;
            foreach (var row in rows)
                row.PropertyChanged += Row_PropertyChanged;

            Groups = new[] { AllGroups }.Concat(groups).ToList();
            SelectedGroup = group is not null && groups.Contains(group) ? group : AllGroups;
            UpdateAcceptText();
        }
    }

    public string Title { get; }

    public string Intro { get; }

    /// <summary>Empty when there is nothing to warn about.</summary>
    public string Warning { get; }

    public bool HasWarning => Warning.Length > 0;

    /// <summary>True when the picker of groups is shown.</summary>
    public bool HasGroups { get; }

    /// <summary>The choices of the picker, «Todos los idiomas» first; empty when there is none.</summary>
    public IReadOnlyList<string> Groups { get; } = [];

    [ObservableProperty]
    private string? _selectedGroup;

    /// <summary>The rows shown: those of the group chosen.</summary>
    [ObservableProperty]
    private IReadOnlyList<BiblePackageRow> _visibleRows;

    [ObservableProperty]
    private string _acceptButtonText;

    /// <summary>The positions (in the list given) of the ticked rows, shown or not.</summary>
    public IReadOnlyList<int> CheckedIndexes =>
        _rows.Select((row, index) => (row, index)).Where(t => t.row.IsChecked).Select(t => t.index).ToList();

    partial void OnSelectedGroupChanged(string? value) =>
        VisibleRows = value is null or AllGroups
            ? _rows
            : _rows.Where(r => r.Group == value).ToList();

    private void Row_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BiblePackageRow.IsChecked))
            UpdateAcceptText();
    }

    private void UpdateAcceptText()
    {
        var count = _rows.Count(r => r.IsChecked);
        AcceptButtonText = count > 0 ? $"{_acceptText} ({count})" : _acceptText;
    }

    /// <summary>«Todas»: ticks the ones on the screen.</summary>
    public void CheckAll() => SetVisible(true);

    /// <summary>«Ninguna»: unticks the ones on the screen.</summary>
    public void CheckNone() => SetVisible(false);

    private void SetVisible(bool isChecked)
    {
        foreach (var row in VisibleRows)
            row.IsChecked = isChecked;
    }

    /// <summary>True when something is ticked; otherwise it says so and the window stays.</summary>
    public bool TryAccept()
    {
        if (_rows.All(r => !r.IsChecked))
        {
            Notice.Show("Marcá al menos una versión.");
            return false;
        }

        return true;
    }
}
