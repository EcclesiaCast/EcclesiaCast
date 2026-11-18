using EcclesiaCast.Core.Media;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Presentation.Tests;

public sealed class MediaInspectorViewModelTests
{
    private static MediaItem Image(int id = 1) => new() { Id = id, Name = "Cruz", Type = MediaType.Image, Category = "Fondos" };

    private static MediaItem Video(int id = 1) => new() { Id = id, Name = "Loop", Type = MediaType.Video, Category = "Fondos" };

    private static MediaInspectorViewModel Make(MediaItem item, params MediaItem[] library) =>
        new(item, ["Fondos", "Afiches"], [item, .. library], new FixedColorPicker("#AABBCC"));

    [Fact]
    public void The_fields_open_with_what_the_item_has()
    {
        var item = Video();
        item.Zoom = 1.25;
        item.OffsetX = 0.034;
        item.OffsetY = -0.5;
        item.FrameWidth = 1280;
        item.FrameHeight = 720;
        item.FillColor = "#112233";
        item.TrimStart = 3;
        item.TrimEnd = 9;
        item.Muted = false;
        item.Volume = 70;
        item.Blur = 12;
        item.EventDate = new DateTime(2026, 12, 25, 18, 30, 0);
        item.ArchiveAfterDays = 4;

        var form = Make(item).Initial();

        Assert.Equal(125, form.ZoomPercent);
        Assert.Equal(3, form.OffsetXPercent);
        Assert.Equal(-50, form.OffsetYPercent);
        Assert.True(form.FixedSize);
        Assert.Equal(1280, form.FrameWidth);
        Assert.Equal(720, form.FrameHeight);
        Assert.Equal("#112233", form.FillColor);
        Assert.Equal(3, form.TrimStart);
        Assert.Equal(9, form.TrimEnd);
        Assert.Equal(70, form.Volume);
        Assert.Equal(12, form.Blur);
        Assert.Equal(new DateTime(2026, 12, 25), form.EventDate);
        Assert.Equal(4, form.ArchiveDays);
    }

    [Fact]
    public void A_new_item_opens_with_the_defaults()
    {
        var form = Make(Image()).Initial();

        Assert.Equal(100, form.ZoomPercent);
        Assert.False(form.FixedSize);
        Assert.Equal(1920, form.FrameWidth);
        Assert.Equal(1080, form.FrameHeight);
        Assert.Equal("#000000", form.FillColor);
        Assert.Null(form.EventDate);
        Assert.Equal(MediaExpiry.DefaultDays, form.ArchiveDays);
    }

    [Theory]
    [InlineData(VideoEndBehavior.Loop, 0)]
    [InlineData(VideoEndBehavior.Stop, 0)]
    [InlineData(VideoEndBehavior.Logo, 1)]
    [InlineData(VideoEndBehavior.NextMedia, 2)]
    public void The_end_of_an_image_is_shown_as_nothing_the_logo_or_the_next_media(VideoEndBehavior end, int shown)
    {
        var item = Image();
        item.EndBehavior = end;

        Assert.Equal(shown, Make(item).Initial().ImageEndIndex);
    }

    [Fact]
    public void The_kind_of_item_decides_what_the_window_offers()
    {
        var image = Make(Image());
        var video = Make(Video());
        var youtube = Make(new MediaItem { Id = 1, Type = MediaType.YouTube, YouTubeId = "abc" });

        Assert.False(image.IsVideo);
        Assert.True(video.IsVideo);
        Assert.True(youtube.IsVideo);
        Assert.False(youtube.IsVideoFile);
        Assert.True(video.IsVideoFile);
        Assert.True(image.CanChangeScaling);
        Assert.False(youtube.CanChangeScaling);
        Assert.Equal("Imagen", image.TypeText);
        Assert.Equal("Video", video.TypeText);
        Assert.Equal("YouTube · abc", youtube.TypeText);
    }

    [Fact]
    public void The_fill_options_leave_out_the_item_itself_and_the_youtube_ones_and_go_by_name()
    {
        var other = new MediaItem { Id = 2, Name = "Zeta", Type = MediaType.Image };
        var another = new MediaItem { Id = 3, Name = "Alfa", Type = MediaType.Video };
        var tube = new MediaItem { Id = 4, Name = "Tube", Type = MediaType.YouTube };

        var image = Make(Image(), other, another, tube);
        var video = Make(Video(), other, another, tube);

        Assert.Equal(["(sólo el color)", "(la misma imagen, desenfocada)", "Alfa", "Zeta"],
            image.FillOptions.Select(o => o.Name));
        Assert.Equal(["(sólo el color)", "(el mismo video, desenfocado)", "Alfa", "Zeta"],
            video.FillOptions.Select(o => o.Name));
    }

    [Fact]
    public void What_can_follow_is_any_other_item_by_name_and_the_choice_is_found_by_id()
    {
        var other = new MediaItem { Id = 2, Name = "Zeta", Type = MediaType.Image };
        var another = new MediaItem { Id = 3, Name = "Alfa", Type = MediaType.Video };
        var vm = Make(Image(), other, another);

        Assert.Equal(["Alfa", "Zeta"], vm.Followers.Select(f => f.Name));
        Assert.Same(other, vm.FollowerFor(2));
        Assert.Null(vm.FollowerFor(null));
        Assert.Null(vm.FollowerFor(99));
        Assert.Equal(MediaItem.FillWithSelfId, vm.FillOptionFor(MediaItem.FillWithSelfId).Id);
        Assert.Equal(0, vm.FillOptionFor(99).Id);
        Assert.Equal(0, vm.FillOptionFor(null).Id);
    }

    [Fact]
    public void The_box_of_the_next_media_shows_only_with_that_choice_of_the_kind_of_item()
    {
        var video = Make(Video());
        var image = Make(Image());

        Assert.True(video.FollowsNextMedia(endIndex: 3, imageEndIndex: 0));
        Assert.False(video.FollowsNextMedia(endIndex: 2, imageEndIndex: 2));
        Assert.True(image.FollowsNextMedia(endIndex: 0, imageEndIndex: 2));
        Assert.False(image.FollowsNextMedia(endIndex: 3, imageEndIndex: 1));
    }

    [Fact]
    public void The_note_under_the_date_says_what_will_happen()
    {
        var today = new DateTime(2026, 11, 18);

        Assert.Equal("Sin fecha: no vence. Con la fecha de un evento, pasa sola a «Eventos pasados» unos días después.",
            MediaInspectorViewModel.EventNote(null, 10, today));
        Assert.Equal("Pasa a «Eventos pasados» el 25/12/2026.",
            MediaInspectorViewModel.EventNote(new DateTime(2026, 12, 15), 10, today));
        Assert.Equal("Esa fecha ya pasó: al guardar se mueve a «Eventos pasados».",
            MediaInspectorViewModel.EventNote(new DateTime(2026, 11, 1), 10, today));
        Assert.Equal("Esa fecha ya pasó: al guardar se mueve a «Eventos pasados».",
            MediaInspectorViewModel.EventNote(new DateTime(2026, 11, 8), 10, today));
    }

    [Fact]
    public void The_colour_picker_is_opened_on_the_colour_shown()
    {
        var picker = new FixedColorPicker("#AABBCC");
        var vm = new MediaInspectorViewModel(Image(), [], [], picker);

        Assert.Equal("#AABBCC", vm.PickFillColor("#000000"));
        Assert.Equal("#000000", picker.AskedWith);
    }

    [Fact]
    public void The_preview_is_a_copy_and_the_item_is_not_touched()
    {
        var item = Image();
        var vm = Make(item);

        var preview = vm.PreviewOf(vm.Initial() with
        {
            ZoomPercent = 200,
            OffsetXPercent = 5000,
            FixedSize = true,
            FrameWidth = 1280,
            FrameHeight = 5,
            ScalingIndex = (int)MediaScaling.Stretch,
            FillColor = "#FF0000",
            Blur = 30,
        });

        Assert.NotSame(item, preview.Media);
        Assert.Equal(2, preview.Media.Zoom);
        Assert.Equal(10, preview.Media.OffsetX);
        Assert.Equal(1280, preview.Media.FrameWidth);
        Assert.Equal(16, preview.Media.FrameHeight);
        Assert.Equal(MediaScaling.Stretch, preview.Media.Scaling);
        Assert.Equal("#FF0000", preview.Media.FillColor);
        Assert.Equal(30, preview.Blur);
        Assert.Equal(1, item.Zoom);
        Assert.Null(item.FrameWidth);
        Assert.Equal("Cruz", item.Name);
    }

    [Fact]
    public void The_preview_fills_with_the_chosen_image_with_itself_or_with_nothing()
    {
        var other = new MediaItem { Id = 2, Name = "Fondo", Type = MediaType.Image };
        var vm = Make(Image(), other);

        var withOther = vm.PreviewOf(vm.Initial() with { FillMediaId = 2 });
        Assert.Same(other, withOther.FillMedia);
        Assert.Equal(2, withOther.Media.FillMediaId);

        var withSelf = vm.PreviewOf(vm.Initial() with { FillMediaId = MediaItem.FillWithSelfId });
        Assert.Same(withSelf.Media, withSelf.FillMedia);

        var withColor = vm.PreviewOf(vm.Initial() with { FillMediaId = 0 });
        Assert.Null(withColor.FillMedia);
        Assert.Null(withColor.Media.FillMediaId);

        var withNone = vm.PreviewOf(vm.Initial() with { FillMediaId = null });
        Assert.Null(withNone.FillMedia);
    }

    [Fact]
    public void Saving_writes_the_fields_into_the_item_and_checks_them()
    {
        var item = Image();
        item.Brightness = 40;
        item.Tint = "#FF0000";
        item.TintStrength = 50;
        var vm = Make(item);

        vm.Apply(vm.Initial() with
        {
            Name = "  Afiche  ",
            Category = "   ",
            EventDate = new DateTime(2026, 12, 25, 20, 0, 0),
            ArchiveDays = 999,
            BehaviorIndex = (int)MediaBehavior.Foreground,
            ScalingIndex = (int)MediaScaling.Fit,
            ShowSeconds = 99999,
            Volume = 55.7,
            Blur = 8,
            ZoomPercent = 1,
            OffsetXPercent = -5000,
            OffsetYPercent = 250,
            FixedSize = true,
            FrameWidth = 5000,
            FrameHeight = 3,
            TrimStart = -2,
            TrimEnd = 7,
            FillColor = "#123456",
            FillMediaId = MediaItem.FillWithSelfId,
        });

        Assert.Equal("Afiche", item.Name);
        Assert.Equal("Fondos", item.Category);
        Assert.Equal(new DateTime(2026, 12, 25), item.EventDate);
        Assert.Equal(365, item.ArchiveAfterDays);
        Assert.Equal(MediaBehavior.Foreground, item.Behavior);
        Assert.Equal(MediaScaling.Fit, item.Scaling);
        Assert.Equal(3600, item.ShowSeconds);
        Assert.Equal(55, item.Volume);
        Assert.Equal(8, item.Blur);
        Assert.Equal(0.1, item.Zoom, 3);
        Assert.Equal(-10, item.OffsetX);
        Assert.Equal(2.5, item.OffsetY);
        Assert.Equal(1920, item.FrameWidth);
        Assert.Equal(16, item.FrameHeight);
        Assert.Equal(0, item.TrimStart);
        Assert.Equal(7, item.TrimEnd);
        Assert.Equal("#123456", item.FillColor);
        Assert.Equal(MediaItem.FillWithSelfId, item.FillMediaId);
        Assert.Equal(0, item.Brightness);
        Assert.Null(item.Tint);
        Assert.Equal(0, item.TintStrength);
    }

    [Fact]
    public void An_empty_name_keeps_the_old_one_and_without_fixed_size_there_is_no_frame()
    {
        var item = Image();
        item.FrameWidth = 800;
        item.FrameHeight = 600;
        var vm = Make(item);

        vm.Apply(vm.Initial() with { Name = "   ", FixedSize = false, FillMediaId = 0 });

        Assert.Equal("Cruz", item.Name);
        Assert.Null(item.FrameWidth);
        Assert.Null(item.FrameHeight);
        Assert.Null(item.FillMediaId);
    }

    [Fact]
    public void A_video_keeps_its_end_and_has_no_seconds()
    {
        var item = Video();
        var vm = Make(item);

        vm.Apply(vm.Initial() with { EndIndex = (int)VideoEndBehavior.Logo, ShowSeconds = 30, ImageEndIndex = 2 });

        Assert.Equal(VideoEndBehavior.Logo, item.EndBehavior);
        Assert.Equal(0, item.ShowSeconds);
        Assert.Null(item.NextMediaId);
    }

    [Fact]
    public void An_image_reads_its_end_from_its_own_box()
    {
        var item = Image();
        var vm = Make(item);

        vm.Apply(vm.Initial() with { ImageEndIndex = 0, ShowSeconds = 12, EndIndex = 3 });
        Assert.Equal(VideoEndBehavior.Stop, item.EndBehavior);
        Assert.Equal(12, item.ShowSeconds);

        vm.Apply(vm.Initial() with { ImageEndIndex = 1 });
        Assert.Equal(VideoEndBehavior.Logo, item.EndBehavior);
    }

    [Fact]
    public void Following_another_media_needs_one_chosen_or_it_falls_back_to_stop()
    {
        var other = new MediaItem { Id = 2, Name = "Otro", Type = MediaType.Image };
        var item = Image();
        var vm = Make(item, other);

        vm.Apply(vm.Initial() with { ImageEndIndex = 2, NextMediaId = 2 });
        Assert.Equal(VideoEndBehavior.NextMedia, item.EndBehavior);
        Assert.Equal(2, item.NextMediaId);

        vm.Apply(vm.Initial() with { ImageEndIndex = 2, NextMediaId = null });
        Assert.Equal(VideoEndBehavior.Stop, item.EndBehavior);
        Assert.Null(item.NextMediaId);

        // A media chosen but not followed is not kept.
        vm.Apply(vm.Initial() with { ImageEndIndex = 1, NextMediaId = 2 });
        Assert.Equal(VideoEndBehavior.Logo, item.EndBehavior);
        Assert.Null(item.NextMediaId);
    }

    [Fact]
    public void Cancelling_leaves_the_item_as_it_was()
    {
        // The window only calls Apply on «Guardar»; opening it and the preview change nothing.
        var item = Image();
        item.Zoom = 1.5;
        var vm = Make(item);

        _ = vm.Initial();
        _ = vm.PreviewOf(vm.Initial() with { ZoomPercent = 300 });

        Assert.Equal(1.5, item.Zoom);
    }
}
