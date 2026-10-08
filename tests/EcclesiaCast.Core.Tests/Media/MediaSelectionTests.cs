using EcclesiaCast.Core.Media;

namespace EcclesiaCast.Core.Tests.Media;

public class MediaSelectionTests
{
    private static readonly int[] Order = [10, 20, 30, 40, 50];

    [Fact]
    public void A_plain_click_leaves_only_that_one()
    {
        var selection = new MediaSelection();
        selection.Only(10);
        selection.Only(30);

        Assert.Equal([30], selection.InOrder(Order));
    }

    [Fact]
    public void Ctrl_click_adds_and_removes_one_without_touching_the_rest()
    {
        var selection = new MediaSelection();
        selection.Only(10);
        selection.Toggle(30);
        selection.Toggle(50);
        selection.Toggle(30);

        Assert.Equal([10, 50], selection.InOrder(Order));
    }

    [Fact]
    public void Shift_click_takes_the_stretch_from_the_last_one_clicked()
    {
        var selection = new MediaSelection();
        selection.Only(20);
        selection.Range(Order, 40, additive: false);

        Assert.Equal([20, 30, 40], selection.InOrder(Order));
    }

    [Fact]
    public void Shift_click_upwards_also_works_and_keeps_the_anchor()
    {
        var selection = new MediaSelection();
        selection.Only(40);
        selection.Range(Order, 20, additive: false);
        selection.Range(Order, 50, additive: false);

        Assert.Equal([40, 50], selection.InOrder(Order));
    }

    [Fact]
    public void Shift_click_replaces_what_was_picked_and_ctrl_shift_keeps_it()
    {
        var selection = new MediaSelection();
        selection.Only(10);
        selection.Toggle(30);
        selection.Range(Order, 50, additive: false);
        Assert.Equal([30, 40, 50], selection.InOrder(Order));

        selection.Only(10);
        selection.Toggle(30);
        selection.Range(Order, 50, additive: true);
        Assert.Equal([10, 30, 40, 50], selection.InOrder(Order));
    }

    [Fact]
    public void Shift_click_with_nothing_picked_before_is_a_plain_click()
    {
        var selection = new MediaSelection();
        selection.Range(Order, 30, additive: false);

        Assert.Equal([30], selection.InOrder(Order));
    }

    [Fact]
    public void Select_all_and_clear()
    {
        var selection = new MediaSelection();
        selection.Set(Order, additive: false);
        Assert.Equal(5, selection.Count);

        selection.Clear();
        Assert.Equal(0, selection.Count);
        Assert.Null(selection.Anchor);
    }

    [Fact]
    public void The_rectangle_replaces_or_adds_depending_on_ctrl()
    {
        var selection = new MediaSelection();
        selection.Only(10);
        selection.Set([30, 40], additive: true);
        Assert.Equal([10, 30, 40], selection.InOrder(Order));

        selection.Set([50], additive: false);
        Assert.Equal([50], selection.InOrder(Order));
    }

    [Fact]
    public void Items_that_are_no_longer_shown_leave_the_selection()
    {
        var selection = new MediaSelection();
        selection.Set(Order, additive: false);
        selection.Retain([20, 40, 99]);

        Assert.Equal([20, 40], selection.InOrder(Order));
        Assert.False(selection.Contains(10));
    }

    [Fact]
    public void The_order_is_the_one_shown_not_the_one_clicked()
    {
        var selection = new MediaSelection();
        selection.Only(50);
        selection.Toggle(10);
        selection.Toggle(30);

        Assert.Equal([10, 30, 50], selection.InOrder(Order));
    }
}
