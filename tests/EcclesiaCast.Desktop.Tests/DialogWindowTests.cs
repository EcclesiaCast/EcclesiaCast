using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using EcclesiaCast.Desktop.Views;
using EcclesiaCast.Presentation.Platform;
using EcclesiaCast.Presentation.Services;

namespace EcclesiaCast.Desktop.Tests;

/// <summary>What each simple dialog answers when its buttons are pressed.</summary>
public sealed class DialogWindowTests
{
    /// <summary>Presses the button that reads <paramref name="text"/>.</summary>
    internal static void Press(Window window, string text) =>
        window.GetLogicalDescendants().OfType<Button>().Single(b => Equals(b.Content, text))
            .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    private static bool Closes(Window window, Action act)
    {
        var closed = false;
        window.Closed += (_, _) => closed = true;
        window.Show();
        act();
        return closed;
    }

    [AvaloniaFact]
    public void Picking_an_option_answers_with_it_and_the_tick()
    {
        var window = new ChoiceWindow("T", "M", ["Uno", "Dos"], "Para el resto");
        var box = window.GetLogicalDescendants().OfType<CheckBox>().Single();

        Assert.True(Closes(window, () =>
        {
            box.IsChecked = true;
            Press(window, "Dos");
        }));

        Assert.Equal(1, window.Chosen);
        Assert.True(window.Remember);
    }

    [AvaloniaFact]
    public void Cancelling_a_choice_picks_nothing()
    {
        var window = new ChoiceWindow("T", "M", ["Uno", "Dos"]);

        Assert.True(Closes(window, () => Press(window, "Cancelar")));

        Assert.Equal(-1, window.Chosen);
        Assert.False(window.Remember);
    }

    [AvaloniaFact]
    public void Accepting_gives_the_texts_trimmed()
    {
        var window = new TextPromptWindow("T", "M", "  hola  ");
        window.AddSecondField("Otro", " chau ");

        Assert.True(Closes(window, () => Press(window, "Aceptar")));

        Assert.True(window.Accepted);
        Assert.Equal("hola", window.Value);
        Assert.Equal("chau", window.SecondValue);
    }

    [AvaloniaFact]
    public void A_notice_answers_with_the_button_pressed_and_closed_it_cancels()
    {
        NoticeWindow Question() => new("M", "T", NoticeKind.Question, ["Cancelar", "No", "Sí"], 2, 0);

        var pressed = Question();
        Assert.True(Closes(pressed, () => Press(pressed, "No")));
        Assert.Equal(1, pressed.Chosen);

        var closed = Question();
        Assert.True(Closes(closed, closed.Close));
        Assert.Equal(0, closed.Chosen);
    }

    [AvaloniaFact]
    public void The_mark_in_a_message_becomes_the_icon()
    {
        var window = new NoticeWindow($"Antes {Notice.OnTopIconMark} después", null, NoticeKind.Info, ["Ok"], 0, 0);

        var inlines = window.FindControl<TextBlock>("MessageText")!.Inlines!;

        Assert.Equal(3, inlines.Count);
        Assert.IsType<InlineUIContainer>(inlines[1]);
        Assert.DoesNotContain(inlines.OfType<Run>(), r => r.Text!.Contains('{'));
    }

    [AvaloniaFact]
    public void Only_a_destructive_question_paints_its_button_red()
    {
        var red = new NoticeWindow("M", null, NoticeKind.Warning, ["Cancelar", "Borrar"], 1, 0, destructive: true);
        var usual = new NoticeWindow("M", null, NoticeKind.Question, ["Cancelar", "Borrar"], 1, 0);

        IBrush? Background(NoticeWindow window)
        {
            window.Show();
            var brush = window.GetLogicalDescendants().OfType<Button>().Single(b => Equals(b.Content, "Borrar")).Background;
            window.Close();
            return brush;
        }

        Assert.Equal(Color.Parse("#C43B3B"), ((ISolidColorBrush)Background(red)!).Color);
        Assert.Equal(Color.Parse("#2F63C9"), ((ISolidColorBrush)Background(usual)!).Color);
    }

    [AvaloniaFact]
    public void Cancelling_a_prompt_accepts_nothing()
    {
        var window = new TextPromptWindow("T", "M", "hola");

        Assert.True(Closes(window, () => Press(window, "Cancelar")));

        Assert.False(window.Accepted);
    }
}
