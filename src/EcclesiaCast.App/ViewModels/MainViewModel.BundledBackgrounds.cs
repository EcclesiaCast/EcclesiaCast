using CommunityToolkit.Mvvm.Input;
using System.IO;
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
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            Log.Warning(ex, "No se pudo agregar el paquete de fondos");
            StatusText = "No se pudo abrir el paquete: tiene que ser un .zip de fondos.";
        }
    }
}
