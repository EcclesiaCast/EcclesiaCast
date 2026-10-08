using EcclesiaCast.Core.Media;

namespace EcclesiaCast.Core.Tests.Media;

public class YtDlpSupportTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 8, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Las_cookies_salen_en_formato_netscape_con_siete_columnas()
    {
        var text = NetscapeCookies.Format(
        [
            new BrowserCookie(".youtube.com", "/", "SID", "abc", Now.AddDays(30), true, false),
        ], Now);

        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.StartsWith("# Netscape HTTP Cookie File", lines[0]);
        var columns = lines[1].Split('\t');
        Assert.Equal(7, columns.Length);
        Assert.Equal(".youtube.com", columns[0]);
        Assert.Equal("TRUE", columns[1]);
        Assert.Equal("/", columns[2]);
        Assert.Equal("TRUE", columns[3]);
        Assert.Equal(Now.AddDays(30).ToUnixTimeSeconds().ToString(), columns[4]);
        Assert.Equal("SID", columns[5]);
        Assert.Equal("abc", columns[6]);
    }

    [Fact]
    public void Una_cookie_solo_para_un_host_no_incluye_subdominios()
    {
        var text = NetscapeCookies.Format(
            [new BrowserCookie("accounts.google.com", "/", "A", "1", Now.AddDays(1), false, false)], Now);

        Assert.Contains("accounts.google.com\tFALSE\t/\tFALSE\t", text);
    }

    [Fact]
    public void Las_cookies_httponly_llevan_el_prefijo_que_yt_dlp_entiende()
    {
        var text = NetscapeCookies.Format(
            [new BrowserCookie(".google.com", "/", "HSID", "x", Now.AddDays(1), true, true)], Now);

        Assert.Contains("\n#HttpOnly_.google.com\tTRUE", text);
    }

    [Fact]
    public void Una_cookie_de_sesion_no_sale_vencida()
    {
        var text = NetscapeCookies.Format(
            [new BrowserCookie(".youtube.com", "/", "S", "1", null, false, false)], Now);

        var expires = long.Parse(text.Split('\n')[1].Split('\t')[4]);
        Assert.True(expires > Now.ToUnixTimeSeconds());
    }

    [Fact]
    public void Una_cookie_repetida_sale_una_sola_vez()
    {
        var cookie = new BrowserCookie(".youtube.com", "/", "SID", "abc", Now.AddDays(1), true, false);

        var text = NetscapeCookies.Format([cookie, cookie with { Value = "otra" }], Now);

        Assert.Equal(2, text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length);
    }

    [Fact]
    public void Un_valor_con_tabulaciones_no_corre_las_columnas()
    {
        var text = NetscapeCookies.Format(
            [new BrowserCookie(".youtube.com", "/", "S", "a\tb\nc", Now.AddDays(1), false, false)], Now);

        Assert.Equal(7, text.Split('\n')[1].Split('\t').Length);
    }

    [Theory]
    [InlineData("ERROR: unable to download video data: HTTP Error 403: Forbidden", DownloadFailureKind.Forbidden)]
    [InlineData("ERROR: Sign in to confirm you're not a bot", DownloadFailureKind.SignInRequired)]
    [InlineData("ERROR: Private video. Sign in if you've been granted access", DownloadFailureKind.Unavailable)]
    [InlineData("WARNING: No supported JavaScript runtime could be found", DownloadFailureKind.Challenge)]
    [InlineData("ERROR: [youtube] abc: Unable to download webpage: <urlopen error [Errno 11001] getaddrinfo failed>", DownloadFailureKind.Network)]
    [InlineData("algo que no conocemos", DownloadFailureKind.Other)]
    [InlineData("", DownloadFailureKind.Other)]
    public void Los_errores_de_yt_dlp_se_clasifican(string error, DownloadFailureKind expected) =>
        Assert.Equal(expected, DownloadFailure.Classify(error));

    [Fact]
    public void La_revision_toca_cuando_nunca_se_hizo()
    {
        Assert.True(ToolUpdatePolicy.IsDue(null, Now));
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2.9, false)]
    [InlineData(3, true)]
    [InlineData(10, true)]
    public void La_revision_toca_pasados_tres_dias(double daysAgo, bool due)
    {
        Assert.Equal(due, ToolUpdatePolicy.IsDue(Now.AddDays(-daysAgo), Now));
    }

    [Fact]
    public void Una_fecha_de_revision_en_el_futuro_se_ignora_y_se_revisa()
    {
        Assert.True(ToolUpdatePolicy.IsDue(Now.AddDays(30), Now));
    }
}
