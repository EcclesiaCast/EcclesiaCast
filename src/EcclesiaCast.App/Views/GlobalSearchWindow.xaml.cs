using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using EcclesiaCast.App.Services;

namespace EcclesiaCast.App.Views;

/// <summary>
/// One box that looks through songs, verses and media at once.
///
/// Until now each panel had its own search, so finding "santo" meant
/// guessing where it lived before looking for it. Mid-service there is no
/// time for that: the preacher says a reference and the operator has one
/// place to type it.
/// </summary>
public partial class GlobalSearchWindow : Window
{
    private readonly Func<string, IReadOnlyList<SearchHit>> _search;

    /// <summary>
    /// Searching runs over the whole library, so it waits for a pause in the
    /// typing rather than running on every keystroke.
    /// </summary>
    private readonly DispatcherTimer _debounce = new() { Interval = TimeSpan.FromMilliseconds(180) };

    public GlobalSearchWindow(Func<string, IReadOnlyList<SearchHit>> search, string initialQuery)
    {
        InitializeComponent();
        _search = search;

        _debounce.Tick += (_, _) =>
        {
            _debounce.Stop();
            Run();
        };

        QueryBox.Text = initialQuery;
        QueryBox.CaretIndex = QueryBox.Text.Length;
        Loaded += (_, _) => QueryBox.Focus();
    }

    /// <summary>What the operator chose, or null if they closed the window.</summary>
    public SearchHit? Result { get; private set; }

    private void Query_Changed(object sender, TextChangedEventArgs e)
    {
        _debounce.Stop();
        _debounce.Start();
    }

    private void Run()
    {
        var query = QueryBox.Text.Trim();
        var hits = query.Length == 0 ? [] : _search(query);

        ResultsList.ItemsSource = hits;
        if (hits.Count > 0)
            ResultsList.SelectedIndex = 0;

        SummaryText.Text = query.Length == 0
            ? "Escribí para buscar."
            : hits.Count switch
            {
                0 => "Nada con eso.",
                1 => "1 resultado.",
                _ => $"{hits.Count} resultados.",
            };
    }

    /// <summary>
    /// The arrows walk the results while the caret stays in the box, so the
    /// operator can keep typing to narrow it down without reaching for the
    /// mouse.
    /// </summary>
    private void Query_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Down or Key.Up) || ResultsList.Items.Count == 0)
            return;

        var step = e.Key == Key.Down ? 1 : -1;
        ResultsList.SelectedIndex = Math.Clamp(ResultsList.SelectedIndex + step, 0, ResultsList.Items.Count - 1);
        ResultsList.ScrollIntoView(ResultsList.SelectedItem);
        e.Handled = true;
    }

    private void Results_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Accept();
            e.Handled = true;
        }
    }

    private void Results_DoubleClick(object sender, MouseButtonEventArgs e) => Accept();

    private void Open_Click(object sender, RoutedEventArgs e) => Accept();

    private void Accept()
    {
        if (ResultsList.SelectedItem is not SearchHit hit)
            return;

        Result = hit;
        DialogResult = true;
    }
}
