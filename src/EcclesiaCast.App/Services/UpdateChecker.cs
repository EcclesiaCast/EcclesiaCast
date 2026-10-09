using System.IO;
using System.Net.Http;
using System.Windows;
using EcclesiaCast.App.Views;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Updates;
using EcclesiaCast.Data.Persistence;
using Serilog;

namespace EcclesiaCast.App.Services;

/// <summary>
/// Looks on GitHub for a newer version, a few seconds after the program opens
/// and at most once a day, and tells the operator — unless the screen is
/// being projected, when it only shows a discreet button in the top bar.
///
/// A search that goes wrong (no internet, the request limit of GitHub) never
/// bothers anyone when the program did it by itself: it is written to the
/// log and that is all. Only a search the operator asked for answers.
/// </summary>
public sealed class UpdateChecker(
    ISettingsStore settings,
    IProjectionWindowService projection,
    string dataDir,
    string dbPath) : IUpdates
{
    private HttpClient? _downloadClient;
    private UpdateWindow? _window;
    private bool _searching;

    public UpdateOffer? Pending { get; private set; }

    public event EventHandler? PendingChanged;

    public bool CheckOnStartup
    {
        get => UpdateSchedule.IsEnabled(settings);
        set => UpdateSchedule.SetEnabled(settings, value);
    }

    /// <summary>
    /// A build made to develop the program, which has no business updating
    /// itself: a Debug build, or one whose version can not be read.
    /// </summary>
    public static bool IsDevelopmentBuild =>
#if DEBUG
        true;
#else
        InstalledVersion is null;
#endif

    private static AppVersion? InstalledVersion => AppVersion.TryParse(AppInfo.Version);

    /// <summary>Starts the quiet search of every start-up. Call once, when the main window is up.</summary>
    public void StartAutomaticCheck()
    {
        // What an earlier update left in the temporary folder, off the UI thread.
        _ = Task.Run(() => UpdateInstaller.CleanUp(UpdateInstaller.DefaultWorkFolder));
        _ = RunAutomaticCheckAsync();
    }

    private async Task RunAutomaticCheckAsync()
    {
        try
        {
            // Not while the program is still getting itself ready.
            await Task.Delay(UpdateSchedule.StartDelay);

            if (!UpdateSchedule.ShouldCheck(settings, DateTimeOffset.Now, IsDevelopmentBuild)
                || InstalledVersion is not { } installed)
                return;

            var offer = await SearchAsync(installed, UpdateSchedule.Skipped(settings));
            SetPending(offer);

            if (offer is null)
                return;

            Log.Information("Hay una versión nueva: {Version}", offer.Version);
            if (CanInterrupt())
                ShowWindow(offer);
        }
        catch (UpdateCheckException ex)
        {
            Log.Information(ex, "No se pudo buscar una versión nueva ({Reason})", ex.Message);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Falló la búsqueda automática de una versión nueva");
        }
    }

    /// <summary>
    /// Nothing opens by itself while the output is on: the notice would land
    /// on the operator in the middle of a song. It also waits for the main
    /// window to be the one in front, so it does not cover a dialog the
    /// operator is typing into; the button is there for whenever they want it.
    /// </summary>
    private bool CanInterrupt() =>
        !projection.IsOutputVisible
        && Application.Current?.MainWindow is { IsActive: true, WindowState: not WindowState.Minimized };

    public async Task CheckNowAsync()
    {
        if (_searching)
            return;

        _searching = true;
        try
        {
            if (InstalledVersion is not { } installed)
            {
                Message("No se pudo saber qué versión de EcclesiaCast tenés instalada, así que no se puede buscar una nueva.");
                return;
            }

            UpdateOffer? offer;
            try
            {
                // A search the operator asked for shows every newer version,
                // the skipped one included.
                offer = await SearchAsync(installed, skipped: null);
            }
            catch (UpdateCheckException ex)
            {
                Log.Information(ex, "No se pudo buscar actualizaciones ({Reason})", ex.Message);
                Message(ex.Failure == UpdateCheckFailure.NoConnection
                    ? "No se pudo buscar: revisá la conexión a internet."
                    : "No se pudo buscar: GitHub no contestó como se esperaba. Probá de nuevo más tarde.");
                return;
            }

            SetPending(offer);
            if (offer is null)
                Message($"Tenés la última versión ({installed}).");
            else
                ShowWindow(offer);
        }
        finally
        {
            _searching = false;
        }
    }

    public void ShowPending()
    {
        if (Pending is { } offer)
            ShowWindow(offer);
    }

    private async Task<UpdateOffer?> SearchAsync(AppVersion installed, AppVersion? skipped)
    {
        using var client = ReleaseFeed.CreateClient(AppInfo.Version);
        var releases = await new ReleaseFeed(client).FetchAsync();

        // Only now: a day without internet does not use up the day's search.
        UpdateSchedule.MarkChecked(settings, DateTimeOffset.Now);
        return UpdatePicker.Choose(installed, releases, skipped);
    }

    private void SetPending(UpdateOffer? offer)
    {
        if (Equals(Pending?.Version, offer?.Version))
        {
            Pending = offer;
            return;
        }

        Pending = offer;
        PendingChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ShowWindow(UpdateOffer offer)
    {
        if (_window is not null)
        {
            _window.Activate();
            return;
        }

        var window = new UpdateWindow(offer, AppInfo.Version, CreateInstaller(), () => projection.IsOutputVisible)
        {
            Owner = Application.Current.MainWindow,
        };
        window.SkipRequested += (_, _) =>
        {
            UpdateSchedule.Skip(settings, offer.Version);
            Log.Information("Se salteó la versión {Version}", offer.Version);
            SetPending(null);
        };
        window.Closed += (_, _) => _window = null;

        _window = window;
        window.Show();
    }

    private UpdateInstaller CreateInstaller()
    {
        // A download takes as long as it takes: no overall time limit here,
        // the downloader gives up on a connection that goes silent.
        _downloadClient ??= CreateDownloadClient();

        var reader = new WindowsSignatureReader();
        return new UpdateInstaller(
            new UpdateDownloader(_downloadClient),
            reader,
            () => SignaturePolicy.OfInstalled(
                reader,
                Environment.ProcessPath,
                Path.Combine(AppContext.BaseDirectory, "unins000.exe")),
            version => Log.Information(
                "Copia de la biblioteca antes de actualizar: {Path}",
                DatabaseBackup.CreateBeforeUpdate(dbPath, Path.Combine(dataDir, "respaldos"), version.ToString())),
            new ProcessInstallerLauncher(),
            UpdateInstaller.DefaultWorkFolder);
    }

    private static HttpClient CreateDownloadClient()
    {
        var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"EcclesiaCast/{AppInfo.Version}");
        return client;
    }

    private static void Message(string text) =>
        MessageBox.Show(text, "Actualizaciones", MessageBoxButton.OK, MessageBoxImage.Information);
}
