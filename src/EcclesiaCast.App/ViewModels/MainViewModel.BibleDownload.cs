using System.IO;
using System.Net;
using System.Net.Http;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using EcclesiaCast.App.Services;
using Serilog;

namespace EcclesiaCast.App.ViewModels;

public sealed partial class MainViewModel
{
    // ── Bajar Biblias desde el programa ──────────────────────────
    // The Bibles are not in the installer: whoever wants them asks for them
    // here, and only they download them. The file is checked against the hash
    // the program knows, then goes through the same window as «Importar
    // paquete de Biblias», where the operator ticks the versions to add.

    private bool _downloadingBiblePack;

    /// <summary>Biblia → 📥 → «Bajar Biblias de internet…», and the same in the Archivo menu.</summary>
    [RelayCommand]
    private async Task DownloadBiblePack()
    {
        if (_downloadingBiblePack)
            return;

        var answer = MessageBox.Show(
            $"Se va a bajar un paquete con las Biblias {AppInfo.BiblePackContents} (unos 6 MB). "
            + "Después elegís cuáles agregar. ¿Seguimos?",
            "Bajar Biblias", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (answer != MessageBoxResult.Yes)
            return;

        _downloadingBiblePack = true;
        var work = Path.Combine(Path.GetTempPath(), "ecclesiacast-biblias-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(work);
            var package = Path.Combine(work, "Biblias" + EcclesiaCast.Core.Bible.BiblePackage.Extension);
            await DownloadBiblePackFile(package);
            StatusText = "Paquete de Biblias bajado.";
            await ImportBiblePackageFileAsync(package);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            Log.Information("El paquete de Biblias todavía no está publicado ({Url})", AppInfo.BiblePackUrl);
            StatusText = "Las Biblias todavía no están para bajar. Si tenés un paquete (.ecbiblias), usá «Importar paquete de Biblias…».";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException
                                       or HttpRequestException or TaskCanceledException)
        {
            Log.Warning(ex, "No se pudo bajar el paquete de Biblias");
            StatusText = ex is InvalidDataException
                ? "El paquete de Biblias bajado no es el esperado: no se agregó nada."
                : "No se pudo bajar el paquete de Biblias. Revisá la conexión y probá de nuevo.";
        }
        finally
        {
            _downloadingBiblePack = false;
            try { Directory.Delete(work, recursive: true); } catch (IOException) { /* se limpia solo */ }
        }
    }

    private async Task DownloadBiblePackFile(string destination)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"EcclesiaCast/{AppInfo.Version}");
        using var response = await client.GetAsync(AppInfo.BiblePackUrl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength;
        await using (var source = await response.Content.ReadAsStreamAsync())
        await using (var target = File.Create(destination))
        {
            var buffer = new byte[81920];
            long read = 0;
            var lastPercent = -1;
            int count;
            while ((count = await source.ReadAsync(buffer)) > 0)
            {
                await target.WriteAsync(buffer.AsMemory(0, count));
                read += count;
                var percent = total is > 0 ? (int)(read * 100 / total.Value) : 0;
                if (percent != lastPercent)
                {
                    lastPercent = percent;
                    StatusText = $"Bajando las Biblias… {percent}%";
                }
            }
        }

        await using var check = File.OpenRead(destination);
        var hash = Convert.ToHexString(await System.Security.Cryptography.SHA256.HashDataAsync(check));
        if (!hash.Equals(AppInfo.BiblePackSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("El paquete bajado no es el esperado.");
    }
}
