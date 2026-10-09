using EcclesiaCast.Core.Updates;

namespace EcclesiaCast.Core.Tests.Updates;

public class ReleaseNotesTextTests
{
    [Fact]
    public void Headings_lose_their_hashes()
    {
        Assert.Equal("Agregado", ReleaseNotesText.ToPlainText("### Agregado"));
        Assert.Equal("Cambios", ReleaseNotesText.ToPlainText("# Cambios #"));
        Assert.Equal("Algo", ReleaseNotesText.ToPlainText("###### Algo"));
    }

    [Fact]
    public void A_heading_sits_right_above_what_it_introduces_with_a_blank_line_before_it()
    {
        var text = ReleaseNotesText.ToPlainText("Primero.\n\n### Agregado\n\n- Una cosa.\n\n### Corregido\n\n- Otra.");

        Assert.Equal("Primero.\n\nAgregado\n• Una cosa.\n\nCorregido\n• Otra.", text);
    }

    [Theory]
    [InlineData("- Uno", "• Uno")]
    [InlineData("* Uno", "• Uno")]
    [InlineData("+ Uno", "• Uno")]
    public void Bullets_become_dots(string markdown, string expected)
    {
        Assert.Equal(expected, ReleaseNotesText.ToPlainText(markdown));
    }

    [Fact]
    public void A_nested_bullet_is_indented_with_a_dash()
    {
        Assert.Equal("• Uno\n    – Adentro", ReleaseNotesText.ToPlainText("- Uno\n  - Adentro"));
    }

    [Fact]
    public void A_wrapped_bullet_is_joined_into_one_line()
    {
        var text = ReleaseNotesText.ToPlainText("- El programa avisa solo cuando\n  hay una versión nueva y la\n  instala con un botón.");

        Assert.Equal("• El programa avisa solo cuando hay una versión nueva y la instala con un botón.", text);
    }

    [Fact]
    public void Bullets_separated_by_blank_lines_keep_the_gap()
    {
        Assert.Equal("• Uno\n\n• Dos", ReleaseNotesText.ToPlainText("- Uno\n\n- Dos"));
    }

    [Fact]
    public void Bold_and_italic_lose_their_marks()
    {
        Assert.Equal("Esto es muy importante y raro.", ReleaseNotesText.ToPlainText("Esto es **muy importante** y *raro*."));
        Assert.Equal("Fuerte y suave", ReleaseNotesText.ToPlainText("__Fuerte__ y _suave_"));
    }

    [Fact]
    public void A_link_is_shown_as_its_text()
    {
        Assert.Equal(
            "Mirá la guía de uso.",
            ReleaseNotesText.ToPlainText("Mirá [la guía](https://github.com/EcclesiaCast/EcclesiaCast/blob/main/docs/guia-de-uso.md) de uso."));
        Assert.Equal("texto", ReleaseNotesText.ToPlainText("[texto](https://x.y/z \"título\")"));
    }

    [Fact]
    public void An_image_is_shown_as_its_description()
    {
        Assert.Equal("Captura", ReleaseNotesText.ToPlainText("![Captura](https://x.y/c.png)"));
    }

    [Fact]
    public void An_address_between_angle_brackets_is_shown_bare()
    {
        Assert.Equal("Más en https://x.y/z", ReleaseNotesText.ToPlainText("Más en <https://x.y/z>"));
    }

    [Fact]
    public void Code_loses_its_backticks_and_keeps_what_is_inside_untouched()
    {
        Assert.Equal("Usá ECCLESIACAST_DATA_DIR para *probar*.", ReleaseNotesText.ToPlainText("Usá `ECCLESIACAST_DATA_DIR` para `*probar*`."));
    }

    [Fact]
    public void Words_with_underscores_are_not_taken_for_italics()
    {
        Assert.Equal("el archivo mi_archivo_largo.txt", ReleaseNotesText.ToPlainText("el archivo mi_archivo_largo.txt"));
    }

    [Fact]
    public void A_lone_asterisk_is_left_alone()
    {
        Assert.Equal("2 * 3 = 6", ReleaseNotesText.ToPlainText("2 * 3 = 6"));
    }

    [Fact]
    public void Html_comments_and_tags_disappear()
    {
        Assert.Equal("Hola mundo", ReleaseNotesText.ToPlainText("Hola <!-- oculto --><b>mundo</b>"));
        Assert.Equal("Antes\n\nDespués", ReleaseNotesText.ToPlainText("Antes\n\n<!--\nvarias\nlíneas\n-->\n\nDespués"));
    }

    [Fact]
    public void Html_entities_are_decoded_and_escapes_removed()
    {
        Assert.Equal("Tom & Jerry *no* cursiva", ReleaseNotesText.ToPlainText("Tom &amp; Jerry \\*no\\* cursiva"));
    }

    [Fact]
    public void A_horizontal_rule_becomes_a_gap()
    {
        Assert.Equal("Uno\n\nDos", ReleaseNotesText.ToPlainText("Uno\n\n---\n\nDos"));
    }

    [Fact]
    public void A_quote_loses_its_mark()
    {
        Assert.Equal("Cuidado con esto.", ReleaseNotesText.ToPlainText("> Cuidado con esto."));
    }

    [Fact]
    public void A_code_block_keeps_its_lines_and_loses_its_fences()
    {
        var text = ReleaseNotesText.ToPlainText("Antes\n\n```powershell\ndotnet build\n  dotnet test\n```\n\nDespués");

        Assert.Equal("Antes\n\ndotnet build\n  dotnet test\n\nDespués", text);
    }

    [Fact]
    public void Windows_line_endings_give_the_same_result()
    {
        var unix = ReleaseNotesText.ToPlainText("### A\n\n- Uno\n  dos\n\n- Tres");
        var windows = ReleaseNotesText.ToPlainText("### A\r\n\r\n- Uno\r\n  dos\r\n\r\n- Tres");

        Assert.Equal(unix, windows);
    }

    [Fact]
    public void Github_automatic_changelog_link_is_dropped()
    {
        Assert.Equal(string.Empty, ReleaseNotesText.ToPlainText("**Full Changelog**: https://github.com/a/b/compare/v1...v2"));
    }

    [Fact]
    public void Nothing_in_gives_nothing_out()
    {
        Assert.Equal(string.Empty, ReleaseNotesText.ToPlainText(null));
        Assert.Equal(string.Empty, ReleaseNotesText.ToPlainText(""));
        Assert.Equal(string.Empty, ReleaseNotesText.ToPlainText("  \n\n  "));
        Assert.Equal(string.Empty, ReleaseNotesText.FromRelease(null));
    }

    [Fact]
    public void A_release_shows_only_what_follows_the_marker()
    {
        var body = "[Code signing policy](https://x.y) · [Política de privacidad](https://x.y/p)\n\n"
            + ReleaseNotesText.NotesMarker + "\n\n### Agregado\n\n- Algo nuevo.";

        Assert.Equal("Agregado\n• Algo nuevo.", ReleaseNotesText.FromRelease(body));
    }

    [Fact]
    public void A_release_without_the_marker_is_shown_whole()
    {
        Assert.Equal("• Uno", ReleaseNotesText.FromRelease("- Uno"));
    }

    [Fact]
    public void The_real_changelog_style_reads_well()
    {
        const string markdown = """
            ## [Sin publicar]

            Lo que salió del primer servicio en vivo (27/9/2026).

            ### Agregado

            - **Proyectar una página web.** El botón **🌐 Web** de la barra de medios abre
              una página en el cuadro central. Se escribe una dirección
              o lo que quieras buscar en Google.

            - **En Propiedades, el cuadro de vista previa.** Se ve la imagen entera.
            """;

        var text = ReleaseNotesText.ToPlainText(markdown);

        Assert.Equal(
            "[Sin publicar]\nLo que salió del primer servicio en vivo (27/9/2026).\n\nAgregado\n"
            + "• Proyectar una página web. El botón 🌐 Web de la barra de medios abre una página en el cuadro central. Se escribe una dirección o lo que quieras buscar en Google.\n\n"
            + "• En Propiedades, el cuadro de vista previa. Se ve la imagen entera.",
            text);
    }
}
