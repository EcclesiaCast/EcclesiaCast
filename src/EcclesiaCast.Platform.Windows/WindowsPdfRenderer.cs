using EcclesiaCast.Presentation.Platform;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;

namespace EcclesiaCast.Platform.Windows;

/// <summary>
/// Draws PDF pages with Windows' own PDF reader (Windows.Data.Pdf), which
/// adds no dependency to the installer.
/// </summary>
public sealed class WindowsPdfRenderer : IDocumentRenderer
{
    public async Task<IRenderableDocument> OpenAsync(string pdfPath) =>
        new Document(await PdfDocument.LoadFromFileAsync(await StorageFile.GetFileFromPathAsync(pdfPath)));

    private sealed class Document(PdfDocument document) : IRenderableDocument
    {
        public int PageCount => (int)document.PageCount;

        public async Task RenderPageAsync(int index, string target, int longEdge)
        {
            using var page = document.GetPage((uint)index);

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
    }
}
