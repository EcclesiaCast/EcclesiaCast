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

    /// <summary>
    /// The installed versions that are the same one as <paramref name="entry"/>:
    /// the ones a new copy of it takes the place of. More than one when the
    /// library already had it twice.
    /// </summary>
    public static IReadOnlyList<BibleVersionInfo> Matching(BiblePackageEntry entry, IEnumerable<BibleVersionInfo> installed) =>
        installed.Where(v => IsSame(entry, v)).ToList();

    /// <summary>
    /// The question asked once for every chosen Bible the library already has.
    /// With <paramref name="othersToo"/> (there are also new ones), «No» brings
    /// only the new ones, so the question says so.
    /// </summary>
    /// <param name="verb">«bajar» or «importar».</param>
    public static string ReplaceQuestion(IReadOnlyList<string> names, string verb, bool othersToo)
    {
        var text = names.Count == 1
            ? $"Ya tenés «{names[0]}» en tu biblioteca.\n\n¿La querés {verb} igual? Se borra la que tenés y queda la nueva en su lugar."
            : $"Ya tenés estas Biblias en tu biblioteca: {string.Join(", ", names)}.\n\n¿Las querés {verb} igual? Se borran las que tenés y quedan las nuevas en su lugar.";
        if (othersToo)
            text += names.Count == 1
                ? $"\n\nSí: se reemplaza.\nNo: se {(verb == "bajar" ? "bajan" : "importan")} sólo las que no tenés."
                : $"\n\nSí: se reemplazan.\nNo: se {(verb == "bajar" ? "bajan" : "importan")} sólo las que no tenés.";
        return text;
    }

    private static bool IsInstalled(BiblePackageEntry entry, List<BibleVersionInfo> installed) =>
        installed.Any(v => IsSame(entry, v));

    private static bool IsSame(BiblePackageEntry entry, BibleVersionInfo version) =>
        SameKey(version.Abbreviation, entry.Abbreviation) || SameKey(version.Name, entry.Name);

    private static string Normalize(string text) =>
        new(text.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
}
