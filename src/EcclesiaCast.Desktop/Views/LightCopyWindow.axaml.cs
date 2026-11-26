using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Desktop.Views;

/// <summary>
/// Makes the light copy of one heavy video while the operator waits. The work
/// is in <see cref="LightCopyViewModel"/>.
/// </summary>
public sealed partial class LightCopyWindow : Window
{
    private readonly LightCopyViewModel _copy;

    public LightCopyWindow(LightCopyViewModel copy)
    {
        InitializeComponent();
        _copy = copy;
        DataContext = copy;

        copy.CloseRequested += Close;
        // Posted: work that ends at once would close the window while it is still opening.
        Opened += (_, _) => Dispatcher.UIThread.Post(async () => await copy.RunAsync());
    }

    private void Close_Click(object? sender, RoutedEventArgs e) => _copy.PressClose();

    protected override void OnClosed(EventArgs e)
    {
        _copy.Closed();
        base.OnClosed(e);
    }
}
