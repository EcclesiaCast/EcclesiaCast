using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;

namespace EcclesiaCast.Desktop.Views;

/// <summary>
/// A window that waits for the operator, the way the view models expect a
/// dialog to (they ask and read the answer on the next line). Avalonia's
/// dialogs answer later; here the screen's thread keeps working inside a
/// nested loop until the window closes, as WPF's ShowDialog does.
/// </summary>
public static class Modal
{
    /// <summary>The window in front: the one the dialog belongs to.</summary>
    public static Window? Owner =>
        Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.Windows.FirstOrDefault(w => w.IsActive && w.IsVisible)
              ?? (desktop.MainWindow is { IsVisible: true } main ? main : null)
            : null;

    public static void Run(Window dialog)
    {
        var frame = new DispatcherFrame();
        dialog.Closed += (_, _) => frame.Continue = false;

        if (Owner is { } owner && !ReferenceEquals(owner, dialog))
        {
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            _ = dialog.ShowDialog(owner);
        }
        else
        {
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            dialog.Show();
        }

        Dispatcher.UIThread.PushFrame(frame);
    }
}
