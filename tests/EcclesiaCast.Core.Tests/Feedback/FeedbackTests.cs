using System.Net;
using EcclesiaCast.Core.Feedback;
using EcclesiaCast.Core.Tests.Updates;

namespace EcclesiaCast.Core.Tests.Feedback;

public class FeedbackTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), $"ecclesiacast-feedback-test-{Guid.NewGuid():N}");

    public void Dispose()
    {
        try { Directory.Delete(_folder, recursive: true); } catch (IOException) { }
    }

    private sealed class FakeHandler(HttpStatusCode status = HttpStatusCode.OK, Exception? fail = null) : HttpMessageHandler
    {
        public string? Body { get; private set; }

        public Uri? Url { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (fail is not null)
                throw fail;
            Url = request.RequestUri;
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status);
        }
    }

    [Fact]
    public async Task Send_posts_every_field_to_the_form_with_its_id()
    {
        var handler = new FakeHandler();
        var sender = new FeedbackSender(new HttpClient(handler));

        var ok = await sender.SendAsync(new FeedbackMessage("Tengo una idea", "Que ande más rápido", "Pan de Vida", "a@b.com", "Versión: 1.1.0"));

        Assert.True(ok);
        Assert.Equal(FeedbackForm.SubmitUrl, handler.Url!.ToString());
        var body = Uri.UnescapeDataString(handler.Body!.Replace('+', ' '));
        Assert.Contains($"{FeedbackForm.KindField}=Tengo una idea", body);
        Assert.Contains($"{FeedbackForm.CommentField}=Que ande más rápido", body);
        Assert.Contains($"{FeedbackForm.ChurchField}=Pan de Vida", body);
        Assert.Contains($"{FeedbackForm.EmailField}=a@b.com", body);
        Assert.Contains($"{FeedbackForm.UsageField}=Versión: 1.1.0", body);
    }

    [Fact]
    public async Task Send_replaces_an_unknown_kind_with_the_last_option()
    {
        var handler = new FakeHandler();
        await new FeedbackSender(new HttpClient(handler)).SendAsync(new FeedbackMessage("inventado", "x", "", "", ""));

        Assert.Contains($"{FeedbackForm.KindField}={Uri.EscapeDataString("Otra cosa").Replace("%20", "+")}", handler.Body);
    }

    [Fact]
    public async Task Send_cuts_a_summary_that_is_too_long_instead_of_failing()
    {
        var handler = new FakeHandler();
        await new FeedbackSender(new HttpClient(handler)).SendAsync(new FeedbackMessage("Otra cosa", "x", "", "", new string('a', 50_000)));

        Assert.True(handler.Body!.Length < 25_000);
    }

    [Fact]
    public async Task Send_says_no_when_the_server_refuses_or_there_is_no_connection()
    {
        Assert.False(await new FeedbackSender(new HttpClient(new FakeHandler(HttpStatusCode.BadRequest)))
            .SendAsync(new FeedbackMessage("Otra cosa", "x", "", "", "")));
        Assert.False(await new FeedbackSender(new HttpClient(new FakeHandler(fail: new HttpRequestException())))
            .SendAsync(new FeedbackMessage("Otra cosa", "x", "", "", "")));
        Assert.False(await new FeedbackSender(new HttpClient(new FakeHandler(fail: new TaskCanceledException())))
            .SendAsync(new FeedbackMessage("Otra cosa", "x", "", "", "")));
    }

    [Fact]
    public void Usage_counts_opens_and_minutes_and_keeps_the_same_id()
    {
        var settings = new MemorySettings();
        var stats = new UsageStats(settings, () => new DateTime(2026, 11, 2));

        stats.RecordStart();
        var id = settings.Values["usage.id"];
        stats.RecordStart();
        stats.AddMinutes(50);
        stats.AddMinutes(0);
        stats.AddMinutes(-3);

        Assert.Equal(2, stats.Opens);
        Assert.Equal(50, stats.Minutes);
        Assert.Equal(id, settings.Values["usage.id"]);
        Assert.Equal("2026-11-02", settings.Values["usage.first"]);
    }

    [Fact]
    public void Summary_has_the_numbers_and_nothing_of_the_library()
    {
        var settings = new MemorySettings();
        var stats = new UsageStats(settings, () => new DateTime(2026, 11, 2));
        stats.RecordStart();
        stats.AddMinutes(30);

        var text = stats.BuildSummary("1.1.0", "Windows 11", 12, ["[ERR] algo falló"]);

        Assert.Contains("Versión: 1.1.0", text);
        Assert.Contains("Veces que se abrió: 1", text);
        Assert.Contains("Minutos de uso (sin contar esta sesión): 30", text);
        Assert.Contains("Minutos de esta sesión: 12", text);
        Assert.Contains("Errores recientes (1):", text);
        Assert.Contains("[ERR] algo falló", text);
    }

    [Fact]
    public void Summary_says_so_when_there_are_no_errors()
    {
        var text = new UsageStats(new MemorySettings()).BuildSummary("1.1.0", "Windows 11", 0, []);

        Assert.Contains("Errores recientes: ninguno", text);
    }

    [Fact]
    public void The_notice_waits_for_a_real_session_and_asks_once_per_version()
    {
        var settings = new MemorySettings();
        var now = new DateTime(2026, 11, 2);

        Assert.False(FeedbackPolicy.ShouldAskOnClose(settings, "1.1.0", now, sessionMinutes: 2));
        Assert.True(FeedbackPolicy.ShouldAskOnClose(settings, "1.1.0", now, sessionMinutes: 40));

        FeedbackPolicy.MarkAsked(settings, "1.1.0");
        Assert.False(FeedbackPolicy.ShouldAskOnClose(settings, "1.1.0", now, 40));
        Assert.True(FeedbackPolicy.ShouldAskOnClose(settings, "1.2.0", now, 40));
    }

    [Fact]
    public void Never_again_silences_every_version()
    {
        var settings = new MemorySettings();
        FeedbackPolicy.NeverAgain(settings);

        Assert.False(FeedbackPolicy.ShouldAskOnClose(settings, "9.9.9", new DateTime(2026, 11, 2), 60));
    }

    [Fact]
    public void The_notice_expires_by_itself()
    {
        Assert.False(FeedbackPolicy.ShouldAskOnClose(
            new MemorySettings(), "1.1.0", FeedbackForm.AutoPromptUntil.AddDays(1), 60));
    }

    [Fact]
    public void Crash_marker_reports_a_run_that_never_ended()
    {
        var first = new CrashMarker(_folder);
        Assert.False(first.Begin());

        // The program was killed: nobody called End.
        Assert.True(new CrashMarker(_folder).Begin());

        var clean = new CrashMarker(_folder);
        clean.End();
        Assert.False(new CrashMarker(_folder).Begin());
    }

    [Fact]
    public void Recent_errors_come_newest_first_without_the_users_folder()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllLines(Path.Combine(_folder, "ecclesiacast-20261101.log"),
        [
            "2026-11-01 10:00:00 [INF] todo bien",
            "2026-11-01 10:01:00 [ERR] viejo C:\\Users\\Ana\\Videos\\a.mp4",
        ]);
        File.WriteAllLines(Path.Combine(_folder, "ecclesiacast-20261102.log"),
        [
            "2026-11-02 09:00:00 [ERR] nuevo en C:\\Users\\Ana\\Documents",
            "   at Algo.Metodo()",
            "2026-11-02 09:05:00 [DBG] ruido",
        ]);

        var errors = RecentErrors.Read(_folder, "C:\\Users\\Ana");

        Assert.Equal(2, errors.Count);
        Assert.Contains("nuevo en %USERPROFILE%\\Documents", errors[0]);
        Assert.Contains("viejo %USERPROFILE%\\Videos", errors[1]);
        Assert.DoesNotContain(errors, e => e.Contains("Ana"));
    }

    [Fact]
    public void Recent_errors_of_a_missing_folder_is_empty()
    {
        Assert.Empty(RecentErrors.Read(Path.Combine(_folder, "no-existe"), "C:\\Users\\Ana"));
    }
}
