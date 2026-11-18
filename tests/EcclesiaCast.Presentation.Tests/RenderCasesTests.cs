using EcclesiaCast.Presentation.Rendering;

namespace EcclesiaCast.Presentation.Tests;

/// <summary>
/// Guards the render test set (tests/casos-de-render): every case can be read,
/// has its three themes, and has the saved reference of the WPF bench for each
/// of them. Nothing is drawn here; the benches do that.
/// </summary>
public class RenderCasesTests
{
    private static readonly string[] ThemeKeys = ["cancion", "biblia", "impact"];

    private static string CasesFolder =>
        RenderCase.FindFolder(AppContext.BaseDirectory)
        ?? throw new DirectoryNotFoundException("tests/casos-de-render was not found.");

    private static string ReferenceFolder => Path.Combine(CasesFolder, "referencia-wpf");

    [Fact]
    public void There_are_about_sixty_cases_and_all_can_be_read()
    {
        var cases = RenderCase.LoadAll(CasesFolder);

        Assert.InRange(cases.Count, 55, 80);
        Assert.Equal(cases.Count, cases.Select(c => c.Id).Distinct().Count());
    }

    [Fact]
    public void Every_case_has_its_id_equal_to_its_file_and_a_description()
    {
        foreach (var path in Directory.GetFiles(CasesFolder, "*.json"))
        {
            var c = RenderCase.Load(path);
            Assert.Equal(Path.GetFileNameWithoutExtension(path), c.Id);
            Assert.False(string.IsNullOrWhiteSpace(c.Description), c.Id);
        }
    }

    [Fact]
    public void Every_case_has_something_to_draw_and_the_three_complete_themes()
    {
        foreach (var c in RenderCase.LoadAll(CasesFolder))
        {
            Assert.Equal(ThemeKeys, c.Themes.Select(t => t.Key));

            var content = new object?[] { c.Slide, c.Countdown, c.Overlay, c.Logo, c.StageBox }.Count(x => x is not null);
            Assert.True(content > 0, $"{c.Id} draws nothing");

            if (c.Slide is not null)
                Assert.False(string.IsNullOrEmpty(c.Slide.MainText), c.Id);

            // The themes are the typefaces the references were taken with.
            Assert.Equal("Segoe UI", c.Themes[0].Theme.FontFamily);
            Assert.Equal("Segoe UI", c.Themes[1].Theme.FontFamily);
            Assert.Equal("Impact", c.Themes[2].Theme.FontFamily);

            foreach (var theme in c.Themes)
            {
                if (c.Slide is not null)
                    Assert.Same(theme.Theme, c.BuildSlide(theme)!.Theme);
                if (c.StageBox is not null)
                    Assert.Equal(theme.Theme.FontFamily, c.BuildStageBox(theme)!.FontFamily);
            }
        }
    }

    [Fact]
    public void Every_case_in_every_theme_has_its_saved_reference_and_no_reference_is_left_over()
    {
        var expected = RenderCase.LoadAll(CasesFolder)
            .SelectMany(c => c.Themes.Select(t => RenderResult.FileKey(c.Id, t.Key)))
            .ToHashSet();

        var saved = Directory.GetFiles(ReferenceFolder, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .ToHashSet();

        Assert.Empty(expected.Except(saved));
        Assert.Empty(saved.Except(expected));
    }

    [Fact]
    public void The_references_are_well_formed_and_belong_to_their_case()
    {
        foreach (var path in Directory.GetFiles(ReferenceFolder, "*.json"))
        {
            var result = RenderResult.Load(path);
            var name = Path.GetFileNameWithoutExtension(path);

            Assert.Equal(name, RenderResult.FileKey(result.Case, result.Theme));
            Assert.Equal(1920, result.Width);
            Assert.Equal(1080, result.Height);
            Assert.NotEmpty(result.Texts);

            foreach (var text in result.Texts)
            {
                Assert.True(text.FontSize > 0, $"{name} {text.Name}: font size");
                Assert.False(string.IsNullOrWhiteSpace(text.FontUsed), $"{name} {text.Name}: font");
                Assert.NotEmpty(text.Lines);
                Assert.True(text.Rect.Width > 0 && text.Rect.Height > 0, $"{name} {text.Name}: block");
            }
        }
    }

    [Fact]
    public void The_references_keep_the_lines_of_the_text_in_order()
    {
        // The words of a slide, minus spaces and breaks, are the words of its lines:
        // a line cut never loses or repeats a letter.
        foreach (var c in RenderCase.LoadAll(CasesFolder).Where(c => c.Slide is not null))
        {
            foreach (var theme in c.Themes.Where(t => t.Theme.TextCase == EcclesiaCast.Core.Themes.TextCase.None))
            {
                var path = Path.Combine(ReferenceFolder, RenderResult.FileKey(c.Id, theme.Key) + ".json");
                var main = RenderResult.Load(path).Texts.Single(t => t.Name == "main");

                Assert.Equal(Squeeze(c.Slide!.MainText), Squeeze(string.Concat(main.Lines)));
            }
        }

        static string Squeeze(string text) => new(text.Where(ch => !char.IsWhiteSpace(ch)).ToArray());
    }
}
