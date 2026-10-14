using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EcclesiaCast.App.Remote;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Bible;
using EcclesiaCast.Core.Diagnostics;
using EcclesiaCast.Core.Displays;
using EcclesiaCast.Core.Logos;
using EcclesiaCast.Core.Media;
using EcclesiaCast.Core.Performance;
using EcclesiaCast.Core.Playlists;
using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Core.Songs;
using EcclesiaCast.Core.Stage;
using EcclesiaCast.Core.Timing;
using EcclesiaCast.Core.Themes;
using EcclesiaCast.Data.Persistence;
using Serilog;

namespace EcclesiaCast.App.ViewModels;

public sealed partial class MainViewModel
{
    // ── YouTube ──────────────────────────────────────────────────

    /// <summary>Opens the embedded browser: sign in with the church account and pick videos.</summary>
    [RelayCommand]
    private void OpenYouTube()
    {
        if (!WebViewProfile.IsRuntimeAvailable())
        {
            MessageBox.Show(
                "Falta el runtime de WebView2 (Microsoft Edge WebView2), que EcclesiaCast usa para YouTube.\n\n" +
                "Se descarga gratis desde el sitio de Microsoft; en Windows 11 suele venir instalado.",
                "EcclesiaCast", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var ids = _youTube.Browse();
        AddYouTubeVideos(ids, "desde el navegador");
    }

    /// <summary>Adds a video from a pasted link.</summary>
    [RelayCommand]
    private void AddYouTubeLink()
    {
        var text = _textPrompt.Ask("Agregar de YouTube", "Pegá el link del video:");
        if (string.IsNullOrWhiteSpace(text))
            return;

        var id = YouTubeUrl.TryParseVideoId(text);
        if (id is null)
        {
            StatusText = "Ese link no parece un video de YouTube.";
            return;
        }

        AddYouTubeVideos([id], "por link");
    }

    private void AddYouTubeVideos(IReadOnlyList<string> videoIds, string source)
    {
        var fresh = videoIds
            .Distinct()
            .Where(id => !_allMedia.Any(m => m.Type == MediaType.YouTube && m.YouTubeId == id))
            .ToList();
        if (fresh.Count == 0)
        {
            if (videoIds.Count > 0)
                StatusText = "Ese video ya estaba en la biblioteca.";
            return;
        }

        // Played over the internet the video is always YouTube's to serve (and
        // its ads, if the account has no Premium); a copy on this computer
        // depends on nothing. The operator picks, once for the whole batch.
        var choice = _textPrompt.Choose(
            "Agregar de YouTube",
            fresh.Count == 1
                ? "¿Cómo querés usar este video?"
                : $"¿Cómo querés usar estos {fresh.Count} videos?",
            [
                "🌐  Reproducir por internet",
                "💾  Descargar para usar sin internet",
            ]);
        if (choice < 0)
            return;

        // A download lands in the tab that was open, not in the YouTube one.
        var openTab = SelectedMediaTab;
        const string tab = "YouTube";

        foreach (var id in fresh)
        {
            _media.Add(new MediaItem
            {
                Name = $"YouTube {id}",
                Path = YouTubeUrl.WatchUrl(id),
                YouTubeId = id,
                Type = MediaType.YouTube,
                ThumbnailPath = YouTubeUrl.ThumbnailUrl(id),
                Category = tab,
                // Announcement videos play full screen with sound.
                Behavior = MediaBehavior.Foreground,
                EndBehavior = VideoEndBehavior.Logo,
                Muted = false,
            });
        }

        LoadMedia();
        SelectedMediaTab = tab;

        if (choice == 0)
        {
            StatusText = $"{fresh.Count} video(s) de YouTube agregado(s) {source}, para reproducir por internet. "
                       + "Renombralos en Propiedades.";
            return;
        }

        var downloaded = 0;
        foreach (var id in fresh)
        {
            if (_allMedia.FirstOrDefault(m => m.Type == MediaType.YouTube && m.YouTubeId == id) is not { } online)
                continue;
            if (DownloadYouTubeCore(online, openTab, removeOnlineEntry: true))
                downloaded++;
        }

        StatusText = downloaded == fresh.Count
            ? StatusText
            : $"Se bajaron {downloaded} de {fresh.Count}. Los que no, quedaron en «{tab}» para reproducir por internet.";
    }

    /// <summary>
    /// Saves a YouTube video as a local file, so the service doesn't depend on
    /// the connection holding up (and the quality stops being YouTube's call).
    /// The copy goes into the tab that is open; the YouTube entry is kept.
    /// </summary>
    [RelayCommand]
    private void DownloadYouTube(MediaItem? item)
    {
        if (item is not { Type: MediaType.YouTube } || string.IsNullOrWhiteSpace(item.YouTubeId))
        {
            StatusText = "Elegí un video de YouTube para descargar.";
            return;
        }

        DownloadYouTubeCore(item, SelectedMediaTab, removeOnlineEntry: false);
    }

    /// <summary>
    /// Runs the download window for <paramref name="item"/> and, when it works,
    /// adds the file to <paramref name="targetTab"/>. With
    /// <paramref name="removeOnlineEntry"/> the YouTube entry is dropped once the
    /// copy exists (it was only added as a stand-in); if the download fails it
    /// stays, so the video is never lost.
    /// </summary>
    private bool DownloadYouTubeCore(MediaItem item, string targetTab, bool removeOnlineEntry)
    {
        var window = new Views.YouTubeDownloadWindow(item.YouTubeId!, item.Name)
        {
            Owner = Application.Current.MainWindow,
        };

        if (window.ShowDialog() != true || window.DownloadedPath is not { } path)
        {
            StatusText = "La descarga no se completó.";
            return false;
        }

        var name = Path.GetFileNameWithoutExtension(path);
        _media.Add(new MediaItem
        {
            Name = name,
            Path = path,
            Type = MediaType.Video,
            ThumbnailPath = MediaThumbnails.Create(path, MediaType.Video, App.VideoEngine),
            Category = targetTab,
            // A downloaded announcement behaves like the YouTube one did.
            Behavior = item.Behavior,
            EndBehavior = item.EndBehavior,
            Muted = item.Muted,
            Volume = item.Volume,
        });

        if (removeOnlineEntry)
            _media.Delete(item.Id);

        LoadMedia();
        SelectedMediaTab = targetTab;
        RefreshLightCopies();
        StatusText = $"«{name}» descargado y agregado a «{targetTab}».";
        return true;
    }

    /// <summary>
    /// The item chosen to fill the screen behind this one. Looked up here
    /// because the presentation layer knows nothing about the library.
    /// </summary>
    private MediaItem? ResolveFill(MediaItem? item) =>
        item is null ? null
        : item.FillsWithSelf ? item
        : item.FillMediaId is { } id and > 0 ? _media.GetAll().FirstOrDefault(m => m.Id == id)
        : null;

    [RelayCommand]
    private void InspectMedia(MediaItem? item)
    {
        if (item is null)
            return;

        // The menu of a card of a presentation holds the object the card was
        // built with, which a reload may have replaced: edit the current one.
        item = CurrentMedia(item);
        if (_mediaInspector.Edit(item, MediaTabs.ToList(), _media.GetAll()))
        {
            _media.Update(item);
            LoadMedia();
            // The edited item goes to the screen as a fresh copy: the one on
            // screen is this same object, edited in place, so nothing would
            // notice the change.
            if (_presentation.Background?.Id == item.Id)
                _presentation.RefreshBackground(Resolved(item).Clone(), ResolveFill(item));
            StatusText = $"«{item.Name}» actualizado.";
        }
    }

    /// <summary>
    /// Right click on a page of a PDF or a deck → «Volver a importar en alta
    /// calidad»: draws every page of that document again at 4K over the same
    /// files. Pages imported before the document was kept ask for the original
    /// once. Nothing is re-imported on its own.
    /// </summary>
    [RelayCommand]
    private async Task ReimportDocumentPage(MediaItem? item)
    {
        if (item is null || !DocumentImporter.IsRenderedPage(item.Path))
            return;

        string? original = null;
        if (!DocumentImporter.HasSource(item.Path))
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = $"Elegí el documento original de «{item.Name}»",
                Filter = "PDF y PowerPoint|*.pdf;*.pptx;*.ppt",
            };
            if (dialog.ShowDialog() != true)
                return;
            original = dialog.FileName;
        }

        NoteAction($"Volver a importar en alta calidad «{item.Name}»");
        StatusText = "Dibujando de nuevo las páginas del documento… puede tardar un rato.";
        var result = await DocumentImporter.ReimportAsync(item.Path, original);
        if (result.Error is not null)
        {
            StatusText = result.Error;
            return;
        }

        // The pictures changed under the same names; their small posters are
        // made again from the sharper pages.
        var folder = Path.GetDirectoryName(item.Path);
        var pages = _media.GetAll()
            .Where(m => m.Type == MediaType.Image
                        && string.Equals(Path.GetDirectoryName(m.Path), folder, StringComparison.OrdinalIgnoreCase))
            .ToList();
        foreach (var page in pages)
        {
            MediaThumbnails.Delete(page.ThumbnailPath);
            page.ThumbnailPath = MediaThumbnails.Create(page.Path, MediaType.Image, videoEngine: null);
            _media.Update(page);
        }

        LoadMedia();

        // A page on screen is shown again from the new file.
        if (pages.FirstOrDefault(p => p.Id == _presentation.Background?.Id) is { } onScreen)
            _presentation.RefreshBackground(Resolved(onScreen).Clone(), ResolveFill(onScreen));

        StatusText = $"{pages.Count} página(s) dibujadas de nuevo en alta calidad.";
    }

    [RelayCommand]
    private void OpenMediaLocation(MediaItem? item)
    {
        if (item is null || !File.Exists(item.Path))
        {
            StatusText = "No se encuentra el archivo.";
            return;
        }

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"/select,\"{item.Path}\"",
            UseShellExecute = true,
        });
    }

    /// <summary>Right click → «Quitar de la biblioteca»: the whole selection when the card belongs to it.</summary>
    [RelayCommand]
    private void DeleteMedia(MediaItem? item)
    {
        if (item is not null)
            RemoveMedia(MediaTargets(item), alwaysAsk: false);
    }

    /// <summary>
    /// Takes media out of the library. One alone goes without asking, as it
    /// always did (the menu is a deliberate click); several — or Supr, which
    /// is easy to hit by accident — ask once, saying how many.
    /// </summary>
    private void RemoveMedia(IReadOnlyList<MediaItem> items, bool alwaysAsk)
    {
        if (items.Count == 0)
            return;

        if (alwaysAsk || items.Count > 1)
        {
            var what = items.Count == 1 ? $"«{items[0].Name}»" : $"{items.Count} medios";
            var answer = _textPrompt.Choose(
                "Quitar de la biblioteca",
                $"Se va a quitar {what} de la biblioteca. Los archivos de tu computadora no se borran "
                + "(sólo las páginas de un PDF o una presentación que el programa dibujó).",
                ["Quitar"]);
            if (answer != 0)
                return;
        }

        NoteAction(items.Count == 1 ? $"Quitar el medio «{items[0].Name}»" : $"Quitar {items.Count} medios");
        foreach (var item in items)
            RemoveMediaCore(item);

        MediaSelection.Clear();
        LoadMedia();
        StatusText = items.Count == 1
            ? $"«{items[0].Name}» quitado de la biblioteca."
            : $"{items.Count} medios quitados de la biblioteca.";
    }

    private void RemoveMediaCore(MediaItem item)
    {
        if (_presentation.Background?.Id == item.Id)
            _presentation.SetBackground(null);
        _media.Delete(item.Id);

        // A page of a PDF or a deck is a file this program made; taking the
        // item out of the library has to take the image with it.
        if (DocumentImporter.IsRenderedPage(item.Path))
            DocumentImporter.Forget(item.Path);

        // The same for the video made from a GIF.
        if (GifImporter.IsConverted(item.Path))
            GifImporter.Forget(item.Path);
    }
}
