using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using EcclesiaCast.Core.Media;
using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Presentation.Rendering;
using LibVLCSharp.Shared;
using Serilog;
using MediaType = EcclesiaCast.Core.Media.MediaType;

namespace EcclesiaCast.Desktop.Platform;

/// <summary>
/// Whether Ctrl (⌘ on a Mac) is held. Avalonia has no keyboard to ask at any
/// moment, so the operator's window tells it on every key.
/// </summary>
public sealed class AvaloniaKeyboardState : IKeyboardState
{
    private KeyModifiers _modifiers;

    public bool IsControlDown =>
        _modifiers.HasFlag(OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Control);

    /// <summary>Listens to every key of this window, before its controls.</summary>
    public void Track(TopLevel window)
    {
        window.AddHandler(InputElement.KeyDownEvent, (_, e) => _modifiers = e.KeyModifiers, RoutingStrategies.Tunnel, true);
        window.AddHandler(InputElement.KeyUpEvent, (_, e) => _modifiers = e.KeyModifiers, RoutingStrategies.Tunnel, true);
    }
}

/// <summary>
/// The filter of a list, which in WPF its view applies by itself. Avalonia
/// has no such view: the filter is kept here, and a screen that shows the
/// list asks for it (<see cref="Visible{T}"/>) and hears when it changes.
/// </summary>
public sealed class AvaloniaListFilter : IListFilter
{
    private readonly ConditionalWeakTable<object, Func<object?, bool>> _filters = new();

    public event EventHandler<object>? Changed;

    public void Apply<T>(ObservableCollection<T> list, Func<T, bool>? filter)
    {
        if (filter is null)
            _filters.Remove(list);
        else
            _filters.AddOrUpdate(list, item => item is T typed && filter(typed));
        Changed?.Invoke(this, list);
    }

    /// <summary>The items of <paramref name="list"/> the filter lets through.</summary>
    public IEnumerable<T> Visible<T>(ObservableCollection<T> list) =>
        _filters.TryGetValue(list, out var filter) ? list.Where(item => filter(item)) : list;
}

/// <summary>How the words of a slide are cut into slides: the shared count, with Avalonia's letters.</summary>
public sealed class AvaloniaTextMeasurer : ITextMeasurer
{
    public IReadOnlyList<(string Main, string? Secondary)> SplitToFit(
        SlideContent slide, Func<string, string>? label = null, Func<string, string>? secondaryLabel = null,
        int maxParts = 6) =>
        SlideTextFit.SplitToFit(AvaloniaTextMetrics.Current, slide, label, secondaryLabel, maxParts);
}

/// <summary>Pictures for the screens, as Avalonia bitmaps.</summary>
public sealed class AvaloniaImageFactory : IImageFactory
{
    public object? FromFile(string path, int? decodeWidth = null)
    {
        try
        {
            using var stream = File.OpenRead(path);
            return decodeWidth is { } width ? Bitmap.DecodeToWidth(stream, width) : new Bitmap(stream);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "No se pudo leer la imagen {Path}", path);
            return null;
        }
    }

    public object? FromBytes(byte[] bytes)
    {
        try
        {
            using var stream = new MemoryStream(bytes);
            return new Bitmap(stream);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "No se pudo leer una imagen");
            return null;
        }
    }

    public (int Width, int Height)? ReadSize(byte[] bytes) =>
        FromBytes(bytes) is Bitmap bitmap ? (bitmap.PixelSize.Width, bitmap.PixelSize.Height) : null;
}

/// <summary>
/// The small pictures of the media cards. The test program shows no cards,
/// so it makes none (the library keeps working without them) and it never
/// sweeps the folder: a library brought from Windows keeps its own.
/// </summary>
public sealed class AvaloniaThumbnailer(IPlatformPaths paths) : IThumbnailer
{
    public string Directory => Path.Combine(paths.DataFolder, "thumbnails");

    public string? Create(string sourcePath, MediaType type, LibVLC? videoEngine) => null;

    public void Delete(string? thumbnailPath)
    {
    }

    public void DeleteOrphans(IEnumerable<string?> referenced)
    {
    }
}

/// <summary>Text to the clipboard, through the operator's window.</summary>
public sealed class AvaloniaClipboardText : IClipboardText
{
    public TopLevel? Window { get; set; }

    public void SetText(string text)
    {
        if (Window?.Clipboard is { } clipboard)
            _ = clipboard.SetTextAsync(text);
    }
}

/// <summary>Opens addresses and folders with what the system has for them.</summary>
public sealed class DesktopShell : IShell
{
    public void OpenUrl(string url) => Open(url);

    public void OpenFolder(string folder) => Open(folder);

    public void ShowInFolder(string file)
    {
        if (OperatingSystem.IsMacOS())
            Run("/usr/bin/open", "-R", file);
        else
            Open(Path.GetDirectoryName(file) ?? file);
    }

    private static void Open(string target)
    {
        if (OperatingSystem.IsMacOS())
            Run("/usr/bin/open", target);
        else
            Run(target);
    }

    private static void Run(string program, params string[] arguments)
    {
        try
        {
            var info = new ProcessStartInfo(program) { UseShellExecute = arguments.Length == 0 };
            foreach (var argument in arguments)
                info.ArgumentList.Add(argument);
            Process.Start(info)?.Dispose();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "No se pudo abrir {Program}", program);
        }
    }
}

/// <summary>
/// What the Avalonia program has. The functions that live in WPF windows
/// (the web panel, capturing a window, YouTube live, PowerPoint, the media
/// keys, «Seguir la canción», the spelling checker) are not here yet, on a
/// Mac or anywhere: their buttons stay hidden instead of failing.
/// </summary>
public sealed class AvaloniaPlatformInfo : IPlatformInfo
{
    public PlatformSystem System => OperatingSystem.IsMacOS() ? PlatformSystem.Mac : PlatformSystem.Windows;

    public bool IsAvailable(Feature feature) => false;
}

/// <summary>A Mac has no installer that leaves a choice behind.</summary>
public sealed class NoInstallerSettings : IInstallerSettings
{
    public string? ReadLowPerformanceChoice() => null;
}

/// <summary>There is no web engine in the Avalonia program yet (the web panel and YouTube).</summary>
public sealed class NoWebEngine : IWebEngine
{
    public bool IsRuntimeAvailable() => false;

    public bool ShowPointer { get; set; }
}
