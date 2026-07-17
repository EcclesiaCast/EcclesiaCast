using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Themes;

namespace EcclesiaCast.Data.Persistence;

/// <summary>Creates the two starting themes on first run and remembers which are the defaults.</summary>
public static class ThemeSeeder
{
    public const string DefaultSongThemeKey = "theme.default.song";
    public const string DefaultBibleThemeKey = "theme.default.bible";

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
                    MaxFontSize = 76,
                    ShowVerseNumbers = false,
                });
            settings.Set(DefaultBibleThemeKey, bible.Id.ToString());
        }
    }

    public static int? GetDefaultId(ISettingsStore settings, string key) =>
        int.TryParse(settings.Get(key), out var id) ? id : null;
}
