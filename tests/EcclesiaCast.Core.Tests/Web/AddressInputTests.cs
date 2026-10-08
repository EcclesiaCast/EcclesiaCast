using EcclesiaCast.Core.Web;

namespace EcclesiaCast.Core.Tests.Web;

public class AddressInputTests
{
    [Theory]
    [InlineData("google.com", "https://google.com/")]
    [InlineData("www.x.org", "https://www.x.org/")]
    [InlineData("  www.canva.com/design/abc  ", "https://www.canva.com/design/abc")]
    [InlineData("biblegateway.com/passage?search=Juan+3", "https://biblegateway.com/passage?search=Juan+3")]
    [InlineData("iglesia.org.ar", "https://iglesia.org.ar/")]
    public void Completes_an_address_without_scheme_with_https(string typed, string expected)
    {
        Assert.Equal(expected, AddressInput.Resolve(typed)!.AbsoluteUri);
    }

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=abc", "https://www.youtube.com/watch?v=abc")]
    [InlineData("http://iglesia.org/", "http://iglesia.org/")]
    [InlineData("HTTPS://Google.com", "https://google.com/")]
    public void Keeps_a_full_address_as_it_is(string typed, string expected)
    {
        Assert.Equal(expected, AddressInput.Resolve(typed)!.AbsoluteUri);
    }

    [Theory]
    [InlineData("localhost:3000", "http://localhost:3000/")]
    [InlineData("192.168.1.20:8080/panel", "http://192.168.1.20:8080/panel")]
    [InlineData("localhost", "http://localhost/")]
    public void Opens_the_own_computer_and_the_local_network_without_https(string typed, string expected)
    {
        Assert.Equal(expected, AddressInput.Resolve(typed)!.AbsoluteUri);
    }

    [Theory]
    [InlineData("juan 3 16 comentario")]
    [InlineData("Juan 3:16")]
    [InlineData("juan3.16")]
    [InlineData("3.16")]
    [InlineData("versiculo")]
    [InlineData("¿qué es la gracia?")]
    [InlineData("google .com")]
    public void Searches_on_google_whatever_is_not_an_address(string typed)
    {
        var uri = AddressInput.Resolve(typed)!;

        Assert.Equal("www.google.com", uri.Host);
        Assert.Equal("/search", uri.AbsolutePath);
        Assert.Equal(typed, Uri.UnescapeDataString(uri.Query["?q=".Length..]));
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("file:///C:/Windows/win.ini")]
    [InlineData("ftp://servidor.com/archivo")]
    public void Never_opens_a_scheme_other_than_http_and_searches_it_instead(string typed)
    {
        var uri = AddressInput.Resolve(typed)!;

        Assert.Equal("www.google.com", uri.Host);
        Assert.Equal(typed, Uri.UnescapeDataString(uri.Query["?q=".Length..]));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Gives_nothing_when_nothing_was_typed(string? typed)
    {
        Assert.Null(AddressInput.Resolve(typed));
    }

    [Theory]
    [InlineData("https://www.canva.com/design/abc", "canva.com")]
    [InlineData("https://iglesia.org/", "iglesia.org")]
    [InlineData("no es una dirección", "no es una dirección")]
    [InlineData("", "Página web")]
    [InlineData(null, "Página web")]
    public void Names_a_page_without_title_by_its_host(string? address, string expected)
    {
        Assert.Equal(expected, AddressInput.DisplayName(address));
    }
}
