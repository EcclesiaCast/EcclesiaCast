using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Core.Songs;
using EcclesiaCast.Core.Themes;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Presentation.Tests;

public sealed class SongDesignerViewModelTests
{
    private static readonly SlideTheme Theme = new()
    {
        FontFamily = "Segoe UI",
        MaxFontSize = 92,
        MarginHorizontal = 110,
        MarginVertical = 80,
    };

    private static Song TwoSlides(string? styleOfSecond = null)
    {
        var song = new Song
        {
            Title = "Cántico",
            Sections =
            [
                new SongSection { Label = "Verso 1", Text = "Primera estrofa" },
                new SongSection { Label = "Coro", Text = "Estribillo" },
            ],
        };
        song.Sections[1].StyleJson = styleOfSecond;
        return song;
    }

    private static SongDesignerViewModel New(Song song, string? picked = "#123456") =>
        new(song, Theme, new FixedColorPicker(picked), new NoFonts());

    private static SongStyleForm Form(
        string font = "Georgia", double size = 70.4, string color = " #00FF00 ", bool bold = true) =>
        new(font, size, bold, Italic: false, color, HAlign.Left, VAlign.Top,
            Underline: true, Strikethrough: false, Shadow: true, ShadowPercent: 50, OutlineWidth: 2,
            OutlineColor: " #111111 ", CaseIndex: 99, LineSpacing: 1.2, FitToWidth: true);

    [Fact]
    public void Lists_the_slides_with_their_labels_and_words()
    {
        var vm = New(TwoSlides());

        Assert.Equal(2, vm.SlideCount);
        Assert.Equal("Coro", vm.SlideLabel(1));
        Assert.Equal("Estribillo", vm.BuildSlide(1).MainText);
        Assert.Null(vm.BuildSlide(1).Caption);
    }

    [Fact]
    public void Choosing_a_slide_gives_it_a_box_over_the_theme_margins_without_counting_as_an_edit()
    {
        var song = TwoSlides();
        var vm = New(song);

        var over = vm.SelectSlide(0);

        Assert.Equal((110d, 80d, 1700d, 920d), (over.BoxX, over.BoxY, over.BoxWidth, over.BoxHeight));
        Assert.Equal(-1, vm.SelectedBox);

        vm.Save();
        Assert.Null(song.Sections[0].StyleJson);
    }

    [Fact]
    public void Choosing_a_slide_keeps_the_box_it_already_has_and_goes_back_to_the_words()
    {
        var song = TwoSlides(System.Text.Json.JsonSerializer.Serialize(
            new SlideOverride(BoxX: 5, BoxY: 6, BoxWidth: 700, BoxHeight: 300)));
        var vm = New(song);
        vm.SelectedBox = 3;

        var over = vm.SelectSlide(1);

        Assert.Equal((5d, 6d, 700d, 300d), (over.BoxX, over.BoxY, over.BoxWidth, over.BoxHeight));
        Assert.Equal(-1, vm.SelectedBox);
    }

    // ── Format panel ─────────────────────────────────────────────

    [Fact]
    public void The_panel_shows_the_theme_where_the_slide_has_no_value()
    {
        var vm = New(TwoSlides());
        var panel = vm.Panel(vm.SelectSlide(0));

        Assert.False(panel.IsExtraBox);
        Assert.Equal(("Segoe UI", 92d), (panel.FontFamily, panel.FontSize));
        Assert.Equal(Theme.TextColor, panel.Color);
        Assert.Equal(1, panel.LineSpacing);
        Assert.Equal(Theme.ShadowOpacity * 100, panel.ShadowPercent);
        Assert.False(panel.Underline);
    }

    [Fact]
    public void Writing_the_panel_stores_the_values_tidied_and_marks_the_slide_as_edited()
    {
        var song = TwoSlides();
        var vm = New(song);
        vm.SelectSlide(0);

        vm.ApplyStyle(0, Form());
        vm.Save();

        var over = song.Sections[0].GetOverride()!;
        Assert.Equal("Georgia", over.FontFamily);
        Assert.Equal(70, over.FontSize);
        Assert.Equal("#00FF00", over.TextColor);
        Assert.Equal("#111111", over.OutlineColor);
        Assert.Equal(0.5, over.ShadowOpacity);
        Assert.Equal(4, (int)over.Case!.Value); // 99 is held to the last case
        Assert.Equal(true, over.Underline);
        Assert.Null(over.Strikethrough);
        Assert.Equal(HAlign.Left, over.AlignH);
        Assert.Equal(VAlign.Top, over.AlignV);
        Assert.Null(song.Sections[1].StyleJson);
    }

    [Fact]
    public void An_empty_font_falls_back_to_the_themes_for_the_words_and_to_none_for_a_box()
    {
        var vm = New(TwoSlides());
        vm.SelectSlide(0);

        vm.ApplyStyle(0, Form(font: "  "));
        Assert.Equal("Segoe UI", vm.OverrideOf(0)!.FontFamily);

        vm.AddTextBox(0);
        vm.ApplyStyle(0, Form(font: ""));
        Assert.Null(vm.OverrideOf(0)!.TextBoxes[0].FontFamily);
    }

    [Fact]
    public void An_extra_box_takes_only_its_own_few_settings()
    {
        var vm = New(TwoSlides());
        vm.SelectSlide(0);
        vm.AddTextBox(0);

        vm.ApplyStyle(0, Form(size: 33.6));

        var box = vm.OverrideOf(0)!.TextBoxes[0];
        Assert.Equal((34d, "#00FF00", true, HAlign.Left, VAlign.Top), (box.FontSize, box.Color, box.Bold, box.AlignH, box.AlignV));
        Assert.Null(vm.OverrideOf(0)!.TextColor);
        Assert.Null(vm.OverrideOf(0)!.Underline);
    }

    [Fact]
    public void The_panel_of_an_extra_box_shows_that_boxs_own_values()
    {
        var vm = New(TwoSlides());
        vm.SelectSlide(0);
        vm.AddTextBox(0);

        var panel = vm.Panel(vm.OverrideOf(0)!);

        Assert.True(panel.IsExtraBox);
        Assert.Equal(48, panel.FontSize);
        Assert.Equal("Segoe UI", panel.FontFamily);
    }

    [Fact]
    public void A_typed_font_counts_only_when_it_differs_from_the_one_in_use()
    {
        var vm = New(TwoSlides());
        vm.SelectSlide(0);

        Assert.False(vm.FontDiffers(0, "segoe ui "));
        Assert.False(vm.FontDiffers(0, "   "));
        Assert.True(vm.FontDiffers(0, "Georgia"));
        Assert.False(vm.FontDiffers(-1, "Georgia"));
    }

    [Fact]
    public void The_colour_picker_answer_is_passed_on_and_cancelling_gives_null()
    {
        Assert.Equal("#123456", New(TwoSlides()).PickColor("#FFFFFF"));
        Assert.Null(New(TwoSlides(), picked: null).PickColor("#FFFFFF"));
    }

    // ── Boxes ────────────────────────────────────────────────────

    [Fact]
    public void Adding_boxes_places_each_one_away_from_the_others_and_selects_it()
    {
        var vm = New(TwoSlides());
        vm.SelectSlide(0);

        vm.AddTextBox(0);
        vm.AddTextBox(0);

        var boxes = vm.OverrideOf(0)!.TextBoxes;
        Assert.Equal(2, boxes.Count);
        Assert.False(boxes[0].Overlaps(boxes[1]));
        Assert.Equal(1, vm.SelectedBox);
        Assert.Equal(["Letra de la canción", "Cuadro 1", "Cuadro 2"], vm.BoxNames(0));
    }

    [Fact]
    public void Removing_a_box_goes_back_to_the_words_and_removing_the_words_does_nothing()
    {
        var vm = New(TwoSlides());
        vm.SelectSlide(0);
        vm.AddTextBox(0);
        vm.AddTextBox(0);
        vm.SelectedBox = 0;

        Assert.True(vm.RemoveTextBox(0));

        Assert.Single(vm.OverrideOf(0)!.TextBoxes);
        Assert.Equal(-1, vm.SelectedBox);
        Assert.False(vm.RemoveTextBox(0));
        Assert.Single(vm.OverrideOf(0)!.TextBoxes);
    }

    [Fact]
    public void Boxes_change_places_in_the_drawing_order_and_stop_at_the_ends()
    {
        var vm = New(TwoSlides());
        vm.SelectSlide(0);
        vm.AddTextBox(0);
        vm.AddTextBox(0);
        vm.ApplyStyle(0, Form()); // box 2 (selected) gets its own look
        vm.SelectedBox = 0;

        Assert.False(vm.MoveBoxOrder(0, -1));
        Assert.True(vm.MoveBoxOrder(0, +1));
        Assert.Equal(1, vm.SelectedBox);
        Assert.Equal("#00FF00", vm.OverrideOf(0)!.TextBoxes[0].Color);
        Assert.False(vm.MoveBoxOrder(0, +1));
    }

    [Fact]
    public void The_buttons_follow_the_chosen_box()
    {
        var vm = New(TwoSlides());

        vm.SelectedBox = -1;
        Assert.Equal((false, false, false), vm.BoxButtons(2));
        vm.SelectedBox = 0;
        Assert.Equal((true, true, false), vm.BoxButtons(2));
        vm.SelectedBox = 1;
        Assert.Equal((true, false, true), vm.BoxButtons(2));
    }

    [Fact]
    public void Tab_walks_through_the_words_and_the_boxes_and_around()
    {
        var vm = New(TwoSlides());
        vm.SelectSlide(0);
        vm.AddTextBox(0);
        vm.AddTextBox(0); // 3 stops: words, box 1, box 2

        vm.SelectedBox = -1;
        Assert.Equal(0, vm.NextBox(0, backwards: false));
        vm.SelectedBox = 1;
        Assert.Equal(-1, vm.NextBox(0, backwards: false));
        vm.SelectedBox = -1;
        Assert.Equal(1, vm.NextBox(0, backwards: true));
        vm.SelectedBox = 0;
        Assert.Equal(-1, vm.NextBox(0, backwards: true));
    }

    [Fact]
    public void The_smallest_box_under_the_pointer_wins_and_later_ones_win_ties()
    {
        var song = TwoSlides();
        var vm = New(song);
        vm.SelectSlide(0); // words: 110,80 1700x920
        vm.AddTextBox(0);  // box 0 at 660,820 600x140
        vm.AddTextBox(0);

        // The point of canvas (480*0.5... ) in the first small box: slide px (700, 850).
        Assert.Equal(0, vm.BoxAt(0, 700 * SongDesignerViewModel.Scale, 850 * SongDesignerViewModel.Scale));
        // Inside the words only.
        Assert.Equal(-1, vm.BoxAt(0, 300 * SongDesignerViewModel.Scale, 300 * SongDesignerViewModel.Scale));
        // Outside everything.
        Assert.Null(vm.BoxAt(0, 5, 5));
        Assert.Null(vm.BoxAt(-1, 300, 300));
    }

    [Fact]
    public void The_ghosts_are_every_box_but_the_chosen_one()
    {
        var vm = New(TwoSlides());
        vm.SelectSlide(0);
        vm.AddTextBox(0);
        vm.AddTextBox(0);

        vm.SelectedBox = -1;
        Assert.Equal(["Cuadro 1", "Cuadro 2"], vm.Ghosts(0).Select(g => g.Label));
        vm.SelectedBox = 1;
        Assert.Equal(["Letra", "Cuadro 1"], vm.Ghosts(0).Select(g => g.Label));
        Assert.Empty(vm.Ghosts(-1));
    }

    [Fact]
    public void The_box_rect_is_the_chosen_boxs()
    {
        var vm = New(TwoSlides());
        var over = vm.SelectSlide(0);
        Assert.Equal((110d, 80d, 1700d, 920d), vm.BoxRect(over));

        vm.AddTextBox(0);
        Assert.Equal((660d, 820d, 600d, 140d), vm.BoxRect(vm.OverrideOf(0)!));
    }

    [Fact]
    public void Moving_or_stretching_the_words_box_is_an_edit_that_is_saved()
    {
        var song = TwoSlides();
        var vm = New(song);
        vm.SelectSlide(0);

        vm.SetBoxGeometry(0, 10, 20, 800, 400);
        vm.Save();

        var over = song.Sections[0].GetOverride()!;
        Assert.Equal((10d, 20d, 800d, 400d), (over.BoxX, over.BoxY, over.BoxWidth, over.BoxHeight));
    }

    [Fact]
    public void Moving_an_extra_box_changes_only_that_box()
    {
        var vm = New(TwoSlides());
        var words = vm.SelectSlide(0);
        vm.AddTextBox(0);

        vm.SetBoxGeometry(0, 1, 2, 300, 100);

        var over = vm.OverrideOf(0)!;
        Assert.Equal((1d, 2d, 300d, 100d), (over.TextBoxes[0].X, over.TextBoxes[0].Y, over.TextBoxes[0].Width, over.TextBoxes[0].Height));
        Assert.Equal(words.BoxX, over.BoxX);
    }

    [Fact]
    public void Box_numbers_for_the_fields_are_rounded_slide_pixels()
    {
        var (x, y, w, h, percent) = SongDesignerViewModel.BoxInfo(55.2, 40, 480, 270);

        Assert.Equal((110d, 80d, 960d, 540d), (x, y, w, h));
        Assert.Equal(50, percent);
    }

    [Fact]
    public void A_percent_resizes_the_box_around_its_centre_inside_the_canvas()
    {
        var (left, top, w, h) = SongDesignerViewModel.ResizeToPercent(100, 100, 200, 100, 50);

        Assert.Equal((480d, 270d), (w, h));
        Assert.Equal(0d, left); // centre 200 - half of 480 would be -40
        Assert.Equal(15d, top); // centre 150 - half of 270

        var big = SongDesignerViewModel.ResizeToPercent(0, 0, 960, 540, 5000);
        Assert.Equal((0d, 0d, 960d, 540d), big);

        var small = SongDesignerViewModel.ResizeToPercent(400, 200, 100, 100, 0);
        Assert.Equal((96d, 54d), (small.Width, small.Height));
    }

    [Fact]
    public void Typed_fields_become_a_box_inside_the_canvas()
    {
        var (left, top, w, h) = SongDesignerViewModel.FromTypedFields(1800, 1000, 600, 5);

        Assert.Equal((300d, 20d), (w, h));
        Assert.Equal(660d, left);
        Assert.Equal(500d, top);
    }

    [Theory]
    [InlineData("HL", 0, 50)]
    [InlineData("HC", 380, 50)]
    [InlineData("HR", 760, 50)]
    [InlineData("VT", 10, 0)]
    [InlineData("VC", 10, 220)]
    [InlineData("VB", 10, 440)]
    [InlineData("CC", 380, 220)]
    [InlineData("??", 10, 50)]
    [InlineData(null, 10, 50)]
    public void The_alignment_buttons_put_a_200x100_box_in_its_place(string? tag, double x, double y)
    {
        var (left, top) = SongDesignerViewModel.AlignBox(tag, 10, 50, 200, 100);

        Assert.Equal((x, y), (left, top));
    }

    // ── Writing on the slide ─────────────────────────────────────

    [Fact]
    public void Editing_the_words_starts_with_the_text_and_hides_it_behind()
    {
        var vm = New(TwoSlides());
        vm.SelectSlide(0);

        var edit = vm.BeginEdit(0);

        Assert.Equal("Primera estrofa", edit.Text);
        Assert.Equal((92d, "Segoe UI"), (edit.FontSize, edit.FontFamily));
        Assert.Equal(string.Empty, edit.PreviewMeanwhile.MainText);
    }

    [Fact]
    public void Editing_an_extra_box_starts_with_its_text_and_blanks_only_that_box_behind()
    {
        var vm = New(TwoSlides());
        vm.SelectSlide(0);
        vm.AddTextBox(0);

        var edit = vm.BeginEdit(0);

        Assert.Equal("Texto", edit.Text);
        Assert.Equal("Primera estrofa", edit.PreviewMeanwhile.MainText);
        Assert.Equal(string.Empty, edit.PreviewMeanwhile.Override!.TextBoxes[0].Text);
        Assert.Equal("Texto", vm.OverrideOf(0)!.TextBoxes[0].Text);
    }

    [Fact]
    public void Finishing_the_words_changes_the_text_but_does_not_mark_the_design_as_edited()
    {
        var song = TwoSlides();
        var vm = New(song);
        vm.SelectSlide(0);

        Assert.True(vm.CommitEdit(0, "  Otra letra  "));
        vm.Save();

        Assert.Equal("Otra letra", song.Sections[0].Text);
        Assert.Null(song.Sections[0].StyleJson);
    }

    [Fact]
    public void Finishing_an_extra_box_changes_its_text_and_marks_the_slide_as_edited()
    {
        var song = TwoSlides();
        var vm = New(song);
        vm.SelectSlide(0);
        vm.AddTextBox(0);

        vm.CommitEdit(0, "Serie");
        vm.Save();

        Assert.Equal("Serie", song.Sections[0].GetOverride()!.TextBoxes[0].Text);
        Assert.Equal("Primera estrofa", song.Sections[0].Text);
    }

    [Fact]
    public void Finishing_on_a_slide_that_is_not_there_does_nothing()
    {
        var vm = New(TwoSlides());

        Assert.False(vm.CommitEdit(-1, "x"));
        Assert.False(vm.CommitEdit(2, "x"));
    }

    [Fact]
    public void Blank_text_does_not_wipe_the_words()
    {
        var song = TwoSlides();
        var vm = New(song);
        vm.CommitEdit(0, "   ");
        vm.Save();

        Assert.Equal("Primera estrofa", song.Sections[0].Text);
    }

    // ── Actions ──────────────────────────────────────────────────

    [Fact]
    public void Applying_to_all_gives_every_slide_the_design_but_keeps_its_own_extra_boxes()
    {
        var song = TwoSlides();
        var vm = New(song);
        vm.SelectSlide(0);
        vm.SelectSlide(1);
        vm.AddTextBox(1);
        vm.SelectedBox = -1;
        vm.ApplyStyle(0, Form(font: "Georgia"));

        vm.ApplyToAll(0);
        vm.Save();

        Assert.Equal("Georgia", song.Sections[1].GetOverride()!.FontFamily);
        Assert.Single(song.Sections[1].GetOverride()!.TextBoxes);
        Assert.Empty(song.Sections[0].GetOverride()!.TextBoxes);
    }

    [Fact]
    public void Applying_a_slide_that_follows_the_theme_puts_all_back_under_it()
    {
        var song = TwoSlides(System.Text.Json.JsonSerializer.Serialize(new SlideOverride(FontFamily: "Georgia")));
        var vm = New(song);

        vm.ApplyToAll(0); // slide 0 has no design at all (never chosen, so no box either)
        vm.Save();

        Assert.Null(song.Sections[1].StyleJson);
    }

    [Fact]
    public void Resetting_a_slide_drops_its_design_and_its_edit_mark()
    {
        var song = TwoSlides();
        var vm = New(song);
        vm.SelectSlide(0);
        vm.ApplyStyle(0, Form());

        var over = vm.ResetSlide(0);
        vm.Save();

        Assert.Equal((110d, 80d), (over.BoxX, over.BoxY));
        Assert.Null(over.FontFamily);
        Assert.Null(song.Sections[0].StyleJson);
    }

    [Fact]
    public void A_new_slide_goes_after_the_chosen_one_and_the_edits_stay_with_their_slides()
    {
        var song = TwoSlides();
        var vm = New(song);
        vm.SelectSlide(1);
        vm.ApplyStyle(1, Form());

        var at = vm.InsertSlideAfter(0);
        vm.CommitEdit(at, "En el medio");

        Assert.Equal(1, at);
        Assert.Equal(["Verso 1", "Nueva", "Coro"], Enumerable.Range(0, vm.SlideCount).Select(vm.SlideLabel));
        // Cancelling has to leave the song alone: nothing is in it yet.
        Assert.Equal(2, song.Sections.Count);

        vm.Save();

        Assert.Equal(["Primera estrofa", "En el medio", "Estribillo"], song.Sections.Select(s => s.Text));
        Assert.Equal([0, 1, 2], song.Sections.Select(s => s.Order));
        Assert.Null(song.Sections[1].StyleJson);
        Assert.Equal("Georgia", song.Sections[2].GetOverride()!.FontFamily);
    }

    [Fact]
    public void A_new_slide_left_empty_is_not_saved()
    {
        var song = TwoSlides();
        var vm = New(song);

        vm.InsertSlideAfter(-1);
        vm.Save();

        Assert.Equal(["Primera estrofa", "Estribillo"], song.Sections.Select(s => s.Text));
    }

    [Fact]
    public void Cancelling_leaves_the_song_as_it_was()
    {
        var song = TwoSlides();
        var vm = New(song);
        vm.SelectSlide(0);
        vm.ApplyStyle(0, Form());
        vm.CommitEdit(0, "Cambiada");

        // No Save: the window was cancelled.
        Assert.False(vm.Saved);
        Assert.Equal("Primera estrofa", song.Sections[0].Text);
        Assert.Null(song.Sections[0].StyleJson);
    }

    [Fact]
    public void Saving_drops_the_decorations_that_are_off_and_keeps_the_rest()
    {
        var song = TwoSlides();
        var vm = New(song);
        vm.SelectSlide(0);
        vm.ApplyStyle(0, Form() with { Underline = false, Strikethrough = false });
        vm.Save();

        var over = song.Sections[0].GetOverride()!;
        Assert.Null(over.Underline);
        Assert.Null(over.Strikethrough);
        Assert.Equal("Georgia", over.FontFamily);
        Assert.True(vm.Saved);
    }
}
