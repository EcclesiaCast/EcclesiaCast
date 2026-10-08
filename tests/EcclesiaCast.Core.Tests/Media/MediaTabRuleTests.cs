using EcclesiaCast.Core.Media;

namespace EcclesiaCast.Core.Tests.Media;

public class MediaTabRuleTests
{
    private static MediaItem Video(MediaBehavior behavior = MediaBehavior.Background, VideoEndBehavior end = VideoEndBehavior.Loop) =>
        new() { Id = 1, Name = "Aviso", Type = MediaType.Video, Behavior = behavior, EndBehavior = end };

    [Fact]
    public void A_tab_that_leaves_it_to_each_media_changes_nothing()
    {
        var item = Video(MediaBehavior.Foreground, VideoEndBehavior.Stop);

        Assert.Same(item, MediaTabRule.Default.Resolve(item));
    }

    [Fact]
    public void A_foreground_tab_puts_a_background_media_on_the_whole_screen_without_touching_the_original()
    {
        var item = Video(MediaBehavior.Background);

        var resolved = new MediaTabRule(MediaTabMode.Foreground, false).Resolve(item);

        Assert.Equal(MediaBehavior.Foreground, resolved.Behavior);
        Assert.Equal(MediaBehavior.Background, item.Behavior);
    }

    [Fact]
    public void A_background_tab_puts_a_foreground_media_behind_the_text()
    {
        var item = Video(MediaBehavior.Foreground);

        var resolved = new MediaTabRule(MediaTabMode.Background, false).Resolve(item);

        Assert.Equal(MediaBehavior.Background, resolved.Behavior);
    }

    [Fact]
    public void A_foreground_tab_that_shows_the_logo_ends_every_media_on_it()
    {
        var resolved = new MediaTabRule(MediaTabMode.Foreground, true).Resolve(Video(MediaBehavior.Foreground, VideoEndBehavior.Stop));

        Assert.Equal(VideoEndBehavior.Logo, resolved.EndBehavior);
    }

    [Fact]
    public void A_media_that_names_the_next_one_keeps_it_over_the_logo_of_the_tab()
    {
        var item = Video(MediaBehavior.Foreground, VideoEndBehavior.NextMedia);
        item.NextMediaId = 7;

        var resolved = new MediaTabRule(MediaTabMode.Foreground, true).Resolve(item);

        Assert.Equal(VideoEndBehavior.NextMedia, resolved.EndBehavior);
        Assert.Equal(7, resolved.NextMediaId);
    }

    [Fact]
    public void The_logo_at_the_end_only_counts_for_a_foreground_tab()
    {
        var item = Video(MediaBehavior.Background, VideoEndBehavior.Loop);

        Assert.Same(item, new MediaTabRule(MediaTabMode.PerItem, true).Resolve(item));
    }

    [Theory]
    [InlineData("item", MediaTabMode.PerItem, false)]
    [InlineData("background", MediaTabMode.Background, false)]
    [InlineData("foreground", MediaTabMode.Foreground, false)]
    [InlineData("foreground+logo", MediaTabMode.Foreground, true)]
    [InlineData("", MediaTabMode.PerItem, false)]
    [InlineData(null, MediaTabMode.PerItem, false)]
    [InlineData("algo raro", MediaTabMode.PerItem, false)]
    public void The_rule_is_read_from_the_settings(string? text, MediaTabMode mode, bool logo)
    {
        Assert.Equal(new MediaTabRule(mode, logo), MediaTabRule.Parse(text));
    }

    [Fact]
    public void What_is_written_to_the_settings_is_read_back_the_same()
    {
        foreach (var rule in new[]
        {
            MediaTabRule.Default,
            new MediaTabRule(MediaTabMode.Background, false),
            new MediaTabRule(MediaTabMode.Foreground, false),
            new MediaTabRule(MediaTabMode.Foreground, true),
        })
        {
            Assert.Equal(rule, MediaTabRule.Parse(rule.ToSetting()));
        }
    }
}

public class MediaTabNamesTests
{
    [Fact]
    public void With_nothing_saved_the_starting_tabs_come_first_then_the_categories_in_use()
    {
        var tabs = MediaTabNames.Merge(null, ["Jóvenes", "Fondos", "jóvenes"]);

        Assert.Equal(["Fondos", "Anuncios", "YouTube", "Jóvenes"], tabs);
    }

    [Fact]
    public void The_saved_order_wins_and_a_category_nobody_listed_goes_last()
    {
        var tabs = MediaTabNames.Merge(["Jóvenes", "Fondos"], ["Fondos", "Cumpleaños"]);

        Assert.Equal(["Jóvenes", "Fondos", "Cumpleaños"], tabs);
    }

    [Fact]
    public void A_tab_deleted_from_the_saved_list_does_not_come_back_by_itself()
    {
        var tabs = MediaTabNames.Merge(["Fondos"], []);

        Assert.Equal(["Fondos"], tabs);
    }

    [Fact]
    public void There_is_always_at_least_one_tab()
    {
        Assert.Equal(["Fondos"], MediaTabNames.Merge([], []));
    }

    [Fact]
    public void A_name_that_another_tab_has_cannot_be_used()
    {
        var tabs = new[] { "Fondos", "Anuncios" };

        Assert.NotNull(MediaTabNames.WhyNotRename(tabs, "Fondos", "ANUNCIOS"));
        Assert.NotNull(MediaTabNames.WhyNotRename(tabs, "Fondos", "  "));
        Assert.Null(MediaTabNames.WhyNotRename(tabs, "Fondos", "🎬 Videos"));
        Assert.Null(MediaTabNames.WhyNotRename(tabs, "Fondos", "FONDOS"));
    }

    [Fact]
    public void The_list_is_saved_and_read_back_with_emojis_and_accents()
    {
        var tabs = new List<string> { "🎬 Videos", "Jóvenes", "Fondos" };

        Assert.Equal(tabs, MediaTabNames.Deserialize(MediaTabNames.Serialize(tabs)));
        Assert.Null(MediaTabNames.Deserialize("esto no es json"));
        Assert.Null(MediaTabNames.Deserialize(null));
    }
}
