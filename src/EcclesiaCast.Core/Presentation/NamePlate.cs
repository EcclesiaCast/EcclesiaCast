using System.Text.Json;

namespace EcclesiaCast.Core.Presentation;

/// <summary>
/// A name plate (a «lower third», asked for in GitHub #37): who is speaking or
/// singing, in a card at the bottom left, over the picture — «Pastor Juan
/// Pérez / Predicación». It rides over everything like the «aviso al pie», on
/// the projector and on the stream.
/// </summary>
public sealed record NamePlate(string Name, string Role = "")
{
    public bool IsEmpty => string.IsNullOrWhiteSpace(Name) && string.IsNullOrWhiteSpace(Role);

    /// <summary>«Juan Pérez · Predicación», for lists and the status bar.</summary>
    public override string ToString() =>
        string.IsNullOrWhiteSpace(Role) ? Name.Trim() : $"{Name.Trim()} · {Role.Trim()}";

    /// <summary>Both lines trimmed; null when nothing is left.</summary>
    public static NamePlate? Clean(string? name, string? role)
    {
        var plate = new NamePlate((name ?? "").Trim(), (role ?? "").Trim());
        return plate.IsEmpty ? null : plate;
    }
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
                .Select(p => NamePlate.Clean(p?.Name, p?.Role))
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
