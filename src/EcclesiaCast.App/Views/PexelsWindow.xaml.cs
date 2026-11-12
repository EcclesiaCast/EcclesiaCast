using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using EcclesiaCast.App.ViewModels;
using EcclesiaCast.Core.Media;

namespace EcclesiaCast.App.Views;

/// <summary>One result of the search, as a card that can be chosen.</summary>
public sealed partial class PexelsCard : ObservableObject
{
    public PexelsCard(PexelsHit hit, bool alreadyHave)
    {
        Hit = hit;
        AlreadyHave = alreadyHave;

        // The picture is read from Pexels in the background; the card is
        // there at once and fills in when it arrives.
        var thumb = new BitmapImage();
        thumb.BeginInit();
        thumb.UriSource = new Uri(hit.Thumbnail);
        thumb.DecodePixelWidth = 320;
        thumb.CacheOption = BitmapCacheOption.OnLoad;
        thumb.EndInit();
        Thumb = thumb;
    }

    public PexelsHit Hit { get; }

    public BitmapImage Thumb { get; }

    public bool AlreadyHave { get; }

    public bool HasDuration => Hit.DurationText.Length > 0;

    public string AuthorLine => string.IsNullOrWhiteSpace(Hit.Author) ? "Pexels" : $"de {Hit.Author}";

    public string Tip => Hit.IsVideo
        ? $"Video de {Hit.Width}×{Hit.Height}, {Hit.DurationText}. Clic para elegirlo."
        : $"Foto de {Hit.Width}×{Hit.Height}. Clic para elegirla.";

    [ObservableProperty]
    private bool _isSelected;
}

/// <summary>
/// «Buscar fondos en Pexels»: searches, shows what it found, and downloads
/// the chosen ones into a tab of the media bar. It stays open beside the
/// program (it is not modal) while it downloads.
/// </summary>
public partial class PexelsWindow : Window
{
    private readonly MainViewModel _vm;
    private readonly ObservableCollection<PexelsCard> _cards = [];
    private CancellationTokenSource? _work;
    private string _query = string.Empty;
    private bool _photos;
    private int _page;
    private bool _busy;

    public PexelsWindow(MainViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        Results.ItemsSource = _cards;
        CreditText.Text = PexelsBackgrounds.Credit;
        Loaded += (_, _) => QueryBox.Focus();
        Closed += (_, _) => _work?.Cancel();
    }

    private void Search_Click(object sender, RoutedEventArgs e) => _ = SearchAsync(newSearch: true);

    private void Kind_Click(object sender, RoutedEventArgs e)
    {
        if (QueryBox.Text.Trim().Length >= 2)
            _ = SearchAsync(newSearch: true);
    }

    private void More_Click(object sender, RoutedEventArgs e) => _ = SearchAsync(newSearch: false);

    private async Task SearchAsync(bool newSearch)
    {
        if (_busy)
            return;

        if (newSearch)
        {
            var query = QueryBox.Text.Trim();
            if (query.Length < 2)
            {
                StatusLine.Text = "Escribí qué fondo buscás.";
                return;
            }

            _query = query;
            _photos = PhotosRadio.IsChecked == true;
            _page = 0;
            _cards.Clear();
            Scroller.ScrollToTop();
        }

        SetBusy(true, "Buscando…");
        _work = new CancellationTokenSource();
        try
        {
            var page = await _vm.SearchPexelsAsync(_query, _photos, _page + 1, _work.Token);
            _page++;
            foreach (var hit in page.Hits)
            {
                var card = new PexelsCard(hit, _vm.HasMediaFile(PexelsFile(hit)));
                card.PropertyChanged += (_, _) => UpdateAddButton();
                _cards.Add(card);
            }

            MoreButton.Visibility = page.HasMore ? Visibility.Visible : Visibility.Collapsed;
            StatusLine.Text = _cards.Count == 0
                ? $"No encontré {(_photos ? "fotos" : "videos")} con «{_query}». Probá con otra palabra."
                : string.Empty;
        }
        catch (PexelsSearchException ex)
        {
            StatusLine.Text = ex.Message;
        }
        catch (OperationCanceledException)
        {
            // The window was closed.
        }
        finally
        {
            SetBusy(false, null);
        }
    }

    private string PexelsFile(PexelsHit hit) =>
        Path.Combine(App.DataDir, "fondos-pexels", PexelsBackgrounds.FileName(hit, _query));

    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        var chosen = _cards.Where(c => c.IsSelected).Select(c => c.Hit).ToList();
        if (chosen.Count == 0 || _busy)
            return;

        SetBusy(true, "Bajando…");
        _work = new CancellationTokenSource();
        try
        {
            var progress = new Progress<string>(text => StatusLine.Text = text);
            var (added, failed) = await _vm.AddPexelsAsync(chosen, _query, progress, _work.Token);
            foreach (var card in _cards)
                card.IsSelected = false;

            StatusLine.Text = (added, failed) switch
            {
                (0, 0) => "Ya los tenías todos.",
                (_, 0) => $"Listo: {added} en la pestaña «{_vm.SelectedMediaTab}».",
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
            SetBusy(false, null);
        }
    }

    private void SetBusy(bool busy, string? status)
    {
        _busy = busy;
        if (status is not null)
            StatusLine.Text = status;
        EmptyText.Visibility = _cards.Count == 0 && !busy && string.IsNullOrEmpty(_query)
            ? Visibility.Visible
            : Visibility.Collapsed;
        MoreButton.IsEnabled = !busy;
        UpdateAddButton();
    }

    private void UpdateAddButton()
    {
        var count = _cards.Count(c => c.IsSelected);
        AddButton.IsEnabled = count > 0 && !_busy;
        AddButton.Content = count switch
        {
            0 => "Bajar los elegidos",
            1 => "Bajar 1",
            _ => $"Bajar {count}",
        };
    }

    private void Credit_Click(object sender, RoutedEventArgs e) =>
        Process.Start(new ProcessStartInfo(PexelsBackgrounds.Site) { UseShellExecute = true });

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
