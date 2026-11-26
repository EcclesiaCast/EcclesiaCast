using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Feedback;
using EcclesiaCast.Presentation.Services;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Presentation.Tests;

public sealed class FeedbackViewModelTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("ecclesiacast-comentarios-").FullName;
    private readonly DictionarySettings _settings = new();
    private readonly List<FeedbackMessage> _sent = [];
    private bool _internet = true;
    private DateTime _now = new(2026, 11, 29, 10, 0, 0);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private FeedbackSession Session() => new(
        _settings, _folder, now: () => _now,
        send: message =>
        {
            _sent.Add(message);
            return Task.FromResult(_internet);
        });

    private FeedbackViewModel Form(FeedbackReason reason) =>
        new(_settings, reason, "Resumen de uso de prueba", message =>
        {
            _sent.Add(message);
            return Task.FromResult(_internet);
        }) { ThanksPause = TimeSpan.Zero };

    [Fact]
    public void Send_waits_for_a_comment_and_the_hint_says_why()
    {
        var form = Form(FeedbackReason.Menu);
        Assert.False(form.CanSend);
        Assert.True(form.ShowCommentHint);

        form.Comment = "   \n ";
        Assert.False(form.CanSend);

        form.Comment = "Anda bien";
        Assert.True(form.CanSend);
        Assert.False(form.ShowCommentHint);
    }

    [Fact]
    public async Task A_usage_summary_alone_is_not_sent()
    {
        var form = Form(FeedbackReason.AfterCrash);

        Assert.Equal(FeedbackSendResult.Empty, await form.SendAsync());

        Assert.Empty(_sent);
        Assert.StartsWith("Escribí qué pasó", form.Status);
        Assert.False(form.Sent);
    }

    [Fact]
    public async Task What_was_written_goes_trimmed_and_the_church_and_email_are_remembered()
    {
        var form = Form(FeedbackReason.Menu);
        form.Comment = "  Falta un botón  ";
        form.Church = " Iglesia Central ";
        form.Email = " hola@ejemplo.org ";

        Assert.Equal(FeedbackSendResult.Sent, await form.SendAsync());

        Assert.Equal(new FeedbackMessage("Algo no funcionó", "Falta un botón", "Iglesia Central", "hola@ejemplo.org", ""),
            Assert.Single(_sent));
        Assert.True(form.Sent);
        Assert.Equal("¡Gracias! Recibimos tu comentario.", form.Status);
        // Off until the window closes: it can not be sent twice.
        Assert.False(form.CanSend);
        Assert.False(form.CanSkip);

        var next = Form(FeedbackReason.Menu);
        Assert.Equal("Iglesia Central", next.Church);
        Assert.Equal("hola@ejemplo.org", next.Email);
    }

    [Fact]
    public async Task The_summary_goes_only_when_ticked()
    {
        var form = Form(FeedbackReason.Menu);
        form.Comment = "Hola";
        form.Attach = true;

        await form.SendAsync();

        Assert.Equal("Resumen de uso de prueba", Assert.Single(_sent).UsageSummary);
    }

    [Fact]
    public async Task Without_internet_the_buttons_come_back()
    {
        _internet = false;
        var form = Form(FeedbackReason.Menu);
        form.Comment = "Hola";

        Assert.Equal(FeedbackSendResult.Failed, await form.SendAsync());

        Assert.True(form.CanSend);
        Assert.True(form.CanSkip);
        Assert.False(form.Sent);
        Assert.StartsWith("No se pudo enviar", form.Status);
        Assert.Null(_settings.Get("feedback.church"));
    }

    [Theory]
    [InlineData(FeedbackReason.Menu, "Enviar comentarios", "Algo no funcionó", false, false)]
    [InlineData(FeedbackReason.Closing, "¿Querés enviarnos algún comentario?", "Otra cosa", false, true)]
    [InlineData(FeedbackReason.AfterCrash, "El programa se cerró de golpe la última vez", "Algo no funcionó", true, false)]
    public void Each_reason_has_its_words_and_choices(
        FeedbackReason reason, string heading, string kind, bool attach, bool offersNever)
    {
        var form = Form(reason);

        Assert.Equal(heading, form.Heading);
        Assert.Equal(kind, form.Kind);
        Assert.Equal(attach, form.Attach);
        Assert.Equal(offersNever, form.OffersNeverAgain);
        Assert.False(form.IsSummaryVisible);
    }

    [Theory]
    [InlineData(FeedbackReason.Closing, "1")]
    [InlineData(FeedbackReason.Menu, null)]
    public void Never_again_counts_only_on_the_notice_of_closing(FeedbackReason reason, string? stored)
    {
        var form = Form(reason);
        form.Never = true;

        form.Closed();

        Assert.Equal(stored, _settings.Get(FeedbackPolicy.NeverKey));
    }

    [Fact]
    public void The_summary_is_shown_and_hidden()
    {
        var form = Form(FeedbackReason.Menu);

        form.TogglePreview();
        Assert.True(form.IsSummaryVisible);
        form.TogglePreview();
        Assert.False(form.IsSummaryVisible);
    }

    [Fact]
    public void Closing_asks_once_per_version_after_a_real_session()
    {
        var session = Session();
        _now = _now.AddMinutes(FeedbackPolicy.MinSessionMinutes - 1);
        Assert.False(session.TakeClosingTurn());

        _now = _now.AddMinutes(1);
        Assert.True(session.TakeClosingTurn());
        Assert.False(session.TakeClosingTurn());
    }

    [Fact]
    public void A_quiet_close_asks_nothing()
    {
        var session = Session();
        _now = _now.AddHours(1);
        session.Quiet = true;

        Assert.False(session.TakeClosingTurn());
        Assert.Null(_settings.Get(FeedbackPolicy.LastVersionKey));
    }

    [Fact]
    public void A_bad_ending_is_asked_about_from_the_second_start_on()
    {
        Assert.False(Session().ShouldAskAfterCrash(lastRunCrashed: true));

        var second = Session();
        Assert.True(second.ShouldAskAfterCrash(lastRunCrashed: true));
        Assert.False(second.ShouldAskAfterCrash(lastRunCrashed: false));

        FeedbackPolicy.NeverAgain(_settings);
        Assert.False(second.ShouldAskAfterCrash(lastRunCrashed: true));
    }

    [Fact]
    public void Past_the_last_day_a_bad_ending_is_not_asked_about()
    {
        Session();
        var second = Session();
        _now = FeedbackForm.AutoPromptUntil.AddDays(1);

        Assert.False(second.ShouldAskAfterCrash(lastRunCrashed: true));
    }

    [Fact]
    public void The_minutes_of_each_run_are_added_up_in_the_summary()
    {
        var first = Session();
        _now = _now.AddMinutes(42);
        first.RecordEnd();

        var form = Session().CreateForm(FeedbackReason.Menu);

        Assert.Contains("Veces que se abrió: 2", form.Summary);
        Assert.Contains("Minutos de uso (sin contar esta sesión): 42", form.Summary);
        Assert.Contains("Windows: ", form.Summary);
    }

    private sealed class DictionarySettings : ISettingsStore
    {
        private readonly Dictionary<string, string> _values = [];

        public string? Get(string key) => _values.GetValueOrDefault(key);

        public void Set(string key, string value) => _values[key] = value;
    }
}
