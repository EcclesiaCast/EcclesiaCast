using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Remote;

namespace EcclesiaCast.Core.Tests.Remote;

public class BroadcastStyleTests
{
    private sealed class MemorySettings : ISettingsStore
    {
        public Dictionary<string, string> Values { get; } = [];

        public string? Get(string key) => Values.TryGetValue(key, out var value) ? value : null;

        public void Set(string key, string value) => Values[key] = value;
    }

    [Fact]
    public void Each_layer_has_its_own_factory_values()
    {
        var bible = BroadcastStyle.Default(OutputChannel.Bible);
        var songs = BroadcastStyle.Default(OutputChannel.Songs);
        var announcements = BroadcastStyle.Default(OutputChannel.Announcements);

        Assert.Equal(BroadcastPosition.Bottom, bible.Position);
        Assert.False(bible.Uppercase);
        Assert.Equal(BroadcastPosition.Bottom, songs.Position);
        Assert.True(songs.Uppercase);
        Assert.Equal(BroadcastPosition.Center, announcements.Position);
        Assert.False(announcements.Uppercase);

        foreach (var style in new[] { bible, songs, announcements })
        {
            Assert.False(style.FollowProjector);
            Assert.Equal(BroadcastPlate.Tab, style.Plate);
            Assert.Equal("#2DD4BF", style.AccentColor);
            Assert.Equal("#14161C", style.PlateColor);
            Assert.Equal(85, style.PlateOpacity);
            Assert.Equal("#FFFFFF", style.TextColor);
            Assert.Equal("Segoe UI", style.FontFamily);
            Assert.Equal(54, style.FontSize);
            Assert.True(style.Bold);
            Assert.True(style.ShowLabel);
            Assert.Equal(70, style.WidthPercent);
            Assert.Equal(6, style.MarginPercent);
            Assert.Equal(BroadcastBackground.None, style.Background);
            Assert.Equal("#000000", style.BackgroundColor);
            Assert.Equal(string.Empty, style.BackgroundFile);
        }
    }

    [Theory]
    [InlineData(OutputChannel.Bible)]
    [InlineData(OutputChannel.Songs)]
    [InlineData(OutputChannel.Announcements)]
    public void A_style_survives_the_round_trip_through_json(OutputChannel channel)
    {
        var style = BroadcastStyle.Default(channel) with
        {
            FollowProjector = true,
            Position = BroadcastPosition.Top,
            Plate = BroadcastPlate.Fillet,
            AccentColor = "#FF8800",
            PlateColor = "#101820",
            PlateOpacity = 40,
            TextColor = "#EEEEEE",
            FontFamily = "Georgia",
            FontSize = 72.5,
            Bold = false,
            Uppercase = !BroadcastStyle.Default(channel).Uppercase,
            ShowLabel = false,
            WidthPercent = 55,
            MarginPercent = 3,
            Background = BroadcastBackground.File,
            BackgroundColor = "#112233",
            BackgroundFile = @"C:\Fondos\escenario.mp4",
        };

        Assert.Equal(style, BroadcastStyle.Parse(style.ToSetting(), channel));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("esto no es json")]
    [InlineData("{\"plate\": ")]
    [InlineData("[1,2,3]")]
    [InlineData("42")]
    [InlineData("null")]
    public void Parsing_garbage_or_nothing_gives_the_factory_style_of_the_layer(string? setting)
    {
        foreach (var channel in new[] { OutputChannel.Bible, OutputChannel.Songs, OutputChannel.Announcements })
            Assert.Equal(BroadcastStyle.Default(channel), BroadcastStyle.Parse(setting, channel));
    }

    [Fact]
    public void A_field_that_cannot_be_read_costs_only_that_field()
    {
        var parsed = BroadcastStyle.Parse(
            "{\"plate\":\"glass\",\"fontSize\":\"grande\",\"position\":\"nowhere\",\"bold\":false,\"accentColor\":\"rojo\"}",
            OutputChannel.Songs);

        Assert.Equal(BroadcastPlate.Glass, parsed.Plate);
        Assert.False(parsed.Bold);
        Assert.Equal(54, parsed.FontSize);
        Assert.Equal(BroadcastPosition.Bottom, parsed.Position);
        Assert.Equal(BroadcastStyle.DefaultAccentColor, parsed.AccentColor);
        Assert.True(parsed.Uppercase);
    }

    [Fact]
    public void Clamp_holds_every_number_inside_its_limits()
    {
        var wild = new BroadcastStyle
        {
            PlateOpacity = 400,
            FontSize = 9000,
            WidthPercent = 5,
            MarginPercent = 80,
        }.Clamp();

        Assert.Equal(100, wild.PlateOpacity);
        Assert.Equal(BroadcastStyle.MaxFontSize, wild.FontSize);
        Assert.Equal(BroadcastStyle.MinWidthPercent, wild.WidthPercent);
        Assert.Equal(BroadcastStyle.MaxMarginPercent, wild.MarginPercent);

        var tiny = new BroadcastStyle
        {
            PlateOpacity = -5,
            FontSize = -1,
            WidthPercent = 500,
            MarginPercent = -3,
        }.Clamp();

        Assert.Equal(0, tiny.PlateOpacity);
        Assert.Equal(BroadcastStyle.MinFontSize, tiny.FontSize);
        Assert.Equal(BroadcastStyle.MaxWidthPercent, tiny.WidthPercent);
        Assert.Equal(0, tiny.MarginPercent);

        Assert.Equal(54, new BroadcastStyle { FontSize = double.NaN }.Clamp().FontSize);
    }

    [Fact]
    public void Clamp_replaces_what_is_not_a_colour_or_a_name()
    {
        var style = new BroadcastStyle
        {
            AccentColor = "rojo",
            PlateColor = "#12",
            TextColor = "#abcdef",
            FontFamily = "  ",
            Plate = (BroadcastPlate)99,
        }.Clamp();

        Assert.Equal(BroadcastStyle.DefaultAccentColor, style.AccentColor);
        Assert.Equal(BroadcastStyle.DefaultPlateColor, style.PlateColor);
        Assert.Equal("#ABCDEF", style.TextColor);
        Assert.Equal("Segoe UI", style.FontFamily);
        Assert.Equal(BroadcastPlate.Tab, style.Plate);
    }

    [Fact]
    public void Out_of_range_numbers_in_a_saved_setting_are_held_in_range()
    {
        var parsed = BroadcastStyle.Parse("{\"plateOpacity\":900,\"widthPercent\":1}", OutputChannel.Bible);

        Assert.Equal(100, parsed.PlateOpacity);
        Assert.Equal(BroadcastStyle.MinWidthPercent, parsed.WidthPercent);
    }

    [Fact]
    public void Every_layer_is_saved_under_its_own_key()
    {
        Assert.Equal("broadcast.style.biblia", BroadcastStyles.SettingKey(OutputChannel.Bible));
        Assert.Equal("broadcast.style.canciones", BroadcastStyles.SettingKey(OutputChannel.Songs));
        Assert.Equal("broadcast.style.anuncios", BroadcastStyles.SettingKey(OutputChannel.Announcements));
    }

    [Fact]
    public void Loading_reads_each_layer_and_falls_back_to_its_factory_style()
    {
        var settings = new MemorySettings();
        var chosen = BroadcastStyle.Default(OutputChannel.Songs) with { Plate = BroadcastPlate.Halo, FontSize = 80 };
        settings.Set(BroadcastStyles.SettingKey(OutputChannel.Songs), chosen.ToSetting());
        settings.Set(BroadcastStyles.SettingKey(OutputChannel.Announcements), "roto");

        var styles = BroadcastStyles.Load(settings);

        Assert.Equal(chosen, styles.Songs);
        Assert.Equal(BroadcastStyle.Default(OutputChannel.Bible), styles.Bible);
        Assert.Equal(BroadcastStyle.Default(OutputChannel.Announcements), styles.Announcements);
    }

    [Fact]
    public void The_page_with_everything_uses_the_announcements_style_and_With_replaces_one_layer()
    {
        var styles = BroadcastStyles.Default;
        var changed = styles.With(OutputChannel.Bible, styles.Bible with { Plate = BroadcastPlate.Line });

        Assert.Equal(BroadcastPlate.Line, changed.For(OutputChannel.Bible).Plate);
        Assert.Equal(styles.Songs, changed.For(OutputChannel.Songs));
        Assert.Equal(styles.Announcements, changed.For(OutputChannel.All));
    }
}
