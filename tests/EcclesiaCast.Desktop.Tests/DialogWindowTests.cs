using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using EcclesiaCast.Desktop.Views;

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
    public void Cancelling_a_prompt_accepts_nothing()
    {
        var window = new TextPromptWindow("T", "M", "hola");

        Assert.True(Closes(window, () => Press(window, "Cancelar")));

        Assert.False(window.Accepted);
    }
}
