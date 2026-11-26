using System.Windows;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.App.Views;

/// <summary>
/// Makes the light copy of one heavy video while the operator waits. The work
/// is in <see cref="LightCopyViewModel"/>.
/// </summary>
public partial class LightCopyWindow : Window
{
    private readonly LightCopyViewModel _copy;

    public LightCopyWindow(LightCopyViewModel copy)
    {
        InitializeComponent();
        _copy = copy;
        DataContext = copy;

        copy.CloseRequested += () => DialogResult = copy.Result == LightCopyResult.Done;
        Loaded += async (_, _) => await copy.RunAsync();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => _copy.PressClose();

    protected override void OnClosed(EventArgs e)
    {
        _copy.Closed();
        base.OnClosed(e);
    }
}
