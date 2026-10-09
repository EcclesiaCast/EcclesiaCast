using EcclesiaCast.Core.Updates;

namespace EcclesiaCast.Core.Tests.Updates;

public class AppVersionTests
{
    private static AppVersion V(string text) =>
        AppVersion.TryParse(text) ?? throw new InvalidOperationException($"No es una versión: {text}");

    [Theory]
    [InlineData("1.0.0-beta.10", "1.0.0-beta.9")]
    [InlineData("1.0.0-beta.7", "1.0.0-beta.6")]
    [InlineData("1.0.0", "1.0.0-rc.1")]
    [InlineData("1.0.0-rc.1", "1.0.0-beta.10")]
    [InlineData("1.0.1", "1.0.0")]
    [InlineData("1.1.0", "1.0.9")]
    [InlineData("2.0.0", "1.99.99")]
    [InlineData("1.0.10", "1.0.9")]
    [InlineData("1.0.0-beta.1", "1.0.0-beta")]
    [InlineData("1.0.0-beta", "1.0.0-alpha")]
    [InlineData("1.0.0-beta.a", "1.0.0-beta.10")]
    [InlineData("1.0.1-beta.1", "1.0.0")]
    public void The_first_version_comes_after_the_second(string later, string earlier)
    {
        Assert.True(V(later) > V(earlier));
        Assert.True(V(earlier) < V(later));
        Assert.True(V(later).CompareTo(V(earlier)) > 0);
        Assert.False(V(later).Equals(V(earlier)));
    }

    [Fact]
    public void Beta_ten_is_after_beta_nine_by_number_and_not_by_text()
    {
        // As text, "10" sorts before "9".
        Assert.True(V("1.0.0-beta.10") > V("1.0.0-beta.9"));
        Assert.True(V("1.0.0-beta.100") > V("1.0.0-beta.99"));
    }

    [Theory]
    [InlineData("v1.0.0", "1.0.0")]
    [InlineData("V1.0.0", "1.0.0")]
    [InlineData("1.0", "1.0.0")]
    [InlineData("v1.2", "1.2.0")]
    [InlineData("1.0.0+abc123", "1.0.0")]
    [InlineData("1.0.0-beta.6+abc123", "1.0.0-beta.6")]
    [InlineData("v1.0.0-beta.7", "1.0.0-beta.7")]
    [InlineData("  1.0.0  ", "1.0.0")]
    [InlineData("1.0.0-beta.01", "1.0.0-beta.1")]
    public void Different_ways_of_writing_a_version_are_the_same_version(string written, string normal)
    {
        Assert.Equal(V(normal), V(written));
        Assert.Equal(V(normal).GetHashCode(), V(written).GetHashCode());
        Assert.Equal(normal, V(written).ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("latest")]
    [InlineData("1")]
    [InlineData("1.0.0.0")]
    [InlineData("a.b.c")]
    [InlineData("1.x.0")]
    [InlineData("1.0.0-")]
    [InlineData("1.0.0-beta..1")]
    [InlineData("1.0.0-beta_1")]
    [InlineData("-1.0.0")]
    [InlineData("1.-1.0")]
    [InlineData("99999999999999999999.0.0")]
    [InlineData("vv1.0.0")]
    public void Text_that_is_not_a_version_is_not_read(string text)
    {
        Assert.False(AppVersion.TryParse(text, out _));
        Assert.Null(AppVersion.TryParse(text));
    }

    [Fact]
    public void A_missing_text_is_not_a_version()
    {
        Assert.Null(AppVersion.TryParse(null));
    }

    [Fact]
    public void Only_the_ones_with_a_suffix_are_pre_releases()
    {
        Assert.True(V("1.0.0-beta.7").IsPreRelease);
        Assert.True(V("1.0.0-rc.1").IsPreRelease);
        Assert.False(V("1.0.0").IsPreRelease);
        Assert.False(V("1.0.0+abc").IsPreRelease);
    }

    [Fact]
    public void The_build_after_the_plus_does_not_change_the_order()
    {
        Assert.Equal(0, V("1.0.0+aaa").CompareTo(V("1.0.0+zzz")));
        Assert.True(V("1.0.0-beta.2+aaa") > V("1.0.0-beta.1+zzz"));
    }

    [Fact]
    public void Versions_sort_from_the_oldest_to_the_newest()
    {
        var shuffled = new[] { "1.0.1", "1.0.0-beta.10", "1.0.0", "1.0.0-rc.1", "1.0.0-beta.9", "0.9.0" }
            .Select(V).ToList();

        shuffled.Sort();

        Assert.Equal(
            ["0.9.0", "1.0.0-beta.9", "1.0.0-beta.10", "1.0.0-rc.1", "1.0.0", "1.0.1"],
            shuffled.Select(v => v.ToString()));
    }

    [Fact]
    public void Anything_is_after_nothing()
    {
        Assert.True(V("0.0.1").CompareTo(null) > 0);
    }
}
