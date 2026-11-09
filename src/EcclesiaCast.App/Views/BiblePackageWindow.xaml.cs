using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

namespace EcclesiaCast.App.Views;

/// <summary>
/// The list of Bible versions with a tick box each, used both to choose which
/// versions go into a package and which ones of a package come in. Given
/// groups (the languages of the Bibles to download), a picker on top shows one
/// group at a time; what is ticked stays ticked when the group changes.
/// </summary>
public partial class BiblePackageWindow : Window
{
    /// <summary>The choice of the picker that shows every row.</summary>
    public const string AllGroups = "Todos los idiomas";

    /// <summary>One version in the list.</summary>
    public sealed partial class Row(string title, string detail, bool isChecked, string? group = null) : ObservableObject
    {
        public string Title { get; } = title;

        public string Detail { get; } = detail;

        /// <summary>The group the picker shows it under (its language).</summary>
        public string? Group { get; } = group;

        [ObservableProperty]
        private bool _isChecked = isChecked;
    }

    private readonly List<Row> _rows;
    private readonly string _acceptText;
    private readonly bool _countOnButton;
    private List<Row> _visible;

    /// <param name="groups">The groups for the picker, in order; with fewer than two there is no picker.</param>
    /// <param name="group">The group shown first; <see cref="AllGroups"/> when it is not among them.</param>
    public BiblePackageWindow(string title, string intro, string? warning, string acceptText, List<Row> rows,
        IReadOnlyList<string>? groups = null, string? group = null)
    {
        InitializeComponent();
        Title = title;
        IntroText.Text = intro;
        _acceptText = acceptText;
        AcceptButton.Content = acceptText;
        if (!string.IsNullOrEmpty(warning))
        {
            WarningText.Text = warning;
            WarningBox.Visibility = Visibility.Visible;
        }

        _rows = rows;
        _visible = rows;
        VersionsList.ItemsSource = rows;

        if (groups is { Count: > 1 })
        {
            // Rows ticked under another language are still downloaded: the
            // button counts them all so none is a surprise.
            _countOnButton = true;
            foreach (var row in rows)
                row.PropertyChanged += Row_PropertyChanged;

            GroupPicker.ItemsSource = new[] { AllGroups }.Concat(groups).ToList();
            GroupPicker.SelectedItem = group is not null && groups.Contains(group) ? group : AllGroups;
            GroupPanel.Visibility = Visibility.Visible;
            UpdateAcceptText();
        }
    }

    /// <summary>The positions (in the list the window was given) of the ticked rows, shown or not.</summary>
    public IReadOnlyList<int> CheckedIndexes =>
        _rows.Select((row, index) => (row, index)).Where(t => t.row.IsChecked).Select(t => t.index).ToList();

    private void GroupPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var group = GroupPicker.SelectedItem as string;
        _visible = group is null or AllGroups
            ? _rows
            : _rows.Where(r => r.Group == group).ToList();
        VersionsList.ItemsSource = _visible;
    }

    private void Row_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Row.IsChecked))
            UpdateAcceptText();
    }

    private void UpdateAcceptText()
    {
        var count = _rows.Count(r => r.IsChecked);
        AcceptButton.Content = _countOnButton && count > 0 ? $"{_acceptText} ({count})" : _acceptText;
    }

    private void CheckAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var row in _visible)
            row.IsChecked = true;
    }

    private void CheckNone_Click(object sender, RoutedEventArgs e)
    {
        foreach (var row in _visible)
            row.IsChecked = false;
    }

    private void Accept_Click(object sender, RoutedEventArgs e)
    {
        if (_rows.All(r => !r.IsChecked))
        {
            MessageBox.Show(this, "Marcá al menos una versión.", "EcclesiaCast",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        DialogResult = true;
    }
}
