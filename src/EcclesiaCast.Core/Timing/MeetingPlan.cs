using System.Text.Json;

namespace EcclesiaCast.Core.Timing;

/// <summary>One part of the meeting and the minutes it is given.</summary>
public sealed record MeetingSection(string Name, int Minutes);

/// <summary>The parts of the meeting, in order, as they are kept in the settings.</summary>
public static class MeetingPlan
{
    public const int MaxMinutes = 600;

    /// <summary>What a church starts with; each one is changed in the tab.</summary>
    public static IReadOnlyList<MeetingSection> Default { get; } =
    [
        new("Alabanza", 25),
        new("Anuncios", 5),
        new("Prédica", 35),
    ];

    /// <summary>A section as it is kept: a name (never empty) and minutes between 0 and <see cref="MaxMinutes"/>.</summary>
    public static MeetingSection Clean(MeetingSection section) =>
        new(string.IsNullOrWhiteSpace(section.Name) ? "Sección" : section.Name.Trim(),
            Math.Clamp(section.Minutes, 0, MaxMinutes));

    public static string Serialize(IEnumerable<MeetingSection> sections) =>
        JsonSerializer.Serialize(sections.Select(Clean));

    /// <summary>The saved sections; the default ones when nothing (or something unreadable) is saved.</summary>
    public static List<MeetingSection> Deserialize(string? setting)
    {
        if (!string.IsNullOrWhiteSpace(setting))
        {
            try
            {
                if (JsonSerializer.Deserialize<List<MeetingSection>>(setting) is { } saved)
                    return saved.Select(Clean).ToList();
            }
            catch (JsonException)
            {
                // A damaged setting is the same as none: start again from the defaults.
            }
        }

        return [.. Default];
    }

    /// <summary>The section after <paramref name="index"/>, or null when it was the last one.</summary>
    public static int? NextIndex(int index, int count) => index + 1 < count ? index + 1 : null;
}
