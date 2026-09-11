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
    private static string Root => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "EcclesiaCast", "documents");

    public static bool IsDocument(string path) =>
        Path.GetExtension(path).ToLowerInvariant() is ".pdf" or ".pptx" or ".ppt";

    /// <summary>
    /// Renders every page to a PNG and returns their paths in order.
    /// <paramref name="width"/> is the long edge in pixels.
    /// </summary>
    public static async Task<DocumentPages> ImportAsync(string path, int width = 1920)
    {
        try
        {
            var pdfPath = path;
            var temporary = false;

            if (Path.GetExtension(path).ToLowerInvariant() is ".pptx" or ".ppt")
            {
                // PowerPoint takes its time with a big deck, and it is driven
                // through COM, which blocks whoever calls it. Off the UI
                // thread, so the operator's window keeps answering.
                if (await Task.Run(() => PowerPointConverter.ToPdf(path)) is not { } converted)
                {
                    return new DocumentPages([],
                        "Para traer una presentación de PowerPoint hace falta tener PowerPoint instalado en esta "
                        + "computadora. Si no lo tenés, abrila donde sí esté y guardala como PDF: el PDF entra sin problema.");
                }

                pdfPath = converted;
                temporary = true;
            }

            var folder = Path.Combine(Root, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);

            var file = await StorageFile.GetFileFromPathAsync(pdfPath);
            var document = await PdfDocument.LoadFromFileAsync(file);

            var pages = new List<string>();
            for (uint index = 0; index < document.PageCount; index++)
            {
                using var page = document.GetPage(index);
                var target = Path.Combine(folder, $"pagina-{index + 1:000}.png");

                using (var stream = new InMemoryRandomAccessStream())
                {
                    await page.RenderToStreamAsync(stream, new PdfPageRenderOptions
                    {
                        DestinationWidth = (uint)width,
                    });

                    using var output = File.Create(target);
                    await stream.AsStreamForRead().CopyToAsync(output);
                }

                pages.Add(target);
            }

            if (temporary)
            {
                try { File.Delete(pdfPath); } catch (IOException) { /* queda en el temporal */ }
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

            if (Directory.Exists(folder) && Directory.GetFileSystemEntries(folder).Length == 0)
                Directory.Delete(folder);
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
