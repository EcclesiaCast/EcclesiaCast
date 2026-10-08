using EcclesiaCast.Core.Presentation;

namespace EcclesiaCast.Core.Tests.Presentation;

public class HighlightSetTests
{
    private const string Text = "La gracia de Dios, por gracia sois salvos";

    private static string Cut(string text, TextSpan span) => text.Substring(span.Start, span.Length);

    private static int SecondGracia() => Text.LastIndexOf("gracia", StringComparison.Ordinal);

    [Fact]
    public void A_typed_term_marks_every_appearance_ignoring_case()
    {
        var set = new HighlightSet(["GRACIA"], []);

        var marked = set.Resolve(Text).Select(s => Cut(Text, s)).ToList();

        Assert.Equal(["gracia", "gracia"], marked);
    }

    [Fact]
    public void Several_terms_each_mark_their_own_words()
    {
        var set = new HighlightSet(HighlightSet.SplitTerms("Dios; salvos"), []);

        var marked = set.Resolve(Text).Select(s => Cut(Text, s)).ToList();

        Assert.Equal(["Dios", "salvos"], marked);
    }

    [Fact]
    public void The_box_text_is_split_by_semicolons_dropping_blanks_and_repeats()
    {
        var terms = HighlightSet.SplitTerms("  Dios ;; gracia ;  ; DIOS ");

        Assert.Equal(["Dios", "gracia", "DIOS"], terms);
        Assert.Equal(["Dios", "gracia"], new HighlightSet(terms, []).Terms);
    }

    [Fact]
    public void A_span_marks_only_that_appearance()
    {
        var set = new HighlightSet([], [new TextSpan(SecondGracia(), 6)]);

        var marked = set.Resolve(Text);

        Assert.Equal([new TextSpan(SecondGracia(), 6)], marked);
    }

    [Fact]
    public void Spans_do_not_count_for_the_second_version_text()
    {
        var set = new HighlightSet(["Dios"], [new TextSpan(3, 6)]);

        var marked = set.Resolve(Text, includeSpans: false).Select(s => Cut(Text, s)).ToList();

        Assert.Equal(["Dios"], marked);
    }

    [Fact]
    public void Overlapping_marks_are_joined_into_one()
    {
        // "gracia de" typed, and "de Dios" pointed at.
        var set = new HighlightSet(["gracia de"], [new TextSpan(10, 7)]);

        var marked = set.Resolve(Text).Select(s => Cut(Text, s)).ToList();

        Assert.Equal(["gracia de Dios"], marked);
    }

    [Fact]
    public void A_span_past_the_end_of_the_text_is_cut_or_ignored()
    {
        var set = new HighlightSet([], [new TextSpan(Text.Length - 3, 10), new TextSpan(500, 4)]);

        var marked = set.Resolve(Text);

        Assert.Equal([new TextSpan(Text.Length - 3, 3)], marked);
    }

    [Fact]
    public void Pointing_marks_only_that_appearance_and_replaces_the_earlier_pointing()
    {
        var first = new TextSpan(3, 6);
        var second = new TextSpan(SecondGracia(), 6);

        var set = HighlightSet.None.Point(first, Text, addToOthers: false)
            .Point(second, Text, addToOthers: false);

        Assert.Equal([second], set.Spans);
    }

    [Fact]
    public void Pointing_with_Shift_adds_to_the_earlier_marks()
    {
        var first = new TextSpan(3, 6);
        var second = new TextSpan(SecondGracia(), 6);

        var set = HighlightSet.None.Point(first, Text, addToOthers: false)
            .Point(second, Text, addToOthers: true);

        Assert.Equal([first, second], set.Spans);
    }

    [Fact]
    public void Pointing_again_at_something_marked_takes_it_off_and_keeps_the_others()
    {
        var first = new TextSpan(3, 6);
        var second = new TextSpan(SecondGracia(), 6);
        var set = new HighlightSet([], [first, second]);

        var after = set.Point(first, Text, addToOthers: true);

        Assert.Equal([second], after.Spans);
    }

    [Fact]
    public void Pointing_at_a_word_inside_a_marked_phrase_takes_the_phrase_off()
    {
        var set = new HighlightSet([], [new TextSpan(3, 14)]);

        var after = set.Point(new TextSpan(10, 3), Text, addToOthers: false);

        Assert.Empty(after.Spans);
    }

    [Fact]
    public void Dragging_over_a_marked_word_marks_the_larger_phrase()
    {
        var set = new HighlightSet([], [new TextSpan(3, 6)]);

        var after = set.Point(new TextSpan(3, 14), Text, addToOthers: false);

        Assert.Equal([new TextSpan(3, 14)], after.Spans);
    }

    [Fact]
    public void Pointing_at_a_word_marked_by_a_typed_term_takes_that_term_off()
    {
        var set = new HighlightSet(["gracia", "salvos"], []);

        var after = set.Point(new TextSpan(3, 6), Text, addToOthers: false);

        Assert.Equal(["salvos"], after.Terms);
        Assert.Empty(after.Spans);
    }

    [Fact]
    public void Pointing_keeps_the_typed_terms()
    {
        var set = new HighlightSet(["Dios"], []);

        var after = set.Point(new TextSpan(3, 6), Text, addToOthers: false);

        Assert.Equal(["Dios"], after.Terms);
        Assert.Equal([new TextSpan(3, 6)], after.Spans);
    }

    [Fact]
    public void Sets_with_the_same_marks_are_equal()
    {
        Assert.Equal(new HighlightSet(["Dios"], [new TextSpan(1, 2)]), new HighlightSet(["dios"], [new TextSpan(1, 2)]));
        Assert.NotEqual(new HighlightSet(["Dios"], []), new HighlightSet(["Dios"], [new TextSpan(1, 2)]));
        Assert.Equal(HighlightSet.None, new HighlightSet([], []));
    }
}
