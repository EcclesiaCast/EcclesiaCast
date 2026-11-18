using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace EcclesiaCast.App.Services;

/// <summary>
/// The dropper of the colour picker: the whole screen is covered by an almost
/// invisible window, a small square next to the pointer shows the colour under
/// it, and a click takes it. Esc or the right button cancel.
/// </summary>
internal static class ScreenColorPicker
{
    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT point);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern uint GetPixel(IntPtr hdc, int x, int y);

    /// <summary>The colour of the screen under the pointer, or null if it cannot be read.</summary>
    private static Color? UnderPointer()
    {
        if (!GetCursorPos(out var point))
            return null;

        var hdc = GetDC(IntPtr.Zero);
        if (hdc == IntPtr.Zero)
            return null;
        try
        {
            var pixel = GetPixel(hdc, point.X, point.Y);
            if (pixel == 0xFFFFFFFF)
                return null;
            return Color.FromRgb((byte)(pixel & 0xFF), (byte)((pixel >> 8) & 0xFF), (byte)((pixel >> 16) & 0xFF));
        }
        finally
        {
            ReleaseDC(IntPtr.Zero, hdc);
        }
    }

    /// <summary>Lets the person click anywhere on the screen; returns that colour, or null if cancelled.</summary>
    public static Color? Pick()
    {
        Color? picked = null;
        Color? current = null;

        var swatch = new Border
        {
            Width = 34,
            Height = 34,
            CornerRadius = new CornerRadius(4),
            BorderBrush = Brushes.White,
            BorderThickness = new Thickness(2),
            IsHitTestVisible = false,
        };
        var code = new TextBlock
        {
            Foreground = Brushes.White,
            Background = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)),
            Padding = new Thickness(5, 2, 5, 2),
            FontSize = 12,
            IsHitTestVisible = false,
        };
        var hint = new TextBlock
        {
            Text = "Clic: tomar este color · Esc: cancelar",
            Foreground = Brushes.White,
            Background = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)),
            Padding = new Thickness(5, 2, 5, 2),
            FontSize = 11,
            IsHitTestVisible = false,
        };
        var follow = new StackPanel { IsHitTestVisible = false };
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        row.Children.Add(swatch);
        code.Margin = new Thickness(6, 0, 0, 0);
        code.VerticalAlignment = VerticalAlignment.Center;
        row.Children.Add(code);
        follow.Children.Add(row);
        hint.Margin = new Thickness(0, 4, 0, 0);
        follow.Children.Add(hint);

        var canvas = new Canvas();
        canvas.Children.Add(follow);

        var window = new Window
        {
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            // Almost transparent: the screen shows through, but the window gets the clicks.
            Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)),
            Topmost = true,
            ShowInTaskbar = false,
            ResizeMode = ResizeMode.NoResize,
            Left = SystemParameters.VirtualScreenLeft,
            Top = SystemParameters.VirtualScreenTop,
            Width = SystemParameters.VirtualScreenWidth,
            Height = SystemParameters.VirtualScreenHeight,
            Cursor = Cursors.Cross,
            Content = canvas,
        };

        void Update(Point at)
        {
            current = UnderPointer();
            if (current is { } c)
            {
                swatch.Background = new SolidColorBrush(c);
                code.Text = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
            }
            // Beside the pointer, never under it (it would be the colour read).
            Canvas.SetLeft(follow, at.X + 18);
            Canvas.SetTop(follow, at.Y + 18);
        }

        window.MouseMove += (_, e) => Update(e.GetPosition(canvas));
        window.MouseLeftButtonUp += (_, e) =>
        {
            Update(e.GetPosition(canvas));
            picked = current;
            window.Close();
        };
        window.MouseRightButtonUp += (_, _) => window.Close();
        window.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
                window.Close();
        };
        window.Loaded += (_, _) =>
        {
            window.Activate();
            window.Focus();
            // Clicks anywhere come here, even on a screen the window does not reach.
            window.CaptureMouse();
            Update(Mouse.GetPosition(canvas));
        };

        window.ShowDialog();
        return picked;
    }
}
