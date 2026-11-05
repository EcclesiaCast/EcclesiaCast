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

/// <summary>How a tab used as a slideshow goes from one media to the next.</summary>
public enum MediaSlideshow
{
    /// <summary>The tab is not a slideshow.</summary>
    Off,

    /// <summary>One after another in the order of the tab, starting over at the end.</summary>
    Sequential,

    /// <summary>In a random order, without repeating until all have come up.</summary>
    Random,
}

/// <summary>
/// The rule of one tab: «everything here is a foreground», for a tab of
/// announcements, or «everything here is a background», for a tab of loops.
/// It is worked out when the media goes on screen, so changing the rule never
/// rewrites the library: take it off and each media goes back to what its own
/// properties say. <paramref name="Muted"/> silences every media of the tab
/// the same way (the tab is for announcements read by someone, say).
/// <paramref name="Slideshow"/> makes the tab play on by itself, in order or at
/// random: each video until it ends, each image for <paramref name="SlideSeconds"/>
/// unless its own Propiedades say otherwise.
/// </summary>
public sealed record MediaTabRule(
    MediaTabMode Mode,
    bool LogoAtEnd,
    bool Muted = false,
    MediaSlideshow Slideshow = MediaSlideshow.Off,
    int SlideSeconds = MediaTabRule.DefaultSlideSeconds)
{
    public const int DefaultSlideSeconds = 10;

    public const int MaxSlideSeconds = 3600;

    public static MediaTabRule Default { get; } = new(MediaTabMode.PerItem, false);

    /// <summary>True when the tab goes from one media to the next by itself.</summary>
    public bool IsSlideshow => Slideshow != MediaSlideshow.Off;

    /// <summary>The media as it should play in this tab: the same object when the rule changes nothing.</summary>
    public MediaItem Resolve(MediaItem item)
    {
        var resolved = ResolveLayer(item);
        if (!IsSlideshow)
            return resolved;

        // A slideshow has to move on: a looping video would never end, and a
        // picture with no seconds would never leave.
        var seconds = item.Type == MediaType.Image && item.ShowSeconds <= 0 ? SlideSeconds : item.ShowSeconds;
        var end = item.Type == MediaType.Video && resolved.EndBehavior == VideoEndBehavior.Loop
            ? VideoEndBehavior.Stop
            : resolved.EndBehavior;
        if (seconds == resolved.ShowSeconds && end == resolved.EndBehavior)
            return resolved;

        if (ReferenceEquals(resolved, item))
            resolved = item.Clone();
        resolved.ShowSeconds = seconds;
        resolved.EndBehavior = end;
        return resolved;
    }

    private MediaItem ResolveLayer(MediaItem item)
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
        if (Muted)
            mode += "+mute";
        if (IsSlideshow)
            mode += (Slideshow == MediaSlideshow.Random ? "+random" : "+show") + $"+secs{SlideSeconds}";
        return mode;
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
        var slideshow = extras.Contains("random") ? MediaSlideshow.Random
            : extras.Contains("show") ? MediaSlideshow.Sequential
            : MediaSlideshow.Off;

        var seconds = DefaultSlideSeconds;
        var secs = extras.FirstOrDefault(e => e.StartsWith("secs", StringComparison.Ordinal));
        if (secs is not null && int.TryParse(secs.AsSpan(4), out var parsed))
            seconds = ClampSeconds(parsed);

        return new MediaTabRule(
            mode.Value,
            mode == MediaTabMode.Foreground && extras.Contains("logo"),
            extras.Contains("mute"),
            slideshow,
            seconds);
    }

    /// <summary>Seconds a picture stays in a slideshow, kept to something that makes sense.</summary>
    public static int ClampSeconds(int seconds) => Math.Clamp(seconds, 1, MaxSlideSeconds);
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

    /// <summary>
    /// A name for a new tab that no other tab has: <paramref name="baseName"/>
    /// itself when it is free, otherwise «X (2)», «X (3)»… Capitals do not
    /// tell tabs apart, as everywhere else in the bar.
    /// </summary>
    public static string Unique(IEnumerable<string> tabs, string baseName)
    {
        var taken = tabs.ToList();
        var name = baseName.Trim();
        if (name.Length == 0)
            name = Starting[0];

        var candidate = name;
        for (var number = 2; taken.Contains(candidate, StringComparer.OrdinalIgnoreCase); number++)
            candidate = $"{name} ({number})";
        return candidate;
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
