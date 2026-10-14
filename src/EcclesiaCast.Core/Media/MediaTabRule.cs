using System.Text.Json;

namespace EcclesiaCast.Core.Media;

/// <summary>How a tab of the media bar decides where its media go.</summary>
public enum MediaTabMode
{
    /// <summary>Each media says it in its own properties.</summary>
    PerItem,

    /// <summary>Everything in the tab goes behind the text.</summary>
    Background,

    /// <summary>Everything in the tab takes the whole screen.</summary>
    Foreground,
}

/// <summary>
/// The rule of one tab: «everything here is a foreground», for a tab of
/// announcements, or «everything here is a background», for a tab of loops.
/// It is worked out when the media goes on screen, so changing the rule never
/// rewrites the library: take it off and each media goes back to what its own
/// properties say. <paramref name="Muted"/> silences every media of the tab
/// the same way (the tab is for announcements read by someone, say).
/// </summary>
public sealed record MediaTabRule(MediaTabMode Mode, bool LogoAtEnd, bool Muted = false)
{
    public static MediaTabRule Default { get; } = new(MediaTabMode.PerItem, false);

    /// <summary>The media as it should play in this tab: the same object when the rule changes nothing.</summary>
    public MediaItem Resolve(MediaItem item)
    {
        var behavior = Mode switch
        {
            MediaTabMode.Background => MediaBehavior.Background,
            MediaTabMode.Foreground => MediaBehavior.Foreground,
            _ => item.Behavior,
        };

        // A media that names its successor keeps it; otherwise a foreground
        // tab set to show the logo ends on it.
        var end = item.EndBehavior;
        if (Mode == MediaTabMode.Foreground && LogoAtEnd && end != VideoEndBehavior.NextMedia)
            end = VideoEndBehavior.Logo;

        // Only ever silences: a media already muted, or a tab that is not
        // muted, leaves the item as it is.
        var muted = item.Muted || Muted;

        if (behavior == item.Behavior && end == item.EndBehavior && muted == item.Muted)
            return item;

        var resolved = item.Clone();
        resolved.Behavior = behavior;
        resolved.EndBehavior = end;
        resolved.Muted = muted;
        return resolved;
    }

    /// <summary>The text kept in the settings: «foreground+logo», «background+mute», «item».</summary>
    public string ToSetting()
    {
        var mode = Mode switch
        {
            MediaTabMode.Background => "background",
            MediaTabMode.Foreground => "foreground",
            _ => "item",
        };
        if (Mode == MediaTabMode.Foreground && LogoAtEnd)
            mode += "+logo";
        return Muted ? mode + "+mute" : mode;
    }

    /// <summary>
    /// Reads the text of <see cref="ToSetting"/>: the mode first, then the
    /// extras after a «+». The texts saved before «mute» existed still read
    /// the same; anything unreadable is the default rule.
    /// </summary>
    public static MediaTabRule Parse(string? setting)
    {
        var parts = (setting ?? string.Empty).Trim().ToLowerInvariant().Split('+');
        var mode = parts[0].Trim() switch
        {
            "background" => MediaTabMode.Background,
            "foreground" => MediaTabMode.Foreground,
            "item" => MediaTabMode.PerItem,
            _ => (MediaTabMode?)null,
        };
        if (mode is null)
            return Default;

        var extras = parts.Skip(1).Select(p => p.Trim()).ToHashSet();
        return new MediaTabRule(
            mode.Value,
            mode == MediaTabMode.Foreground && extras.Contains("logo"),
            extras.Contains("mute"));
    }
}

/// <summary>
/// The list of tabs of the media bar. A tab is the category written on its
/// media, but one with nothing in it (just created) or in a chosen order has
/// to be remembered by itself, so the list is kept apart.
/// </summary>
public static class MediaTabNames
{
    /// <summary>The tabs every library starts with, in this order.</summary>
    public static readonly IReadOnlyList<string> Starting = ["Fondos", "Anuncios", "YouTube"];

    /// <summary>
    /// The saved tabs in their order, followed by any category in use that is
    /// not among them. With nothing saved yet, the starting tabs come first and
    /// the rest follow. Never empty: a bar with no tab has nowhere to add media.
    /// </summary>
    public static List<string> Merge(IReadOnlyList<string>? saved, IEnumerable<string> categories)
    {
        var tabs = new List<string>();
        foreach (var name in (saved ?? Starting).Concat(categories))
        {
            var clean = name?.Trim();
            if (!string.IsNullOrEmpty(clean) && !tabs.Contains(clean, StringComparer.OrdinalIgnoreCase))
                tabs.Add(clean);
        }

        if (tabs.Count == 0)
            tabs.Add(Starting[0]);
        return tabs;
    }

    /// <summary>
    /// Why <paramref name="name"/> cannot be the new name of the tab
    /// <paramref name="current"/> (empty, or another tab already has it), or
    /// null when it can. Changing only the capitals of the same tab is fine.
    /// </summary>
    public static string? WhyNotRename(IEnumerable<string> tabs, string current, string? name)
    {
        var wanted = name?.Trim();
        if (string.IsNullOrEmpty(wanted))
            return "La pestaña necesita un nombre.";

        var taken = tabs.Any(t =>
            string.Equals(t, wanted, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(t, current, StringComparison.OrdinalIgnoreCase));
        return taken ? $"Ya hay una pestaña llamada «{wanted}»." : null;
    }

    public static string Serialize(IEnumerable<string> tabs) => JsonSerializer.Serialize(tabs);

    /// <summary>The saved list, or null when nothing (or something unreadable) was saved.</summary>
    public static List<string>? Deserialize(string? setting)
    {
        if (string.IsNullOrWhiteSpace(setting))
            return null;

        try
        {
            return JsonSerializer.Deserialize<List<string>>(setting);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
