using EcclesiaCast.Core.Stage;
using EcclesiaCast.Presentation.Services;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Presentation.Tests;

[Collection("ViewModels")]
public sealed class StageDesignerViewModelTests
{
    private static StageLayout Layout(string name, params StageBox[] boxes) =>
        new() { Name = name, Boxes = [.. boxes] };

    private static StageBox Box(double x, double y, double w, double h, StageBoxKind kind = StageBoxKind.FixedText) =>
        new() { X = x, Y = y, Width = w, Height = h, Kind = kind };

    private static StageDesignerViewModel New(
        IReadOnlyList<StageLayout> layouts, string active = "Cantantes",
        string? answer = null, string? picked = "#00FF00") =>
        new(layouts, active, (_, _, _) => answer, new FixedColorPicker(picked), new NoFonts());

    [Fact]
    public void Opens_on_the_arrangement_in_use_and_works_on_copies()
    {
        var original = Layout("Cantantes", Box(0, 0, 100, 100));
        var vm = New([Layout("Pastor"), original], "cantantes");

        Assert.Equal("Cantantes", vm.InUse.Name);
        Assert.Same(vm.InUse, vm.Current);
        Assert.Equal(["Pastor", "Cantantes"], vm.Layouts.Select(l => l.Name));

        vm.Current.Boxes[0].X = 500;
        Assert.Equal(0, original.Boxes[0].X);
    }

    [Fact]
    public void With_no_arrangements_there_is_one_called_Cantantes()
    {
        var vm = New([]);

        Assert.Equal(StageLayoutStore.SingersName, vm.Current.Name);
        Assert.Single(vm.Layouts);
    }

    [Fact]
    public void An_unknown_active_name_opens_the_first_one()
    {
        var vm = New([Layout("A"), Layout("B")], "Z");

        Assert.Equal("A", vm.InUse.Name);
    }

    [Fact]
    public void The_picker_marks_the_one_in_use()
    {
        var vm = New([Layout("A"), Layout("B")], "A");

        Assert.Equal("A  ·  en uso", vm.LayoutText(vm.Layouts[0]));
        Assert.Equal("B", vm.LayoutText(vm.Layouts[1]));
        Assert.False(vm.CanUseCurrent);

        vm.OpenLayout(vm.Layouts[1]);
        Assert.True(vm.CanUseCurrent);
        vm.UseCurrent();
        Assert.False(vm.CanUseCurrent);
        Assert.Equal("B", vm.Build().ActiveName);
        Assert.Contains("«B»", vm.InUseText);
    }

    [Fact]
    public void A_new_arrangement_gets_a_unique_name_and_one_live_text_box()
    {
        var vm = New([Layout("Escenario")], "Escenario", answer: "Escenario");

        Assert.True(vm.NewLayout());

        Assert.NotEqual("Escenario", vm.Current.Name);
        Assert.Equal(2, vm.Layouts.Count);
        Assert.Equal(StageBoxKind.LiveText, vm.Current.Boxes.Single().Kind);
    }

    [Fact]
    public void Cancelling_the_name_prompt_adds_and_renames_nothing()
    {
        var vm = New([Layout("A")], "A", answer: null);

        Assert.False(vm.NewLayout());
        Assert.False(vm.RenameLayout());

        Assert.Single(vm.Layouts);
        Assert.Equal("A", vm.Current.Name);
    }

    [Fact]
    public void An_empty_name_counts_as_cancelling()
    {
        var vm = New([Layout("A")], "A", answer: "");

        Assert.False(vm.NewLayout());
        Assert.False(vm.RenameLayout());
    }

    [Fact]
    public void Renaming_keeps_the_name_unique_among_the_others()
    {
        var vm = New([Layout("A"), Layout("B")], "A", answer: "B");

        Assert.True(vm.RenameLayout());

        Assert.NotEqual("B", vm.Current.Name);
        Assert.StartsWith("B", vm.Current.Name);
    }

    [Fact]
    public void Renaming_to_its_own_name_keeps_it()
    {
        var vm = New([Layout("A"), Layout("B")], "A", answer: "A");

        vm.RenameLayout();

        Assert.Equal("A", vm.Current.Name);
    }

    [Fact]
    public void Duplicating_copies_the_boxes_under_new_ids()
    {
        var box = Box(10, 20, 300, 100);
        var vm = New([Layout("A", box)], "A");
        var oldId = vm.Current.Boxes[0].Id;

        vm.DuplicateLayout();

        Assert.Equal("A copia", vm.Current.Name);
        Assert.Equal(2, vm.Layouts.Count);
        Assert.NotEqual(oldId, vm.Current.Boxes[0].Id);
        Assert.Equal(10, vm.Current.Boxes[0].X);
        Assert.Equal(oldId, vm.Layouts[0].Boxes[0].Id);
    }

    [Fact]
    public void The_last_arrangement_cannot_be_deleted_and_says_so()
    {
        using var notices = new ScriptedNotices();
        var vm = New([Layout("A")], "A");

        Assert.False(vm.DeleteLayout());

        Assert.Single(vm.Layouts);
        Assert.Equal("Tiene que quedar al menos un escenario.", notices.Asked.Single().Message);
    }

    [Fact]
    public void Deleting_asks_first_and_does_nothing_when_the_answer_is_no()
    {
        using var notices = new ScriptedNotices().No();
        var vm = New([Layout("A"), Layout("B")], "A");

        Assert.False(vm.DeleteLayout());

        Assert.Equal(2, vm.Layouts.Count);
        Assert.Equal("¿Eliminar el escenario «A»?", notices.Asked.Single().Message);
    }

    [Fact]
    public void Deleting_the_one_in_use_passes_the_use_to_the_one_that_takes_its_place()
    {
        using var notices = new ScriptedNotices().Yes();
        var vm = New([Layout("A"), Layout("B"), Layout("C")], "B");
        vm.OpenLayout(vm.Layouts[1]);

        Assert.True(vm.DeleteLayout());

        Assert.Equal(["A", "C"], vm.Layouts.Select(l => l.Name));
        Assert.Equal("C", vm.Current.Name);
        Assert.Equal("C", vm.InUse.Name);
    }

    [Fact]
    public void Deleting_the_last_of_the_list_goes_to_the_one_before()
    {
        using var notices = new ScriptedNotices().Yes();
        var vm = New([Layout("A"), Layout("B")], "A");
        vm.OpenLayout(vm.Layouts[1]);

        vm.DeleteLayout();

        Assert.Equal("A", vm.Current.Name);
        Assert.Equal("A", vm.InUse.Name);
    }

    // ── Boxes ────────────────────────────────────────────────────

    [Fact]
    public void A_fixed_text_box_is_listed_with_the_start_of_its_text()
    {
        Assert.Equal(StageBox.KindName(StageBoxKind.Clock),
            StageDesignerViewModel.BoxTitle(Box(0, 0, 1, 1, StageBoxKind.Clock)));

        var fixedText = Box(0, 0, 1, 1);
        fixedText.Text = "Bienvenidos a la casa\r\nde Dios, hermanos";
        Assert.Equal(StageBox.KindName(StageBoxKind.FixedText) + ": Bienvenidos a la casa …",
            StageDesignerViewModel.BoxTitle(fixedText));

        fixedText.Text = "Hola";
        Assert.EndsWith(": Hola", StageDesignerViewModel.BoxTitle(fixedText));

        fixedText.Text = "  ";
        Assert.Equal(StageBox.KindName(StageBoxKind.FixedText), StageDesignerViewModel.BoxTitle(fixedText));
    }

    [Theory]
    [InlineData(StageBoxKind.LiveText, 1600, 560, 110, true)]
    [InlineData(StageBoxKind.Clock, 460, 150, 120, false)]
    [InlineData(StageBoxKind.Notes, 1600, 600, 60, false)]
    [InlineData(StageBoxKind.FixedText, 900, 100, 48, false)]
    public void A_new_box_has_the_size_that_reads_well_for_its_kind_and_sits_in_the_middle(
        StageBoxKind kind, double w, double h, double fontSize, bool bold)
    {
        var box = StageDesignerViewModel.NewBox(kind);

        Assert.Equal((w, h, fontSize, bold), (box.Width, box.Height, box.FontSize, box.Bold));
        Assert.Equal((1920 - w) / 2, box.X);
        Assert.Equal((1080 - h) / 2, box.Y);
        Assert.Equal(kind == StageBoxKind.Notes ? StageAlign.Left : StageAlign.Center, box.Align);
        Assert.Equal(kind == StageBoxKind.FixedText ? "Texto" : string.Empty, box.Text);
    }

    [Fact]
    public void Adding_a_box_puts_it_in_the_open_arrangement_and_selects_it()
    {
        var vm = New([Layout("A")], "A");
        var box = StageDesignerViewModel.NewBox(StageBoxKind.Clock);

        vm.AddBox(box);

        Assert.Same(box, vm.Selected);
        Assert.Contains(box, vm.Current.Boxes);
    }

    [Fact]
    public void Duplicating_a_box_moves_the_copy_down_and_right_but_not_off_the_canvas()
    {
        var vm = New([Layout("A", Box(100, 200, 300, 100), Box(1800, 1000, 100, 70))], "A");
        vm.Selected = vm.Current.Boxes[0];

        var copy = vm.DuplicateSelected()!;

        Assert.Equal((140, 240), (copy.X, copy.Y));
        Assert.NotEqual(vm.Current.Boxes[0].Id, copy.Id);
        Assert.Same(copy, vm.Selected);

        vm.Selected = vm.Current.Boxes[1];
        var edge = vm.DuplicateSelected()!;
        Assert.Equal((1820, 1010), (edge.X, edge.Y));
    }

    [Fact]
    public void Duplicating_with_nothing_selected_does_nothing()
    {
        var vm = New([Layout("A", Box(0, 0, 10, 10))], "A");
        vm.Selected = null;

        Assert.Null(vm.DuplicateSelected());
        Assert.Single(vm.Current.Boxes);
    }

    [Fact]
    public void Deleting_a_box_selects_the_one_that_takes_its_place()
    {
        var vm = New([Layout("A", Box(0, 0, 10, 10), Box(1, 1, 10, 10), Box(2, 2, 10, 10))], "A");
        var boxes = vm.Current.Boxes.ToList();

        vm.Selected = boxes[1];
        Assert.Same(boxes[1], vm.DeleteSelected());
        Assert.Same(boxes[2], vm.Selected);

        Assert.Same(boxes[2], vm.DeleteSelected());
        Assert.Same(boxes[0], vm.Selected);

        vm.DeleteSelected();
        Assert.Null(vm.Selected);
        Assert.Empty(vm.Current.Boxes);
        Assert.Null(vm.DeleteSelected());
    }

    [Fact]
    public void The_topmost_box_under_a_point_wins()
    {
        var vm = New([Layout("A", Box(0, 0, 500, 500), Box(100, 100, 100, 100))], "A");

        Assert.Same(vm.Current.Boxes[1], vm.BoxAt(150, 150));
        Assert.Same(vm.Current.Boxes[0], vm.BoxAt(400, 400));
        Assert.Null(vm.BoxAt(900, 900));
    }

    // ── Properties ───────────────────────────────────────────────

    private static (StageDesignerViewModel Vm, StageBox Box) WithBox(Action<StageBox>? setup = null)
    {
        var box = Box(100, 100, 400, 200);
        setup?.Invoke(box);
        var vm = New([Layout("A", box)], "A");
        vm.Selected = vm.Current.Boxes[0];
        return (vm, vm.Selected);
    }

    [Fact]
    public void Properties_are_written_into_the_selected_box()
    {
        var (vm, box) = WithBox();

        vm.SetKind(StageBoxKind.Clock);
        vm.SetText("Hola");
        vm.SetBold(true);
        vm.SetAlign("Right");

        Assert.Equal(StageBoxKind.Clock, box.Kind);
        Assert.Equal("Hola", box.Text);
        Assert.True(box.Bold);
        Assert.Equal(StageAlign.Right, box.Align);
    }

    [Fact]
    public void Nothing_changes_when_no_box_is_selected()
    {
        var (vm, box) = WithBox();
        vm.Selected = null;

        vm.SetKind(StageBoxKind.Clock);
        vm.SetText("x");
        vm.SetGeometry(5, 5, 50, 50);
        vm.MoveTo(0, 0);
        vm.Nudge(1, 0, 10);
        vm.Resize("E", 10, 0);

        Assert.Null(vm.PickColor());
        Assert.Equal((StageBoxKind.FixedText, string.Empty, 100d, 400d), (box.Kind, box.Text, box.X, box.Width));
    }

    [Fact]
    public void A_typed_font_is_trimmed_and_empty_means_the_default()
    {
        var (vm, box) = WithBox();

        vm.SetFont("  Georgia ");
        Assert.Equal("Georgia", box.FontFamily);

        vm.SetFont("   ");
        Assert.Null(box.FontFamily);
    }

    [Fact]
    public void Font_size_stays_between_8_and_600()
    {
        var (vm, box) = WithBox();

        vm.SetFontSize(2);
        Assert.Equal(8, box.FontSize);
        vm.SetFontSize(9000);
        Assert.Equal(600, box.FontSize);
        vm.SetFontSize(72);
        Assert.Equal(72, box.FontSize);
    }

    [Fact]
    public void The_colour_picked_goes_into_the_box_and_cancelling_keeps_it()
    {
        var (vm, box) = WithBox();
        Assert.Equal("#00FF00", vm.PickColor());
        Assert.Equal("#00FF00", box.Color);

        var cancelled = New([Layout("A", Box(0, 0, 1, 1))], "A", picked: null);
        cancelled.Selected = cancelled.Current.Boxes[0];
        var before = cancelled.Selected.Color;
        Assert.Null(cancelled.PickColor());
        Assert.Equal(before, cancelled.Selected.Color);
    }

    [Fact]
    public void Typed_geometry_stays_inside_the_canvas()
    {
        var (vm, box) = WithBox();

        vm.SetGeometry(1900, 1070, 500, 5);

        Assert.Equal((500d, 20d), (box.Width, box.Height));
        Assert.Equal(1420, box.X);
        Assert.Equal(1060, box.Y);

        vm.SetGeometry(-10, -10, 99999, 99999);
        Assert.Equal((1920d, 1080d, 0d, 0d), (box.Width, box.Height, box.X, box.Y));
    }

    [Fact]
    public void Dragging_rounds_and_keeps_the_box_inside_the_canvas()
    {
        var (vm, box) = WithBox();

        vm.MoveTo(10.4, 20.6);
        Assert.Equal((10d, 21d), (box.X, box.Y));

        vm.MoveTo(-50, 5000);
        Assert.Equal((0d, 880d), (box.X, box.Y));

        vm.MoveTo(5000, -5);
        Assert.Equal((1520d, 0d), (box.X, box.Y));
    }

    [Fact]
    public void Arrow_keys_move_by_the_step_and_stop_at_the_edges()
    {
        var (vm, box) = WithBox();

        vm.Nudge(-1, 0, 10);
        vm.Nudge(0, 1, 1);
        Assert.Equal((90d, 101d), (box.X, box.Y));

        vm.Nudge(-1, 0, 500);
        vm.Nudge(0, -1, 500);
        Assert.Equal((0d, 0d), (box.X, box.Y));

        vm.Nudge(1, 0, 5000);
        vm.Nudge(0, 1, 5000);
        Assert.Equal((1520d, 880d), (box.X, box.Y));
    }

    [Fact]
    public void Stretching_a_handle_changes_the_size_and_rounds()
    {
        var (vm, box) = WithBox();

        vm.Resize("SE", 100.4, 50.6);
        Assert.Equal((100d, 100d, 500d, 251d), (box.X, box.Y, box.Width, box.Height));

        vm.Resize("NW", 20, 10);
        Assert.Equal((120d, 110d, 480d, 241d), (box.X, box.Y, box.Width, box.Height));
    }

    [Fact]
    public void Every_kind_of_box_has_a_hint()
    {
        foreach (var kind in Enum.GetValues<StageBoxKind>())
            Assert.NotEmpty(StageDesignerViewModel.KindHint(kind));
    }

    [Fact]
    public void Build_hands_back_the_arrangements_and_the_one_in_use()
    {
        var vm = New([Layout("A"), Layout("B")], "B");

        var state = vm.Build();

        Assert.Equal("B", state.ActiveName);
        Assert.Equal(["A", "B"], state.Layouts.Select(l => l.Name));
    }
}
