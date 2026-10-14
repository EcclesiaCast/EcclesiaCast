namespace EcclesiaCast.Core.Media;

/// <summary>
/// Decides which library items need their poster (the little picture shown
/// in the media bar) made again. The posters are PNG files kept apart from the
/// media, so they can disappear on their own — a sweep, a restored backup, a
/// library copied to another machine — while the database still points at
/// them; the bar then shows a blank card until something rebuilds them.
/// </summary>
public static class PosterPolicy
{
    /// <summary>
    /// True when the item should get a poster generated: it has none, or the
    /// file it points to is gone. YouTube items use the web thumbnail (a URL,
    /// not a file) and are left alone, and so is an item whose media file is
    /// missing too, because there is nothing to draw it from. A saved web page
    /// is in the same case: its poster was a photo of the page, and a page
    /// cannot be drawn again without opening it. A window has no poster at all
    /// (its path names a program, not a file).
    /// </summary>
    public static bool NeedsPoster(MediaItem item, Func<string, bool> fileExists)
    {
        if (item.Type is MediaType.YouTube or MediaType.Web or MediaType.Window || string.IsNullOrWhiteSpace(item.Path))
            return false;

        if (!fileExists(item.Path))
            return false;

        return string.IsNullOrWhiteSpace(item.ThumbnailPath) || !fileExists(item.ThumbnailPath);
    }
}
