using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using EcclesiaCast.Presentation.Platform;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Desktop.Views;

/// <summary>
/// Finds a song on the internet and brings it into the library in one step.
/// The search, the choice of the result and the import are in
/// <see cref="WebSongSearchViewModel"/>. There is no browser here, so the
/// search by a line of the lyrics stays off.
/// </summary>
public sealed partial class WebSongSearchWindow : Window
{
    private readonly WebSongSearchViewModel _viewModel;
    private readonly IShell _shell;

    public WebSongSearchWindow(WebSongSearchViewModel viewModel, IShell shell)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _shell = shell;
        HintText.Text = _viewModel.Hint;
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

        // Avalonia tells of a change later, also of the ones made here to fill
        // the fields. Telling the view model about those changes nothing.
        TitleBox.TextChanged += (_, _) => Preview_Changed();
        LyricsBox.TextChanged += (_, _) => Preview_Changed();
        QueryBox.AddHandler(KeyDownEvent, Query_KeyDown, RoutingStrategies.Tunnel);

        Opened += (_, _) => QueryBox.Focus();
        Closed += (_, _) => _viewModel.Dispose();

        ShowState();
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e) => ShowState();

    private void ShowState()
    {
        SearchStatusText.Text = _viewModel.SearchStatus;
        StatusText.Text = _viewModel.Status;
        PreviewInfoText.Text = _viewModel.PreviewInfo;
        ImportButton.IsEnabled = _viewModel.CanImport;
        SourceLink.IsVisible = _viewModel.SourceLinkVisible;
        SourceLink.Content = _viewModel.SourceLinkText.Length > 0 ? _viewModel.SourceLinkText : "Ver en la web";
    }

    private void FillPreview(string title, string artist, string lyrics, bool scrollToTop)
    {
        TitleBox.Text = title;
        ArtistBox.Text = artist;
        LyricsBox.Text = lyrics;
        if (scrollToTop)
        {
            LyricsBox.CaretIndex = 0;
            LyricsBox.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault()?.ScrollToHome();
        }
    }

    private void Query_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        e.Handled = true;
        Search();
    }

    private void Search_Click(object? sender, RoutedEventArgs e) => Search();

    /// <summary>Writes <paramref name="query"/> in the box and looks for it (the screens bench).</summary>
    internal void Search(string query)
    {
        QueryBox.Text = query;
        Search();
    }

    private async void Search() => await _viewModel.SearchAsync(QueryBox.Text ?? string.Empty);

    private async void Results_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ResultsList.SelectedItem is WebSongRow row)
            await _viewModel.ShowPreviewAsync(row);
    }

    private void Preview_Changed() => _viewModel.PreviewEdited(
        TitleBox.Text ?? string.Empty, ArtistBox.Text ?? string.Empty, LyricsBox.Text ?? string.Empty);

    private void OpenSource_Click(object? sender, RoutedEventArgs e)
    {
        if (_viewModel.SourceUrl is { } url)
            _shell.OpenUrl(url);
    }

    private void Import_Click(object? sender, RoutedEventArgs e) => _viewModel.Import(
        TitleBox.Text ?? string.Empty, ArtistBox.Text ?? string.Empty, LyricsBox.Text ?? string.Empty);

    private void Close_Click(object? sender, RoutedEventArgs e) => Close();
}
