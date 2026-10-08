using EcclesiaCast.Core.Web;

namespace EcclesiaCast.Core.Tests.Web;

public class WebCapturesTests
{
    private static readonly DateTime Now = new(2026, 10, 15, 12, 0, 0);

    [Fact]
    public void Names_a_photo_by_the_moment_it_was_taken()
    {
        Assert.Equal("web-20261008-143005.png", WebCaptures.FileName(new DateTime(2026, 10, 8, 14, 30, 5)));
    }

    [Fact]
    public void Two_photos_in_the_same_second_do_not_share_a_name()
    {
        var taken = new DateTime(2026, 10, 8, 14, 30, 5);
        var existing = new HashSet<string> { "web-20261008-143005.png", "web-20261008-143005-2.png" };

        Assert.Equal("web-20261008-143005-3.png", WebCaptures.UniqueFileName(taken, existing.Contains));
    }

    [Fact]
    public void Keeps_the_plain_name_when_it_is_free()
    {
        var taken = new DateTime(2026, 10, 8, 14, 30, 5);

        Assert.Equal("web-20261008-143005.png", WebCaptures.UniqueFileName(taken, _ => false));
    }

    [Fact]
    public void Deletes_the_photos_older_than_seven_days()
    {
        var files = new[]
        {
            (@"C:\capturas\web-20261001-100000.png", Now.AddDays(-14)),
            (@"C:\capturas\web-20261012-100000.png", Now.AddDays(-3)),
        };

        var expired = WebCaptures.Expired(files, Now, []);

        Assert.Equal([@"C:\capturas\web-20261001-100000.png"], expired);
    }

    [Fact]
    public void A_photo_exactly_seven_days_old_is_still_kept()
    {
        var files = new[] { (@"C:\capturas\web-20261008-120000.png", Now.AddDays(-7)) };

        Assert.Empty(WebCaptures.Expired(files, Now, []));
    }

    [Fact]
    public void Keeps_the_photos_saved_in_the_media_library_however_old()
    {
        var files = new[]
        {
            (@"C:\capturas\web-20260901-100000.png", Now.AddDays(-44)),
            (@"C:\capturas\web-20260902-100000.png", Now.AddDays(-43)),
        };

        var expired = WebCaptures.Expired(files, Now, [@"c:\CAPTURAS\web-20260901-100000.png", null]);

        Assert.Equal([@"C:\capturas\web-20260902-100000.png"], expired);
    }

    [Fact]
    public void Never_touches_a_file_that_is_not_a_photo_of_the_program()
    {
        var files = new[]
        {
            (@"C:\capturas\notas.png", Now.AddDays(-90)),
            (@"C:\capturas\web-20260101-100000.txt", Now.AddDays(-90)),
        };

        Assert.Empty(WebCaptures.Expired(files, Now, []));
    }
}
