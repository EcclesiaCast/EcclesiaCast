using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using EcclesiaCast.Core.Displays;
using EcclesiaCast.Desktop.Controls;
using EcclesiaCast.Desktop.Platform;
using EcclesiaCast.Platform.Mac;
using Serilog;

namespace EcclesiaCast.Desktop.Views;

/// <summary>
/// What the output and the stage windows share: no border, the exact size
/// of their screen, shown without taking the keyboard from the operator,
/// cursor hidden. <see cref="IOutputWindowNative"/> does what Avalonia does
/// not: on Windows it never takes the keyboard and covers the screen in
/// pixels; on a Mac it goes above the menu bar and the Dock and stays on
/// every Space. macOS's own full screen is never used.
/// </summary>
public abstract class FullScreenWindow : Window
{
    private readonly IOutputWindowNative _native;
    private bool _prepared;

    protected FullScreenWindow(IOutputWindowNative native)
    {
        _native = native;

        WindowDecorations = WindowDecorations.None;
        ShowActivated = false;
        ShowInTaskbar = false;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.Manual;
        Background = Brushes.Black;
        Cursor = new Cursor(StandardCursorType.None);
        FontFamily = AvaloniaFontCatalog.Current.Resolve("Segoe UI");

        // On Windows the plain «always on top» is enough. On a Mac Avalonia's
        // Topmost sets its own (lower) level, which would undo the native one.
        Topmost = !OperatingSystem.IsMacOS();
    }

    /// <summary>The screen it was last shown on.</summary>
    public DisplayInfo? Display { get; private set; }

    /// <summary>
    /// Shows the window over the whole of <paramref name="display"/>: first
    /// where and how big (Avalonia counts in points, so a Retina screen is
    /// its pixels over its scale), then shown, then the native call.
    /// </summary>
    protected void ShowOnDisplay(DisplayInfo display)
    {
        Display = display;
        var scale = display.Scale > 0 ? display.Scale : 1;
        Position = new PixelPoint(display.X, display.Y);
        Width = display.Width / scale;
        Height = display.Height / scale;

        if (!_prepared && NativeHandle() is { IsEmpty: false } early)
        {
            _native.Prepare(early);
            _prepared = true;
        }

        Show();

        if (NativeHandle() is { IsEmpty: false } handle)
        {
            if (!_prepared)
            {
                _native.Prepare(handle);
                _prepared = true;
            }

            _native.Place(handle, display);
            Log.Information("{Window} en {Display}: {W}×{H} pixeles, escala {Scale}",
                Title, display.DeviceName, display.Width, display.Height, display.Scale);

            // Again a moment later: shows in the log whether anything put the
            // window back below the menu bar after it opened.
            if (OperatingSystem.IsMacOS())
                DispatcherTimer.RunOnce(
                    () => MacOutputWindowNative.Describe(NativeHandle(), "un segundo después"),
                    TimeSpan.FromSeconds(1));
        }
    }

    private NativeWindowHandle NativeHandle() =>
        TryGetPlatformHandle() is { } handle ? new NativeWindowHandle(handle.Handle, handle.HandleDescriptor) : default;

    /// <summary>
    /// The way out, flashed for a few seconds: the window takes no keyboard
    /// by design, and with a single screen it covers the operator's panel.
    /// </summary>
    protected static Border Hint(string text) => new()
    {
        IsVisible = false,
        IsHitTestVisible = false,
        VerticalAlignment = VerticalAlignment.Top,
        HorizontalAlignment = HorizontalAlignment.Center,
        Background = new SolidColorBrush(Color.Parse("#CC2F63C9")),
        CornerRadius = new CornerRadius(0, 0, 10, 10),
        Padding = new Thickness(26, 12),
        Child = new TextBlock { Text = text, FontSize = 20, Foreground = Brushes.White },
    };

    private int _hintVersion;

    protected void Flash(Border hint)
    {
        var version = ++_hintVersion;
        Fades.Stop(hint, OpacityProperty);
        hint.Opacity = 1;
        hint.IsVisible = true;

        DispatcherTimer.RunOnce(async () =>
        {
            if (version != _hintVersion)
                return;
            if (await Fades.Fade(hint, 1, 0, TimeSpan.FromMilliseconds(700)) && version == _hintVersion)
                hint.IsVisible = false;
        }, TimeSpan.FromSeconds(6));
    }
}
