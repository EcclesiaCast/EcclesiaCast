using System.Security.Cryptography;

namespace EcclesiaCast.Core.Updates;

/// <summary>Which step of installing an update went wrong.</summary>
public enum UpdateFailure
{
    /// <summary>The installer could not be downloaded.</summary>
    Download,

    /// <summary>It was downloaded but is not what the release announced.</summary>
    Invalid,

    /// <summary>The copy of the library could not be made, so nothing was installed.</summary>
    Backup,

    /// <summary>Windows would not run the installer (the operator said no to the permission).</summary>
    Launch,
}

/// <summary>
/// An update that did not go through. The message is already in Spanish and
/// meant to be read by the operator.
/// </summary>
public sealed class UpdateException(UpdateFailure failure, string message, Exception? inner = null)
    : Exception(message, inner)
{
    public UpdateFailure Failure { get; } = failure;

    public static UpdateException DownloadFailed(Exception? inner = null) =>
        new(UpdateFailure.Download,
            "No se pudo descargar la actualización. Revisá la conexión a internet y probá de nuevo.", inner);

    public static UpdateException NotValid(string reason) =>
        new(UpdateFailure.Invalid, "La actualización descargada no es válida; no se instaló nada.",
            new InvalidDataException(reason));
}

/// <summary>
/// Brings the installer down to a folder of the program's own. It only
/// downloads: whether the file is the right one is decided by
/// <see cref="UpdateVerifier"/>, and nothing is ever run from here.
/// </summary>
public sealed class UpdateDownloader(HttpClient client)
{
    /// <summary>How long a download may go without receiving a single byte before it is given up.</summary>
    public TimeSpan StallTimeout { get; init; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Downloads <paramref name="asset"/> into <paramref name="folder"/> and returns the file's path.
    /// </summary>
    /// <param name="progress">Percent from 0 to 100, or -1 when the size is not known.</param>
    /// <exception cref="UpdateException">The download failed or was cut short.</exception>
    /// <exception cref="OperationCanceledException">The operator cancelled.</exception>
    public async Task<string> DownloadAsync(
        ReleaseAsset asset, string folder, IProgress<double>? progress, CancellationToken cancellation)
    {
        // The name comes from the network: keep only the file name, so a
        // crafted one can not walk out of the folder.
        var name = Path.GetFileName(asset.Name);
        if (string.IsNullOrWhiteSpace(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw UpdateException.NotValid($"El nombre del archivo no sirve: {asset.Name}");

        Directory.CreateDirectory(folder);
        var destination = Path.Combine(folder, name);
        var partial = destination + ".part";

        try
        {
            File.Delete(destination);

            // Something that stops answering must not leave the window waiting forever.
            using var stall = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            stall.CancelAfter(StallTimeout);

            using var response = await client.GetAsync(asset.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, stall.Token);
            response.EnsureSuccessStatusCode();

            var total = response.Content.Headers.ContentLength;
            await using (var source = await response.Content.ReadAsStreamAsync(stall.Token))
            await using (var target = new FileStream(partial, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                var buffer = new byte[81920];
                long received = 0;
                var lastPercent = -2;
                int count;

                progress?.Report(total is > 0 ? 0 : -1);
                while ((count = await source.ReadAsync(buffer, stall.Token)) > 0)
                {
                    stall.CancelAfter(StallTimeout);
                    await target.WriteAsync(buffer.AsMemory(0, count), stall.Token);
                    received += count;

                    var percent = total is > 0 ? (int)(received * 100 / total.Value) : -1;
                    if (percent != lastPercent)
                    {
                        lastPercent = percent;
                        progress?.Report(percent);
                    }
                }
            }

            File.Move(partial, destination, overwrite: true);
            return destination;
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or UnauthorizedAccessException
                                       or OperationCanceledException
                                       && !cancellation.IsCancellationRequested)
        {
            // A connection that closes early surfaces as an IOException; the
            // stall timer and the HttpClient limit surface as cancellations.
            TryDelete(partial);
            throw UpdateException.DownloadFailed(ex);
        }
        catch
        {
            TryDelete(partial);
            throw;
        }
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }
}

/// <summary>Checks a downloaded installer against what the release announced.</summary>
public static class UpdateVerifier
{
    /// <summary>
    /// Null when the file is what <paramref name="asset"/> says it is, or the
    /// reason it is not. The size is always compared; the SHA-256 only when
    /// GitHub gave one.
    /// </summary>
    public static async Task<string?> CheckAsync(string path, ReleaseAsset asset, CancellationToken cancellation = default)
    {
        var length = new FileInfo(path).Length;
        if (asset.Size > 0 && length != asset.Size)
            return $"El tamaño es {length} y tendría que ser {asset.Size}";

        if (asset.Sha256 is not null)
        {
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
            var hash = Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellation)).ToLowerInvariant();
            if (hash != asset.Sha256)
                return $"El SHA-256 es {hash} y tendría que ser {asset.Sha256}";
        }

        return null;
    }
}
