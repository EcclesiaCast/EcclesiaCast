using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Displays;

namespace EcclesiaCast.App.Views;

/// <summary>
/// «🪟 En una ventana»: the same output in an ordinary window, for a church
/// that shares it by Discord, a notebook with a single screen, or trying things
/// without a projector. It has a border and a taskbar button, it moves,
/// minimizes and closes (closing switches the output off), and it keeps the
/// shape chosen on its right-click menu, 16:9 or 4:3, while it is resized.
/// </summary>
public partial class OutputWindow
{
    /// <summary>This is the floating window, not a full screen on a display.</summary>
    public bool IsFloating { get; }

    private bool _closingForGood;

    /// <summary>The floating window and the full screen swap: this one goes for real.</summary>
    public void CloseForGood()
    {
        _closingForGood = true;
        Close();
    }

    private static double Ratio => FloatingOutput.RatioOf(FloatingOutputSettings.Current.Shape);

    private void MakeFloating()
    {
        var layout = FloatingOutputSettings.Current;
        Title = "EcclesiaCast — Salida";
        WindowStyle = WindowStyle.SingleBorderWindow;
        ResizeMode = ResizeMode.CanResize;
        ShowInTaskbar = true;
        // Opens without taking the keyboard from the operator panel.
        ShowActivated = false;
        Topmost = layout.AlwaysOnTop;
        MinWidth = 260;
        MinHeight = 180;
        // The pointer stays: it is a window to grab, move and right-click.
        ClearValue(CursorProperty);

        ContextMenu = new ContextMenu();
        ContextMenuOpening += (_, _) => FillMenu();
        Closing += OnFloatingClosing;
        IsVisibleChanged += (_, _) =>
        {
            if (!IsVisible)
                SaveLayout();
        };
    }

    // ── Tamaño y lugar ───────────────────────────────────────────

    private void SetUpFloating(IntPtr hwnd)
    {
        HwndSource.FromHwnd(hwnd)?.AddHook(FloatingHook);

        var layout = FloatingOutputSettings.Current;
        var picture = layout.Picture;
        var (frameWidth, frameHeight) = FrameOf(hwnd);
        var width = picture.Width + frameWidth;
        var height = picture.Height + frameHeight;

        var primary = System.Windows.Forms.Screen.PrimaryScreen ?? System.Windows.Forms.Screen.AllScreens[0];
        var screens = System.Windows.Forms.Screen.AllScreens.Select(s => ToRect(s.Bounds)).ToList();
        var work = ToRect(primary.WorkingArea);
        var wanted = double.IsNaN(layout.Left) || double.IsNaN(layout.Top)
            ? new ScreenRect(int.MinValue / 2, int.MinValue / 2, int.MinValue / 2 + width, int.MinValue / 2 + height)
            : new ScreenRect((int)layout.Left, (int)layout.Top, (int)layout.Left + width, (int)layout.Top + height);
        var (left, top) = FloatingOutput.Rescue(wanted, screens, work);

        SetWindowPos(hwnd, IntPtr.Zero, left, top, width, height, SWP_NOZORDER | SWP_NOACTIVATE);
    }

    private static ScreenRect ToRect(System.Drawing.Rectangle r) => new(r.Left, r.Top, r.Right, r.Bottom);

    /// <summary>What the border and the title bar take, in screen pixels.</summary>
    private static (int Width, int Height) FrameOf(IntPtr hwnd)
    {
        GetWindowRect(hwnd, out var window);
        GetClientRect(hwnd, out var client);
        return (Math.Max(0, (window.Right - window.Left) - (client.Right - client.Left)),
                Math.Max(0, (window.Bottom - window.Top) - (client.Bottom - client.Top)));
    }

    /// <summary>Makes the inside of the window — the picture — exactly that size, in screen pixels.</summary>
    private void SetPictureSize(PictureSize size)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero)
            return;

        if (WindowState != WindowState.Normal)
            WindowState = WindowState.Normal;

        var (frameWidth, frameHeight) = FrameOf(hwnd);
        SetWindowPos(hwnd, IntPtr.Zero, 0, 0, size.Width + frameWidth, size.Height + frameHeight,
            SWP_NOMOVE | SWP_NOZORDER | SWP_NOACTIVATE);
        SaveLayout();
    }

    /// <summary>The picture centred at its shape inside whatever the window measures (maximized, say).</summary>
    private void Frame_SizeChanged(object sender, SizeChangedEventArgs e) => FitStage();

    private void FitStage()
    {
        if (!IsFloating)
            return;

        var fit = FloatingOutput.Fit(Frame.ActualWidth, Frame.ActualHeight, Ratio);
        Stage.Width = fit.Width;
        Stage.Height = fit.Height;
        Stage.HorizontalAlignment = HorizontalAlignment.Center;
        Stage.VerticalAlignment = VerticalAlignment.Center;
    }

    /// <summary>Keeps size, place and «siempre visible» for the next time.</summary>
    private void SaveLayout()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero || WindowState != WindowState.Normal)
            return;

        GetWindowRect(hwnd, out var window);
        GetClientRect(hwnd, out var client);
        if (client.Right - client.Left <= 0)
            return;

        FloatingOutputSettings.Set(FloatingOutputSettings.Current with
        {
            PictureWidth = client.Right - client.Left,
            Left = window.Left,
            Top = window.Top,
            AlwaysOnTop = Topmost,
        });
    }

    private const int WM_SIZING = 0x0214;
    private const int WM_EXITSIZEMOVE = 0x0232;

    /// <summary>
    /// While a border or a corner is dragged, Windows proposes the new
    /// rectangle: it is changed so the picture keeps its shape.
    /// </summary>
    private IntPtr FloatingHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_SIZING && lParam != IntPtr.Zero)
        {
            var edge = (FloatingOutput.Edge)wParam.ToInt32();
            if (!Enum.IsDefined(edge))
                return IntPtr.Zero;

            var rect = Marshal.PtrToStructure<RECT>(lParam);
            var (frameWidth, frameHeight) = FrameOf(hwnd);
            var kept = FloatingOutput.KeepShape(edge,
                new ScreenRect(rect.Left, rect.Top, rect.Right, rect.Bottom), frameWidth, frameHeight, Ratio);
            Marshal.StructureToPtr(new RECT { Left = kept.Left, Top = kept.Top, Right = kept.Right, Bottom = kept.Bottom },
                lParam, false);
            handled = true;
            return new IntPtr(1);
        }

        if (msg == WM_EXITSIZEMOVE)
            SaveLayout();

        return IntPtr.Zero;
    }

    /// <summary>Closing the window is switching the output off; it is kept to open again.</summary>
    private void OnFloatingClosing(object? sender, CancelEventArgs e)
    {
        SaveLayout();

        // The program itself is closing (the operator panel is already gone): it goes.
        if (_closingForGood || Application.Current?.MainWindow is not { IsVisible: true })
            return;

        e.Cancel = true;
        TurnOffRequested?.Invoke(this, EventArgs.Empty);
    }

    // ── Menú del clic derecho ────────────────────────────────────

    private void FillMenu()
    {
        var menu = ContextMenu!;
        menu.Items.Clear();
        var shape = FloatingOutputSettings.Current.Shape;

        menu.Items.Add(ShapeItem("Forma: 16:9 (pantalla ancha)", OutputShape.Wide, shape));
        menu.Items.Add(ShapeItem("Forma: 4:3 (proyector cuadrado)", OutputShape.Classic, shape));
        menu.Items.Add(new Separator());

        foreach (var size in FloatingOutput.QuickSizes(shape))
        {
            var item = new MenuItem { Header = $"Tamaño: {size}" };
            item.Click += (_, _) => SetPictureSize(size);
            menu.Items.Add(item);
        }

        menu.Items.Add(new Separator());
        var onTop = new MenuItem
        {
            Header = "Siempre visible",
            IsCheckable = true,
            IsChecked = Topmost,
            ToolTip = "Queda por encima de las otras ventanas, también del programa",
        };
        onTop.Click += (_, _) =>
        {
            Topmost = onTop.IsChecked;
            SaveLayout();
            FloatingOutputSettings.Set(FloatingOutputSettings.Current with { AlwaysOnTop = Topmost });
        };
        menu.Items.Add(onTop);

        menu.Items.Add(new Separator());
        var off = new MenuItem { Header = "Apagar la salida" };
        off.Click += (_, _) => TurnOffRequested?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(off);
    }

    private MenuItem ShapeItem(string header, OutputShape value, OutputShape current)
    {
        var item = new MenuItem { Header = header, IsCheckable = true, IsChecked = value == current };
        item.Click += (_, _) => ChangeShape(value);
        return item;
    }

    /// <summary>The other shape keeps the height the picture has, so the window only widens or narrows.</summary>
    private void ChangeShape(OutputShape shape)
    {
        if (shape == FloatingOutputSettings.Current.Shape)
            return;

        var height = Math.Max(1, (int)Math.Round(Stage.ActualHeight * DpiScale()));
        FloatingOutputSettings.Set(FloatingOutputSettings.Current with { Shape = shape });
        FitStage();
        SetPictureSize(FloatingOutput.WithWidth(shape, (int)Math.Round(height * FloatingOutput.RatioOf(shape))));
    }

    private double DpiScale() => System.Windows.Media.VisualTreeHelper.GetDpi(this).DpiScaleY;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private const uint SWP_NOMOVE = 0x0002;

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hWnd, out RECT rect);
}
