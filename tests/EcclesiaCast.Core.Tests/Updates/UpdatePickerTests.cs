using EcclesiaCast.Core.Updates;

namespace EcclesiaCast.Core.Tests.Updates;

public class UpdatePickerTests
{
    private static AppVersion V(string text) => AppVersion.TryParse(text)!;

    private static ReleaseInfo Release(
        string tag, bool pre = false, bool draft = false, string body = "", bool installer = true)
    {
        var version = tag.TrimStart('v');
        var assets = installer
            ? new[] { new ReleaseAsset($"EcclesiaCast-{version}-setup.exe", $"https://example.com/{tag}/setup.exe", 1000, null) }
            : [];
        return new ReleaseInfo(tag, pre, draft, body, assets);
    }

    [Fact]
    public void A_stable_install_is_offered_a_newer_stable_version()
    {
        var offer = UpdatePicker.Choose(V("1.0.0"), [Release("v1.0.1")]);

        Assert.NotNull(offer);
        Assert.Equal(V("1.0.1"), offer.Version);
        Assert.Equal("v1.0.1", offer.Tag);
    }

    [Fact]
    public void A_stable_install_is_never_offered_a_pre_release()
    {
        var releases = new[]
        {
            Release("v1.1.0-beta.1", pre: true),
            Release("v1.0.1-rc.1", pre: true),
        };

        Assert.Null(UpdatePicker.Choose(V("1.0.0"), releases));
    }

    [Fact]
    public void A_release_marked_as_pre_release_on_github_counts_even_if_its_tag_looks_stable()
    {
        Assert.Null(UpdatePicker.Choose(V("1.0.0"), [Release("v1.0.1", pre: true)]));
    }

    [Fact]
    public void A_tag_with_a_suffix_counts_as_pre_release_even_if_github_did_not_mark_it()
    {
        Assert.Null(UpdatePicker.Choose(V("1.0.0"), [Release("v1.0.1-beta.1", pre: false)]));
    }

    [Fact]
    public void A_beta_install_is_offered_the_next_beta_and_the_stable_version()
    {
        var releases = new[]
        {
            Release("v1.0.0-beta.8", pre: true),
            Release("v1.0.0", pre: false),
        };

        Assert.Equal(V("1.0.0"), UpdatePicker.Choose(V("1.0.0-beta.7"), releases)!.Version);
        Assert.Equal(V("1.0.0-beta.8"), UpdatePicker.Choose(V("1.0.0-beta.7"), releases.Take(1))!.Version);
    }

    [Fact]
    public void The_newest_of_several_is_the_one_offered_whatever_the_order_of_the_list()
    {
        var releases = new[]
        {
            Release("v1.0.0-beta.9", pre: true),
            Release("v1.0.0-beta.10", pre: true),
            Release("v1.0.0-beta.8", pre: true),
        };

        Assert.Equal(V("1.0.0-beta.10"), UpdatePicker.Choose(V("1.0.0-beta.7"), releases)!.Version);
    }

    [Fact]
    public void A_draft_is_never_offered()
    {
        Assert.Null(UpdatePicker.Choose(V("1.0.0"), [Release("v1.0.1", draft: true)]));
    }

    [Fact]
    public void The_version_already_installed_and_older_ones_are_not_offered()
    {
        var releases = new[] { Release("v1.0.0"), Release("v0.9.0"), Release("v1.0.0-rc.1", pre: true) };

        Assert.Null(UpdatePicker.Choose(V("1.0.0"), releases));
    }

    [Fact]
    public void A_build_suffix_on_the_installed_version_is_ignored()
    {
        Assert.Null(UpdatePicker.Choose(V("1.0.0+abc123"), [Release("v1.0.0")]));
    }

    [Fact]
    public void A_skipped_version_is_not_offered()
    {
        Assert.Null(UpdatePicker.Choose(V("1.0.0"), [Release("v1.0.1")], skipped: V("1.0.1")));
    }

    [Fact]
    public void Skipping_one_version_does_not_hide_the_next_one()
    {
        var offer = UpdatePicker.Choose(V("1.0.0"), [Release("v1.0.1"), Release("v1.0.2")], skipped: V("1.0.1"));

        Assert.Equal(V("1.0.2"), offer!.Version);
    }

    [Fact]
    public void Skipping_the_newest_still_offers_an_older_one_that_is_newer_than_the_installed()
    {
        var offer = UpdatePicker.Choose(V("1.0.0"), [Release("v1.0.1"), Release("v1.0.2")], skipped: V("1.0.2"));

        Assert.Equal(V("1.0.1"), offer!.Version);
    }

    [Fact]
    public void A_release_without_the_installer_is_not_offered()
    {
        Assert.Null(UpdatePicker.Choose(V("1.0.0"), [Release("v1.0.1", installer: false)]));
    }

    [Fact]
    public void Only_a_file_named_like_the_installer_counts()
    {
        var release = new ReleaseInfo("v1.0.1", false, false, "",
        [
            new ReleaseAsset("notas.txt", "https://example.com/notas.txt", 10, null),
            new ReleaseAsset("EcclesiaCast-1.0.1-portable.zip", "https://example.com/p.zip", 10, null),
        ]);

        Assert.Null(UpdatePicker.Choose(V("1.0.0"), [release]));
    }

    [Fact]
    public void The_installer_name_is_matched_without_caring_about_capitals()
    {
        var release = new ReleaseInfo("v1.0.1", false, false, "",
            [new ReleaseAsset("ecclesiacast-1.0.1-SETUP.EXE", "https://example.com/s.exe", 10, null)]);

        Assert.NotNull(UpdatePicker.Choose(V("1.0.0"), [release]));
    }

    [Fact]
    public void An_installer_that_is_not_served_over_https_is_not_offered()
    {
        var release = new ReleaseInfo("v1.0.1", false, false, "",
            [new ReleaseAsset("EcclesiaCast-1.0.1-setup.exe", "http://example.com/s.exe", 10, null)]);

        Assert.Null(UpdatePicker.Choose(V("1.0.0"), [release]));
    }

    [Fact]
    public void A_tag_that_is_not_a_version_is_ignored()
    {
        var releases = new[] { Release("nightly"), Release("latest"), Release("v1.0.1") };

        Assert.Equal(V("1.0.1"), UpdatePicker.Choose(V("1.0.0"), releases)!.Version);
    }

    [Fact]
    public void An_empty_list_offers_nothing()
    {
        Assert.Null(UpdatePicker.Choose(V("1.0.0"), []));
    }

    [Fact]
    public void The_offer_carries_the_notes_as_plain_text_and_the_installer()
    {
        var offer = UpdatePicker.Choose(V("1.0.0"), [Release("v1.0.1", body: "### Corregido\n\n- **Biblia.** Ahora abre.")]);

        Assert.Equal("Corregido\n• Biblia. Ahora abre.", offer!.Notes);
        Assert.Equal("EcclesiaCast-1.0.1-setup.exe", offer.Installer.Name);
    }

    [Fact]
    public void A_manual_search_with_no_skipped_version_offers_everything_newer()
    {
        Assert.NotNull(UpdatePicker.Choose(V("1.0.0"), [Release("v1.0.1")], skipped: null));
    }
}
