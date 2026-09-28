using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Displays;

namespace EcclesiaCast.App.Views;

/// <summary>
/// The stage display, full screen on its own monitor. What it shows is the
/// <see cref="Controls.StageView"/>; this window only places it, keeps it out
/// of the way of the keyboard and offers the way out.
/// </summary>
public partial class StageWindow : Window
{
    private DisplayInfo? _display;

    public StageWindow()
    {
        InitializeComponent();
    }

    /// <summary>Which parts are shown; the operator picks them in Ajustes.</summary>
    public void ApplyOptions(StageOptions options) => View.Options = options;

    /// <summary>Raised when the operator double-clicks the stage screen to close it.</summary>
    public event EventHandler? CloseRequested;

    public void ShowOn(DisplayInfo display)
    {
        _display = display;
        Show();
        MoveToDisplay();
        ShowHelpHint();
    }

    /// <summary>
    /// Flashes the way out for a few seconds. The window takes no keyboard
    /// focus by design, so Esc from the operator panel can't reach it; a
    /// double click can, and it works even when this window covers the panel.
    /// </summary>
    private void ShowHelpHint()
    {
        HelpHint.Visibility = Visibility.Visible;
        HelpHint.Opacity = 1;

        var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(700))
        {
            BeginTime = TimeSpan.FromSeconds(6),
        };
        fade.Completed += (_, _) => HelpHint.Visibility = Visibility.Collapsed;
        HelpHint.BeginAnimation(OpacityProperty, fade);
    }

    private void Stage_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) =>
        CloseRequested?.Invoke(this, EventArgs.Empty);

    // ── Posición en el monitor de escenario ──────────────────────

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var hwnd = new WindowInteropHelper(this).Handle;
        _ = SetWindowLong(hwnd, GWL_EXSTYLE, GetWindowLong(hwnd, GWL_EXSTYLE) | WS_EX_NOACTIVATE);
        MoveToDisplay();
    }

    private void MoveToDisplay()
    {
        if (_display is null)
            return;

        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero)
            return;

        SetWindowPos(hwnd, IntPtr.Zero,
            _display.X, _display.Y, _display.Width, _display.Height,
            SWP_NOZORDER | SWP_FRAMECHANGED | SWP_SHOWWINDOW | SWP_NOACTIVATE);
    }

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_FRAMECHANGED = 0x0020;
    private const uint SWP_SHOWWINDOW = 0x0040;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
}
