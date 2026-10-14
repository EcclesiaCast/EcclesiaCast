using EcclesiaCast.Core.Web;

namespace EcclesiaCast.Core.Tests.Web;

public class WebImagesTests
{
    [Fact]
    public void A_search_asks_google_images_in_spanish()
    {
        var url = WebImages.SearchUrl("cruz de madera", animated: false, freeLicense: false);

        Assert.Equal("https://www.google.com/search?tbm=isch&hl=es&q=cruz%20de%20madera", url.AbsoluteUri);
    }

    [Fact]
    public void Animated_and_free_license_become_the_tools_filter()
    {
        Assert.EndsWith("&tbs=itp:animated", WebImages.SearchUrl("fuego", true, false).AbsoluteUri);
        Assert.EndsWith("&tbs=sur:f", WebImages.SearchUrl("fuego", false, true).AbsoluteUri);
        Assert.EndsWith("&tbs=itp:animated,sur:f", WebImages.SearchUrl("fuego", true, true).AbsoluteUri);
    }

    [Fact]
    public void What_was_typed_cannot_add_parameters_to_the_address()
    {
        var url = WebImages.SearchUrl("a&tbs=x#y", false, false).AbsoluteUri;

        Assert.DoesNotContain("tbs=x", url);
        Assert.Contains("a%26tbs%3Dx%23y", url);
    }

    [Theory]
    [InlineData(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0, 0 }, ".jpg")]
    [InlineData(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, ".png")]
    [InlineData(new byte[] { (byte)'G', (byte)'I', (byte)'F', (byte)'8', (byte)'9', (byte)'a' }, ".gif")]
    [InlineData(new byte[] { (byte)'B', (byte)'M', 0, 0 }, ".bmp")]
    public void The_extension_comes_from_the_bytes(byte[] head, string expected)
    {
        Assert.Equal(expected, WebImages.ExtensionFor(head));
    }

    [Fact]
    public void A_webp_is_told_from_other_riff_files()
    {
        var webp = "RIFF\0\0\0\0WEBPVP8 "u8.ToArray();
        var wave = "RIFF\0\0\0\0WAVEfmt "u8.ToArray();

        Assert.Equal(".webp", WebImages.ExtensionFor(webp));
        Assert.Null(WebImages.ExtensionFor(wave));
    }

    [Fact]
    public void A_web_page_or_nothing_is_not_a_picture()
    {
        Assert.Null(WebImages.ExtensionFor("<!doctype html>"u8));
        Assert.Null(WebImages.ExtensionFor([]));
    }

    [Theory]
    [InlineData("cruz de madera", "cruz de madera")]
    [InlineData("  cruz   de  madera ", "cruz de madera")]
    [InlineData("a/b\\c:d*e?\"f<g>h|i", "abcdefghi")]
    [InlineData("", "Imagen de Google")]
    [InlineData("???", "Imagen de Google")]
    [InlineData("..", "Imagen de Google")]
    public void The_file_name_is_what_was_searched_without_what_windows_forbids(string typed, string expected)
    {
        Assert.Equal(expected, WebImages.FileNameFor(typed));
    }

    [Fact]
    public void A_long_search_gives_a_short_file_name()
    {
        Assert.True(WebImages.FileNameFor(new string('a', 200)).Length <= 40);
    }

    [Fact]
    public void A_taken_name_gets_the_first_free_number()
    {
        var taken = new HashSet<string> { "cruz.jpg", "cruz (2).jpg" };

        Assert.Equal("cruz (3).jpg", WebImages.UniqueName("cruz", ".jpg", taken.Contains));
        Assert.Equal("paloma.png", WebImages.UniqueName("paloma", ".png", taken.Contains));
    }

    [Fact]
    public void A_base64_data_address_gives_its_bytes()
    {
        var bytes = WebImages.FromDataUri("data:image/gif;base64,R0lGODlhAQABAAAAACw=");

        Assert.NotNull(bytes);
        Assert.Equal(".gif", WebImages.ExtensionFor(bytes));
    }

    [Theory]
    [InlineData("https://example.com/a.jpg")]
    [InlineData("data:text/html;base64,PGI+")]
    [InlineData("data:image/png,not-base64")]
    [InlineData("data:image/png;base64,!!!")]
    [InlineData(null)]
    public void Anything_else_is_not_a_data_picture(string? address)
    {
        Assert.Null(WebImages.FromDataUri(address));
    }
}
