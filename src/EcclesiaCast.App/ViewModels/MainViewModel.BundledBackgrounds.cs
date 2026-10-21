using System.IO;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Media;
using Serilog;

namespace EcclesiaCast.App.ViewModels;

/// <summary>
/// The sample backgrounds that come with the installer (the «Fondos de
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
    /// own tab. Twelve small pictures: it takes a moment, behind the loading
    /// screen and before the library is read.
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
                    _media.Add(new MediaItem
                    {
                        Name = BundledBackgrounds.DisplayName(file),
                        Path = copy,
                        Type = MediaType.Image,
                        ThumbnailPath = MediaThumbnails.Create(copy, MediaType.Image, videoEngine: null),
                        Category = BundledBackgrounds.Tab,
                        Behavior = MediaBehavior.Background,
                        // They are drawn at 1920×1080: they fill the screen as they are.
                        Scaling = MediaScaling.Fill,
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
}
