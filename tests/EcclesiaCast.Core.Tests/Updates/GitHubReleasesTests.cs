using System.Text.Json;
using EcclesiaCast.Core.Updates;

namespace EcclesiaCast.Core.Tests.Updates;

public class GitHubReleasesTests
{
    private static IReadOnlyList<ReleaseInfo> Sample() =>
        GitHubReleases.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Updates", "releases-sample.json")));

    [Fact]
    public void The_sample_answer_is_read_release_by_release()
    {
        var releases = Sample();

        // The entry without a tag is dropped; the rest keep their order.
        Assert.Equal(["v1.0.1", "v1.0.0", "v1.0.0-beta.5", "v1.0.0-beta.4"], releases.Select(r => r.Tag));
    }

    [Fact]
    public void Draft_and_pre_release_flags_are_read()
    {
        var releases = Sample();

        Assert.True(releases[0].IsDraft);
        Assert.False(releases[0].IsPreRelease);
        Assert.False(releases[1].IsDraft);
        Assert.False(releases[1].IsPreRelease);
        Assert.True(releases[2].IsPreRelease);
    }

    [Fact]
    public void An_asset_gives_its_name_address_size_and_sha256()
    {
        var asset = Sample()[1].Assets.Single();

        Assert.Equal("EcclesiaCast-1.0.0-setup.exe", asset.Name);
        Assert.Equal("https://github.com/EcclesiaCast/EcclesiaCast/releases/download/v1.0.0/EcclesiaCast-1.0.0-setup.exe", asset.DownloadUrl);
        Assert.Equal(139000123, asset.Size);
        Assert.Equal("e6e8e112435ec0acf9a71fb530483323669fcb84769be56dfc3c3b881b91d47d", asset.Sha256);
    }

    [Fact]
    public void An_old_asset_without_digest_has_no_sha256()
    {
        var installer = Sample()[2].Assets.First();

        Assert.Equal("EcclesiaCast-1.0.0-beta.5-setup.exe", installer.Name);
        Assert.Null(installer.Sha256);
    }

    [Fact]
    public void A_digest_of_another_algorithm_is_not_taken_for_a_sha256()
    {
        var other = Sample()[2].Assets.Single(a => a.Name == "notas.txt");

        Assert.Null(other.Sha256);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("sha256:")]
    [InlineData("sha256:1234")]
    [InlineData("md5:d41d8cd98f00b204e9800998ecf8427e")]
    [InlineData("sha256:zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz")]
    public void A_digest_that_is_not_a_full_sha256_is_ignored(string? digest)
    {
        Assert.Null(GitHubReleases.Sha256Of(digest));
    }

    [Fact]
    public void The_body_is_kept_as_it_comes_and_a_missing_one_is_empty()
    {
        var releases = Sample();

        Assert.Contains("Actualizaciones automáticas", releases[1].Body);
        Assert.Equal(string.Empty, releases[3].Body);
        Assert.Empty(releases[3].Assets);
    }

    [Fact]
    public void The_notes_of_the_sample_read_as_plain_text()
    {
        var offer = UpdatePicker.Choose(AppVersion.TryParse("1.0.0-beta.7")!, Sample());

        Assert.NotNull(offer);
        Assert.Equal(AppVersion.TryParse("1.0.0"), offer.Version);
        Assert.Equal(
            "Agregado\n• Actualizaciones automáticas. El programa avisa solo cuando hay una versión nueva.\n\nCorregido\n• Se arregló un error al abrir la Biblia.",
            offer.Notes);
    }

    [Fact]
    public void An_empty_list_is_valid()
    {
        Assert.Empty(GitHubReleases.Parse("[]"));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"message\":\"API rate limit exceeded\"}")]
    [InlineData("no es json")]
    [InlineData("")]
    public void An_answer_that_is_not_a_list_is_refused(string json)
    {
        Assert.ThrowsAny<JsonException>(() => GitHubReleases.Parse(json));
    }

    [Fact]
    public void Odd_values_inside_a_release_do_not_break_the_reading()
    {
        var json = """
            [
              { "tag_name": "v1.0.1", "draft": "yes", "prerelease": null, "body": 5,
                "assets": [ 3, { "name": "a.exe" }, { "name": "b.exe", "browser_download_url": "https://x/b.exe", "size": "grande" } ] },
              7,
              null
            ]
            """;

        var releases = GitHubReleases.Parse(json);

        var release = Assert.Single(releases);
        Assert.False(release.IsDraft);
        Assert.False(release.IsPreRelease);
        Assert.Equal(string.Empty, release.Body);
        var asset = Assert.Single(release.Assets);
        Assert.Equal("b.exe", asset.Name);
        Assert.Equal(0, asset.Size);
    }
}
