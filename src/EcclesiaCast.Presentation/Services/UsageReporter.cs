using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Diagnostics;
using Serilog;

namespace EcclesiaCast.Presentation.Services;

/// <summary>
/// Tells the count of use (see <see cref="UsageCount"/>) that this
/// installation projected something today. It runs on its own, never waits on
/// the screen and never complains: without internet, or if the service does
/// not answer, today simply goes uncounted and it tries again next time.
/// </summary>
public static class UsageReporter
{
    private static readonly HttpClient Client = CreateClient();
    private static int _sending;

    // After a failed try, the next one waits: without internet every slide
    // tried again (15/11: 104 tries in one morning).
    private static readonly TimeSpan RetryAfter = TimeSpan.FromHours(1);
    private static DateTimeOffset _nextTry = DateTimeOffset.MinValue;

    /// <summary>
    /// This copy, as the count sees it: a test version, a test library or a
    /// loose folder are not counted (they would count us, not the churches).
    /// Worked out once; none of it changes while the program runs.
    /// </summary>
    private static readonly Lazy<UsageCopy> ThisCopy = new(() =>
    {
        var copy = UsageCopy.Of(
            AppInfo.Version,
            AppInfo.IsDevelopmentBuild,
            Environment.GetEnvironmentVariable("ECCLESIACAST_DATA_DIR"),
            // Only the Windows installer leaves an uninstaller to tell an
            // installed copy by; the Mac one has nothing of the kind.
            !OperatingSystem.IsWindows()
            || File.Exists(Path.Combine(AppContext.BaseDirectory, UsageCopy.UninstallerFile)));
        if (!copy.Counts)
            Log.Information("Esta copia no se cuenta en el uso: {Copy}", copy);
        return copy;
    });

    /// <summary>Called whenever something goes on the air. Does nothing after the first time each day.</summary>
    public static void NoteProjected(ISettingsStore settings)
    {
        var now = DateTimeOffset.Now;
        if (now < _nextTry || !UsageCount.ShouldSend(settings, now, ThisCopy.Value))
            return;

        // One send at a time: the next slide comes a second later.
        if (Interlocked.Exchange(ref _sending, 1) == 1)
            return;

        var ping = UsageCount.Ping(settings, now, AppInfo.Version);
        _ = Task.Run(async () =>
        {
            try
            {
                using var response = await Client.PostAsJsonAsync(AppInfo.CurrentUsageCountUrl, new
                {
                    instalacion = ping.Install,
                    dia = ping.Day,
                    version = ping.Version,
                    iglesia = ping.Church,
                });

                if (response.IsSuccessStatusCode)
                {
                    UsageCount.MarkSent(settings, now);
                    Log.Information("Uso del día contado ({Day})", ping.Day);
                }
                else
                {
                    _nextTry = DateTimeOffset.Now + RetryAfter;
                    Log.Information("El contador de uso respondió {Status}", (int)response.StatusCode);
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _nextTry = DateTimeOffset.Now + RetryAfter;
                Log.Information("No se pudo contar el uso del día ({Reason}); vuelve a probar en una hora", ex.Message);
            }
            finally
            {
                Interlocked.Exchange(ref _sending, 0);
            }
        });
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"EcclesiaCast/{AppInfo.Version}");
        return client;
    }
}
