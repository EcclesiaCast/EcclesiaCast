using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Themes;

namespace EcclesiaCast.Data.Persistence;

/// <summary>Creates the two starting themes on first run and remembers which are the defaults.</summary>
public static class ThemeSeeder
{
    public const string DefaultSongThemeKey = "theme.default.song";
    public const string DefaultBibleThemeKey = "theme.default.bible";

    /// <summary>
    /// Starting size for verses. A verse is read, not sung along to from
    /// memory, so it wants the screen: 76 left a short verse sitting small in
    /// the middle, where BibleShow fills the frame. The fit still shrinks a
    /// long passage (or two versions) until it fits.
    /// </summary>
    private const double BibleMaxFontSize = 120;

    /// <summary>The size the Bible theme used to start from, before 120.</summary>
    private const double OldBibleMaxFontSize = 76;

    private const string BibleSizeUpgradedKey = "theme.bible.size-upgraded";

    public static void EnsureDefaults(IThemeRepository themes, ISettingsStore settings)
    {
        if (GetDefaultId(settings, DefaultSongThemeKey) is null
            || themes.Get(GetDefaultId(settings, DefaultSongThemeKey)!.Value) is null)
        {
            var song = themes.GetAll().FirstOrDefault(t => t.Kind == ThemeKind.Song)
                ?? themes.Save(new SlideTheme
                {
                    Name = "Canciones",
                    Kind = ThemeKind.Song,
                    TransparentBackground = true,
                });
            settings.Set(DefaultSongThemeKey, song.Id.ToString());
        }

        if (GetDefaultId(settings, DefaultBibleThemeKey) is null
            || themes.Get(GetDefaultId(settings, DefaultBibleThemeKey)!.Value) is null)
        {
            var bible = themes.GetAll().FirstOrDefault(t => t.Kind == ThemeKind.Bible)
                ?? themes.Save(new SlideTheme
                {
                    Name = "Biblia",
                    Kind = ThemeKind.Bible,
                    Bold = false,
                    MaxFontSize = BibleMaxFontSize,
                    ShowVerseNumbers = false,
                    TransparentBackground = true,
                });
            settings.Set(DefaultBibleThemeKey, bible.Id.ToString());
        }

        UpgradeBibleSize(themes, settings);
    }

    /// <summary>
    /// Once per library: a Bible theme still on the old starting size gets the
    /// new one. A size anybody chose by hand is left exactly as it is.
    /// </summary>
    private static void UpgradeBibleSize(IThemeRepository themes, ISettingsStore settings)
    {
        if (settings.Get(BibleSizeUpgradedKey) == "1")
            return;

        foreach (var theme in themes.GetAll().Where(t => t.Kind == ThemeKind.Bible))
        {
            if (Math.Abs(theme.MaxFontSize - OldBibleMaxFontSize) > 0.01)
                continue;
            theme.MaxFontSize = BibleMaxFontSize;
            themes.Save(theme);
        }

        settings.Set(BibleSizeUpgradedKey, "1");
    }

    public static int? GetDefaultId(ISettingsStore settings, string key) =>
        int.TryParse(settings.Get(key), out var id) ? id : null;
}
