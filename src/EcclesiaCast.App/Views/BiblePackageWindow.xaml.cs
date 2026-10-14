using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;

namespace EcclesiaCast.App.Views;

/// <summary>
/// The list of Bible versions with a tick box each, used both to choose which
/// versions go into a package and which ones of a package come in.
/// </summary>
public partial class BiblePackageWindow : Window
{
    /// <summary>One version in the list.</summary>
    public sealed partial class Row(string title, string detail, bool isChecked) : ObservableObject
    {
        public string Title { get; } = title;

        public string Detail { get; } = detail;

        [ObservableProperty]
        private bool _isChecked = isChecked;
    }

    private readonly List<Row> _rows;

    public BiblePackageWindow(string title, string intro, string? warning, string acceptText, List<Row> rows)
    {
        InitializeComponent();
        Title = title;
        IntroText.Text = intro;
        AcceptButton.Content = acceptText;
        if (!string.IsNullOrEmpty(warning))
        {
            WarningText.Text = warning;
            WarningBox.Visibility = Visibility.Visible;
        }

        _rows = rows;
        VersionsList.ItemsSource = rows;
    }

    /// <summary>The positions (in the list the window was given) of the ticked rows.</summary>
    public IReadOnlyList<int> CheckedIndexes =>
        _rows.Select((row, index) => (row, index)).Where(t => t.row.IsChecked).Select(t => t.index).ToList();

    private void CheckAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var row in _rows)
            row.IsChecked = true;
    }

    private void CheckNone_Click(object sender, RoutedEventArgs e)
    {
        foreach (var row in _rows)
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
