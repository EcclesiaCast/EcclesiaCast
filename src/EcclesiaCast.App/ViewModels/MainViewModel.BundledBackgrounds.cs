using CommunityToolkit.Mvvm.Input;
using System.IO;
using System.Net.Http;
using System.Windows;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Media;
using Serilog;

namespace EcclesiaCast.App.ViewModels;

/// <summary>
/// The sample video backgrounds that come with the installer (the «Fondos de
/// ejemplo» box), brought into the library the first time the program opens.
/// </summary>
public sealed partial class MainViewModel
{
    /// <summary>Where the installer copies them, when the box was ticked.</summary>
    private static string BundledBackgroundsFolder => Path.Combine(AppContext.BaseDirectory, "fondos");

    /// <summary>
    /// Where the library keeps its own copy. The program's folder is replaced
    /// at every update and goes away with an uninstall; the library must not
    /// lose its pictures with it.
    /// </summary>
    private static string SampleBackgroundsFolder => Path.Combine(App.DataDir, "fondos-de-ejemplo");

    private static bool IsSampleBackground(string path) =>
        path.StartsWith(SampleBackgroundsFolder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Brings the sample backgrounds the library does not have yet into their
    /// own tab. Ten short videos (about 30 MB) to copy: a moment, behind the
    /// loading screen and before the library is read.
    /// </summary>
    /// <returns>What the status bar should say, or null when nothing came in.</returns>
    private string? ImportBundledBackgrounds()
    {
        try
        {
            var source = BundledBackgroundsFolder;
            if (!Directory.Exists(source))
                return null;

            var remembered = _settings.Get(BundledBackgrounds.ImportedSettingKey);
            var pending = BundledBackgrounds.Pending(
                Directory.EnumerateFiles(source).Select(Path.GetFileName).OfType<string>(), remembered);
            if (pending.Count == 0)
                return null;

            Directory.CreateDirectory(SampleBackgroundsFolder);
            var known = _media.GetAll().Select(m => m.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var added = 0;
            foreach (var file in pending)
            {
                var copy = Path.Combine(SampleBackgroundsFolder, file);
                if (!known.Contains(copy))
                {
                    File.Copy(Path.Combine(source, file), copy, overwrite: true);
                    // The poster comes from the shell; the ones it cannot make
                    // are made with VLC afterwards (RefreshVideoThumbnailsAsync).
                    _media.Add(new MediaItem
                    {
                        Name = BundledBackgrounds.DisplayName(file),
                        Path = copy,
                        Type = MediaType.Video,
                        ThumbnailPath = MediaThumbnails.Create(copy, MediaType.Video, videoEngine: null),
                        Category = BundledBackgrounds.Tab,
                        Behavior = MediaBehavior.Background,
                        // Drawn at 1920×1080 to loop without a seam, and silent.
                        Scaling = MediaScaling.Fill,
                        EndBehavior = VideoEndBehavior.Loop,
                        Muted = true,
                    });
                    added++;
                }

                // Written down one by one: if one fails, the ones before it
                // must not come in twice next time.
                remembered = BundledBackgrounds.Remember(remembered, [file]);
                _settings.Set(BundledBackgrounds.ImportedSettingKey, remembered);
            }

            if (added == 0)
                return null;

            Log.Information("Fondos de ejemplo traídos a la biblioteca: {Count}", added);
            return added == 1
                ? $"Se agregó un fondo de ejemplo en la pestaña «{BundledBackgrounds.Tab}»."
                : $"Se agregaron {added} fondos de ejemplo en la pestaña «{BundledBackgrounds.Tab}».";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warning(ex, "No se pudieron traer los fondos de ejemplo del instalador");
            return null;
        }
    }

    /// <summary>Where the packs are unzipped: the library's own copy, like the sample backgrounds.</summary>
    private static string PackBackgroundsFolder => Path.Combine(App.DataDir, "fondos-agregados");

    /// <summary>
    /// «Archivo → Agregar paquete de fondos…»: unzips a pack of backgrounds
    /// into the library's own folder and brings each one into the «Fondos
    /// agregados» tab, looping and silent, like the sample ones.
    /// </summary>
    [RelayCommand]
    private async Task ImportBackgroundPack()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Agregar paquete de fondos",
            Filter = "Paquete de fondos (.zip)|*.zip",
        };
        if (dialog.ShowDialog() != true)
            return;

        try
        {
            StatusText = "Abriendo el paquete de fondos…";
            var files = await Task.Run(() => BackgroundPack.Extract(dialog.FileName, PackBackgroundsFolder));
            if (files.Count == 0)
            {
                StatusText = "El paquete no tiene videos ni imágenes para usar de fondo.";
                return;
            }

            AddPackBackgrounds(files);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            Log.Warning(ex, "No se pudo agregar el paquete de fondos");
            StatusText = "No se pudo abrir el paquete: tiene que ser un .zip de fondos.";
        }
    }

    /// <summary>Brings unzipped pack files into the library's «Fondos agregados» tab.</summary>
    private void AddPackBackgrounds(IReadOnlyList<string> files)
    {
        foreach (var file in files)
        {
            var type = MediaFiles.Classify(file) == MediaFileKind.Image ? MediaType.Image : MediaType.Video;
            _media.Add(new MediaItem
            {
                Name = Path.GetFileNameWithoutExtension(file),
                Path = file,
                Type = type,
                ThumbnailPath = MediaThumbnails.Create(file, type, videoEngine: null),
                Category = BackgroundPack.Tab,
                Behavior = MediaBehavior.Background,
                Scaling = MediaScaling.Fill,
                EndBehavior = VideoEndBehavior.Loop,
                Muted = true,
            });
        }

        LoadMedia();
        Log.Information("Paquete de fondos agregado: {Count}", files.Count);
        StatusText = files.Count == 1
            ? $"Se agregó un fondo en la pestaña «{BackgroundPack.Tab}»."
            : $"Se agregaron {files.Count} fondos en la pestaña «{BackgroundPack.Tab}».";
    }

    private bool _downloadingBackgroundPack;

    /// <summary>
    /// «Archivo → Bajar más fondos de internet…»: downloads the pack of extra
    /// backgrounds from GitHub (about 22 MB), checks that it is exactly the file
    /// the program knows, and adds what the library does not have yet.
    /// </summary>
    [RelayCommand]
    private async Task DownloadBackgroundPack()
    {
        if (_downloadingBackgroundPack)
            return;

        var answer = MessageBox.Show(
            "Se van a bajar 9 fondos en video (unos 22 MB) desde GitHub y a agregar a la pestaña «"
            + BackgroundPack.Tab + "». ¿Seguimos?",
            "Bajar más fondos", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (answer != MessageBoxResult.Yes)
            return;

        _downloadingBackgroundPack = true;
        var work = Path.Combine(Path.GetTempPath(), "ecclesiacast-fondos-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(work);
            var zip = Path.Combine(work, "paquete.zip");
            await DownloadBackgroundPackFile(zip);

            StatusText = "Abriendo el paquete de fondos…";
            var added = await Task.Run(() =>
            {
                // Unzipped apart: the ones the library already has are left out,
                // so bajando dos veces no quedan copias «(2)».
                var unpacked = BackgroundPack.Extract(zip, Path.Combine(work, "fondos"));
                Directory.CreateDirectory(PackBackgroundsFolder);
                var moved = new List<string>();
                foreach (var file in unpacked)
                {
                    var target = Path.Combine(PackBackgroundsFolder, Path.GetFileName(file));
                    if (File.Exists(target))
                        continue;
                    File.Move(file, target);
                    moved.Add(target);
                }

                return moved;
            });

            if (added.Count == 0)
                StatusText = "Ya tenías todos los fondos de ese paquete.";
            else
                AddPackBackgrounds(added);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException
                                       or HttpRequestException or TaskCanceledException)
        {
            Log.Warning(ex, "No se pudo bajar el paquete de fondos");
            StatusText = "No se pudo bajar el paquete de fondos. Revisá la conexión y probá de nuevo.";
        }
        finally
        {
            _downloadingBackgroundPack = false;
            try { Directory.Delete(work, recursive: true); } catch (IOException) { /* se limpia solo */ }
        }
    }

    private async Task DownloadBackgroundPackFile(string destination)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"EcclesiaCast/{AppInfo.Version}");
        using var response = await client.GetAsync(AppInfo.BackgroundPackUrl, HttpCompletionOption.ResponseHeadersRead);
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
                    StatusText = $"Bajando los fondos… {percent}%";
                }
            }
        }

        await using var check = File.OpenRead(destination);
        var hash = Convert.ToHexString(await System.Security.Cryptography.SHA256.HashDataAsync(check));
        if (!hash.Equals(AppInfo.BackgroundPackSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("El paquete bajado no es el esperado.");
    }
}
