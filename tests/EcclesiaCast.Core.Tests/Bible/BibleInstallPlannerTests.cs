using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Bible;

namespace EcclesiaCast.Core.Tests.Bible;

public class BibleInstallPlannerTests
{
    private static BiblePackageEntry Entry(string name, string abbreviation) => new("x.json", name, abbreviation, "es");

    private static BibleVersionInfo Installed(string name, string abbreviation) => new(1, name, abbreviation, "es", 100);

    private static readonly BiblePackageEntry Rv1909 = Entry("Reina-Valera 1909", "RV1909");
    private static readonly BiblePackageEntry Blm = Entry("Biblia libre para el mundo", "BLM");

    [Fact]
    public void On_a_fresh_library_every_bundled_bible_is_pending()
    {
        var pending = BibleInstallPlanner.PendingBundled([Rv1909, Blm], [], null);

        Assert.Equal(["RV1909", "BLM"], pending.Select(e => e.Abbreviation));
    }

    [Fact]
    public void A_bible_already_installed_is_not_brought_in_again()
    {
        var pending = BibleInstallPlanner.PendingBundled([Rv1909, Blm], [Installed("Mi RV", "RV1909")], null);

        Assert.Equal(["BLM"], pending.Select(e => e.Abbreviation));
    }

    [Fact]
    public void The_same_bible_under_another_abbreviation_is_found_by_its_name()
    {
        var pending = BibleInstallPlanner.PendingBundled([Rv1909], [Installed("reina valera 1909", "RV09")], null);

        Assert.Empty(pending);
    }

    [Fact]
    public void An_abbreviation_matches_whatever_the_case_spaces_or_punctuation()
    {
        Assert.True(BibleInstallPlanner.SameKey("RV 1909", "rv-1909"));
        Assert.False(BibleInstallPlanner.SameKey("RV1909", "RVR1960"));
        Assert.False(BibleInstallPlanner.SameKey("", "  "));
    }

    [Fact]
    public void A_bible_the_operator_deleted_does_not_come_back_at_the_next_start()
    {
        var remembered = BibleInstallPlanner.Remember(null, ["RV1909", "BLM"]);

        // Both were brought in once; now the library has none of them.
        var pending = BibleInstallPlanner.PendingBundled([Rv1909, Blm], [], remembered);

        Assert.Empty(pending);
    }

    [Fact]
    public void A_bible_added_to_a_later_installer_is_still_brought_in()
    {
        var remembered = BibleInstallPlanner.Remember(null, ["RV1909"]);
        var vbl = Entry("Versión Biblia Libre", "VBL");

        var pending = BibleInstallPlanner.PendingBundled([Rv1909, vbl], [], remembered);

        Assert.Equal(["VBL"], pending.Select(e => e.Abbreviation));
    }

    [Fact]
    public void Remembering_keeps_what_was_already_there_and_does_not_repeat()
    {
        var first = BibleInstallPlanner.Remember(null, ["RV1909"]);
        var second = BibleInstallPlanner.Remember(first, ["rv1909", "BLM"]);

        Assert.Equal(["RV1909", "BLM"], BibleInstallPlanner.ParseRemembered(second));
    }

    [Fact]
    public void An_empty_or_missing_setting_remembers_nothing()
    {
        Assert.Empty(BibleInstallPlanner.ParseRemembered(null));
        Assert.Empty(BibleInstallPlanner.ParseRemembered("  "));
    }

    [Fact]
    public void A_package_is_offered_once_and_again_only_when_the_file_changes()
    {
        var first = BibleInstallPlanner.Fingerprint("Paquete.ecbiblias", 1000, new DateTime(2026, 10, 14, 0, 0, 0, DateTimeKind.Utc));
        var sameFile = BibleInstallPlanner.Fingerprint("PAQUETE.ecbiblias", 1000, new DateTime(2026, 10, 14, 0, 0, 0, DateTimeKind.Utc));
        var newer = BibleInstallPlanner.Fingerprint("Paquete.ecbiblias", 1200, new DateTime(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc));

        Assert.True(BibleInstallPlanner.IsNewPackage(null, first));
        Assert.False(BibleInstallPlanner.IsNewPackage(first, sameFile));
        Assert.True(BibleInstallPlanner.IsNewPackage(first, newer));
    }
}
