using System.Text;
using EcclesiaCast.Core.Songs;

namespace EcclesiaCast.Core.Tests.Songs;

public class TxtSongImporterTests
{
    [Fact]
    public void Filename_becomes_the_title()
    {
        var song = TxtSongImporter.FromText("Sublime Gracia.txt", "Sublime gracia del Señor");

        Assert.Equal("Sublime Gracia", song.Title);
        Assert.Equal(string.Empty, song.Artist);
    }

    [Fact]
    public void Filename_with_dash_splits_title_and_artist()
    {
        var song = TxtSongImporter.FromText("Mi Gozo - Generación 12.txt", "Letra");

        Assert.Equal("Mi Gozo", song.Title);
        Assert.Equal("Generación 12", song.Artist);
    }

    [Fact]
    public void Content_is_parsed_into_paragraph_slides()
    {
        var song = TxtSongImporter.FromText("Test.txt", "Uno\ndos\n\nTres");

        Assert.Equal(2, song.Sections.Count);
        Assert.Equal("Uno\ndos", song.Sections[0].Text);
    }
}

public class RtfTests
{
    [Fact]
    public void Converts_a_propresenter_style_block_to_plain_text()
    {
        const string rtf = @"{\rtf0\ansi\ansicpg1252{\fonttbl\f0\fnil ArialMT;}" +
            @"{\colortbl;\red255\green255\blue255;}{\*\expandedcolortbl;\csgenericrgb\c100000;}" +
            @"\uc1\pard\qc\fs100 Mejor es un d\u237 ?a en tu casa" +
            @"\par\pard\qc Que mil a\u241 ?os lejos de ti}";

        var text = Rtf.ToPlainText(rtf);

        Assert.Equal("Mejor es un día en tu casa\nQue mil años lejos de ti", text);
    }

    [Fact]
    public void Decodes_hex_escapes()
    {
        Assert.Equal("Amén", Rtf.ToPlainText(@"{\rtf0 Am\'e9n}"));
    }

    [Fact]
    public void Keeps_escaped_braces_and_backslashes()
    {
        Assert.Equal(@"a{b}c\d", Rtf.ToPlainText(@"{\rtf0 a\{b\}c\\d}"));
    }

    [Fact]
    public void Style_only_blocks_produce_empty_text()
    {
        const string rtf = @"{\rtf0\ansi{\fonttbl\f0\fnil ArialMT;}{\colortbl;\red0\green0\blue0;}\uc1\pard\fs100}";

        Assert.Equal(string.Empty, Rtf.ToPlainText(rtf));
    }
}

public class ProPresenterImporterTests
{
    private static byte[] BuildFile(params string[] rtfBlocks)
    {
        var random = new byte[] { 0x0a, 0x24, 0x92, 0x01, 0xff, 0x00, 0x12 };
        var stream = new MemoryStream();
        foreach (var block in rtfBlocks)
        {
            stream.Write(random);
            stream.Write(Encoding.Latin1.GetBytes(block));
        }
        stream.Write(random);
        return stream.ToArray();
    }

    [Fact]
    public void Extracts_slides_in_order_from_binary_content()
    {
        var file = BuildFile(
            @"{\rtf0\ansi\uc1\pard Primera l\u237 ?nea}",
            @"{\rtf0\ansi\uc1\pard Segunda}");

        var song = ProPresenterImporter.FromFile("Mi Canción.pro", file);

        Assert.Equal("Mi Canción", song.Title);
        Assert.Equal(2, song.Sections.Count);
        Assert.Equal("Primera línea", song.Sections[0].Text);
        Assert.Equal("Segunda", song.Sections[1].Text);
    }

    [Fact]
    public void Skips_empty_style_blocks_and_consecutive_duplicates()
    {
        var file = BuildFile(
            @"{\rtf0\ansi{\fonttbl\f0\fnil Arial;}\pard\fs100}",
            @"{\rtf0 Coro}",
            @"{\rtf0 Coro}",
            @"{\rtf0 Verso}");

        var song = ProPresenterImporter.FromFile("x.pro", file);

        Assert.Equal(2, song.Sections.Count);
        Assert.Equal("Coro", song.Sections[0].Text);
        Assert.Equal("Verso", song.Sections[1].Text);
    }
}
