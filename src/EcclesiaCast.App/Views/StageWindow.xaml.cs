using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Displays;
using EcclesiaCast.Core.Stage;

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

    /// <summary>Which arrangement is drawn; the operator picks it under ▾ Escenario.</summary>
    public void ApplyLayout(StageLayout layout) => View.Layout = layout;

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
        _native.Prepare(new NativeWindowHandle(hwnd));
        MoveToDisplay();
    }

    private void MoveToDisplay()
    {
        if (_display is null)
            return;

        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero)
            return;

        _native.Place(new NativeWindowHandle(hwnd), _display);
    }

    /// <summary>Full screen, on top and never taking the keyboard (Platform.Windows).</summary>
    private readonly IOutputWindowNative _native = new WindowsOutputWindowNative();
}
