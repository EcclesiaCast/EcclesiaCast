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
            new MediaTabRule(MediaTabMode.PerItem, false, true),
            new MediaTabRule(MediaTabMode.Background, false, true),
            new MediaTabRule(MediaTabMode.Foreground, false, true),
            new MediaTabRule(MediaTabMode.Foreground, true, true),
        })
        {
            Assert.Equal(rule, MediaTabRule.Parse(rule.ToSetting()));
        }
    }

    [Theory]
    [InlineData("item+mute", MediaTabMode.PerItem, false, true)]
    [InlineData("background+mute", MediaTabMode.Background, false, true)]
    [InlineData("foreground+logo+mute", MediaTabMode.Foreground, true, true)]
    [InlineData("foreground+mute+logo", MediaTabMode.Foreground, true, true)]
    [InlineData(" Foreground + Mute ", MediaTabMode.Foreground, false, true)]
    [InlineData("foreground+logo", MediaTabMode.Foreground, true, false)]
    public void The_mute_is_read_from_the_settings_next_to_the_rest(string text, MediaTabMode mode, bool logo, bool muted)
    {
        Assert.Equal(new MediaTabRule(mode, logo, muted), MediaTabRule.Parse(text));
    }

    [Fact]
    public void A_rule_nobody_can_read_is_not_muted()
    {
        Assert.Equal(MediaTabRule.Default, MediaTabRule.Parse("mute"));
        Assert.Equal(MediaTabRule.Default, MediaTabRule.Parse("algo raro+mute"));
    }

    [Fact]
    public void The_text_saved_for_a_muted_tab_ends_in_mute_and_the_old_ones_do_not_change()
    {
        Assert.Equal("item+mute", new MediaTabRule(MediaTabMode.PerItem, false, true).ToSetting());
        Assert.Equal("foreground+logo+mute", new MediaTabRule(MediaTabMode.Foreground, true, true).ToSetting());
        Assert.Equal("foreground+logo", new MediaTabRule(MediaTabMode.Foreground, true).ToSetting());
        Assert.Equal("item", MediaTabRule.Default.ToSetting());
    }

    [Fact]
    public void A_muted_tab_silences_a_media_that_had_sound_without_touching_the_original()
    {
        var item = Video();
        item.Muted = false;

        var resolved = new MediaTabRule(MediaTabMode.PerItem, false, true).Resolve(item);

        Assert.True(resolved.Muted);
        Assert.False(item.Muted);
        Assert.Equal(item.Behavior, resolved.Behavior);
    }

    [Fact]
    public void A_muted_tab_changes_nothing_in_a_media_that_was_already_silent()
    {
        var item = Video();
        item.Muted = true;

        Assert.Same(item, new MediaTabRule(MediaTabMode.PerItem, false, true).Resolve(item));
    }

    [Fact]
    public void A_tab_that_is_not_muted_never_gives_sound_to_a_silent_media_or_takes_it_from_a_loud_one()
    {
        var silent = Video();
        silent.Muted = true;
        var loud = Video();
        loud.Muted = false;

        Assert.Same(silent, MediaTabRule.Default.Resolve(silent));
        Assert.Same(loud, MediaTabRule.Default.Resolve(loud));
    }

    [Fact]
    public void A_muted_foreground_tab_applies_the_mute_together_with_the_rest_of_the_rule()
    {
        var item = Video(MediaBehavior.Background);
        item.Muted = false;

        var resolved = new MediaTabRule(MediaTabMode.Foreground, true, true).Resolve(item);

        Assert.True(resolved.Muted);
        Assert.Equal(MediaBehavior.Foreground, resolved.Behavior);
        Assert.Equal(VideoEndBehavior.Logo, resolved.EndBehavior);
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
