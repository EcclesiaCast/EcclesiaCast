using System.IO;
using System.Net.Http;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Bible;
using Serilog;

namespace EcclesiaCast.App.ViewModels;

public sealed partial class MainViewModel
{
    // ── Bajar Biblias desde el programa ──────────────────────────
    // The Bibles are not in the installer: whoever wants one asks for it here,
    // and only that computer downloads it. The program reads the list
    // (AppInfo.BibleCatalogUrl), shows it by language with a tick box each,
    // downloads only what was ticked, checks every file against the SHA-256 the
    // list gives, and adds it like any imported package.

    private bool _downloadingBibles;

    /// <summary>Biblia → 📥 → «Bajar Biblias de internet…», and the same in the Archivo menu.</summary>
    [RelayCommand]
    private async Task DownloadBiblePack()
    {
        if (_downloadingBibles)
            return;

        if (!Uri.TryCreate(AppInfo.CurrentBibleCatalogUrl, UriKind.Absolute, out var catalogUrl)
            || !BibleCatalog.IsAllowed(catalogUrl))
        {
            StatusText = "Todavía no hay Biblias para bajar. Si tenés un paquete (.ecbiblias), usá «Importar paquete de Biblias…».";
            return;
        }

        _downloadingBibles = true;
        var work = Path.Combine(Path.GetTempPath(), "ecclesiacast-biblias-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd($"EcclesiaCast/{AppInfo.Version}");

            StatusText = "Buscando las Biblias para bajar…";
            var items = BibleCatalog.Parse(await ReadCatalog(client, catalogUrl), catalogUrl);
            if (items.Count == 0)
            {
                StatusText = "Todavía no hay Biblias para bajar.";
                return;
            }

            var installed = _bibles.GetVersions();
            var marks = BibleInstallPlanner.Offer(items.Select(i => i.AsEntry(i.Abbreviation)), installed)
                .Select(c => c.AlreadyInstalled)
                .ToList();
            var chosen = _biblePackageDialog.ChooseToDownload(items, marks);
            if (chosen is null || chosen.Count == 0)
            {
                StatusText = string.Empty;
                return;
            }

            Directory.CreateDirectory(work);
            var hadVersions = BibleVersionOptions.Count > 0;
            var imported = new List<string>();
            var failed = new List<string>();
            for (var n = 0; n < chosen.Count; n++)
            {
                var item = chosen[n];
                try
                {
                    var file = Path.Combine(work, $"{n}{BiblePackage.Extension}");
                    await DownloadBible(client, item, file, n + 1, chosen.Count);
                    StatusText = $"Agregando {item.Abbreviation}…";
                    imported.Add(await Task.Run(() => ImportDownloadedBible(file, item)));
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException
                                               or FormatException or HttpRequestException or TaskCanceledException)
                {
                    Log.Warning(ex, "No se pudo bajar la Biblia {Name} de {Url}", item.Name, item.Url);
                    failed.Add(item.Abbreviation);
                }
            }

            LoadBibleVersions();
            if (!hadVersions && imported.Count > 0)
                RefreshAfterVersionChange();

            StatusText = imported.Count switch
            {
                0 => "No se agregó ninguna Biblia.",
                1 => $"Se agregó la Biblia «{imported[0]}».",
                _ => $"Se agregaron {imported.Count} Biblias: {string.Join(", ", imported)}.",
            };
            if (failed.Count > 0)
                StatusText += $" No se pudo bajar: {string.Join(", ", failed)} (revisá la conexión y probá de nuevo).";
            Log.Information("Biblias bajadas: {Imported} (fallaron {Failed})",
                string.Join(", ", imported), string.Join(", ", failed));
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or FormatException
                                       or HttpRequestException or TaskCanceledException)
        {
            Log.Warning(ex, "No se pudo leer la lista de Biblias de {Url}", catalogUrl);
            StatusText = ex is FormatException
                ? ex.Message
                : "No se pudo ver qué Biblias hay para bajar. Revisá la conexión y probá de nuevo.";
        }
        finally
        {
            _downloadingBibles = false;
            try { if (Directory.Exists(work)) Directory.Delete(work, recursive: true); } catch (IOException) { /* se limpia solo */ }
        }
    }

    private static async Task<string> ReadCatalog(HttpClient client, Uri url)
    {
        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength is > BibleCatalog.MaxBytes)
            throw new InvalidDataException("La lista de Biblias es demasiado grande.");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var limited = new MemoryStream();
        var buffer = new byte[16384];
        int count;
        while ((count = await stream.ReadAsync(buffer)) > 0)
        {
            limited.Write(buffer, 0, count);
            if (limited.Length > BibleCatalog.MaxBytes)
                throw new InvalidDataException("La lista de Biblias es demasiado grande.");
        }

        return System.Text.Encoding.UTF8.GetString(limited.ToArray());
    }

    /// <summary>Downloads one Bible and checks it is exactly the file of the list (size and SHA-256).</summary>
    private async Task DownloadBible(HttpClient client, BibleCatalogItem item, string destination, int number, int total)
    {
        using var response = await client.GetAsync(item.Url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        await using (var source = await response.Content.ReadAsStreamAsync())
        await using (var target = File.Create(destination))
        {
            var buffer = new byte[81920];
            long read = 0;
            var lastPercent = -1;
            int count;
            while ((count = await source.ReadAsync(buffer)) > 0)
            {
                read += count;
                if (read > item.Size)
                    throw new InvalidDataException("La Biblia bajada no es la esperada (más grande).");
                await target.WriteAsync(buffer.AsMemory(0, count));
                var percent = (int)(read * 100 / item.Size);
                if (percent != lastPercent)
                {
                    lastPercent = percent;
                    StatusText = total == 1
                        ? $"Bajando {item.Abbreviation}… {percent}%"
                        : $"Bajando {item.Abbreviation} ({number} de {total})… {percent}%";
                }
            }
        }

        await using var check = File.OpenRead(destination);
        var hash = Convert.ToHexString(await System.Security.Cryptography.SHA256.HashDataAsync(check));
        if (check.Length != item.Size || !hash.Equals(item.Sha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("La Biblia bajada no es la esperada.");
    }

    /// <summary>Adds the version a downloaded file brings; returns the name it got in the library.</summary>
    private string ImportDownloadedBible(string file, BibleCatalogItem item)
    {
        BiblePackageEntry entry;
        using (var stream = File.OpenRead(file))
            entry = BiblePackage.ReadManifest(stream).FirstOrDefault()
                    ?? throw new InvalidDataException("El archivo no trae ninguna Biblia.");

        ParsedBible bible;
        using (var stream = File.OpenRead(file))
            bible = BiblePackage.ReadVersion(stream, entry);
        if (bible.VerseCount == 0)
            throw new InvalidDataException("El archivo no trae versículos.");

        // The list's names win: they are the ones the operator chose from.
        return _bibles.Import(item.Name, item.Abbreviation, item.Language, bible).Name;
    }
}
