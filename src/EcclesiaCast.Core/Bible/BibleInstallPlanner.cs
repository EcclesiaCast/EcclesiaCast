using EcclesiaCast.Core.Abstractions;

namespace EcclesiaCast.Core.Bible;

/// <summary>A version a package offers, and whether the library already has it.</summary>
public sealed record BiblePackageChoice(BiblePackageEntry Entry, bool AlreadyInstalled);

/// <summary>
/// Decides which Bibles a package brings that the library does not have yet.
/// Two versions are the same one when their abbreviation or their name match
/// once case, spaces and punctuation are ignored («RV 1909» = «rv1909»).
/// </summary>
public static class BibleInstallPlanner
{
    /// <summary>Setting that remembers which bundled Bibles were already brought in.</summary>
    public const string ImportedSettingKey = "bibles.bundled.imported";

    /// <summary>Setting that remembers which package the operator was already offered.</summary>
    public const string PackageSeenSettingKey = "bibles.package.seen";

    private const char Separator = '|';

    /// <summary>The same version, however it was typed.</summary>
    public static bool SameKey(string a, string b) => Normalize(a) == Normalize(b) && Normalize(a).Length > 0;

    /// <summary>Every offered version, marked when the library already has it.</summary>
    public static IReadOnlyList<BiblePackageChoice> Offer(
        IEnumerable<BiblePackageEntry> offered, IEnumerable<BibleVersionInfo> installed)
    {
        var have = installed.ToList();
        return offered
            .Select(e => new BiblePackageChoice(e, IsInstalled(e, have)))
            .ToList();
    }

    /// <summary>
    /// What to bring in from the Bibles the installer left: not installed, and
    /// not brought in before. A Bible the operator deleted stays deleted
    /// because its abbreviation is in <paramref name="rememberedSetting"/>.
    /// </summary>
    public static IReadOnlyList<BiblePackageEntry> PendingBundled(
        IEnumerable<BiblePackageEntry> offered, IEnumerable<BibleVersionInfo> installed, string? rememberedSetting)
    {
        var have = installed.ToList();
        var remembered = ParseRemembered(rememberedSetting);
        return offered
            .Where(e => !IsInstalled(e, have) && !remembered.Any(r => SameKey(r, e.Abbreviation)))
            .ToList();
    }

    /// <summary>The setting's value after adding the abbreviations that were just brought in.</summary>
    public static string Remember(string? rememberedSetting, IEnumerable<string> abbreviations)
    {
        var all = ParseRemembered(rememberedSetting).ToList();
        foreach (var abbreviation in abbreviations)
        {
            if (!all.Any(r => SameKey(r, abbreviation)))
                all.Add(abbreviation.Replace(Separator, ' ').Trim());
        }

        return string.Join(Separator, all);
    }

    public static IReadOnlyList<string> ParseRemembered(string? setting) =>
        string.IsNullOrWhiteSpace(setting)
            ? []
            : setting.Split(Separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>
    /// True when the package at hand is not the one the operator was last
    /// offered. The fingerprint is made of the file's name, size and date, so a
    /// package from a later install is offered again but the same one never is.
    /// </summary>
    public static bool IsNewPackage(string? seenFingerprint, string fingerprint) =>
        !string.Equals(seenFingerprint, fingerprint, StringComparison.Ordinal);

    public static string Fingerprint(string fileName, long length, DateTime lastWriteUtc) =>
        $"{fileName.ToLowerInvariant()}|{length}|{lastWriteUtc.Ticks}";

    private static bool IsInstalled(BiblePackageEntry entry, List<BibleVersionInfo> installed) =>
        installed.Any(v => SameKey(v.Abbreviation, entry.Abbreviation) || SameKey(v.Name, entry.Name));

    private static string Normalize(string text) =>
        new(text.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
}
