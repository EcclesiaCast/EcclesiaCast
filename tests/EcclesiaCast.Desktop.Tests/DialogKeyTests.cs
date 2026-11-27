using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Desktop.Views;
using EcclesiaCast.Presentation.Platform;
using EcclesiaCast.Presentation.Services;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Desktop.Tests;

/// <summary>Enter and Esc in the simple dialogs do what they do in WPF.</summary>
public sealed class DialogKeyTests
{
    private static Func<bool> Open(Window window)
    {
        var closed = false;
        window.Closed += (_, _) => closed = true;
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return () => closed;
    }

    private static void Hit(Window window, PhysicalKey key)
    {
        var closed = false;
        void Closed(object? sender, EventArgs e) => closed = true;
        window.Closed += Closed;
        window.KeyPressQwerty(key, RawInputModifiers.None);
        // A closed window takes no more keys.
        if (!closed)
            window.KeyReleaseQwerty(key, RawInputModifiers.None);
        window.Closed -= Closed;
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void Prompt_enter_accepts_and_escape_cancels()
    {
        var accept = new TextPromptWindow("T", "M", "hola");
        var accepted = Open(accept);
        Hit(accept, PhysicalKey.Enter);
        Assert.True(accepted());
        Assert.True(accept.Accepted);

        var cancel = new TextPromptWindow("T", "M", "hola");
        var cancelled = Open(cancel);
        Hit(cancel, PhysicalKey.Escape);
        Assert.True(cancelled());
        Assert.False(cancel.Accepted);
    }

    [AvaloniaFact]
    public void Choice_enter_takes_the_first_and_escape_none()
    {
        var first = new ChoiceWindow("T", "M", ["Uno", "Dos"]);
        var done = Open(first);
        Hit(first, PhysicalKey.Enter);
        Assert.True(done());
        Assert.Equal(0, first.Chosen);

        var none = new ChoiceWindow("T", "M", ["Uno", "Dos"]);
        var left = Open(none);
        Hit(none, PhysicalKey.Escape);
        Assert.True(left());
        Assert.Equal(-1, none.Chosen);
    }

    [AvaloniaFact]
    public void Notice_enter_takes_the_default_and_escape_cancels()
    {
        NoticeWindow Question() => new("M", "T", NoticeKind.Question, ["Cancelar", "No", "Sí"], 2, 0);

        var enter = Question();
        var done = Open(enter);
        Hit(enter, PhysicalKey.Enter);
        Assert.True(done());
        Assert.Equal(2, enter.Chosen);

        var escape = Question();
        var left = Open(escape);
        Hit(escape, PhysicalKey.Escape);
        Assert.True(left());
        Assert.Equal(0, escape.Chosen);
    }

    [AvaloniaTheory]
    [InlineData("MinutesBox")]
    [InlineData("ClockBox")]
    public void Countdown_enter_in_a_time_box_starts_it(string box)
    {
        var countdown = new CountdownViewModel(CountdownSettings.Default, false);
        var window = new CountdownWindow(countdown);
        var closed = Open(window);
        window.FindControl<TextBox>(box)!.Focus();
        Dispatcher.UIThread.RunJobs();

        Hit(window, PhysicalKey.Enter);

        Assert.True(closed());
        Assert.True(window.Accepted);
    }

    [AvaloniaFact]
    public void Countdown_escape_cancels()
    {
        var window = new CountdownWindow(new CountdownViewModel(CountdownSettings.Default, false));
        var closed = Open(window);
        Hit(window, PhysicalKey.Escape);
        Assert.True(closed());
        Assert.False(window.Accepted);
    }

    [AvaloniaFact]
    public void Feedback_enter_in_the_comment_writes_a_line_and_escape_waits_while_sending()
    {
        var answer = new TaskCompletionSource<bool>();
        var form = new FeedbackViewModel(new MemorySettings(), FeedbackReason.Menu, "r", _ => answer.Task)
        {
            ThanksPause = TimeSpan.Zero,
        };
        var window = new FeedbackWindow(form);
        var closed = Open(window);
        var comment = window.FindControl<TextBox>("CommentBox")!;
        comment.Focus();
        comment.Text = "hola";
        Dispatcher.UIThread.RunJobs();

        Hit(window, PhysicalKey.Enter);
        Assert.False(closed());
        Assert.False(form.IsSending);

        DialogWindowTests.Press(window, "Enviar");
        Hit(window, PhysicalKey.Escape);
        Assert.False(closed());

        answer.SetResult(false);
        Dispatcher.UIThread.RunJobs();
        Hit(window, PhysicalKey.Escape);
        Assert.True(closed());
    }

    [AvaloniaFact]
    public void About_transitions_and_quick_text_close_with_escape()
    {
        Window[] windows =
        [
            new AboutWindow(new RecordingShell(), "1"),
            new TransitionsWindow(new MemorySettings()),
            new QuickTextWindow("hola"),
        ];
        foreach (var window in windows)
        {
            var closed = Open(window);
            Hit(window, PhysicalKey.Escape);
            Assert.True(closed(), window.GetType().Name);
        }
    }
}
