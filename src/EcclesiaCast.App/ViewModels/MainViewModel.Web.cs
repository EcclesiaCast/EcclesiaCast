using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Media;
using EcclesiaCast.Core.Web;
using Serilog;

namespace EcclesiaCast.App.ViewModels;

/// <summary>
/// Projecting a web page: the panel that takes the place of the slide grid,
/// the photo of the page, the page live, and the pages saved in the media bar.
///
/// What goes to the screen is always a media item that is not in the library
/// (a negative id, like the logos used as a background): a photo is an
/// ordinary image in the folder of captures, and the live page is a
/// <see cref="MediaType.Web"/> whose picture is <see cref="WebLiveFeed"/>.
/// </summary>
public sealed partial class MainViewModel
{
    private const string WebPointerKey = "web.pointer";

    /// <summary>The id of the page shown live: there is only ever one.</summary>
    private const int WebLiveId = -2_000_000;

    /// <summary>
    /// Photos each need their own id: putting a background with the id of the
    /// one on screen is taken as «the same, leave it», so a second photo
    /// would never replace the first.
    /// </summary>
    private int _nextWebPhotoId = WebLiveId - 1;

    /// <summary>The panel in the central box, set by the window.</summary>
    public IWebPageHost? WebHost { get; set; }

    /// <summary>True while the web panel is where the slide grid is.</summary>
    [ObservableProperty]
    private bool _isWebPanelOpen;

    /// <summary>True while the page is on the screen in motion.</summary>
    [ObservableProperty]
    private bool _isWebLive;

    /// <summary>The «Mostrar el puntero» box: an arrow on the screen where the mouse is over the page.</summary>
    [ObservableProperty]
    private bool _showWebPointer = true;

    /// <summary>
    /// The last photo taken: «Guardar en medios» saves it, as long as the
    /// panel still shows the page it was taken from.
    /// </summary>
    private (string Path, string? Address, string Name)? _lastWebPhoto;

    partial void OnShowWebPointerChanged(bool value)
    {
        WebLiveFeed.ShowPointer = value;
        _settings.Set(WebPointerKey, value ? "1" : "0");
    }

    private void InitializeWeb()
    {
        ShowWebPointer = _settings.Get(WebPointerKey) != "0";
        WebLiveFeed.ShowPointer = ShowWebPointer;
        _ = CleanOldWebCapturesAsync();
    }

    /// <summary>
    /// Photos pile up with every press of «Proyectar foto»: the ones older
    /// than a week go when the program starts, except those saved in the
    /// media library, which are the operator's now.
    /// </summary>
    private async Task CleanOldWebCapturesAsync()
    {
        try
        {
            var keep = _media.GetAll().Select(m => m.Path).ToList();
            var deleted = await Task.Run(() =>
            {
                var folder = WebCapturesFolder;
                if (!Directory.Exists(folder))
                    return 0;

                var files = Directory.EnumerateFiles(folder, "web-*.png")
                    .Select(path => (Path: path, LastWrite: File.GetLastWriteTime(path)));
                var count = 0;
                foreach (var path in WebCaptures.Expired(files, DateTime.Now, keep))
                {
                    try
                    {
                        File.Delete(path);
                        count++;
                    }
                    catch (IOException)
                    {
                        // Still open somewhere: the next start gets it.
                    }
                }

                return count;
            });

            if (deleted > 0)
                Log.Information("Se borraron {Count} fotos de páginas web de más de {Days} días", deleted, WebCaptures.KeepDays);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "No se pudieron limpiar las fotos de páginas web");
        }
    }

    private static string WebCapturesFolder => Path.Combine(App.DataDir, "capturas");

    // ── El panel ─────────────────────────────────────────────────

    /// <summary>The «🌐 Web» button: the page takes the place of the slide grid.</summary>
    [RelayCommand]
    private void OpenWeb()
    {
        if (!EnsureWebRuntime())
            return;

        IsWebPanelOpen = true;
        StatusText = IsWebLive
            ? "Panel web abierto: la página sigue en vivo en la pantalla."
            : "Panel web: escribí una dirección o lo que quieras buscar. Se proyecta con 📷 o con 🔴 En vivo.";
    }

    /// <summary>«✕ Cerrar»: back to the slides. A page that is live keeps being projected.</summary>
    [RelayCommand]
    private void CloseWeb()
    {
        IsWebPanelOpen = false;
        StatusText = IsWebLive
            ? "Panel web cerrado: la página sigue en vivo en la pantalla."
            : "Panel web cerrado.";
    }

    private bool EnsureWebRuntime()
    {
        if (WebViewProfile.IsRuntimeAvailable())
            return true;

        MessageBox.Show(
            "Falta el runtime de WebView2 (Microsoft Edge WebView2), que EcclesiaCast usa para YouTube y las páginas web.\n\n" +
            "Se descarga gratis desde el sitio de Microsoft; en Windows 11 suele venir instalado.",
            "EcclesiaCast", MessageBoxButton.OK, MessageBoxImage.Information);
        return false;
    }

    /// <summary>Opens a page in the panel, without projecting anything.</summary>
    private void OpenWebPage(string address, string name)
    {
        if (!EnsureWebRuntime() || WebHost is null)
            return;

        NoteAction($"Abrir la página «{name}» en el panel web");
        IsWebPanelOpen = true;
        WebHost.Open(address);
        StatusText = $"«{name}» abierta en el panel web. Se proyecta con 📷 o con 🔴 En vivo.";
    }

    // ── Modo foto ────────────────────────────────────────────────

    /// <summary>
    /// «📷 Proyectar foto»: a picture of the page as it is right now, shown as
    /// one more image — on the whole screen over the lyrics, fitted — and
    /// kept in the folder of captures. The screen does not change again until
    /// the next photo, however much the operator keeps browsing.
    /// </summary>
    [RelayCommand]
    private async Task ProjectWebPhoto()
    {
        if (WebHost is not { IsReady: true, CurrentAddress: { } address } host)
        {
            StatusText = "Abrí una página para sacarle una foto.";
            return;
        }

        var name = host.CurrentTitle ?? AddressInput.DisplayName(address);
        NoteAction($"Sacar una foto de la página «{name}»");

        var folder = WebCapturesFolder;
        var path = Path.Combine(folder, WebCaptures.UniqueFileName(DateTime.Now, file => File.Exists(Path.Combine(folder, file))));
        if (!await host.CaptureAsync(path))
        {
            StatusText = "No se pudo sacar la foto de la página.";
            return;
        }

        _lastWebPhoto = (path, address, name);

        ApplyBackgroundCore(new MediaItem
        {
            Id = _nextWebPhotoId--,
            Name = name,
            Path = path,
            ThumbnailPath = path,
            Type = MediaType.Image,
            // A page is read, not decorated: all of it has to show, and over
            // the lyrics, like an announcement.
            Scaling = MediaScaling.Fit,
            Behavior = MediaBehavior.Foreground,
            EndBehavior = VideoEndBehavior.Stop,
        });
        LiveTitle = $"🌐 {name} (foto)";
        StatusText = $"Foto de «{name}» en pantalla. Seguí navegando: la pantalla no cambia hasta la próxima foto.";
    }

    // ── Modo en vivo ─────────────────────────────────────────────

    /// <summary>«🔴 En vivo»: on, the screen follows the page; off, it comes off the screen.</summary>
    [RelayCommand]
    private void ToggleWebLive()
    {
        if (IsWebLive)
        {
            ClearBackground();
            StatusText = "La página dejó de proyectarse en vivo.";
            return;
        }

        if (WebHost is not { CurrentAddress: { } address } host)
        {
            StatusText = "Abrí una página para proyectarla en vivo.";
            return;
        }

        StartWebLive(address, host.CurrentTitle ?? AddressInput.DisplayName(address));
    }

    /// <summary>
    /// Puts the page on the screen in motion. It takes the place of whatever
    /// background there was; projecting anything else as the background takes
    /// it off (see <see cref="UpdateStateFlags"/>, which turns the button off).
    /// </summary>
    private void StartWebLive(string address, string name)
    {
        NoteAction($"Proyectar en vivo la página «{name}»");
        ApplyBackgroundCore(new MediaItem
        {
            Id = WebLiveId,
            Name = name,
            Path = address,
            Type = MediaType.Web,
            Scaling = MediaScaling.Fit,
            Behavior = MediaBehavior.Foreground,
            EndBehavior = VideoEndBehavior.Stop,
        });
        LiveTitle = $"🌐 {name} (en vivo)";
        StatusText = $"«{name}» en vivo: la pantalla sigue lo que hacés en la página.";
    }

    // ── Guardar en los medios ────────────────────────────────────

    /// <summary>
    /// «Guardar en medios». With a photo taken of the page that is open, it is
    /// saved as an image; without one, the page itself is saved — a card with
    /// its title and a thumbnail that opens it again with a click.
    /// </summary>
    [RelayCommand]
    private async Task SaveWebToMedia()
    {
        if (WebHost is not { IsReady: true, CurrentAddress: { } address } host)
        {
            StatusText = "Abrí una página para guardarla en los medios.";
            return;
        }

        var tab = SelectedMediaTab;

        if (_lastWebPhoto is { } photo && photo.Address == address && File.Exists(photo.Path))
        {
            NoteAction($"Guardar la foto de «{photo.Name}» en los medios");
            _media.Add(new MediaItem
            {
                Name = photo.Name,
                Path = photo.Path,
                Type = MediaType.Image,
                ThumbnailPath = MediaThumbnails.Create(photo.Path, MediaType.Image, videoEngine: null),
                Category = tab,
                Scaling = MediaScaling.Fit,
                Behavior = MediaBehavior.Foreground,
            });
            _lastWebPhoto = null;
            LoadMedia();
            StatusText = $"Foto de «{photo.Name}» guardada en «{tab}» como imagen.";
            return;
        }

        if (_allMedia.Any(m => m.Type == MediaType.Web && m.Path == address
                               && string.Equals(m.Category, tab, StringComparison.OrdinalIgnoreCase)))
        {
            StatusText = $"Esa página ya está en «{tab}».";
            return;
        }

        var name = host.CurrentTitle ?? AddressInput.DisplayName(address);
        NoteAction($"Guardar la página «{name}» en los medios");
        var thumbnail = await MakeWebThumbnailAsync(host);

        _media.Add(new MediaItem
        {
            Name = name,
            Path = address,
            Type = MediaType.Web,
            ThumbnailPath = thumbnail,
            Category = tab,
            Scaling = MediaScaling.Fit,
            Behavior = MediaBehavior.Foreground,
            EndBehavior = VideoEndBehavior.Stop,
        });
        LoadMedia();
        StatusText = $"Página «{name}» guardada en «{tab}». Un clic en la tarjeta la abre en el panel web.";
    }

    /// <summary>The card's picture: a photo of the page, made small like the other posters. Null if it could not be taken.</summary>
    private static async Task<string?> MakeWebThumbnailAsync(IWebPageHost host)
    {
        var temp = Path.Combine(Path.GetTempPath(), $"ecclesiacast-web-{Guid.NewGuid():N}.png");
        try
        {
            if (!await host.CaptureAsync(temp))
                return null;

            var poster = MediaThumbnails.Create(temp, MediaType.Image, videoEngine: null);

            // When no poster could be drawn the answer is the source itself,
            // which is about to go: keep a copy where the library keeps its own.
            if (string.Equals(poster, temp, StringComparison.OrdinalIgnoreCase))
            {
                Directory.CreateDirectory(MediaThumbnails.Directory);
                poster = Path.Combine(MediaThumbnails.Directory, $"{Guid.NewGuid():N}.png");
                File.Copy(temp, poster);
            }

            return poster;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "No se pudo hacer la miniatura de la página");
            return null;
        }
        finally
        {
            try
            {
                File.Delete(temp);
            }
            catch (IOException)
            {
                // Es un archivo temporal: el sistema lo limpia.
            }
        }
    }

    /// <summary>Right click on a saved page → «Cambiar nombre…» (its Propiedades are about framing, which a page has none of).</summary>
    [RelayCommand]
    private void RenameMedia(MediaItem? item)
    {
        if (item is null)
            return;

        var typed = _textPrompt.Ask("Cambiar nombre", "Nombre de la tarjeta:", item.Name);
        if (string.IsNullOrWhiteSpace(typed) || typed.Trim() == item.Name)
            return;

        NoteAction($"Cambiar el nombre de «{item.Name}» a «{typed.Trim()}»");
        item.Name = typed.Trim();
        _media.Update(item);
        LoadMedia();
        StatusText = $"Ahora se llama «{item.Name}».";
    }
}
