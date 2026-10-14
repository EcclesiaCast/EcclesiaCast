using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Web;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Serilog;

namespace EcclesiaCast.App.Controls;

/// <summary>
/// A web page in the place of the slide grid. The browser is a
/// <see cref="WebView2CompositionControl"/>: unlike the ordinary WebView2 it
/// draws into a WPF image, so it can be scaled by a Viewbox, covered by other
/// controls — and shown, as the same moving picture, in the output window
/// (see <see cref="WebLiveFeed"/>).
///
/// The page is always laid out at 1920×1080 and shrunk to fit the panel, so
/// what the operator sees is exactly what the congregation will see.
/// </summary>
public partial class WebPanel : UserControl, IWebPageHost
{
    /// <summary>The size the page is laid out at: a full-HD screen.</summary>
    private const int PageWidth = 1920;
    private const int PageHeight = 1080;

    private WebView2CompositionControl? _web;
    private Task? _starting;
    private string? _pendingAddress;
    private bool _navigated;

    public WebPanel()
    {
        InitializeComponent();
        AddressBox.IsKeyboardFocusWithinChanged += (_, _) => KeyboardFocusChanged?.Invoke(this, EventArgs.Empty);
        ImageQueryBox.IsKeyboardFocusWithinChanged += (_, _) => KeyboardFocusChanged?.Invoke(this, EventArgs.Empty);
        UpdateButtons();
    }

    // ── Estado: abierto, vivo pero oculto, o apagado ─────────────

    /// <summary>
    /// Shows the panel, hides it, or hides it keeping the page alive. A
    /// collapsed browser stops updating (it counts as not visible), so a page
    /// that is still being projected is only made invisible and untouchable:
    /// it stays in the layout and keeps drawing.
    /// </summary>
    public void SetState(bool open, bool keepAlive)
    {
        if (open)
        {
            Visibility = Visibility.Visible;
            Opacity = 1;
            IsHitTestVisible = true;
            Toolbar.Visibility = Visibility.Visible;
            if (_web is not null)
                _web.Focusable = true;
            EnsureStarted();
        }
        else if (keepAlive && _web is not null)
        {
            Visibility = Visibility.Visible;
            Opacity = 0;
            IsHitTestVisible = false;
            Toolbar.Visibility = Visibility.Collapsed;
            // Nothing invisible may take the keyboard.
            _web.Focusable = false;
            WebLiveFeed.SetPointer(null);
        }
        else
        {
            Visibility = Visibility.Collapsed;
            WebLiveFeed.SetPointer(null);
        }
    }

    /// <summary>
    /// True while the keyboard is typing into the page or the address box. The
    /// window's own shortcuts (F1–F4, Esc, the arrows that move the slides…)
    /// must stay out of the way then: they are keys of the page.
    /// </summary>
    public bool HasKeyboardInside =>
        AddressBox.IsKeyboardFocusWithin || ImageQueryBox.IsKeyboardFocusWithin || (_web is { } web && (web.IsKeyboardFocusWithin || web.HasFocusWithin()));

    /// <summary>Raised when <see cref="HasKeyboardInside"/> may have changed.</summary>
    public event EventHandler? KeyboardFocusChanged;

    // ── IWebPageHost ─────────────────────────────────────────────

    public event EventHandler<WebImageChosen>? ImageChosen;

    public bool IsReady => _web?.CoreWebView2 is not null;

    public string? CurrentAddress
    {
        get
        {
            var source = _web?.CoreWebView2?.Source;
            return string.IsNullOrWhiteSpace(source) || source == "about:blank" ? null : source;
        }
    }

    public string? CurrentTitle
    {
        get
        {
            var title = _web?.CoreWebView2?.DocumentTitle;
            // A page with no title reports its own address as one.
            return string.IsNullOrWhiteSpace(title) || title == CurrentAddress ? null : title.Trim();
        }
    }

    public void Open(string address)
    {
        if (AddressInput.Resolve(address) is not { } uri)
            return;

        EnsureStarted();
        if (_web?.CoreWebView2 is { } core)
            Navigate(core, uri);
        else
            _pendingAddress = uri.AbsoluteUri;
    }

    public async Task<bool> CaptureAsync(string pngPath)
    {
        if (_web?.CoreWebView2 is not { } core)
            return false;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(pngPath)!);
            await using var file = File.Create(pngPath);
            await core.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, file);
            return true;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "No se pudo sacar la foto de la página");
            TryDelete(pngPath);
            return false;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // Ya no importa: es un archivo a medio escribir de una foto fallida.
        }
    }

    // ── El navegador, creado la primera vez ──────────────────────

    /// <summary>Creates the browser the first time the panel is needed, not when the program starts.</summary>
    private void EnsureStarted()
    {
        if (_starting is not null)
            return;

        _web = new WebView2CompositionControl
        {
            Width = PageWidth,
            Height = PageHeight,
            DefaultBackgroundColor = System.Drawing.Color.White,
        };
        _web.IsKeyboardFocusWithinChanged += (_, _) => KeyboardFocusChanged?.Invoke(this, EventArgs.Empty);
        _web.SourceChanged += (_, _) => OnPageChanged();
        _web.NavigationCompleted += (_, _) => OnPageChanged();
        _web.ZoomFactorChanged += (_, _) => ShowZoom();
        PageBox.Child = _web;

        _starting = StartAsync();
    }

    private async Task StartAsync()
    {
        try
        {
            // The same profile as YouTube's browser: a Google or Canva session
            // opened in one place is open in the other.
            var environment = await WebViewProfile.GetEnvironmentAsync();
            await _web!.EnsureCoreWebView2Async(environment);

            var core = _web.CoreWebView2;

            // A page that opens another window (a link «target=_blank», a
            // sign-in popup) opens it here: there is only this one panel.
            core.NewWindowRequested += (_, e) =>
            {
                e.Handled = true;
                if (!string.IsNullOrEmpty(e.Uri))
                    core.Navigate(e.Uri);
            };

            core.ContextMenuRequested += OnContextMenuRequested;

            AttachFeed();

            if (_pendingAddress is { } address)
            {
                _pendingAddress = null;
                Navigate(core, new Uri(address));
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "No se pudo iniciar el navegador del panel web");
            _starting = null;
            if (_web is not null)
            {
                PageBox.Child = null;
                _web = null;
            }

            Placeholder.Text = "No se pudo abrir el navegador.\n\nInstalá el runtime de WebView2 (Microsoft Edge WebView2) y volvé a probar.";
            PlaceholderCover.Visibility = Visibility.Visible;
        }
    }

    /// <summary>
    /// Right click on the page. The browser's own menu opens far from the
    /// pointer (the page is drawn at 1920×1080 and shrunk by a Viewbox, which
    /// the browser knows nothing about), so it is replaced by one of ours,
    /// opened where the mouse is. On a Google Images result it offers
    /// «Agregar a medios» with the ORIGINAL picture: Google puts the address of
    /// the full-size file in the link of every result as soon as it is
    /// right-clicked, so the thumbnail does not have to be opened first.
    /// </summary>
    private void OnContextMenuRequested(object? sender, CoreWebView2ContextMenuRequestedEventArgs e)
    {
        if (_web?.CoreWebView2 is not { } core)
            return;

        // Everything is read now: the arguments are only good inside this call.
        var target = e.ContextMenuTarget;
        var link = target.HasLinkUri ? target.LinkUri : null;
        var imageSource = target.Kind == CoreWebView2ContextMenuTargetKind.Image && target.HasSourceUri ? target.SourceUri : null;
        var selection = target.HasSelection ? target.SelectionText : null;
        var page = core.Source;
        e.Handled = true;

        var menu = new ContextMenu { PlacementTarget = _web, Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint };

        MenuItem Add(string header, Action action, bool enabled = true)
        {
            var item = new MenuItem { Header = header, IsEnabled = enabled };
            item.Click += (_, _) => action();
            menu.Items.Add(item);
            return item;
        }

        // The picture: the original from Google's link, or the picture's own address.
        WebImageChosen? chosen = null;
        if (WebImages.FromImgres(link) is { } original)
            chosen = new WebImageChosen(original.Image, original.Page ?? page);
        else if (imageSource is not null)
            chosen = new WebImageChosen(imageSource, page, IsThumbnail: WebImages.IsGoogleThumbnail(imageSource));

        if (chosen is { IsThumbnail: false })
        {
            Add("➕ Agregar a medios", () => ImageChosen?.Invoke(this, chosen));
        }
        else if (IsGoogleImages(page))
        {
            // Over Google's own overlay (the picture opened beside the results is
            // often covered by one) the browser reports no picture at all: the
            // one that is open is looked for in the page instead.
            Add("➕ Agregar a medios", async () =>
            {
                var found = await FindOpenImageAsync();
                ImageChosen?.Invoke(this, found ?? chosen ?? new WebImageChosen(string.Empty, page));
            });
        }
        else if (chosen is not null)
        {
            Add("➕ Agregar a medios", () => ImageChosen?.Invoke(this, chosen));
        }

        if (link is not null && WebImages.FromImgres(link) is null)
        {
            Add("Abrir vínculo", () => core.Navigate(link));
            Add("Copiar vínculo", () => TryCopy(link));
        }

        if (!string.IsNullOrEmpty(selection))
            Add("Copiar", () => TryCopy(selection));

        if (menu.Items.Count > 0)
            menu.Items.Add(new Separator());

        Add("◀ Atrás", () => _web?.GoBack(), _web.CanGoBack);
        Add("▶ Adelante", () => _web?.GoForward(), _web.CanGoForward);
        Add("⟳ Recargar", () => _web?.Reload());

        menu.IsOpen = true;
    }

    /// <summary>The page is Google Images (its results, and the picture opened beside them).</summary>
    private static bool IsGoogleImages(string? address) =>
        Uri.TryCreate(address, UriKind.Absolute, out var uri)
        && uri.Host.Contains("google.", StringComparison.OrdinalIgnoreCase)
        && (uri.Query.Contains("udm=2", StringComparison.Ordinal) || uri.Query.Contains("tbm=isch", StringComparison.Ordinal));

    /// <summary>
    /// The picture Google Images has opened beside its results: the biggest one
    /// in view that is not one of Google's own small copies. Its address is the
    /// original file, and the link around it is the page it came from.
    /// </summary>
    private const string OpenImageScript = """
        (() => {
          const small = s => !s || /^data:|encrypted-tbn/.test(s);
          let best = null, area = 0;
          for (const i of document.images) {
            const r = i.getBoundingClientRect(), s = i.currentSrc || i.src;
            if (small(s) || r.width < 200 || r.height < 150) continue;
            if (r.bottom <= 0 || r.top >= innerHeight || r.right <= 0 || r.left >= innerWidth) continue;
            if (r.width * r.height > area) { area = r.width * r.height; best = i; }
          }
          const link = best && best.closest('a');
          return JSON.stringify({ src: best ? (best.currentSrc || best.src) : null, page: link ? link.href : null });
        })()
        """;

    private async Task<WebImageChosen?> FindOpenImageAsync()
    {
        if (_web?.CoreWebView2 is not { } core)
            return null;

        try
        {
            // ExecuteScriptAsync returns the script's JSON-encoded result: a string, so quoted.
            var raw = await core.ExecuteScriptAsync(OpenImageScript);
            var json = System.Text.Json.JsonSerializer.Deserialize<string>(raw);
            if (string.IsNullOrEmpty(json))
                return null;

            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var src = doc.RootElement.TryGetProperty("src", out var s) && s.ValueKind == System.Text.Json.JsonValueKind.String
                ? s.GetString()
                : null;
            if (string.IsNullOrEmpty(src))
                return null;

            var page = doc.RootElement.TryGetProperty("page", out var p) && p.ValueKind == System.Text.Json.JsonValueKind.String
                ? p.GetString()
                : core.Source;
            return new WebImageChosen(src, page);
        }
        catch (Exception ex) when (ex is System.Text.Json.JsonException or InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            Log.Debug(ex, "No se pudo buscar la imagen abierta en la página");
            return null;
        }
    }

    /// <summary>«➕ Agregar la abierta»: the picture opened beside Google's results goes to the media bar.</summary>
    private async void AddOpenImage_Click(object sender, RoutedEventArgs e)
    {
        var found = await FindOpenImageAsync();
        ImageChosen?.Invoke(this, found ?? new WebImageChosen(string.Empty, CurrentAddress));
    }

    private static void TryCopy(string text)
    {
        try
        {
            Clipboard.SetText(text);
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            // Another program has the clipboard open for a moment: nothing to do.
        }
    }

    /// <summary>
    /// Hands the image the browser draws into to <see cref="WebLiveFeed"/>.
    /// The control builds it with its template, so it is looked for once the
    /// control is on screen.
    /// </summary>
    private void AttachFeed()
    {
        if (_web is null)
            return;

        _web.ApplyTemplate();
        if (FindImage(_web) is { } image)
        {
            WebLiveFeed.Attach(image);
            return;
        }

        // Not built yet: when the control has been laid out.
        void Retry(object? sender, RoutedEventArgs e)
        {
            _web.Loaded -= Retry;
            if (FindImage(_web) is { } late)
                WebLiveFeed.Attach(late);
            else
                Log.Warning("El navegador del panel web no tiene imagen: no se puede proyectar en vivo");
        }

        if (_web.IsLoaded)
            Dispatcher.InvokeAsync(() => Retry(null, new RoutedEventArgs()), System.Windows.Threading.DispatcherPriority.Loaded);
        else
            _web.Loaded += Retry;
    }

    private static Image? FindImage(DependencyObject parent)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is Image image)
                return image;
            if (FindImage(child) is { } found)
                return found;
        }

        return null;
    }

    private void Navigate(CoreWebView2 core, Uri uri)
    {
        _navigated = true;
        PlaceholderCover.Visibility = Visibility.Collapsed;
        core.Navigate(uri.AbsoluteUri);
        AddressBox.Text = uri.AbsoluteUri;
    }

    private void OnPageChanged()
    {
        if (!AddressBox.IsKeyboardFocusWithin && CurrentAddress is { } address)
            AddressBox.Text = address;

        UpdateButtons();
        ShowZoom();
    }

    private void UpdateButtons()
    {
        BackButton.IsEnabled = _web?.CanGoBack == true;
        ForwardButton.IsEnabled = _web?.CanGoForward == true;
    }

    // ── La barra ─────────────────────────────────────────────────

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        if (_web?.CanGoBack == true)
            _web.GoBack();
    }

    private void Forward_Click(object sender, RoutedEventArgs e)
    {
        if (_web?.CanGoForward == true)
            _web.GoForward();
    }

    private void Reload_Click(object sender, RoutedEventArgs e)
    {
        if (_web?.CoreWebView2 is not null && _navigated)
            _web.Reload();
    }

    private void ZoomIn_Click(object sender, RoutedEventArgs e) => ChangeZoom(+0.1);

    private void ZoomOut_Click(object sender, RoutedEventArgs e) => ChangeZoom(-0.1);

    private void ChangeZoom(double delta)
    {
        if (_web?.CoreWebView2 is null)
            return;

        _web.ZoomFactor = Math.Clamp(Math.Round(_web.ZoomFactor + delta, 2), 0.25, 5);
        ShowZoom();
    }

    private void ShowZoom() =>
        ZoomText.Text = $"{Math.Round((_web?.CoreWebView2 is null ? 1 : _web.ZoomFactor) * 100):0} %";

    // ── La caja de dirección ─────────────────────────────────────

    private void AddressBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) =>
        AddressBox.SelectAll();

    /// <summary>The first click into the box selects everything, as in a browser; the next ones place the cursor.</summary>
    private void AddressBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (AddressBox.IsKeyboardFocusWithin)
            return;

        AddressBox.Focus();
        e.Handled = true;
    }

    private void AddressBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            AddressBox.Text = CurrentAddress ?? string.Empty;
            AddressBox.SelectAll();
            e.Handled = true;
            return;
        }

        if (e.Key != Key.Enter)
            return;

        e.Handled = true;
        if (AddressInput.Resolve(AddressBox.Text) is not { } uri)
            return;

        EnsureStarted();
        if (_web?.CoreWebView2 is { } core)
            Navigate(core, uri);
        else
            _pendingAddress = uri.AbsoluteUri;

        // The page takes the keyboard: the next thing typed is for it.
        _web?.Focus();
    }

    // ── El puntero ───────────────────────────────────────────────

    private void PageBox_MouseMove(object sender, MouseEventArgs e)
    {
        if (_web is null)
            return;

        // As a fraction of the page: the arrow lands on the same spot of it
        // on any screen. GetPosition accounts for the Viewbox's scale.
        var at = e.GetPosition(_web);
        WebLiveFeed.SetPointer(new Point(at.X / PageWidth, at.Y / PageHeight));
    }

    private void PageBox_MouseLeave(object sender, MouseEventArgs e) => WebLiveFeed.SetPointer(null);
}
