using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using EcclesiaCast.Core.Capture;
using Serilog;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Imaging;
using WinRT;

namespace EcclesiaCast.App.Services;

/// <summary>
/// Windows.Graphics.Capture on one window, drawn into a <see cref="WriteableBitmap"/>
/// that <see cref="WindowCaptureFeed"/> hands to whoever projects it.
///
/// The frames arrive on a worker thread of the capture's own. All that is done
/// there is to copy the picture out of the video card into a reused byte array;
/// the UI thread only writes that array into the bitmap, at render priority.
/// Nothing is done for a frame while the previous one is still on its way to
/// the bitmap (the capture is faster than the screen needs), and frames beyond
/// the rate limit are dropped: the UI thread is the one thing that must never
/// fall behind (see <see cref="UiWatchdog"/>).
///
/// The yellow border Windows draws around a captured window stays: turning it
/// off needs Windows 11. It only shows on the operator's screen.
/// </summary>
public sealed class WindowCapture : IWindowCapture
{
    private const int FullRate = 30;
    private const int LowRate = 12;
    private const int Buffers = 2;
    private const DirectXPixelFormat PixelFormat = DirectXPixelFormat.B8G8R8A8UIntNormalized;

    private static readonly Guid GraphicsCaptureItemIid = new("79C3F95B-31F7-4EC2-A464-632EF5D30760");

    private readonly Dispatcher _ui;
    private DispatcherTimer? _minimizedTimer;

    private GraphicsCaptureItem? _item;
    private Direct3D11CaptureFramePool? _pool;
    private GraphicsCaptureSession? _session;
    private IDirect3DDevice? _device;
    private nint _hwnd;

    private Windows.Graphics.SizeInt32 _size;
    private byte[]? _pixels;
    private WriteableBitmap? _bitmap;

    // Written from the capture's thread and the UI thread.
    private int _busy;
    private long _lastFrameAt;
    private volatile int _generation;
    private long _frames;
    private bool _minimized;

    public WindowCapture()
    {
        _ui = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
    }

    public event EventHandler? Ended;

    public event EventHandler? MinimizedChanged;

    public bool IsSupported
    {
        get
        {
            try
            {
                return GraphicsCaptureSession.IsSupported();
            }
            catch
            {
                return false;
            }
        }
    }

    public bool IsRunning => _session is not null;

    public bool IsMinimized => _minimized;

    /// <summary>How many pictures have reached the bitmap since this started (for the smoke test).</summary>
    public long FramesPainted => Interlocked.Read(ref _frames);

    public IReadOnlyList<RawWindow> ListWindows() =>
        WindowCandidates.Filter(WindowNative.EnumerateAll(), Environment.ProcessId);

    // ── Empezar y parar ──────────────────────────────────────────

    public bool Start(RawWindow window, bool showCursor, out string? error)
    {
        Stop();
        try
        {
            if (!WindowNative.IsWindow((nint)window.Handle))
            {
                error = "Esa ventana ya no está abierta.";
                return false;
            }

            var item = CreateItemForWindow((nint)window.Handle);
            return Begin(item, showCursor, (nint)window.Handle, out error);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "No se pudo capturar la ventana «{Title}»", window.Title);
            Stop();
            error = "Windows no dejó capturar esa ventana. Si es de un programa abierto «como administrador», " +
                    "tiene que abrirse EcclesiaCast también así, o probá con otra.";
            return false;
        }
    }

    public async Task<(string? Title, string? Error)> StartFromPickerAsync(nint owner, bool showCursor)
    {
        try
        {
            var picker = new GraphicsCapturePicker();
            WinRT.Interop.InitializeWithWindow.Initialize(picker, owner);
            var item = await picker.PickSingleItemAsync();
            if (item is null)
                return (null, null);

            Stop();
            return Begin(item, showCursor, 0, out var error) ? (item.DisplayName, null) : (null, error);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "No se pudo abrir el selector de ventanas de Windows");
            Stop();
            return (null, "No se pudo abrir el selector de ventanas de Windows.");
        }
    }

    private bool Begin(GraphicsCaptureItem item, bool showCursor, nint hwnd, out string? error)
    {
        _item = item;
        _hwnd = hwnd;
        _device = CreateDevice();
        _size = Normalize(item.Size);
        _minimized = false;

        var generation = ++_generation;
        _pool = Direct3D11CaptureFramePool.CreateFreeThreaded(_device, PixelFormat, Buffers, _size);
        _pool.FrameArrived += (pool, _) => _ = OnFrameAsync(pool, generation);
        item.Closed += (_, _) => _ui.BeginInvoke(() =>
        {
            if (generation != _generation)
                return;

            Stop();
            Ended?.Invoke(this, EventArgs.Empty);
        });

        _session = _pool.CreateCaptureSession(item);
        // Choosing whether the pointer is drawn needs Windows 10 2004; before
        // that Windows always draws it.
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041))
            _session.IsCursorCaptureEnabled = showCursor;
        _session.StartCapture();

        if (hwnd != 0)
        {
            _minimizedTimer = new DispatcherTimer(DispatcherPriority.Background, _ui) { Interval = TimeSpan.FromSeconds(1) };
            _minimizedTimer.Tick += (_, _) => CheckMinimized();
            _minimizedTimer.Start();
        }

        Log.Information("Captura de ventana iniciada: {Name}", item.DisplayName);
        error = null;
        return true;
    }

    public void Stop()
    {
        _generation++;
        _minimizedTimer?.Stop();
        _minimizedTimer = null;

        try
        {
            _session?.Dispose();
            _pool?.Dispose();
            _device?.Dispose();
        }
        catch (Exception ex)
        {
            // Closing a capture that Windows already ended can complain: there is nothing left to free.
            Log.Debug(ex, "Al parar la captura de ventana");
        }

        _session = null;
        _pool = null;
        _device = null;
        _item = null;
        _hwnd = 0;
        _bitmap = null;
        _pixels = null;
        _minimized = false;
        Interlocked.Exchange(ref _busy, 0);

        if (_ui.CheckAccess())
            WindowCaptureFeed.Publish(null);
        else
            _ui.BeginInvoke(() => WindowCaptureFeed.Publish(null));
    }

    private void CheckMinimized()
    {
        var minimized = _hwnd != 0 && WindowNative.IsIconic(_hwnd);
        if (minimized == _minimized)
            return;

        _minimized = minimized;
        MinimizedChanged?.Invoke(this, EventArgs.Empty);
    }

    private static Windows.Graphics.SizeInt32 Normalize(Windows.Graphics.SizeInt32 size) =>
        new() { Width = Math.Max(1, size.Width), Height = Math.Max(1, size.Height) };

    // ── Cada cuadro ──────────────────────────────────────────────

    private async Task OnFrameAsync(Direct3D11CaptureFramePool pool, int generation)
    {
        Direct3D11CaptureFrame? frame = null;
        var started = false;
        try
        {
            frame = pool.TryGetNextFrame();
            if (frame is null || generation != _generation)
                return;

            // The window changed size: the pool has to be rebuilt to the new
            // size. This frame was drawn for the old one, so it goes.
            var content = Normalize(frame.ContentSize);
            if (content.Width != _size.Width || content.Height != _size.Height)
            {
                _size = content;
                if (_device is { } device)
                    pool.Recreate(device, PixelFormat, Buffers, content);
                return;
            }

            // Still on its way to the screen, or over the rate: drop it.
            var now = Stopwatch.GetTimestamp();
            var interval = Stopwatch.Frequency / (PerformanceMode.IsLow ? LowRate : FullRate);
            if (Volatile.Read(ref _busy) == 1 || now - Interlocked.Read(ref _lastFrameAt) < interval)
                return;

            Interlocked.Exchange(ref _busy, 1);
            started = true;
            Interlocked.Exchange(ref _lastFrameAt, now);

            using var copy = await SoftwareBitmap.CreateCopyFromSurfaceAsync(frame.Surface);
            var width = copy.PixelWidth;
            var height = copy.PixelHeight;
            var needed = width * height * 4;
            if (_pixels is null || _pixels.Length != needed)
                _pixels = new byte[needed];

            var pixels = _pixels;
            copy.CopyToBuffer(pixels.AsBuffer());

            // From here the UI thread owns the array until it clears the flag.
            started = false;
            await _ui.InvokeAsync(() => Paint(generation, pixels, width, height), DispatcherPriority.Render);
        }
        catch (Exception ex) when (generation != _generation)
        {
            // The capture was stopped under this frame.
            Log.Debug(ex, "Cuadro de captura descartado al parar");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Falló un cuadro de la captura de ventana");
        }
        finally
        {
            if (started)
                Interlocked.Exchange(ref _busy, 0);
            frame?.Dispose();
        }
    }

    /// <summary>Writes the array into the bitmap, on the UI thread, and lets the next frame in.</summary>
    private void Paint(int generation, byte[] pixels, int width, int height)
    {
        try
        {
            if (generation != _generation)
                return;

            if (_bitmap is null || _bitmap.PixelWidth != width || _bitmap.PixelHeight != height)
            {
                _bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);
                WindowCaptureFeed.Publish(_bitmap);
            }

            _bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 4, 0);
            Interlocked.Increment(ref _frames);
        }
        finally
        {
            Interlocked.Exchange(ref _busy, 0);
        }
    }

    // ── Direct3D y el elemento de captura, por interoperabilidad ──

    [ComImport, Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IGraphicsCaptureItemInterop
    {
        nint CreateForWindow(nint window, [In] ref Guid iid);

        nint CreateForMonitor(nint monitor, [In] ref Guid iid);
    }

    [DllImport("combase.dll", PreserveSig = false)]
    private static extern void RoGetActivationFactory(nint classId, [In] ref Guid iid, out nint factory);

    [DllImport("combase.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    private static extern void WindowsCreateString(string source, int length, out nint hstring);

    [DllImport("combase.dll", PreserveSig = false)]
    private static extern void WindowsDeleteString(nint hstring);

    [DllImport("d3d11.dll")]
    private static extern int D3D11CreateDevice(
        nint adapter, uint driverType, nint software, uint flags,
        nint featureLevels, uint featureLevelCount, uint sdkVersion,
        out nint device, out uint featureLevel, out nint context);

    [DllImport("d3d11.dll", PreserveSig = false)]
    private static extern void CreateDirect3D11DeviceFromDXGIDevice(nint dxgiDevice, out nint graphicsDevice);

    private static GraphicsCaptureItem CreateItemForWindow(nint hwnd)
    {
        var className = "Windows.Graphics.Capture.GraphicsCaptureItem";
        WindowsCreateString(className, className.Length, out var hstring);
        nint factory;
        try
        {
            var interopIid = typeof(IGraphicsCaptureItemInterop).GUID;
            RoGetActivationFactory(hstring, ref interopIid, out factory);
        }
        finally
        {
            WindowsDeleteString(hstring);
        }

        try
        {
            var interop = (IGraphicsCaptureItemInterop)Marshal.GetObjectForIUnknown(factory);
            var itemIid = GraphicsCaptureItemIid;
            var pointer = interop.CreateForWindow(hwnd, ref itemIid);
            try
            {
                return MarshalInterface<GraphicsCaptureItem>.FromAbi(pointer);
            }
            finally
            {
                Marshal.Release(pointer);
            }
        }
        finally
        {
            Marshal.Release(factory);
        }
    }

    private static IDirect3DDevice CreateDevice()
    {
        const uint hardware = 1, warp = 5, bgraSupport = 0x20, sdkVersion = 7;

        var result = D3D11CreateDevice(0, hardware, 0, bgraSupport, 0, 0, sdkVersion, out var device, out _, out var context);
        if (result < 0)
        {
            // No usable video card driver: the software renderer is slower but works.
            result = D3D11CreateDevice(0, warp, 0, bgraSupport, 0, 0, sdkVersion, out device, out _, out context);
            Marshal.ThrowExceptionForHR(result);
        }

        var dxgiIid = new Guid("54ec77fa-1377-44e6-8c32-88fd5f44c84c");
        nint dxgi = 0;
        nint graphics = 0;
        try
        {
            Marshal.ThrowExceptionForHR(Marshal.QueryInterface(device, in dxgiIid, out dxgi));
            CreateDirect3D11DeviceFromDXGIDevice(dxgi, out graphics);
            return MarshalInterface<IDirect3DDevice>.FromAbi(graphics);
        }
        finally
        {
            if (graphics != 0)
                Marshal.Release(graphics);
            if (dxgi != 0)
                Marshal.Release(dxgi);
            if (context != 0)
                Marshal.Release(context);
            Marshal.Release(device);
        }
    }
}
