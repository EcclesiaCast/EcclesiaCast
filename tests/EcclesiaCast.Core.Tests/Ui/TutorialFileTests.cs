using System.Text.RegularExpressions;
using EcclesiaCast.Core.Ui;

namespace EcclesiaCast.Core.Tests.Ui;

public class TutorialFileTests
{
    private const string Valid = """
        # comentario
        titulo: Uno
        resumen: Un resumen
        icono: 🎵
        orden: 3

        [paso]
        señalar: SongsList
        titulo: A
        texto: Primera línea
        sigue acá.

        [paso]
        titulo: B
        texto: Sin elemento.

        [paso]
        senalar: SlidesPanel
        titulo: C
        texto: Con ene escrita sin tilde.
        """;

    [Fact]
    public void Reads_the_card_and_the_steps_and_joins_the_lines_of_a_text()
    {
        Assert.True(TutorialFile.TryParse("uno", Valid, out var t, out _));

        Assert.Equal("Uno", t!.Title);
        Assert.Equal("🎵", t.Icon);
        Assert.Equal(3, t.Order);
        Assert.Equal(3, t.Steps.Count);
        Assert.Equal("SongsList", t.Steps[0].Target);
        Assert.Equal("Primera línea sigue acá.", t.Steps[0].Text);
        Assert.Null(t.Steps[1].Target);
        Assert.Equal("SlidesPanel", t.Steps[2].Target);
    }

    [Theory]
    [InlineData("resumen: x\n[paso]\ntitulo: A\ntexto: a\n[paso]\ntitulo: B\ntexto: b\n[paso]\ntitulo: C\ntexto: c")] // sin título
    [InlineData("titulo: X\n[paso]\ntitulo: A\ntexto: a")]                                                    // muy corto
    [InlineData("titulo: X\n[paso]\ntitulo: A\n[paso]\ntitulo: B\ntexto: b\n[paso]\ntitulo: C\ntexto: c")]   // paso sin texto
    public void Rejects_a_file_that_does_not_make_sense_instead_of_breaking_the_list(string text)
    {
        Assert.False(TutorialFile.TryParse("x", text, out var t, out var error));
        Assert.Null(t);
        Assert.NotEmpty(error);
    }

    [Fact]
    public void Rejects_a_text_too_long_to_read_in_a_few_seconds()
    {
        var long_ = new string('a', WelcomeTour.MaxTextLength + 1);
        var text = $"titulo: X\n[paso]\ntitulo: A\ntexto: {long_}\n[paso]\ntitulo: B\ntexto: b\n[paso]\ntitulo: C\ntexto: c";

        Assert.False(TutorialFile.TryParse("x", text, out _, out var error));
        Assert.Contains("máximo", error);
    }

    [Fact]
    public void Ships_the_eight_first_tutorials_in_order()
    {
        var all = TutorialFile.LoadBuiltIn();

        Assert.Equal(
            ["Crear una canción nueva", "Armar la lista del domingo", "Proyectar la Biblia", "Poner fondos y videos",
             "Conectar con OBS o vMix", "Control desde el celular", "Placas de nombre", "Importar canciones de otro programa"],
            all.Select(t => t.Title));
        Assert.Equal(all.Count, all.Select(t => t.Id).Distinct().Count());
    }

    [Fact]
    public void Every_shipped_tutorial_has_five_to_eight_short_steps_with_a_summary()
    {
        Assert.All(TutorialFile.LoadBuiltIn(), t =>
        {
            Assert.InRange(t.Steps.Count, 5, 8);
            Assert.False(string.IsNullOrWhiteSpace(t.Summary));
            Assert.All(t.Steps, s => Assert.InRange(s.Text.Length, 20, WelcomeTour.MaxTextLength));
        });
    }

    [Fact]
    public void The_shipped_texts_say_español_and_funciona_never_castellano_or_anda()
    {
        var text = string.Join(' ', TutorialFile.LoadBuiltIn().SelectMany(t => t.Steps.Select(s => s.Title + " " + s.Text)));

        Assert.DoesNotContain("castellano", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotMatch(@"\banda(n)?\b", text);
    }

    [Fact]
    public void Every_element_a_shipped_tutorial_lights_is_named_in_the_main_window()
    {
        var xaml = File.ReadAllText(Path.Combine(RepoRoot(), "src", "EcclesiaCast.App", "MainWindow.xaml"));
        var names = Regex.Matches(xaml, "x:Name=\"([A-Za-z0-9_]+)\"").Select(m => m.Groups[1].Value).ToHashSet();

        var missing = TutorialFile.LoadBuiltIn()
            .SelectMany(t => t.Steps.Where(s => s.Target is not null).Select(s => $"{t.Id}: {s.Target}"))
            .Where(x => !names.Contains(x.Split(": ")[1]))
            .ToList();

        Assert.Empty(missing);
    }

    [Fact]
    public void A_file_in_the_data_folder_adds_a_tutorial_or_replaces_a_shipped_one_and_a_bad_one_is_ignored()
    {
        var folder = Path.Combine(Path.GetTempPath(), "tutoriales-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            File.WriteAllText(Path.Combine(folder, "nuevo.txt"), Valid.Replace("orden: 3", "orden: 99"));
            File.WriteAllText(Path.Combine(folder, "05-obs-vmix.txt"), Valid.Replace("titulo: Uno", "titulo: OBS de la iglesia"));
            File.WriteAllText(Path.Combine(folder, "roto.txt"), "esto no es un tutorial");

            var all = TutorialFile.LoadAll(folder);

            Assert.Equal(9, all.Count);
            Assert.Equal("Uno", all[^1].Title);
            Assert.Contains(all, t => t.Title == "OBS de la iglesia");
            Assert.DoesNotContain(all, t => t.Title == "Conectar con OBS o vMix");
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "src", "EcclesiaCast.App")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new DirectoryNotFoundException("No encontré la carpeta del repositorio.");
    }
}
