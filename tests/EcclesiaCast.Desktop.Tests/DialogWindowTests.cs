using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Desktop.Controls;
using EcclesiaCast.Desktop.Views;
using EcclesiaCast.Presentation.Platform;
using EcclesiaCast.Presentation.Services;
using EcclesiaCast.Presentation.ViewModels;

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
    public void The_about_window_opens_its_links_and_closes()
    {
        var shell = new RecordingShell();
        var window = new AboutWindow(shell, "1.5.0");

        Assert.True(Closes(window, () =>
        {
            Press(window, "github.com/EcclesiaCast");
            Press(window, "colaborar con EcclesiaCast");
            Press(window, "Cerrar");
        }));

        Assert.Equal([AboutWindow.GitHubUrl, AppInfo.DonateUrl], shell.Opened);
    }

    [AvaloniaFact]
    public void A_number_field_steps_reads_and_holds_its_range()
    {
        var field = new NumericField { Minimum = 0, Maximum = 10, Step = 0.1, Decimals = 1 };
        var changes = 0;
        field.ValueChanged += (_, _) => changes++;
        var window = new Window { Content = field };
        window.Show();
        var entry = field.FindControl<TextBox>("Entry")!;

        field.SetSilently(0.5);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0, changes);

        field.FindControl<RepeatButton>("Up")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0.6, field.Value, 6);

        entry.Text = "2,5";
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(2.5, field.Value);

        entry.Text = "99";
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(10, field.Value);
        window.Close();
    }

    [AvaloniaFact]
    public void Saving_the_transitions_applies_them_and_cancel_does_not()
    {
        var before = TransitionSettings.Current;
        try
        {
            var settings = new MemorySettings();
            var cancelled = new TransitionsWindow(settings);
            Assert.True(Closes(cancelled, () => Press(cancelled, "Cancelar")));
            Assert.Null(settings.Get(OutputTransitions.SettingKey));

            var window = new TransitionsWindow(settings);
            Assert.True(Closes(window, () =>
            {
                window.FindControl<NumericField>("LogoField")!.Value = 2;
                Press(window, "Guardar");
            }));

            Assert.Equal(2, OutputTransitions.Parse(settings.Get(OutputTransitions.SettingKey)).LogoSeconds);
        }
        finally
        {
            TransitionSettings.Set(before, new MemorySettings());
        }
    }

    [AvaloniaFact]
    public async Task The_comments_wait_for_a_text_then_send_and_close()
    {
        var sent = new List<string>();
        var form = new FeedbackViewModel(
            new MemorySettings(), FeedbackReason.Menu, "resumen",
            message =>
            {
                sent.Add(message.Comment);
                return Task.FromResult(true);
            })
        { ThanksPause = TimeSpan.Zero };
        var window = new FeedbackWindow(form);
        var closed = false;
        window.Closed += (_, _) => closed = true;
        window.Show();
        var send = window.GetLogicalDescendants().OfType<Button>().Single(b => Equals(b.Content, "Enviar"));
        Assert.False(send.IsEffectivelyEnabled);

        window.FindControl<TextBox>("CommentBox")!.Text = "Anda bien";
        Dispatcher.UIThread.RunJobs();
        Assert.True(send.IsEffectivelyEnabled);
        Assert.False(window.FindControl<TextBlock>("CommentHint")!.IsVisible);

        Press(window, "Ver qué se envía");
        Press(window, "Enviar");
        await Task.Delay(50);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(["Anda bien"], sent);
        Assert.True(closed);
        Assert.True(form.IsSummaryVisible);
    }

    [AvaloniaFact]
    public void Cancelling_a_prompt_accepts_nothing()
    {
        var window = new TextPromptWindow("T", "M", "hola");

        Assert.True(Closes(window, () => Press(window, "Cancelar")));

        Assert.False(window.Accepted);
    }
}
