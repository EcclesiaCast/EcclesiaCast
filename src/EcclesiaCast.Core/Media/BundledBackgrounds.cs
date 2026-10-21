namespace EcclesiaCast.Core.Media;

/// <summary>
/// The sample backgrounds the installer leaves in the <c>fondos</c> folder
/// next to the program. At start-up the ones not brought in yet go to the
/// library, in a tab of their own. What was brought in is written down, so a
/// background the operator takes out stays out instead of coming back at every
/// start.
/// </summary>
public static class BundledBackgrounds
{
    /// <summary>Setting that remembers which sample backgrounds were already brought in.</summary>
    public const string ImportedSettingKey = "media.bundled.imported";

    /// <summary>The tab they go to, so they don't mix with the church's own backgrounds.</summary>
    public const string Tab = "Fondos de ejemplo";

    /// <summary>The kind of file the installer leaves.</summary>
    public const string Extension = ".jpg";

    private const char Separator = '|';

    /// <summary>
    /// The files to bring in, by name: the ones not brought in before, in
    /// alphabetical order (the order the tab shows them in).
    /// </summary>
    public static IReadOnlyList<string> Pending(IEnumerable<string> fileNames, string? rememberedSetting)
    {
        var remembered = ParseRemembered(rememberedSetting);
        return fileNames
            .Where(f => f.EndsWith(Extension, StringComparison.OrdinalIgnoreCase))
            .Where(f => !remembered.Contains(f, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    /// <summary>The setting's value after adding the files that were just brought in.</summary>
    public static string Remember(string? rememberedSetting, IEnumerable<string> fileNames)
    {
        var all = ParseRemembered(rememberedSetting).ToList();
        foreach (var name in fileNames)
        {
            var clean = name.Replace(Separator, ' ').Trim();
            if (clean.Length > 0 && !all.Contains(clean, StringComparer.OrdinalIgnoreCase))
                all.Add(clean);
        }

        return string.Join(Separator, all);
    }

    /// <summary>The name a background gets in the library: its file's name, without «.jpg».</summary>
    public static string DisplayName(string fileName) => Path.GetFileNameWithoutExtension(fileName);

    private static IReadOnlyList<string> ParseRemembered(string? setting) =>
        string.IsNullOrWhiteSpace(setting)
            ? []
            : setting.Split(Separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
