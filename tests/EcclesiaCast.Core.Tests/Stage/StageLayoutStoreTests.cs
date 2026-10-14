using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Stage;

namespace EcclesiaCast.Core.Tests.Stage;

public class StageLayoutStoreTests
{
    private sealed class MemorySettings : ISettingsStore
    {
        public Dictionary<string, string> Values { get; } = [];

        public string? Get(string key) => Values.GetValueOrDefault(key);

        public void Set(string key, string value) => Values[key] = value;
    }

    [Fact]
    public void A_layout_survives_a_trip_through_json()
    {
        var original = new StageLayout
        {
            Name = "Jóvenes",
            Boxes =
            [
                new StageBox
                {
                    Id = "abc", Kind = StageBoxKind.Timer, X = 100, Y = 50, Width = 500, Height = 200,
                    FontSize = 90, Color = "#FF8800", Align = StageAlign.Right, Bold = true, Text = "hola",
                },
            ],
        };

        var back = Assert.Single(StageLayoutStore.Parse(StageLayoutStore.Serialize([original])));

        Assert.Equal("Jóvenes", back.Name);
        var box = Assert.Single(back.Boxes);
        Assert.Equal(StageBoxKind.Timer, box.Kind);
        Assert.Equal((100d, 50d, 500d, 200d), (box.X, box.Y, box.Width, box.Height));
        Assert.Equal(90, box.FontSize);
        Assert.Equal("#FF8800", box.Color);
        Assert.Equal(StageAlign.Right, box.Align);
        Assert.True(box.Bold);
        Assert.Equal("hola", box.Text);
    }

    [Fact]
    public void The_kind_is_saved_as_a_word_so_the_setting_can_be_read_by_eye()
    {
        var json = StageLayoutStore.Serialize([new StageLayout { Boxes = [new StageBox { Kind = StageBoxKind.LiveText }] }]);

        Assert.Contains("\"LiveText\"", json);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("esto no es json")]
    [InlineData("{\"no\":\"una lista\"}")]
    public void Damaged_or_missing_json_gives_no_layouts_instead_of_an_error(string? json)
    {
        Assert.Empty(StageLayoutStore.Parse(json));
    }

    [Fact]
    public void Boxes_are_pulled_back_inside_the_canvas()
    {
        var json = "[{\"name\":\"X\",\"boxes\":[{\"kind\":\"Clock\",\"x\":5000,\"y\":-30,\"width\":99999,\"height\":1,\"fontSize\":0}]}]";

        var box = Assert.Single(Assert.Single(StageLayoutStore.Parse(json)).Boxes);

        Assert.InRange(box.X + box.Width, 0, StageLayout.CanvasWidth);
        Assert.True(box.Y >= 0);
        Assert.True(box.Height >= 20);
        Assert.True(box.FontSize >= 8);
    }

    [Fact]
    public void Three_layouts_ship_with_the_program()
    {
        var names = StageLayoutStore.Defaults().Select(l => l.Name);

        Assert.Equal(["Cantantes", "Predicador", "Igual a la pantalla principal"], names);
    }

    [Fact]
    public void The_singers_layout_has_the_live_words_and_the_next_slide()
    {
        var singers = StageLayoutStore.Defaults()[0];

        Assert.Contains(singers.Boxes, b => b.Kind == StageBoxKind.LiveText);
        Assert.Contains(singers.Boxes, b => b.Kind == StageBoxKind.NextSlide);
        Assert.DoesNotContain(singers.Boxes, b => b.Kind == StageBoxKind.Notes);
    }

    [Fact]
    public void The_preacher_layout_has_time_stopwatch_and_notes()
    {
        var preacher = StageLayoutStore.Defaults()[1];

        Assert.Contains(preacher.Boxes, b => b.Kind == StageBoxKind.Clock);
        Assert.Contains(preacher.Boxes, b => b.Kind == StageBoxKind.Timer);
        Assert.Contains(preacher.Boxes, b => b.Kind == StageBoxKind.Notes);
        Assert.DoesNotContain(preacher.Boxes, b => b.Kind == StageBoxKind.LiveText);
    }

    [Fact]
    public void The_mirror_layout_is_one_box_over_the_whole_canvas()
    {
        var box = Assert.Single(StageLayoutStore.Defaults()[2].Boxes);

        Assert.Equal(StageBoxKind.Mirror, box.Kind);
        Assert.Equal((0d, 0d, StageLayout.CanvasWidth, StageLayout.CanvasHeight), (box.X, box.Y, box.Width, box.Height));
    }

    [Fact]
    public void Every_factory_box_sits_inside_the_canvas()
    {
        foreach (var layout in StageLayoutStore.Defaults().Append(StageLayoutStore.FromLegacy(true, true, true, true, 96)))
        {
            foreach (var box in layout.Boxes)
            {
                Assert.True(box.X >= 0 && box.Y >= 0, $"{layout.Name}/{box.Kind}");
                Assert.True(box.X + box.Width <= StageLayout.CanvasWidth + 0.001, $"{layout.Name}/{box.Kind}");
                Assert.True(box.Y + box.Height <= StageLayout.CanvasHeight + 0.001, $"{layout.Name}/{box.Kind}");
            }
        }
    }

    [Fact]
    public void A_fresh_library_starts_on_the_singers_layout()
    {
        var state = StageLayoutStore.Load(new MemorySettings());

        Assert.Equal(3, state.Layouts.Count);
        Assert.Equal("Cantantes", state.Active.Name);
    }

    [Fact]
    public void The_old_options_become_a_layout_that_starts_active()
    {
        var settings = new MemorySettings();
        settings.Values["stage.clock"] = "0";
        settings.Values["stage.notes"] = "1";
        settings.Values["stage.scale"] = "120";

        var state = StageLayoutStore.Load(settings);

        Assert.Equal(4, state.Layouts.Count);
        Assert.Equal("Mi escenario", state.Active.Name);
        Assert.DoesNotContain(state.Active.Boxes, b => b.Kind == StageBoxKind.Clock);
        Assert.Contains(state.Active.Boxes, b => b.Kind == StageBoxKind.Timer);
        Assert.Contains(state.Active.Boxes, b => b.Kind == StageBoxKind.Notes);
        Assert.Equal(120, state.Active.Boxes.Single(b => b.Kind == StageBoxKind.LiveText).FontSize);
    }

    [Fact]
    public void Turning_off_next_and_notes_gives_the_live_words_all_the_room()
    {
        var full = StageLayoutStore.FromLegacy(true, true, true, true, 96);
        var bare = StageLayoutStore.FromLegacy(true, true, false, false, 96);

        Assert.True(
            bare.Boxes.Single(b => b.Kind == StageBoxKind.LiveText).Height
            > full.Boxes.Single(b => b.Kind == StageBoxKind.LiveText).Height);
        Assert.DoesNotContain(bare.Boxes, b => b.Kind == StageBoxKind.NextSlide);
    }

    [Fact]
    public void Saved_layouts_win_over_the_old_options()
    {
        var settings = new MemorySettings();
        settings.Values["stage.clock"] = "0";
        StageLayoutStore.Save(settings, [new StageLayout { Name = "Solo uno" }], "Solo uno");

        var state = StageLayoutStore.Load(settings);

        Assert.Equal(["Solo uno"], state.Layouts.Select(l => l.Name));
        Assert.Equal("Solo uno", state.Active.Name);
    }

    [Fact]
    public void An_active_name_that_no_longer_exists_falls_back_to_the_first_layout()
    {
        var settings = new MemorySettings();
        StageLayoutStore.Save(settings, StageLayoutStore.Defaults(), "Borrada");

        var state = StageLayoutStore.Load(settings);

        Assert.Equal("Cantantes", state.Active.Name);
    }

    [Fact]
    public void A_repeated_name_gets_a_number()
    {
        Assert.Equal("predicador 2", StageLayoutStore.UniqueName(["Cantantes", "Predicador"], "predicador"));
        Assert.Equal("Predicador 3", StageLayoutStore.UniqueName(["Predicador", "Predicador 2"], "Predicador"));
        Assert.Equal("Jóvenes", StageLayoutStore.UniqueName(["Cantantes"], " Jóvenes "));
        Assert.Equal("Escenario", StageLayoutStore.UniqueName([], "  "));
    }

    [Fact]
    public void Cloning_a_layout_does_not_share_its_boxes()
    {
        var original = StageLayoutStore.Defaults()[0];
        var copy = original.Clone();

        copy.Boxes[0].FontSize = 999;
        copy.Name = "Otra";

        Assert.NotEqual(999, original.Boxes[0].FontSize);
        Assert.Equal("Cantantes", original.Name);
    }

    [Fact]
    public void A_typeface_survives_a_trip_through_json()
    {
        var original = new StageLayout
        {
            Name = "Notas",
            Boxes = [new StageBox { Kind = StageBoxKind.Notes, FontFamily = "Georgia" }],
        };

        var back = Assert.Single(StageLayoutStore.Parse(StageLayoutStore.Serialize([original])));

        Assert.Equal("Georgia", Assert.Single(back.Boxes).FontFamily);
    }

    [Fact]
    public void A_layout_saved_before_typefaces_reads_back_with_none()
    {
        var json = "[{\"name\":\"Viejo\",\"boxes\":[{\"kind\":\"Notes\",\"x\":0,\"y\":0,\"width\":400,\"height\":120,\"fontSize\":60}]}]";

        var box = Assert.Single(Assert.Single(StageLayoutStore.Parse(json)).Boxes);

        Assert.Null(box.FontFamily);
    }

    [Fact]
    public void A_blank_typeface_is_read_as_none_and_spaces_are_trimmed()
    {
        var json = "[{\"name\":\"X\",\"boxes\":["
            + "{\"kind\":\"Notes\",\"fontFamily\":\"   \"},"
            + "{\"kind\":\"Notes\",\"fontFamily\":\"  Georgia  \"}]}]";

        var boxes = Assert.Single(StageLayoutStore.Parse(json)).Boxes;

        Assert.Null(boxes[0].FontFamily);
        Assert.Equal("Georgia", boxes[1].FontFamily);
    }
}
