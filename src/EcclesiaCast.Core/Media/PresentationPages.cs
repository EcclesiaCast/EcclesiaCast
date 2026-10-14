using System.Text;

namespace EcclesiaCast.Core.Media;

/// <summary>
/// Which media of a tab make up a presentation, and how to tell that the
/// pages shown in the grid are no longer the tab's. A tab marked as a
/// presentation opens in the central grid like a song does, one card per page.
/// </summary>
public static class PresentationPages
{
    /// <summary>
    /// The pages of <paramref name="tab"/>, in the order the library lists
    /// them: the images and the videos kept on this computer. A YouTube video
    /// or a saved web page is not a page — it needs the internet, or a browser
    /// of its own, in the middle of a talk.
    /// </summary>
    public static List<MediaItem> For(string? tab, IEnumerable<MediaItem> library) =>
        string.IsNullOrWhiteSpace(tab)
            ? []
            : library
                .Where(m => string.Equals(m.Category, tab, StringComparison.OrdinalIgnoreCase))
                .Where(m => m.Type is MediaType.Image or MediaType.Video)
                .ToList();

    /// <summary>
    /// A text that changes whenever the grid built from these pages would
    /// look different (another page, another order, another poster or name).
    /// The library is reloaded after almost every change; the grid is only
    /// rebuilt when this differs.
    /// </summary>
    public static string Signature(IEnumerable<MediaItem> pages)
    {
        var text = new StringBuilder();
        foreach (var page in pages)
            text.Append(page.Id).Append('|').Append(page.Name).Append('|').Append(page.Path).Append('|')
                .Append(page.ThumbnailPath).Append(';');
        return text.ToString();
    }
}
