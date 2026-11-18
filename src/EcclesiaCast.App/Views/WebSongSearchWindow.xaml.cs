using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using EcclesiaCast.App.Services;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.App.Views;

/// <summary>
/// Finds a song on the internet and brings it into the library in one step.
/// The search, the choice of the result and the import are in
/// <see cref="WebSongSearchViewModel"/>; what stays here is the invisible
/// browser the full search of musica.com runs in, and placing the controls.
/// </summary>
public partial class WebSongSearchWindow : Window
{
    private readonly WebSongSearchViewModel _viewModel;
    private MusicaComBrowserSearch? _browserSearch;

    /// <summary>True while the view model fills the fields: that is not the operator typing.</summary>
    private bool _filling;

    public WebSongSearchWindow(WebSongSearchViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        ResultsList.ItemsSource = _viewModel.Results;

        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        _viewModel.PreviewTextChanged += FillPreview;
        _viewModel.SelectRequested += row =>
        {
            if (ResultsList.SelectedItem is null)
                ResultsList.SelectedItem = row;
        };
        _viewModel.ImportDone += () =>
        {
            QueryBox.Focus();
            QueryBox.SelectAll();
        };

        Loaded += (_, _) =>
        {
            if (WebViewProfile.IsRuntimeAvailable())
            {
                _browserSearch = new MusicaComBrowserSearch(new WindowInteropHelper(this).Handle);
                _viewModel.BrowserSearch = _browserSearch.SearchAsync;
            }

            QueryBox.Focus();
        };

        Closed += (_, _) =>
        {
            _browserSearch?.Dispose();
            _viewModel.Dispose();
        };

        ShowState();
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e) => ShowState();

    private void ShowState()
    {
        SearchStatusText.Text = _viewModel.SearchStatus;
        StatusText.Text = _viewModel.Status;
        PreviewInfoText.Text = _viewModel.PreviewInfo;
        ImportButton.IsEnabled = _viewModel.CanImport;
        SourceLink.Visibility = _viewModel.SourceLinkVisible ? Visibility.Visible : Visibility.Collapsed;
        SourceLinkText.Text = _viewModel.SourceLinkText.Length > 0 ? _viewModel.SourceLinkText : "Ver en la web";
    }

    private void FillPreview(string title, string artist, string lyrics, bool scrollToTop)
    {
        _filling = true;
        TitleBox.Text = title;
        ArtistBox.Text = artist;
        LyricsBox.Text = lyrics;
        _filling = false;
        if (scrollToTop)
            LyricsBox.ScrollToHome();
    }

    // ── Búsqueda ─────────────────────────────────────────────────

    private void Query_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;
        e.Handled = true;
        Search();
    }

    private void Search_Click(object sender, RoutedEventArgs e) => Search();

    private async void Search() => await _viewModel.SearchAsync(QueryBox.Text);

    // ── Vista previa ─────────────────────────────────────────────

    private async void Results_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ResultsList.SelectedItem is not WebSongRow row)
            return;

        await _viewModel.ShowPreviewAsync(row);
    }

    private void Preview_Changed(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (!_filling)
            _viewModel.PreviewEdited(TitleBox.Text, ArtistBox.Text, LyricsBox.Text);
    }

    private void OpenSource_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SourceUrl is not { } url)
            return;

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true,
        });
    }

    // ── Importación ──────────────────────────────────────────────

    private void Import_Click(object sender, RoutedEventArgs e) =>
        _viewModel.Import(TitleBox.Text, ArtistBox.Text, LyricsBox.Text);
}
