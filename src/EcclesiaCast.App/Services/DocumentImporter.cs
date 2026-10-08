using System.IO;
using Serilog;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;

namespace EcclesiaCast.App.Services;

/// <summary>What came out of importing a document, page by page.</summary>
public sealed record DocumentPages(IReadOnlyList<string> Pages, string? Error);

/// <summary>
/// Turns a PDF — or a PowerPoint deck, through PowerPoint itself — into one
/// image per page, so the pages join the media library and project like any
/// other background.
///
/// Rendering happens once, on import, rather than every time a page goes up:
/// a service is no place to find out that a document is slow to draw, and
/// the rest of the program already knows how to show an image.
/// </summary>
public static class DocumentImporter
{
    /// <summary>Where the rendered pages live, beside the rest of the church's data.</summary>
    private static string Root => Path.Combine(App.DataDir, "documents");

    public static bool IsDocument(string path) =>
        Path.GetExtension(path).ToLowerInvariant() is ".pdf" or ".pptx" or ".ppt";

    /// <summary>
    /// The long edge, in pixels, pages are drawn at: 4K. The output is 1920
    /// wide, so this leaves room to zoom in on a page (the Encuadre) and still
    /// have real detail, instead of enlarging a screen-sized copy.
    /// </summary>
    public const int PageLongEdge = 3840;

    /// <summary>The PDF a document's pages were drawn from, kept beside them to draw them again.</summary>
    private const string SourceName = "origen.pdf";

    /// <summary>
    /// Renders every page to a PNG and returns their paths in order.
    /// <paramref name="longEdge"/> is the long edge in pixels: a portrait page
    /// is that tall, a landscape one that wide.
    /// </summary>
    public static async Task<DocumentPages> ImportAsync(string path, int longEdge = PageLongEdge)
    {
        try
        {
            var folder = Path.Combine(Root, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);

            if (await KeepSourceAsync(path, folder) is { } failure)
            {
                try { Directory.Delete(folder, true); } catch (IOException) { /* se queda vacía */ }
                return new DocumentPages([], failure);
            }

            var document = await LoadAsync(Path.Combine(folder, SourceName));
            var pages = new List<string>();
            for (uint index = 0; index < document.PageCount; index++)
            {
                var target = Path.Combine(folder, $"pagina-{index + 1:000}.png");
                await RenderPageAsync(document, index, target, longEdge);
                pages.Add(target);
            }

            Log.Information("Documento {Path} importado: {Pages} páginas", path, pages.Count);
            return new DocumentPages(pages, null);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "No se pudo importar el documento {Path}", path);
            return new DocumentPages([], $"No se pudo leer el documento: {ex.Message}");
        }
    }

    /// <summary>
    /// True when the document this page came from is still kept, so its pages
    /// can be drawn again without asking for the file. Pages imported before
    /// the source was kept have none.
    /// </summary>
    public static bool HasSource(string pagePath) =>
        Path.GetDirectoryName(pagePath) is { } folder && File.Exists(Path.Combine(folder, SourceName));

    /// <summary>
    /// Draws all the pages of the document <paramref name="pagePath"/> belongs
    /// to again, at 4K, over the same files — so every library item that
    /// points at them is sharper without being touched. Uses the kept PDF;
    /// <paramref name="source"/> (the original PDF or deck, picked by the
    /// operator) is only needed when none was kept.
    /// </summary>
    public static async Task<DocumentPages> ReimportAsync(
        string pagePath, string? source = null, int longEdge = PageLongEdge)
    {
        try
        {
            var folder = Path.GetDirectoryName(pagePath);
            if (folder is null || !IsRenderedPage(pagePath))
                return new DocumentPages([], "Esta imagen no es una página importada de un documento.");

            // A document picked by hand replaces the kept one only once it has
            // proved to be the same (same number of pages): a wrong pick must
            // not overwrite a good source.
            var kept = Path.Combine(folder, SourceName);
            var candidate = kept;
            string? staged = null;
            if (source is not null)
            {
                staged = Path.Combine(folder, "candidato.pdf");
                if (await KeepSourceAsync(source, folder, staged) is { } failure)
                    return new DocumentPages([], failure);
                candidate = staged;
            }

            if (!File.Exists(candidate))
                return new DocumentPages([], "Falta el documento original para dibujar las páginas de nuevo.");

            var existing = Directory.GetFiles(folder, "pagina-*.png").Order(StringComparer.OrdinalIgnoreCase).ToList();
            var document = await LoadAsync(candidate);

            // The pages are rewritten by position, so it has to be the same document.
            if (document.PageCount != existing.Count)
            {
                if (staged is not null)
                    File.Delete(staged);
                return new DocumentPages([],
                    $"Ese documento tiene {document.PageCount} páginas y en la biblioteca hay {existing.Count}: "
                    + "no parece ser el mismo. Elegí el original de estas páginas.");
            }

            for (var index = 0; index < existing.Count; index++)
            {
                // Drawn aside and swapped in, so a failure halfway never leaves a half-written page.
                var temporary = existing[index] + ".nuevo";
                await RenderPageAsync(document, (uint)index, temporary, longEdge);
                File.Move(temporary, existing[index], overwrite: true);
            }

            if (staged is not null)
                File.Move(staged, kept, overwrite: true);

            Log.Information("Documento {Folder} dibujado de nuevo: {Pages} páginas", folder, existing.Count);
            return new DocumentPages(existing, null);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "No se pudo volver a importar {Path}", pagePath);
            return new DocumentPages([], $"No se pudo leer el documento: {ex.Message}");
        }
    }

    /// <summary>
    /// Puts the document, as a PDF, in the folder of its pages. A deck goes
    /// through PowerPoint first. Returns the message to show when it can't.
    /// </summary>
    private static async Task<string?> KeepSourceAsync(string path, string folder, string? target = null)
    {
        target ??= Path.Combine(folder, SourceName);

        if (Path.GetExtension(path).ToLowerInvariant() is ".pptx" or ".ppt")
        {
            // PowerPoint takes its time with a big deck, and it is driven
            // through COM, which blocks whoever calls it. Off the UI
            // thread, so the operator's window keeps answering.
            if (await Task.Run(() => PowerPointConverter.ToPdf(path)) is not { } converted)
            {
                return "Para traer una presentación de PowerPoint hace falta tener PowerPoint instalado en esta "
                    + "computadora. Si no lo tenés, abrila donde sí esté y guardala como PDF: el PDF entra sin problema.";
            }

            File.Move(converted, target, overwrite: true);
        }
        else
        {
            File.Copy(path, target, overwrite: true);
        }

        return null;
    }

    private static async Task<PdfDocument> LoadAsync(string pdfPath) =>
        await PdfDocument.LoadFromFileAsync(await StorageFile.GetFileFromPathAsync(pdfPath));

    private static async Task RenderPageAsync(PdfDocument document, uint index, string target, int longEdge)
    {
        using var page = document.GetPage(index);

        // Only one side is given and the other follows the page's shape.
        var options = new PdfPageRenderOptions();
        if (page.Size.Width >= page.Size.Height)
            options.DestinationWidth = (uint)longEdge;
        else
            options.DestinationHeight = (uint)longEdge;

        using var stream = new InMemoryRandomAccessStream();
        await page.RenderToStreamAsync(stream, options);

        using var output = File.Create(target);
        await stream.AsStreamForRead().CopyToAsync(output);
    }

    /// <summary>
    /// Deletes the image of a page the operator removed from the library, and
    /// the document's folder once its last page is gone. Rendered pages are
    /// EcclesiaCast's own files — unlike the church's videos, which are only
    /// ever pointed at — so removing the item has to remove them too or they
    /// pile up in AppData for good.
    /// </summary>
    public static void Forget(string pagePath)
    {
        var folder = Path.GetDirectoryName(pagePath);
        if (folder is null || !folder.StartsWith(Root, StringComparison.OrdinalIgnoreCase))
            return;

        try
        {
            if (File.Exists(pagePath))
                File.Delete(pagePath);

            // The kept PDF goes with the last page: it is only there to draw them again.
            if (Directory.Exists(folder) && Directory.GetFiles(folder, "pagina-*.png").Length == 0)
                Directory.Delete(folder, true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A page still open somewhere stays on disk; nothing breaks.
            Log.Debug(ex, "No se pudo borrar la página {Path}", pagePath);
        }
    }

    /// <summary>True when this file is a page EcclesiaCast rendered itself.</summary>
    public static bool IsRenderedPage(string path) =>
        path.StartsWith(Root, StringComparison.OrdinalIgnoreCase);
}
