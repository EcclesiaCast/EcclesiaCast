namespace EcclesiaCast.Core.Updates;

/// <summary>The stages <see cref="UpdateInstaller.PrepareAsync"/> goes through, in order.</summary>
public enum UpdateStep
{
    Downloading,
    Verifying,
    BackingUp,
}

/// <param name="Percent">0 to 100 while downloading; -1 when there is no way to tell how far along it is.</param>
public sealed record UpdateProgress(UpdateStep Step, double Percent);

/// <summary>
/// Runs the installer. Behind an interface so that no test ever starts one:
/// the tests give it a launcher that does nothing.
/// </summary>
public interface IInstallerLauncher
{
    /// <exception cref="UpdateException">Windows would not start it.</exception>
    void Launch(string installerPath, string arguments);
}

/// <summary>
/// Everything that happens between «Actualizar ahora» and the moment the
/// installer starts: download, check, and a copy of the library. The order is
/// the point of it: nothing is run until the file has been proven to be the
/// one that was announced, and the library is copied before the new version,
/// which may migrate it, gets a chance to touch it.
/// </summary>
public sealed class UpdateInstaller(
    UpdateDownloader downloader,
    ISignatureReader signatures,
    Func<SignatureInfo> installedSignature,
    Action<AppVersion> backUpLibrary,
    IInstallerLauncher launcher,
    string workFolder)
{
    /// <summary>
    /// What the installer is started with. It shows only its progress bar
    /// (/SILENT), does not ask anything (/SP-), can not be cancelled halfway
    /// (/NOCANCEL), closes the program if it is still open, and /RELANZAR is
    /// the word the installer script looks for to open the program again.
    /// </summary>
    public const string InstallerArguments =
        "/SILENT /SP- /NOCANCEL /CLOSEAPPLICATIONS /NORESTARTAPPLICATIONS /RELANZAR";

    /// <summary>Where installers are downloaded: a folder of the program's own inside the temporary one.</summary>
    public static string DefaultWorkFolder => Path.Combine(Path.GetTempPath(), "EcclesiaCast-actualizacion");

    /// <summary>
    /// Downloads and checks the installer and copies the library. Returns the
    /// path of the installer, ready to <see cref="Launch"/>.
    /// </summary>
    /// <exception cref="UpdateException">A step failed; nothing was installed.</exception>
    public async Task<string> PrepareAsync(
        UpdateOffer offer, IProgress<UpdateProgress>? progress, CancellationToken cancellation)
    {
        var path = await downloader.DownloadAsync(
            offer.Installer, workFolder,
            progress is null ? null : new Progress<double>(percent => progress.Report(new UpdateProgress(UpdateStep.Downloading, percent))),
            cancellation);

        progress?.Report(new UpdateProgress(UpdateStep.Verifying, -1));
        try
        {
            await VerifyAsync(path, offer, cancellation);
        }
        catch
        {
            // A file that did not pass must not stay around to be run by mistake.
            TryDelete(path);
            throw;
        }

        progress?.Report(new UpdateProgress(UpdateStep.BackingUp, -1));
        try
        {
            backUpLibrary(offer.Version);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Whatever the reason (a full disk, a locked file, SQLite saying
            // no): without a copy there is no way back from a migration that
            // goes wrong, so the update does not go ahead.
            TryDelete(path);
            throw new UpdateException(UpdateFailure.Backup,
                "No se pudo hacer la copia de seguridad de la biblioteca; no se instaló nada.", ex);
        }

        return path;
    }

    /// <summary>Starts the installer. The program should close right after this returns.</summary>
    public void Launch(string installerPath) => launcher.Launch(installerPath, InstallerArguments);

    /// <summary>Deletes what is left of an earlier update. Called when the program starts.</summary>
    public static void CleanUp(string folder)
    {
        try
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The installer of an update still running is in there: next time.
        }
    }

    private async Task VerifyAsync(string path, UpdateOffer offer, CancellationToken cancellation)
    {
        var problem = await UpdateVerifier.CheckAsync(path, offer.Installer, cancellation);
        if (problem is not null)
            throw UpdateException.NotValid(problem);

        var installed = installedSignature();
        var candidate = signatures.Read(path);
        if (!SignaturePolicy.Accepts(installed, candidate))
        {
            throw UpdateException.NotValid(
                $"La firma no coincide: el programa instalado está firmado por «{installed.Signer}» "
                + (candidate.IsSigned
                    ? $"y el instalador por «{candidate.Signer}» (confiable: {candidate.IsTrusted})"
                    : "y el instalador no está firmado"));
        }
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }
}
