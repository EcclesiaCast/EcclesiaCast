using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using EcclesiaCast.App.ViewModels;
using EcclesiaCast.Core.Displays;
using EcclesiaCast.Core.Media;

namespace EcclesiaCast.App.Views;

public partial class OutputWindow : Window
{
    private DisplayInfo? _display;

    public OutputWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        YouTube.Ended += (_, _) => VideoEnded?.Invoke(this, EventArgs.Empty);
        Video.Ended += (_, _) => VideoEnded?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Raised when the projected video finishes and shouldn't loop.</summary>
    public event EventHandler? VideoEnded;

    /// <summary>Shows this window fullscreen on the given display.</summary>
    public void ShowOn(DisplayInfo display)
    {
        _display = display;
        Show();
        MoveToDisplay();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is INotifyPropertyChanged oldVm)
            oldVm.PropertyChanged -= OnProjectionChanged;
        if (e.NewValue is INotifyPropertyChanged newVm)
            newVm.PropertyChanged += OnProjectionChanged;
        UpdateVideo();
    }

    private void OnProjectionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProjectionViewModel.Background))
            UpdateVideo();
    }

    private void UpdateVideo()
    {
        var background = (DataContext as ProjectionViewModel)?.Background;

        if (background is { Type: MediaType.YouTube })
        {
            Video.Show(null);
            YouTube.Visibility = Visibility.Visible;
            YouTube.Play(background);
        }
        else
        {
            YouTube.Clear();
            YouTube.Visibility = Visibility.Collapsed;
            Video.Show(background);
        }
    }

    // ── Posición en el monitor de salida ─────────────────────────

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var hwnd = new WindowInteropHelper(this).Handle;
        _ = SetWindowLong(hwnd, GWL_EXSTYLE,
            GetWindowLong(hwnd, GWL_EXSTYLE) | WS_EX_NOACTIVATE);

        MoveToDisplay();
    }

    protected override void OnClosed(EventArgs e)
    {
        Video.Stop();
        YouTube.Clear();
        base.OnClosed(e);
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
        IntPtr hWnd, IntPtr hWndInsertAfter,
        int x, int y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
}
