using EcclesiaCast.Core.Bible;
using EcclesiaCast.Core.Songs;
using EcclesiaCast.Core.Themes;
using EcclesiaCast.Data.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace EcclesiaCast.Proto;

/// <summary>
/// The prototype's own library: a folder of its own (never the real
/// EcclesiaCast one), the same database and migrations as the program, the
/// bundled RV1909 Bible and two test songs on the first start.
/// </summary>
public sealed class ProtoData
{
    public const string FolderName = "EcclesiaCast-proto";
    private const string SeededKey = "proto.seeded";

    public string Folder { get; }
    public string DbPath { get; }
    public string LogFolder => Path.Combine(Folder, "logs");

    public SongRepository Songs { get; }
    public BibleRepository Bibles { get; }
    public ThemeRepository Themes { get; }
    public SqliteSettingsStore Settings { get; }

    private ProtoData(string folder)
    {
        Folder = folder;
        DbPath = Path.Combine(folder, "ecclesiacast.db");
        Songs = new SongRepository(DbPath);
        Bibles = new BibleRepository(DbPath);
        Themes = new ThemeRepository(DbPath);
        Settings = new SqliteSettingsStore(DbPath);
    }

    /// <summary>
    /// Where the library lives. On a Mac it has to be
    /// <c>~/Library/Application Support</c>: .NET gives that for
    /// <see cref="Environment.SpecialFolder.LocalApplicationData"/> (since
    /// .NET 8), while <see cref="Environment.SpecialFolder.ApplicationData"/>
    /// gives <c>~/.config</c> there. On Windows it is the roaming AppData, like
    /// the program. <c>ECCLESIACAST_PROTO_DATA_DIR</c> overrides both.
    /// </summary>
    public static string DefaultFolder()
    {
        var overridden = Environment.GetEnvironmentVariable("ECCLESIACAST_PROTO_DATA_DIR");
        if (!string.IsNullOrWhiteSpace(overridden))
            return overridden;

        var root = OperatingSystem.IsMacOS()
            ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
            : Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(root, FolderName);
    }

    /// <summary>
    /// The folder holding the bundled Bibles: next to the program, or in the
    /// Mac bundle's <c>Contents/Resources</c>.
    /// </summary>
    public static string BiblesFolder()
    {
        var beside = Path.Combine(AppContext.BaseDirectory, "bibles");
        if (Directory.Exists(beside))
            return beside;

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "Resources", "bibles"));
    }

    /// <summary>Creates or opens the library, migrating it and seeding it once.</summary>
    public static ProtoData Open(string folder, string biblesFolder)
    {
        Directory.CreateDirectory(folder);
        var data = new ProtoData(folder);

        using (var db = new AppDbContext(data.DbPath))
            db.Database.Migrate();

        ThemeSeeder.EnsureDefaults(data.Themes, data.Settings);

        var imported = BundledBibleImporter.Run(biblesFolder, data.Bibles, data.Settings);
        foreach (var name in imported.Imported)
            Log.Information("Biblia importada: {Name}", name);
        foreach (var name in imported.Failed)
            Log.Error("No se pudo importar la Biblia {Name}", name);

        if (data.Settings.Get(SeededKey) != "1")
        {
            data.Seed();
            data.Settings.Set(SeededKey, "1");
        }

        return data;
    }

    private void Seed()
    {
        Songs.Save(new Song
        {
            Title = "Prueba: Sublime gracia",
            Artist = "Dominio público",
            Sections =
            [
                new SongSection { Order = 0, Label = "Verso 1", Text = "Sublime gracia del Señor\nque a un pecador salvó;\nfui ciego mas hoy veo yo,\nperdido y Él me halló." },
                new SongSection { Order = 1, Label = "Verso 2", Text = "Su gracia me enseñó a temer,\nmis dudas ahuyentó;\n¡oh, cuán precioso fue a mi ser\ncuando Él me transformó!" },
                new SongSection { Order = 2, Label = "Verso 3", Text = "En los peligros o aflicción\nque yo he tenido aquí,\nsu gracia siempre me libró\ny me guiará feliz." },
            ],
        });

        Songs.Save(new Song
        {
            Title = "Prueba: renglones largos ñandú",
            Artist = "EcclesiaCast",
            Sections =
            [
                new SongSection { Order = 0, Label = "Verso", Text = "Una línea bastante larga para ver dónde corta el renglón en la Mac y si coincide con Windows: ¿acá? ¡o acá!" },
                new SongSection { Order = 1, Label = "Coro", Text = "Áéíóú ÁÉÍÓÚ ñÑ üÜ ¿¡\nmayúsculas y acentos" },
            ],
        });

        // The starting themes have no outline. The prototype has to show
        // outline AND shadow, so its own library turns the outline on.
        foreach (var theme in Themes.GetAll())
        {
            theme.OutlineWidth = 6;
            theme.OutlineColor = "#000000";
            Themes.Save(theme);
        }

        Log.Information("Primer arranque: dos canciones de prueba y contorno 6 en los temas");
    }

    public SlideTheme SongTheme() => DefaultTheme(ThemeSeeder.DefaultSongThemeKey);

    public SlideTheme BibleTheme() => DefaultTheme(ThemeSeeder.DefaultBibleThemeKey);

    private SlideTheme DefaultTheme(string key) =>
        ThemeSeeder.GetDefaultId(Settings, key) is { } id && Themes.Get(id) is { } theme
            ? theme
            : new SlideTheme();
}
