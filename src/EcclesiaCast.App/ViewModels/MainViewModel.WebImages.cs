using System.IO;
using System.Net.Http;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Web;
using Serilog;

namespace EcclesiaCast.App.ViewModels;

/// <summary>
/// Pictures and GIFs from Google Images. There is no search of our own: the
/// web panel shows Google Images itself (so its filters, in Spanish, are all
/// there) and the browser's right-click menu gets «Agregar a medios», which
/// downloads the picture and brings it into the open tab like any file.
/// </summary>
public sealed partial class MainViewModel
{
    private static readonly HttpClient ImageHttp = CreateImageClient();

    private static HttpClient CreateImageClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        // Some sites turn away anything that does not look like a browser.
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36");
        client.DefaultRequestHeaders.Accept.ParseAdd("image/*,*/*;q=0.5");
        return client;
    }

    /// <summary>What was typed in the image box of the web panel.</summary>
    [ObservableProperty]
    private string _webImageQuery = string.Empty;

    /// <summary>The «Sólo licencia libre» box: Google's «Derechos de uso» filter.</summary>
    [ObservableProperty]
    private bool _webImageFreeOnly;

    /// <summary>Where the pictures brought in from the web live: the library points here.</summary>
    private static string WebImagesFolder => Path.Combine(App.DataDir, "imagenes-web");

    /// <summary>True when this is a picture the program downloaded (it goes with its library item).</summary>
    private static bool IsDownloadedWebImage(string path) =>
        path.StartsWith(WebImagesFolder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

    /// <summary>«🖼 Imágenes» of the media bar: the web panel, on Google Images.</summary>
    [RelayCommand]
    private void OpenImageSearch()
    {
        if (!EnsureWebRuntime() || WebHost is null)
            return;

        NoteAction("Abrir la búsqueda de imágenes");
        IsWebPanelOpen = true;
        var query = WebImageQuery.Trim();
        if (query.Length > 0)
            WebHost.Open(WebImages.SearchUrl(query, animated: false, WebImageFreeOnly).AbsoluteUri);
        else
            WebHost.Open("https://www.google.com/imghp?hl=es");

        StatusText = $"Google Imágenes en el panel web. Clic en una imagen para verla grande y, sobre la grande, "
                     + $"clic derecho → «Agregar a medios» (va a «{SelectedMediaTab}»).";
    }

    /// <summary>The buttons of the image row: <paramref name="kind"/> is «images» or «gifs».</summary>
    [RelayCommand]
    private void SearchWebImages(string? kind)
    {
        var query = WebImageQuery.Trim();
        if (query.Length == 0)
        {
            StatusText = "Escribí qué imagen buscás (por ejemplo: cruz, paloma, fuego).";
            return;
        }

        if (!EnsureWebRuntime() || WebHost is null)
            return;

        var animated = kind == "gifs";
        NoteAction($"Buscar {(animated ? "GIF" : "imágenes")} de «{query}»");
        IsWebPanelOpen = true;
        WebHost.Open(WebImages.SearchUrl(query, animated, WebImageFreeOnly).AbsoluteUri);
        StatusText = $"Buscando «{query}» en Google Imágenes. Clic en una imagen para verla grande y, sobre la grande, "
                     + $"clic derecho → «Agregar a medios» (va a «{SelectedMediaTab}»).";
    }

    private void OnWebImageChosen(object? sender, WebImageChosen chosen) => _ = AddWebImageAsync(chosen);

    /// <summary>
    /// Downloads the picture the operator right-clicked and brings it into the
    /// open tab. The bytes decide what it is (a server's word is not trusted),
    /// so an animated GIF goes through the same conversion as a dropped one.
    /// </summary>
    private async Task AddWebImageAsync(WebImageChosen chosen)
    {
        // Only Google's small copy is known: taking it would put a blurry picture
        // in the library. Better to say how to get the real one.
        if (chosen.IsThumbnail)
        {
            StatusText = "Eso es la miniatura de Google y se vería borrosa. Hacé clic en la imagen para verla grande "
                         + "a la derecha y probá con clic derecho sobre la grande.";
            return;
        }

        StatusText = "Bajando la imagen…";
        try
        {
            var bytes = WebImages.FromDataUri(chosen.Source) ?? await DownloadWebImageAsync(chosen);
            if (bytes is null)
            {
                StatusText = "No se pudo bajar la imagen: el sitio no la dejó. Probá con otra.";
                return;
            }

            var extension = WebImages.ExtensionFor(bytes.AsSpan(0, Math.Min(bytes.Length, 16)));
            if (extension is null)
            {
                StatusText = "Eso no es una imagen que se pueda agregar. Hacé clic en la imagen para verla grande y probá sobre la grande.";
                return;
            }

            var folder = WebImagesFolder;
            Directory.CreateDirectory(folder);
            var baseName = WebImages.FileNameFor(WebImageQuery);
            var file = WebImages.UniqueName(baseName, extension, name => File.Exists(Path.Combine(folder, name)));
            var path = Path.Combine(folder, file);
            await File.WriteAllBytesAsync(path, bytes);

            var size = ReadImageSize(bytes);
            NoteAction($"Agregar la imagen «{baseName}» de Google a los medios");
            ImportMediaFiles([path]);

            if (size is { Width: < WebImages.SmallWidth } small)
                StatusText = $"Imagen «{baseName}» agregada a «{SelectedMediaTab}», pero es chica ({small.Width}×{small.Height}): "
                             + "en una pantalla grande se va a ver pixelada. Si podés, elegí una más grande.";
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException or UnauthorizedAccessException)
        {
            Log.Warning(ex, "No se pudo agregar la imagen {Source}", chosen.Source);
            StatusText = "No se pudo bajar la imagen. Revisá la conexión a internet y probá de nuevo.";
        }
    }

    /// <summary>The bytes at the picture's address, or null when the site refuses or the file is too big.</summary>
    private static async Task<byte[]?> DownloadWebImageAsync(WebImageChosen chosen)
    {
        if (!Uri.TryCreate(chosen.Source, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            return null;

        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        // Many sites only serve their pictures to a page of their own or of a search engine.
        if (Uri.TryCreate(chosen.Page, UriKind.Absolute, out var page) && page.Scheme is "http" or "https")
            request.Headers.Referrer = page;

        using var response = await ImageHttp.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        if (!response.IsSuccessStatusCode)
        {
            Log.Information("La imagen {Source} respondió {Status}", chosen.Source, (int)response.StatusCode);
            return null;
        }

        if (response.Content.Headers.ContentLength is > WebImages.MaxBytes)
            return null;

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(chunk)) > 0)
        {
            buffer.Write(chunk, 0, read);
            if (buffer.Length > WebImages.MaxBytes)
                return null;
        }

        return buffer.ToArray();
    }

    /// <summary>Width and height of a picture, or null when Windows cannot read them (a format it has no codec for).</summary>
    private static (int Width, int Height)? ReadImageSize(byte[] bytes)
    {
        try
        {
            using var stream = new MemoryStream(bytes);
            var frame = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None).Frames[0];
            return (frame.PixelWidth, frame.PixelHeight);
        }
        catch (Exception ex) when (ex is NotSupportedException or InvalidOperationException or IOException or ArgumentException)
        {
            return null;
        }
    }
}
