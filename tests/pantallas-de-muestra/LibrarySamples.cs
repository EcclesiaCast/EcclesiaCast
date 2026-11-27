using System.IO;
using System.Reflection;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Bible;
using EcclesiaCast.Core.Songs;
using EcclesiaCast.Core.Songs.Web;
using EcclesiaCast.Core.Themes;
using EcclesiaCast.Presentation.Platform;

namespace EcclesiaCast.ScreenSamples;

/// <summary>
/// The sample data of the screens of the Bible, the songs and Ajustes
/// (batch 16). The WPF bench and the Avalonia one compile this same file, so
/// both pictures of a screen are always made with the same things.
/// </summary>
internal static class LibrarySamples
{
    public static IReadOnlyList<SlideTheme> Themes() =>
        [new SlideTheme { Id = 1, Name = "Azul clásico" }, new SlideTheme { Id = 2, Name = "Letra grande" }];

    public static Song Song() => new()
    {
        Id = 7,
        Title = "Cuán grande es Él",
        Artist = "Tradicional",
        Copyright = "Dominio público",
        ThemeId = 9,
        Sections = LyricsParser.Parse(
            "[Estrofa 1]\nSeñor mi Dios, al contemplar los cielos\nel firmamento y las estrellas mil\n\n"
            + "[Coro]\nMi corazón entona la canción\n¡Cuán grande es Él!"),
    };

    public static IReadOnlyList<BiblePackageChoice> PackageChoices() =>
    [
        new(new BiblePackageEntry("a.json", "Reina Valera 1909", "RV1909", "es"), AlreadyInstalled: true),
        new(new BiblePackageEntry("b.json", "Biblia del Oso 1569", "BDO", "es"), AlreadyInstalled: false),
    ];

    public static IReadOnlyList<BibleVersionInfo> InstalledVersions() =>
    [
        new(1, "Reina Valera 1909", "RV1909", "es", 31102),
        new(2, "Biblia del Oso 1569", "BDO", "es", 31102),
    ];

    public static IReadOnlyList<BibleCatalogItem> Catalog() =>
    [
        Item("Reina Valera 1909", "RV1909", "es", 1_450_000, "Dominio público"),
        Item("Biblia del Oso 1569", "BDO", "es", 900, string.Empty),
        Item("World English Bible", "WEB", "en", 1_300_000, "Dominio público"),
    ];

    public static IReadOnlyList<bool> CatalogInstalled() => [true, false, false];

    private static BibleCatalogItem Item(string name, string abbreviation, string language, long size, string license) =>
        new(name, abbreviation, language, new Uri("https://example.com/" + abbreviation + ".json"), size,
            new string('a', 64), license);

    public const string SearchQuery = "santo";

    public static IReadOnlyList<EcclesiaCast.Presentation.Services.SearchHit> SearchHits(string query) =>
    [
        new(EcclesiaCast.Presentation.Services.SearchHitKind.Song, 1, "🎵", "Santo, santo, santo", "Reginald Heber"),
        new(EcclesiaCast.Presentation.Services.SearchHitKind.Verse, 1, "📖", "Isaías 6:3 · RV1909",
            "Santo, santo, santo, Jehová de los ejércitos"),
        new(EcclesiaCast.Presentation.Services.SearchHitKind.Media, 3, "🎞", "Fondo santuario", "Video"),
    ];

    /// <summary>
    /// A trash with a song and a theme, deleted a few days before today at
    /// noon: both benches, run the same day, write the same dates.
    /// </summary>
    public static (ISongRepository Songs, IThemeRepository Themes) Trash(bool empty = false)
    {
        var day = DateTime.UtcNow.Date.AddHours(15);
        IReadOnlyList<Song> songs = empty
            ? []
            : [new Song { Id = 1, Title = "Renuévame", DeletedAt = day.AddDays(-2) }];
        IReadOnlyList<SlideTheme> themes = empty
            ? []
            : [new SlideTheme { Id = 2, Name = "Prueba de colores", DeletedAt = day.AddDays(-29) }];
        return (Sample.Of<ISongRepository>(("GetDeleted", songs)), Sample.Of<IThemeRepository>(("GetDeleted", themes)));
    }

    /// <summary>
    /// A made-up documents folder with two ProPresenter libraries, always in
    /// the same place (its path is written on the screen). Nothing of this
    /// computer is looked at.
    /// </summary>
    public static IPlatformPaths ImportPaths()
    {
        var root = Path.Combine(Path.GetTempPath(), "ecclesiacast-banco-canciones");
        if (Directory.Exists(root))
            Directory.Delete(root, recursive: true);

        Library(root, "Alabanza", 3);
        Library(root, "Himnario", 12);
        return new SamplePaths(root);
    }

    private static void Library(string root, string name, int songs)
    {
        var folder = Path.Combine(root, "Documents", "ProPresenter", "Libraries", name);
        Directory.CreateDirectory(folder);
        for (var i = 1; i <= songs; i++)
            File.WriteAllText(Path.Combine(folder, $"Canción {i}.pro"), @"{\rtf1\ansi Letra}");
    }

    public static ISongRepository NoSongs() =>
        Sample.Of<ISongRepository>(("GetTitles", (IReadOnlyList<SongTitle>)[]));

    private sealed class SamplePaths(string root) : IPlatformPaths
    {
        public string DataFolder => Path.Combine(root, "datos");
        public string DownloadsFolder => Path.Combine(root, "videos");
        public string MusicFolder => Path.Combine(root, "musica");
        public string AudioCacheFolder => Path.Combine(root, "cache");
        public string ToolsFolder => Path.Combine(root, "tools");
        public string ModelsFolder => Path.Combine(root, "modelos");
        public string LightCopiesFolder => Path.Combine(root, "copias");
        public string BundledFolder => Path.Combine(root, "incluido");
        public string SoftouchFolder => Path.Combine(root, "Softouch");
        public IReadOnlyList<string> DocumentFolders => [Path.Combine(root, "Documents")];
    }

    /// <summary>A web search that answers at once with the same songs, without going out to the internet.</summary>
    public sealed class WebSongs : IWebSongSource
    {
        public Task<IReadOnlyList<WebSongHit>> SearchMusicaComAsync(string query, CancellationToken cancel = default) =>
            Task.FromResult<IReadOnlyList<WebSongHit>>(
            [
                new(WebSongSource.MusicaCom, "Renuévame", "Marcos Witt", "https://www.musica.com/letras.asp?letra=1"),
                new(WebSongSource.MusicaCom, "Renuévame Señor", "Anónimo", "https://www.musica.com/letras.asp?letra=2"),
            ]);

        public Task<IReadOnlyList<WebSongHit>> SearchLrclibAsync(string query, CancellationToken cancel = default) =>
            Task.FromResult<IReadOnlyList<WebSongHit>>(
                [new(WebSongSource.Lrclib, "Renuévame", "Danilo Montero", "https://lrclib.net/api/get/3", "Renuévame\nSeñor Jesús")]);

        public Task<WebSong?> GetSongAsync(WebSongHit hit, CancellationToken cancel = default) =>
            Task.FromResult<WebSong?>(new WebSong(hit.Title, hit.Artist,
                "Renuévame, Señor Jesús\nya no quiero ser igual\n\nPon en mí tu corazón"));

        public void Dispose()
        {
        }
    }

    /// <summary>A stand-in for an interface: it answers what it was told, and nothing else.</summary>
    public class Sample : DispatchProxy
    {
        private (string Method, object? Answer)[] _answers = [];

        public static T Of<T>(params (string Method, object? Answer)[] answers) where T : class
        {
            var proxy = Create<T, Sample>();
            ((Sample)(object)proxy)._answers = answers;
            return proxy;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            foreach (var (method, answer) in _answers)
            {
                if (method == targetMethod!.Name)
                    return answer;
            }

            var type = targetMethod!.ReturnType;
            return type != typeof(void) && type.IsValueType ? Activator.CreateInstance(type) : null;
        }
    }
}
