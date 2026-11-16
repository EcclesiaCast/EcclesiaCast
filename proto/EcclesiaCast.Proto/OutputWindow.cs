using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using Serilog;

namespace EcclesiaCast.Proto;

/// <summary>
/// The projector's window: no border, the exact size of the chosen screen,
/// shown without taking the keyboard from the operator, cursor hidden. On a
/// Mac the native call puts it over the menu bar and the Dock; macOS's own
/// full screen is never used (it makes a Space of its own and leaves when
/// another program comes forward).
/// </summary>
public sealed class OutputWindow : Window
{
    public SlideView Slide { get; } = new();

    public Image VideoImage { get; } = new() { Stretch = Stretch.UniformToFill, IsHitTestVisible = false };

    /// <summary>The bounds of the screen it was opened on, to notice when it goes away.</summary>
    public PixelRect ScreenBounds { get; }

    /// <summary>Esc or a double click with a single screen: the operator wants it gone.</summary>
    public event EventHandler? CloseRequested;

    private readonly bool _singleScreen;

    public OutputWindow(Screen screen, bool singleScreen)
    {
        _singleScreen = singleScreen;
        ScreenBounds = screen.Bounds;

        Title = "EcclesiaCast Proto — salida";
        WindowDecorations = WindowDecorations.None;
        ShowActivated = false;
        ShowInTaskbar = false;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.Manual;
        Background = Brushes.Black;
        Cursor = new Cursor(StandardCursorType.None);

        // On Windows the plain «always on top» is enough. On a Mac Avalonia's
        // Topmost sets its own (lower) level, which would undo the native one.
        Topmost = !OperatingSystem.IsMacOS();

        var scaling = screen.Scaling <= 0 ? 1 : screen.Scaling;
        Position = screen.Bounds.Position;
        Width = screen.Bounds.Width / scaling;
        Height = screen.Bounds.Height / scaling;

        var layers = new Grid();
        layers.Children.Add(VideoImage);
        layers.Children.Add(Slide);
        Content = layers;

        Opened += (_, _) =>
        {
            Log.Information("Salida abierta en {Screen}: {Bounds}, escala {Scaling}; ventana {W}×{H} puntos",
                screen.DisplayName, screen.Bounds, screen.Scaling, Width, Height);
            MacWindowNative.MakeOutputWindow(this);

            // Again a moment later: shows whether anything put the window
            // back below the menu bar after it opened.
            DispatcherTimer.RunOnce(() => MacWindowNative.Describe(this, "un segundo después"), TimeSpan.FromSeconds(1));
        };

        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape && _singleScreen)
                CloseRequested?.Invoke(this, EventArgs.Empty);
        };
        DoubleTapped += (_, _) =>
        {
            if (_singleScreen)
                CloseRequested?.Invoke(this, EventArgs.Empty);
        };
    }
}
