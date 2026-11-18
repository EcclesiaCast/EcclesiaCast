namespace EcclesiaCast.Presentation.Platform;

/// <summary>Draws the pages of a PDF as pictures.</summary>
public interface IDocumentRenderer
{
    /// <summary>Opens the PDF at <paramref name="pdfPath"/> to draw its pages.</summary>
    Task<IRenderableDocument> OpenAsync(string pdfPath);
}

/// <summary>An open PDF.</summary>
public interface IRenderableDocument
{
    int PageCount { get; }

    /// <summary>
    /// Draws page <paramref name="index"/> (from 0) as a PNG at
    /// <paramref name="target"/>. <paramref name="longEdge"/> is the long edge
    /// in pixels: a portrait page is that tall, a landscape one that wide.
    /// </summary>
    Task RenderPageAsync(int index, string target, int longEdge);
}

/// <summary>Turns a slide deck into a PDF with the program the church has for it.</summary>
public interface IDeckConverter
{
    /// <summary>
    /// The PDF's path, or null when the program that opens decks is not
    /// installed. Blocks while it works: call it off the screen's thread.
    /// </summary>
    string? ToPdf(string deckPath);
}
