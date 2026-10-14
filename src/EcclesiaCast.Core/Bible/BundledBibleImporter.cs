using EcclesiaCast.Core.Abstractions;

namespace EcclesiaCast.Core.Bible;

/// <summary>What bringing in the installer's Bibles did: the ones now in the library and the files that could not be read.</summary>
public sealed record BundledImportResult(IReadOnlyList<string> Imported, IReadOnlyList<string> Failed);

/// <summary>
/// Brings into the library the Bibles the installer left in a folder (the
/// <c>bibles</c> folder next to the program) that it does not have yet. What
/// was brought in is written down in the settings, so a Bible the operator
/// deletes afterwards stays deleted instead of coming back at every start.
/// </summary>
public static class BundledBibleImporter
{
    public static BundledImportResult Run(string folder, IBibleRepository bibles, ISettingsStore settings)
    {
        var imported = new List<string>();
        var failed = new List<string>();
        if (!Directory.Exists(folder))
            return new BundledImportResult(imported, failed);

        foreach (var file in Directory.EnumerateFiles(folder, "*" + BiblePackage.Extension).Order(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                IReadOnlyList<BiblePackageEntry> offered;
                using (var stream = File.OpenRead(file))
                    offered = BiblePackage.ReadManifest(stream);

                var installed = bibles.GetVersions();
                var pending = BibleInstallPlanner.PendingBundled(offered, installed, settings.Get(BibleInstallPlanner.ImportedSettingKey));

                // Whatever the library already has is written down too: if the
                // operator deletes it later, it must not come back.
                var alreadyThere = offered.Except(pending).Select(e => e.Abbreviation);
                Remember(settings, alreadyThere);

                foreach (var entry in pending)
                {
                    ParsedBible bible;
                    using (var stream = File.OpenRead(file))
                        bible = BiblePackage.ReadVersion(stream, entry);

                    if (bible.VerseCount == 0)
                    {
                        failed.Add(entry.Name);
                        continue;
                    }

                    var saved = bibles.Import(entry.Name, entry.Abbreviation, entry.Language, bible);
                    Remember(settings, [entry.Abbreviation]);
                    imported.Add(saved.Name);
                }
            }
            catch (Exception ex) when (ex is IOException or FormatException or UnauthorizedAccessException)
            {
                failed.Add(Path.GetFileName(file));
            }
        }

        return new BundledImportResult(imported, failed);
    }

    private static void Remember(ISettingsStore settings, IEnumerable<string> abbreviations)
    {
        var list = abbreviations.ToList();
        if (list.Count == 0)
            return;

        var before = settings.Get(BibleInstallPlanner.ImportedSettingKey);
        var after = BibleInstallPlanner.Remember(before, list);
        if (after != before)
            settings.Set(BibleInstallPlanner.ImportedSettingKey, after);
    }
}
