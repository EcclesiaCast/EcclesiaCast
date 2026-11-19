using System.Text.Json;

namespace EcclesiaCast.Core.Presentation;

/// <summary>
/// A name plate (a «lower third», asked for in GitHub #37): who is speaking or
/// singing, in a card at the bottom left, over the picture — «Pastor Juan
/// Pérez / Predicación». It rides over everything like the «aviso al pie», on
/// the projector and on the stream.
/// </summary>
/// <param name="ImagePath">
/// A picture or a short video beside the words (the church's logo, a photo, a
/// moving logo); null for words only. A video loops with no sound.
/// </param>
/// <param name="ImageRight">The picture goes on the right of the words instead of the left.</param>
/// <param name="Animation">How the card comes in (it always fades out).</param>
public sealed record NamePlate(
    string Name,
    string Role = "",
    string? ImagePath = null,
    bool ImageRight = false,
    NamePlateAnimation Animation = NamePlateAnimation.Slide)
{
    public bool IsEmpty => string.IsNullOrWhiteSpace(Name) && string.IsNullOrWhiteSpace(Role);

    public bool HasImage => !string.IsNullOrWhiteSpace(ImagePath);

    /// <summary>The picture beside the words is a video (by its extension).</summary>
    public bool ImageIsVideo => HasImage && NamePlateMedia.IsVideo(ImagePath);

    /// <summary>«Juan Pérez · Predicación», for lists and the status bar.</summary>
    public override string ToString() =>
        string.IsNullOrWhiteSpace(Role) ? Name.Trim() : $"{Name.Trim()} · {Role.Trim()}";

    /// <summary>Both lines trimmed; null when nothing is left.</summary>
    public static NamePlate? Clean(string? name, string? role) => Clean(name, role, null, false, NamePlateAnimation.Slide);

    /// <inheritdoc cref="Clean(string?, string?)"/>
    public static NamePlate? Clean(string? name, string? role, string? imagePath, bool imageRight, NamePlateAnimation animation)
    {
        var plate = new NamePlate(
            (name ?? "").Trim(),
            (role ?? "").Trim(),
            string.IsNullOrWhiteSpace(imagePath) ? null : imagePath.Trim(),
            imageRight,
            Enum.IsDefined(animation) ? animation : NamePlateAnimation.Slide);
        return plate.IsEmpty ? null : plate;
    }
}

/// <summary>How a name plate comes on the screen.</summary>
public enum NamePlateAnimation
{
    /// <summary>Slides in from the left while it fades in.</summary>
    Slide,

    /// <summary>Rises a little from below.</summary>
    Rise,

    /// <summary>Opens from its left edge, like a blind.</summary>
    Wipe,

    /// <summary>Only fades in.</summary>
    Fade,
}

/// <summary>The words for each animation, and which files a plate can carry.</summary>
public static class NamePlateMedia
{
    public static readonly IReadOnlyList<(NamePlateAnimation Animation, string Label)> Animations =
    [
        (NamePlateAnimation.Slide, "Deslizar desde el costado"),
        (NamePlateAnimation.Rise, "Subir desde abajo"),
        (NamePlateAnimation.Wipe, "Abrirse como persiana"),
        (NamePlateAnimation.Fade, "Sólo aparecer"),
    ];

    private static readonly HashSet<string> Images = new(StringComparer.OrdinalIgnoreCase)
        { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp" };

    private static readonly HashSet<string> Videos = new(StringComparer.OrdinalIgnoreCase)
        { ".mp4", ".webm", ".mov", ".m4v", ".wmv", ".avi", ".mkv" };

    /// <summary>For the file dialog: every picture and video a plate can carry.</summary>
    public const string DialogFilter =
        "Imágenes y videos|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp;*.mp4;*.webm;*.mov;*.m4v;*.wmv;*.avi;*.mkv";

    public static bool IsImage(string? path) => path is not null && Images.Contains(System.IO.Path.GetExtension(path));

    public static bool IsVideo(string? path) => path is not null && Videos.Contains(System.IO.Path.GetExtension(path));

    /// <summary>A file a plate can show beside its words.</summary>
    public static bool CanCarry(string? path) => IsImage(path) || IsVideo(path);
}

/// <summary>
/// The plates the church keeps ready (the pastor, the worship leader, a
/// visitor), saved in the settings so each Sunday they are one click away.
/// </summary>
public static class NamePlateList
{
    public const string SettingKey = "nameplates.list";

    /// <summary>How many plates the list keeps.</summary>
    public const int MaxPlates = 40;

    public static IReadOnlyList<NamePlate> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            return (JsonSerializer.Deserialize<List<NamePlate>>(json) ?? [])
                .Select(p => p is null ? null : NamePlate.Clean(p.Name, p.Role, p.ImagePath, p.ImageRight, p.Animation))
                .OfType<NamePlate>()
                .Take(MaxPlates)
                .ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static string ToSetting(IEnumerable<NamePlate> plates) =>
        JsonSerializer.Serialize(plates.Take(MaxPlates).ToList());

    /// <summary>
    /// The list with this plate saved: the same name and role are not repeated
    /// (a plate used again moves to the top, where it is found first).
    /// </summary>
    public static IReadOnlyList<NamePlate> Saving(IReadOnlyList<NamePlate> list, NamePlate plate) =>
        new[] { plate }
            .Concat(list.Where(p => !Same(p, plate)))
            .Take(MaxPlates)
            .ToList();

    public static IReadOnlyList<NamePlate> Removing(IReadOnlyList<NamePlate> list, NamePlate plate) =>
        list.Where(p => !Same(p, plate)).ToList();

    private static bool Same(NamePlate a, NamePlate b) =>
        string.Equals(a.Name.Trim(), b.Name.Trim(), StringComparison.CurrentCultureIgnoreCase)
        && string.Equals(a.Role.Trim(), b.Role.Trim(), StringComparison.CurrentCultureIgnoreCase);
}
