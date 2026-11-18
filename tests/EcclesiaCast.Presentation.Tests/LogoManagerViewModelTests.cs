using EcclesiaCast.Core.Logos;
using EcclesiaCast.Core.Media;
using EcclesiaCast.Presentation.Services;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Presentation.Tests;

[Collection("ViewModels")]
public sealed class LogoManagerViewModelTests
{
    private sealed class Rig : IDisposable
    {
        public ViewModelWorld World { get; } = new();

        public RecordingThumbnailer Thumbnails { get; } = new();

        public FixedColorPicker Picker { get; } = new("#112233");

        public LogoManagerViewModel Vm { get; }

        public Rig(params Logo[] logos)
        {
            foreach (var logo in logos)
                World.Logos.Save(logo);
            Vm = New();
        }

        public LogoManagerViewModel New() =>
            new(World.Logos, Thumbnails, Picker, new NoFonts(), World.Timers);

        public void Tick() => World.Timers.Created.Last(t => t.Interval == TimeSpan.FromMilliseconds(400)).RaiseTick();

        public void Dispose() => World.Dispose();
    }

    private static Logo Named(string name, int order, LogoKind kind = LogoKind.Image) =>
        new() { Name = name, Order = order, Kind = kind };

    private static LogoForm FormOf(Logo logo) => LogoForm.Of(logo);

    [Fact]
    public void Lists_the_logos_and_picks_the_one_asked_for_or_the_first()
    {
        using var rig = new Rig(Named("General", 0), Named("Jóvenes", 1));

        Assert.Equal(["General", "Jóvenes"], rig.Vm.Items.Select(l => l.Name));
        Assert.Equal("General", rig.Vm.Pick(null)!.Name);
        Assert.Equal("Jóvenes", rig.Vm.Pick(rig.Vm.Items[1].Id)!.Name);
        Assert.Equal("General", rig.Vm.Pick(12345)!.Name);
    }

    [Fact]
    public void With_no_logos_there_is_nothing_to_pick()
    {
        using var rig = new Rig();

        Assert.Empty(rig.Vm.Items);
        Assert.Null(rig.Vm.Pick(null));
    }

    [Fact]
    public void A_new_logo_goes_at_the_end_as_an_image_and_counts_as_a_change()
    {
        using var rig = new Rig(Named("General", 0));

        var created = rig.Vm.CreateNew();

        Assert.Equal("Logo nuevo", created.Name);
        Assert.Equal(LogoKind.Image, created.Kind);
        Assert.Equal(1, created.Order);
        Assert.True(rig.Vm.ChangesMade);
        Assert.Equal(2, rig.Vm.Items.Count);
        Assert.Equal(2, rig.World.Logos.GetAll().Count);
    }

    [Fact]
    public void Duplicating_makes_a_copy_with_its_own_name_at_the_end()
    {
        using var rig = new Rig(Named("General", 0));

        var copy = rig.Vm.Duplicate(rig.Vm.Items[0]);

        Assert.Equal("General (copia)", copy.Name);
        Assert.NotEqual(rig.Vm.Items[0].Id, copy.Id);
        Assert.Equal(1, copy.Order);
        Assert.Equal(2, rig.World.Logos.GetAll().Count);
    }

    [Fact]
    public void Removing_asks_first_and_changes_nothing_on_no()
    {
        using var rig = new Rig(Named("General", 0));
        using var notices = new ScriptedNotices().No();

        var removed = rig.Vm.Delete(rig.Vm.Items[0]);

        Assert.False(removed);
        Assert.False(rig.Vm.ChangesMade);
        Assert.Single(rig.World.Logos.GetAll());
        Assert.Empty(rig.Thumbnails.Deleted);
        Assert.Equal("¿Eliminar el logo \"General\"?", notices.Asked[0].Message);
        Assert.Equal(["Cancelar", "Eliminar"], notices.Asked[0].Buttons);
    }

    [Fact]
    public void Removing_on_yes_erases_the_logo_and_its_poster()
    {
        var logo = Named("Video", 0, LogoKind.Video);
        logo.PosterPath = "poster.png";
        using var rig = new Rig(logo);
        using var notices = new ScriptedNotices().Yes();

        var removed = rig.Vm.Delete(rig.Vm.Items[0]);

        Assert.True(removed);
        Assert.True(rig.Vm.ChangesMade);
        Assert.Empty(rig.World.Logos.GetAll());
        Assert.Empty(rig.Vm.Items);
        Assert.Equal(["poster.png"], rig.Thumbnails.Deleted);
    }

    [Fact]
    public void Moving_swaps_the_order_and_writes_it()
    {
        using var rig = new Rig(Named("A", 0), Named("B", 1), Named("C", 2));

        var moved = rig.Vm.Move(rig.Vm.Items[2], -1);

        Assert.True(moved);
        Assert.Equal(["A", "C", "B"], rig.World.Logos.GetAll().OrderBy(l => l.Order).Select(l => l.Name));
        Assert.Equal(["A", "C", "B"], rig.Vm.Items.Select(l => l.Name));
        Assert.True(rig.Vm.ChangesMade);
    }

    [Fact]
    public void Moving_past_the_ends_does_nothing()
    {
        using var rig = new Rig(Named("A", 0), Named("B", 1));

        Assert.False(rig.Vm.Move(rig.Vm.Items[0], -1));
        Assert.False(rig.Vm.Move(rig.Vm.Items[1], +1));

        Assert.False(rig.Vm.ChangesMade);
        Assert.Equal(["A", "B"], rig.Vm.Items.Select(l => l.Name));
    }

    [Fact]
    public void A_change_in_the_fields_shows_in_the_preview_now_and_is_written_when_the_clock_ticks()
    {
        using var rig = new Rig(Named("General", 0));
        var logo = rig.Vm.Items[0];
        var previews = new List<Logo>();
        var saved = new List<Logo>();
        rig.Vm.PreviewChanged += previews.Add;
        rig.Vm.Saved += saved.Add;

        rig.Vm.ApplyFields(logo, FormOf(logo) with { Name = "  Mujeres  " });

        var preview = Assert.Single(previews);
        Assert.Equal("Mujeres", preview.Name);
        Assert.NotSame(logo, preview);
        Assert.Empty(saved);
        Assert.False(rig.Vm.ChangesMade);
        Assert.Equal("General", rig.World.Logos.Get(logo.Id)!.Name);

        rig.Tick();

        Assert.Single(saved);
        Assert.True(rig.Vm.ChangesMade);
        Assert.Equal("Mujeres", rig.World.Logos.Get(logo.Id)!.Name);
    }

    [Fact]
    public void Many_changes_in_a_row_write_only_once_and_the_clock_is_restarted_each_time()
    {
        using var rig = new Rig(Named("General", 0));
        var logo = rig.Vm.Items[0];
        var saved = 0;
        rig.Vm.Saved += _ => saved++;

        rig.Vm.ApplyFields(logo, FormOf(logo) with { ZoomPercent = 110 });
        rig.Vm.ApplyFields(logo, FormOf(logo) with { ZoomPercent = 120 });
        rig.Vm.ApplyFields(logo, FormOf(logo) with { ZoomPercent = 130 });
        rig.Tick();
        rig.Tick();

        Assert.Equal(1, saved);
        Assert.Equal(1.3, rig.World.Logos.Get(logo.Id)!.Zoom, 3);
    }

    [Fact]
    public void Flushing_writes_what_was_waiting_at_once_and_does_nothing_when_nothing_waits()
    {
        using var rig = new Rig(Named("General", 0));
        var logo = rig.Vm.Items[0];

        rig.Vm.FlushSave();
        Assert.False(rig.Vm.ChangesMade);

        rig.Vm.ApplyFields(logo, FormOf(logo) with { Name = "Otro" });
        rig.Vm.FlushSave();

        Assert.True(rig.Vm.ChangesMade);
        Assert.Equal("Otro", rig.World.Logos.Get(logo.Id)!.Name);
        Assert.False(rig.World.Timers.Created.Last(t => t.Interval == TimeSpan.FromMilliseconds(400)).IsEnabled);
    }

    [Fact]
    public void Creating_a_logo_first_writes_the_change_that_was_waiting()
    {
        using var rig = new Rig(Named("General", 0));
        var logo = rig.Vm.Items[0];
        rig.Vm.ApplyFields(logo, FormOf(logo) with { Name = "Cambiado" });

        rig.Vm.CreateNew();

        Assert.Equal("Cambiado", rig.World.Logos.Get(logo.Id)!.Name);
    }

    [Fact]
    public void The_fields_are_checked_before_they_go_into_the_logo()
    {
        using var rig = new Rig(Named("General", 0));
        var logo = rig.Vm.Items[0];

        rig.Vm.ApplyFields(logo, FormOf(logo) with
        {
            Name = "   ",
            FontFamily = " ",
            TextColor = " #FF0000 ",
            BackgroundColor = " #000000 ",
            KindIndex = 9,
            ScalingIndex = -3,
            ZoomPercent = 1000,
            OffsetXPercent = 5000,
            OffsetYPercent = -5000,
            FixedSize = true,
            FrameWidth = 4000,
            FrameHeight = 2,
        });

        Assert.Equal("Logo", logo.Name);
        Assert.Equal("Segoe UI", logo.FontFamily);
        Assert.Equal("#FF0000", logo.TextColor);
        Assert.Equal("#000000", logo.BackgroundColor);
        Assert.Equal(LogoKind.Text, logo.Kind);
        Assert.Equal((MediaScaling)0, logo.Scaling);
        Assert.Equal(4, logo.Zoom);
        Assert.Equal(10, logo.OffsetX);
        Assert.Equal(-10, logo.OffsetY);
        Assert.Equal(1920, logo.FrameWidth);
        Assert.Equal(16, logo.FrameHeight);
    }

    [Fact]
    public void Without_a_fixed_size_there_is_no_frame_and_a_small_zoom_is_raised()
    {
        using var rig = new Rig(Named("General", 0));
        var logo = rig.Vm.Items[0];

        rig.Vm.ApplyFields(logo, FormOf(logo) with { FixedSize = false, FrameWidth = 800, FrameHeight = 600, ZoomPercent = 1 });

        Assert.Null(logo.FrameWidth);
        Assert.Null(logo.FrameHeight);
        Assert.Equal(0.1, logo.Zoom, 3);
    }

    [Fact]
    public void The_choice_for_the_borders_is_kept_only_while_the_kind_still_offers_it()
    {
        var saved = MediaLookup.All;
        MediaLookup.All = () => [new MediaItem { Id = 5, Name = "Fondo", Type = MediaType.Image }];
        try
        {
            using var rig = new Rig(Named("General", 0));
            var logo = rig.Vm.Items[0];

            rig.Vm.ApplyFields(logo, FormOf(logo) with { FillMediaId = 5 });
            Assert.Equal(5, logo.FillMediaId);

            rig.Vm.ApplyFields(logo, FormOf(logo) with { FillMediaId = MediaItem.FillWithSelfId });
            Assert.Equal(MediaItem.FillWithSelfId, logo.FillMediaId);

            // A text logo has no blurred copy of itself.
            rig.Vm.ApplyFields(logo, FormOf(logo) with { KindIndex = (int)LogoKind.Text, FillMediaId = MediaItem.FillWithSelfId });
            Assert.Null(logo.FillMediaId);

            rig.Vm.ApplyFields(logo, FormOf(logo) with { FillMediaId = 99 });
            Assert.Null(logo.FillMediaId);

            rig.Vm.ApplyFields(logo, FormOf(logo) with { FillMediaId = 0 });
            Assert.Null(logo.FillMediaId);
        }
        finally
        {
            MediaLookup.All = saved;
        }
    }

    [Fact]
    public void The_fill_options_depend_on_the_kind_and_list_the_images_of_the_library_by_name()
    {
        var saved = MediaLookup.All;
        MediaLookup.All = () =>
        [
            new MediaItem { Id = 2, Name = "Zeta", Type = MediaType.Image },
            new MediaItem { Id = 1, Name = "Alfa", Type = MediaType.Image },
            new MediaItem { Id = 3, Name = "Video", Type = MediaType.Video },
        ];
        try
        {
            using var rig = new Rig();

            Assert.Equal(["El color de fondo", "La misma imagen, desenfocada", "Alfa", "Zeta"],
                rig.Vm.FillOptions(LogoKind.Image).Select(o => o.Name));
            Assert.Equal(["El color de fondo", "Una imagen fija del video, desenfocada", "Alfa", "Zeta"],
                rig.Vm.FillOptions(LogoKind.Video).Select(o => o.Name));
            Assert.Equal(["El color de fondo", "Alfa", "Zeta"],
                rig.Vm.FillOptions(LogoKind.Text).Select(o => o.Name));
        }
        finally
        {
            MediaLookup.All = saved;
        }
    }

    [Fact]
    public void Choosing_an_image_keeps_no_poster_and_a_logo_with_its_starting_name_takes_the_file_name()
    {
        using var rig = new Rig();
        var logo = rig.Vm.CreateNew();
        logo.PosterPath = "old.png";

        rig.Vm.SetFile(logo, Path.Combine("C:", "logos", "cruz.png"));

        Assert.Equal("cruz", logo.Name);
        Assert.Equal(Path.Combine("C:", "logos", "cruz.png"), logo.Path);
        Assert.Null(logo.PosterPath);
        Assert.Equal(["old.png"], rig.Thumbnails.Deleted);
        Assert.Empty(rig.Thumbnails.Created);
        var stored = rig.World.Logos.Get(logo.Id)!;
        Assert.Equal("cruz", stored.Name);
        Assert.Equal(logo.Path, stored.Path);
    }

    [Fact]
    public void Choosing_a_video_makes_a_poster_and_keeps_a_name_the_operator_gave()
    {
        using var rig = new Rig(Named("Bienvenida", 0, LogoKind.Video));
        var logo = rig.Vm.Items[0];

        rig.Vm.SetFile(logo, Path.Combine("C:", "logos", "loop.mp4"));

        Assert.Equal("Bienvenida", logo.Name);
        Assert.Equal("poster-of-loop.mp4", logo.PosterPath);
        Assert.Equal("poster-of-loop.mp4", rig.World.Logos.Get(logo.Id)!.PosterPath);
        Assert.True(rig.Vm.ChangesMade);
    }

    [Fact]
    public void The_colour_picker_is_opened_on_the_colour_shown()
    {
        using var rig = new Rig();

        Assert.Equal("#112233", rig.Vm.PickColor("#FFFFFF"));
        Assert.Equal("#FFFFFF", rig.Picker.AskedWith);
    }

    [Theory]
    [InlineData(0, null, null)]
    [InlineData(1, 1920.0, 1080.0)]
    [InlineData(2, 1280.0, 720.0)]
    [InlineData(3, 1440.0, 1080.0)]
    [InlineData(4, 1080.0, 1080.0)]
    [InlineData(5, 1080.0, 1920.0)]
    [InlineData(6, null, null)]
    public void Each_size_preset_stands_for_its_size(int index, double? width, double? height)
    {
        var size = LogoManagerViewModel.PresetSize(index);

        Assert.Equal(width, size?.Width);
        Assert.Equal(height, size?.Height);
    }

    [Fact]
    public void Only_an_image_or_a_video_can_be_framed()
    {
        Assert.True(LogoManagerViewModel.CanFrame(Named("a", 0, LogoKind.Image)));
        Assert.True(LogoManagerViewModel.CanFrame(Named("a", 0, LogoKind.Video)));
        Assert.False(LogoManagerViewModel.CanFrame(Named("a", 0, LogoKind.Text)));
        Assert.False(LogoManagerViewModel.CanFrame(null));
    }

    [Fact]
    public void The_fields_show_the_framing_in_percent_and_the_defaults_when_there_is_no_frame()
    {
        var form = LogoForm.Of(new Logo { Zoom = 0, OffsetX = 0.12345, OffsetY = -0.5 });

        Assert.Equal(100, form.ZoomPercent);
        Assert.Equal(12.3, form.OffsetXPercent);
        Assert.Equal(-50, form.OffsetYPercent);
        Assert.False(form.FixedSize);
        Assert.Equal(1920, form.FrameWidth);
        Assert.Equal(1080, form.FrameHeight);

        var framed = LogoForm.Of(new Logo { Zoom = 1.5, FrameWidth = 1280, FrameHeight = 720 });
        Assert.Equal(150, framed.ZoomPercent);
        Assert.True(framed.FixedSize);
        Assert.Equal(1280, framed.FrameWidth);
    }
}
